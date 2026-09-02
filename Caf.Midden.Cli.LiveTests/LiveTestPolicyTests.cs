using System.Reflection;

namespace Caf.Midden.Cli.LiveTests;

public class LiveTestPolicyTests
{
    [Fact]
    public void CloudTests_AreExplicitAndCategorized()
    {
        var cloudTestMethods = typeof(LiveTestPolicyTests).Assembly
            .GetTypes()
            .Where(type => type != typeof(LiveTestPolicyTests))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Select(method => (Method: method, Fact: method.GetCustomAttribute<FactAttribute>()))
            .Where(test => test.Fact is not null)
            .ToList();

        Assert.NotEmpty(cloudTestMethods);

        foreach (var test in cloudTestMethods)
        {
            Assert.True(test.Fact!.Explicit, $"{test.Method.DeclaringType?.Name}.{test.Method.Name} must be explicit.");

            var traits = test.Method.DeclaringType!
                .GetCustomAttributes<TraitAttribute>()
                .Concat(test.Method.GetCustomAttributes<TraitAttribute>())
                .ToList();

            Assert.Contains(traits, trait => trait.Name == "Category" && trait.Value == "LiveIntegration");
            Assert.Contains(traits, trait => trait.Name == "Provider" && !string.IsNullOrWhiteSpace(trait.Value));
        }
    }
}