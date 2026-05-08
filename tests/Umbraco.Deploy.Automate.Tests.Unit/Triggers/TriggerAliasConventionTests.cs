using System.Reflection;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class TriggerAliasConventionTests
{
    private const string ExpectedAliasPrefix = "umbracoDeploy.";

    public static IEnumerable<object[]> TriggerTypes() =>
        typeof(TaskCompletedTrigger).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<TriggerAttribute>() is not null)
            .Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(TriggerTypes))]
    public void TriggerAlias_StartsWithExpectedPrefix(Type triggerType)
    {
        var attribute = triggerType.GetCustomAttribute<TriggerAttribute>();

        attribute.ShouldNotBeNull();
        attribute.Alias.ShouldStartWith(ExpectedAliasPrefix);
    }

    [Fact]
    public void TriggerAliases_AreUnique()
    {
        var aliases = typeof(TaskCompletedTrigger).Assembly
            .GetTypes()
            .Select(t => t.GetCustomAttribute<TriggerAttribute>())
            .Where(a => a is not null)
            .Select(a => a!.Alias)
            .ToList();

        aliases.ShouldBeUnique();
    }
}
