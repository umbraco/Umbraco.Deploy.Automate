using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Environments;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class RemoteCompletedTriggerTests
{
    private readonly RemoteCompletedTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification();

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracoDeploy.remoteCompleted");
    }

    [Fact]
    public void MapEvent_MapsCompleteInfos()
    {
        var workId = Guid.NewGuid();
        var infos = new CompleteInfos(workId, "DeployTask", "staging", 20, 18, 3.14);
        var notification = new RemoteCompletedNotification(infos, new EventMessages());

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<RemoteCompletedTriggerOutput>)events[0]).Output;
        output.WorkId.ShouldBe(workId);
        output.WorkItemType.ShouldBe("DeployTask");
        output.WorkItemEnvironment.ShouldBe("staging");
        output.WorkCount.ShouldBe(20);
        output.ProcessCount.ShouldBe(18);
        output.Duration.ShouldBe(3.14);
    }

    private static RemoteCompletedNotification BuildNotification()
    {
        var infos = new CompleteInfos(Guid.NewGuid(), "DeployTask", "production", 10, 10, 1.0);
        return new RemoteCompletedNotification(infos, new EventMessages());
    }
}
