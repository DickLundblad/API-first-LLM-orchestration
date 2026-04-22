using System.Reflection;

namespace ApiFirst.LlmOrchestration.Registry;

/// <summary>
/// Discovers test methods from test assemblies and links them to capabilities.
/// </summary>
public sealed class TestDiscoveryService
{
    /// <summary>
    /// Discover test methods from a loaded assembly.
    /// Supports NUnit, xUnit, and MSTest.
    /// </summary>
    public static IReadOnlyList<TestMethodInfo> DiscoverTestsFromAssembly(Assembly assembly)
    {
        var tests = new List<TestMethodInfo>();

        foreach (var type in assembly.GetTypes())
        {
            // Skip non-test classes
            if (!IsTestClass(type))
                continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsTestMethod(method))
                {
                    tests.Add(new TestMethodInfo
                    {
                        FullName = $"{type.FullName}.{method.Name}",
                        MethodName = method.Name,
                        ClassName = type.Name,
                        AssemblyName = assembly.GetName().Name ?? "Unknown"
                    });
                }
            }
        }

        return tests;
    }

    /// <summary>
    /// Discover test methods by scanning a test assembly file.
    /// </summary>
    public static IReadOnlyList<TestMethodInfo> DiscoverTestsFromFile(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return Array.Empty<TestMethodInfo>();
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return DiscoverTestsFromAssembly(assembly);
        }
        catch
        {
            return Array.Empty<TestMethodInfo>();
        }
    }

    /// <summary>
    /// Link discovered tests to capabilities in a registry using heuristics.
    /// </summary>
    public static void LinkTestsToCapabilities(
        CapabilityRegistry registry,
        IReadOnlyList<TestMethodInfo> tests)
    {
        foreach (var capability in registry.GetAllCapabilities())
        {
            var linkedTests = FindMatchingTests(capability, tests);

            if (linkedTests.Count > 0)
            {
                // Update capability with linked tests
                var updatedCapability = capability with
                {
                    ApiTestIds = linkedTests.Select(t => t.FullName).ToList()
                };
                registry.RegisterCapability(updatedCapability);
            }
        }
    }

    /// <summary>
    /// Find tests that match a capability using various heuristics.
    /// </summary>
    private static List<TestMethodInfo> FindMatchingTests(
        UseCaseCapability capability,
        IReadOnlyList<TestMethodInfo> tests)
    {
        var matches = new List<TestMethodInfo>();

        foreach (var test in tests)
        {
            // Strategy 1: Direct operation ID match
            foreach (var operationId in capability.ApiOperationIds)
            {
                if (MatchesOperationId(test, operationId))
                {
                    matches.Add(test);
                    break;
                }
            }

            // Strategy 2: Capability ID match
            if (MatchesCapabilityId(test, capability.Id))
            {
                if (!matches.Contains(test))
                    matches.Add(test);
            }

            // Strategy 3: Category/tag match
            if (MatchesCategory(test, capability.Category))
            {
                if (!matches.Contains(test))
                    matches.Add(test);
            }
        }

        return matches;
    }

    private static bool MatchesOperationId(TestMethodInfo test, string operationId)
    {
        var testNameLower = test.MethodName.ToLowerInvariant();
        var operationIdLower = operationId.ToLowerInvariant();

        // Direct substring match
        if (testNameLower.Contains(operationIdLower))
            return true;

        // Remove "Test" suffix and compare
        var testNameWithoutSuffix = testNameLower.Replace("test", "").Replace("async", "");
        if (testNameWithoutSuffix.Contains(operationIdLower) || operationIdLower.Contains(testNameWithoutSuffix))
            return true;

        // Word-based matching (e.g., "Enroll_BenjaminCooper" matches "EnrollCourse")
        var operationWords = SplitCamelCase(operationId);
        foreach (var word in operationWords)
        {
            if (testNameLower.Contains(word.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static bool MatchesCapabilityId(TestMethodInfo test, string capabilityId)
    {
        var testNameLower = test.MethodName.ToLowerInvariant();
        var capabilityIdLower = capabilityId.ToLowerInvariant();

        return testNameLower.Contains(capabilityIdLower);
    }

    private static bool MatchesCategory(TestMethodInfo test, string category)
    {
        var testNameLower = test.MethodName.ToLowerInvariant();
        var categoryLower = category.ToLowerInvariant();

        // Match if test name contains category
        return testNameLower.Contains(categoryLower);
    }

    private static List<string> SplitCamelCase(string input)
    {
        var result = new List<string>();
        var current = "";

        foreach (var c in input)
        {
            if (char.IsUpper(c) && current.Length > 0)
            {
                result.Add(current);
                current = c.ToString();
            }
            else
            {
                current += c;
            }
        }

        if (current.Length > 0)
            result.Add(current);

        return result;
    }

    private static bool IsTestClass(Type type)
    {
        // NUnit: [TestFixture] or has [Test] methods
        if (type.GetCustomAttributes().Any(a =>
            a.GetType().Name == "TestFixtureAttribute" ||
            a.GetType().Name == "TestClassAttribute"))
        {
            return true;
        }

        // xUnit: Public class with public methods marked with [Fact] or [Theory]
        if (type.IsPublic && type.GetMethods().Any(IsTestMethod))
        {
            return true;
        }

        return false;
    }

    private static bool IsTestMethod(MethodInfo method)
    {
        var attributes = method.GetCustomAttributes();

        foreach (var attr in attributes)
        {
            var attrName = attr.GetType().Name;

            // NUnit
            if (attrName == "TestAttribute" ||
                attrName == "TestCaseAttribute" ||
                attrName == "TestCaseSourceAttribute")
            {
                return true;
            }

            // xUnit
            if (attrName == "FactAttribute" ||
                attrName == "TheoryAttribute")
            {
                return true;
            }

            // MSTest
            if (attrName == "TestMethodAttribute")
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Information about a discovered test method.
/// </summary>
public sealed record TestMethodInfo
{
    public required string FullName { get; init; }
    public required string MethodName { get; init; }
    public required string ClassName { get; init; }
    public required string AssemblyName { get; init; }
}
