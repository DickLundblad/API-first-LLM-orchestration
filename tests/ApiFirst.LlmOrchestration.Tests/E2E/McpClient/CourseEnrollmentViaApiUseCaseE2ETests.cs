using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace ApiFirst.LlmOrchestration.Tests.E2E.McpClient;

[Explicit("Integration test: requires a running InternalAI API and valid credentials.")]
public sealed class CourseEnrollmentViaApiUseCaseE2ETests
{
    private const string LoginEndpoint = "api/auth/login";
    private const string TeamQueryFilter = "filter=all&includeInactive=true";
    private const string TeamEndpoint = "api/team?" + TeamQueryFilter;
    private const string CoursesEndpoint = "api/courses";
    private const string DefaultMemberName = "Benjamin Cooper";
    private const string DefaultCourseName = "AI Pair Programming with GitHub Copilot";
    private const string EnrollmentStatusInProgress = "in_progress";
    private const string EnrollmentStatusApply = "apply";

    private static string BaseUrl => Environment.GetEnvironmentVariable("API_TEST_BASE_URL") ?? "http://localhost:5000";
    private static string Username => Environment.GetEnvironmentVariable("API_TEST_USERNAME") ?? "admin";
    private static string Password => Environment.GetEnvironmentVariable("API_TEST_PASSWORD") ?? "Admin1234!";

    public static IEnumerable<TestCaseData> EnrollmentCases()
    {
        yield return new TestCaseData(DefaultMemberName, DefaultCourseName, EnrollmentStatusInProgress)
            .SetName("Enroll_BenjaminCooper_AiPairProgramming_InProgress");

        yield return new TestCaseData(DefaultMemberName, DefaultCourseName, EnrollmentStatusApply)
            .SetName("Enroll_BenjaminCooper_AiPairProgramming_Apply");
    }

    
    [TestCaseSource(nameof(EnrollmentCases))]
    public async Task Enroll_existing_course_for_member_by_name(string memberName, string courseName, string enrollmentStatus)
    {
        using var httpClient = CreateHttpClient();
        await EnsureAuthenticatedAsync(httpClient);

        var memberId = await ResolveMemberIdAsync(httpClient, memberName);
        var courseId = await ResolveCourseIdAsync(httpClient, courseName);
        await EnrollCourseAsync(httpClient, memberId, courseId, enrollmentStatus);
        await AssertEnrollmentExistsAsync(httpClient, memberId, memberName, courseName);
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl.EndsWith("/") ? BaseUrl : BaseUrl + "/", UriKind.Absolute)
        };
    }

    private static async Task EnrollCourseAsync(HttpClient httpClient, int memberId, int courseId, string enrollmentStatus)
    {
        var enrollResponse = await httpClient.PostAsJsonAsync(GetConsultantCoursesEndpoint(memberId), new
        {
            courseId,
            status = enrollmentStatus
        });

        if (enrollResponse.StatusCode != HttpStatusCode.Created && enrollResponse.StatusCode != HttpStatusCode.Conflict)
        {
            var errorBody = await enrollResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Enrollment failed with {(int)enrollResponse.StatusCode}: {errorBody}");
        }
    }

    private static async Task AssertEnrollmentExistsAsync(HttpClient httpClient, int memberId, string memberName, string courseName)
    {
        var verifyResponse = await httpClient.GetAsync(GetConsultantCoursesEndpoint(memberId));
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
        var loginResponse = await httpClient.PostAsJsonAsync(LoginEndpoint, new
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
        var members = await GetTeamMembersAsync(httpClient);
        return FindIdByName(members, memberName, "member");
    }

    private static async Task<int> ResolveCourseIdAsync(HttpClient httpClient, string courseName)
    {
        var response = await httpClient.GetAsync(CoursesEndpoint);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var courses = doc.RootElement.GetProperty("data").GetProperty("courses");

        return FindIdByName(courses, courseName, "course");
    }

    private static async Task<JsonElement> GetTeamMembersAsync(HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(TeamEndpoint);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static int FindIdByName(JsonElement items, string expectedName, string itemKind)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            if (!string.Equals(nameElement.GetString(), expectedName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return item.GetProperty("id").GetInt32();
        }

        Assert.Fail($"Could not resolve {itemKind} id for '{expectedName}'.");
        return -1;
    }

    private static string GetConsultantCoursesEndpoint(int memberId)
    {
        return $"api/consultants/{memberId}/courses";
    }

    private static string Sanitize(string input)
    {
        var chars = input.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Case" : new string(chars);
    }
}

