using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.E2E.McpClient;

[Explicit("Integration test: requires a running InternalAI API and valid credentials.")]
public sealed class CourseEnrollmentViaApiUseCaseE2ETests
{
    private static string BaseUrl => Environment.GetEnvironmentVariable("API_TEST_BASE_URL") ?? "http://localhost:5000";
    private static string Username => Environment.GetEnvironmentVariable("API_TEST_USERNAME") ?? "admin";
    private static string Password => Environment.GetEnvironmentVariable("API_TEST_PASSWORD") ?? "Admin1234!";

    public static IEnumerable<TestCaseData> EnrollmentCases()
    {
        var envMember = Environment.GetEnvironmentVariable("API_TEST_MEMBER_NAME");
        var envCourse = Environment.GetEnvironmentVariable("API_TEST_COURSE_NAME");
        var envStatus = Environment.GetEnvironmentVariable("API_TEST_ENROLLMENT_STATUS");

        if (!string.IsNullOrWhiteSpace(envMember)
            && !string.IsNullOrWhiteSpace(envCourse)
            && !string.IsNullOrWhiteSpace(envStatus))
        {
            yield return new TestCaseData(envMember, envCourse, envStatus)
                .SetName($"Enroll_{Sanitize(envMember)}_{Sanitize(envCourse)}_{Sanitize(envStatus)}");
            yield break;
        }

        yield return new TestCaseData("Benjamin Cooper", "AI Pair Programming with GitHub Copilot", "in_progress")
            .SetName("Enroll_BenjaminCooper_AiPairProgramming_InProgress");

        yield return new TestCaseData("Benjamin Cooper", "AI Pair Programming with GitHub Copilot", "apply")
            .SetName("Enroll_BenjaminCooper_AiPairProgramming_Apply");
    }

    
    [TestCaseSource(nameof(EnrollmentCases))]
    public async Task Enroll_existing_course_for_member_by_name(string memberName, string courseName, string enrollmentStatus)
    {
        using var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl.EndsWith("/") ? BaseUrl : BaseUrl + "/", UriKind.Absolute)
        };

        await EnsureAuthenticatedAsync(httpClient);

        var memberId = await ResolveMemberIdAsync(httpClient, memberName);
        var courseId = await ResolveCourseIdAsync(httpClient, courseName);

        var enrollResponse = await httpClient.PostAsJsonAsync(
            $"api/consultants/{memberId}/courses",
            new
            {
                courseId,
                status = enrollmentStatus
            });

        if (enrollResponse.StatusCode != HttpStatusCode.Created && enrollResponse.StatusCode != HttpStatusCode.Conflict)
        {
            var errorBody = await enrollResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Enrollment failed with {(int)enrollResponse.StatusCode}: {errorBody}");
        }

        var verifyResponse = await httpClient.GetAsync($"api/consultants/{memberId}/courses");
        Assert.That(verifyResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
        using var verifyDoc = JsonDocument.Parse(verifyBody);
        var root = verifyDoc.RootElement;

        Assert.That(root.TryGetProperty("data", out var enrollmentsElement), Is.True, "Response must contain 'data'.");

        var found = enrollmentsElement.EnumerateArray().Any(item =>
            item.TryGetProperty("courseName", out var courseNameElement)
            && string.Equals(courseNameElement.GetString(), courseName, StringComparison.OrdinalIgnoreCase));

        Assert.That(found, Is.True, $"Expected course '{courseName}' to be enrolled for member '{memberName}'.");
    }

    private static async Task EnsureAuthenticatedAsync(HttpClient httpClient)
    {
        var loginResponse = await httpClient.PostAsJsonAsync("api/auth/login", new
        {
            username = Username,
            password = Password
        });

        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var loginBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.Ignore($"Skipping integration test. Login failed with {(int)loginResponse.StatusCode}: {loginBody}");
        }
    }

    private static async Task<int> ResolveMemberIdAsync(HttpClient httpClient, string memberName)
    {
        var response = await httpClient.GetAsync("api/team?filter=all&includeInactive=true");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var members = doc.RootElement.GetProperty("data");

        foreach (var member in members.EnumerateArray())
        {
            if (!member.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            if (!string.Equals(nameElement.GetString(), memberName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return member.GetProperty("id").GetInt32();
        }

        Assert.Fail($"Could not resolve member id for '{memberName}'.");
        return -1;
    }

    private static async Task<int> ResolveCourseIdAsync(HttpClient httpClient, string courseName)
    {
        var response = await httpClient.GetAsync("api/courses");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var courses = doc.RootElement.GetProperty("data").GetProperty("courses");

        foreach (var course in courses.EnumerateArray())
        {
            if (!course.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            if (!string.Equals(nameElement.GetString(), courseName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return course.GetProperty("id").GetInt32();
        }

        Assert.Fail($"Could not resolve course id for '{courseName}'.");
        return -1;
    }

    private static string Sanitize(string input)
    {
        var chars = input.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Case" : new string(chars);
    }
}

