using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;
using Umbraco.Deploy.Core.Work;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class TaskCompletedTriggerTests
{
    private readonly TaskCompletedTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification();

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.taskCompleted");
    }

    [Fact]
    public void MapEvent_ReturnsSystemInitiatorType()
    {
        var notification = BuildNotification();

        var events = _trigger.MapEvent(notification).ToList();

        events[0].InitiatorType.ShouldBe("system");
    }

    [Fact]
    public void MapEvent_MapsWorkItemProperties()
    {
        var workItemId = Guid.NewGuid();
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(workItemId);
        workItem.Setup(x => x.OwnerName).Returns("Deploy User");
        workItem.Setup(x => x.OwnerEmail).Returns("deploy@example.com");
        workItem.Setup(x => x.Duration).Returns(2.5);
        workItem.Setup(x => x.WorkCount).Returns(15);
        workItem.Setup(x => x.ProcessCount).Returns(12);
        workItem.Setup(x => x.EventTrigger).Returns("manual");
        var notification = new TaskCompletedNotification(workItem.Object, new EventMessages());

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<TaskCompletedTriggerOutput>)events[0]).Output;
        output.WorkItemId.ShouldBe(workItemId);
        output.OwnerName.ShouldBe("Deploy User");
        output.OwnerEmail.ShouldBe("deploy@example.com");
        output.Duration.ShouldBe(2.5);
        output.WorkCount.ShouldBe(15);
        output.ProcessCount.ShouldBe(12);
        output.EventTrigger.ShouldBe("manual");
    }

    private static TaskCompletedNotification BuildNotification()
    {
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(Guid.NewGuid());
        workItem.Setup(x => x.OwnerName).Returns("Test User");
        workItem.Setup(x => x.OwnerEmail).Returns("test@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("auto");
        return new TaskCompletedNotification(workItem.Object, new EventMessages());
    }
}
