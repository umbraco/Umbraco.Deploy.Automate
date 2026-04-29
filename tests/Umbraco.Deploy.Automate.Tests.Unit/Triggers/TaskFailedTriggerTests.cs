using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;
using Umbraco.Deploy.Core.Work;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class TaskFailedTriggerTests
{
    private readonly TaskFailedTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification();

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.taskFailed");
    }

    [Fact]
    public void MapEvent_MapsExceptionDetails_WhenExceptionPresent()
    {
        var exception = new InvalidOperationException("Deployment failed due to conflict");
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(Guid.NewGuid());
        workItem.Setup(x => x.OwnerName).Returns("Test User");
        workItem.Setup(x => x.OwnerEmail).Returns("test@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("auto");
        workItem.Setup(x => x.Exception).Returns(exception);
        var notification = new TaskFailedNotification(workItem.Object, new EventMessages());

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<TaskFailedTriggerOutput>)events[0]).Output;
        output.ExceptionMessage.ShouldBe("Deployment failed due to conflict");
        output.ExceptionType.ShouldBe("InvalidOperationException");
    }

    [Fact]
    public void MapEvent_MapsNullExceptionDetails_WhenNoException()
    {
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(Guid.NewGuid());
        workItem.Setup(x => x.OwnerName).Returns("Test User");
        workItem.Setup(x => x.OwnerEmail).Returns("test@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("auto");
        workItem.Setup(x => x.Exception).Returns((Exception?)null);
        var notification = new TaskFailedNotification(workItem.Object, new EventMessages());

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<TaskFailedTriggerOutput>)events[0]).Output;
        output.ExceptionMessage.ShouldBeNull();
        output.ExceptionType.ShouldBeNull();
    }

    private static TaskFailedNotification BuildNotification()
    {
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(Guid.NewGuid());
        workItem.Setup(x => x.OwnerName).Returns("Test User");
        workItem.Setup(x => x.OwnerEmail).Returns("test@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("auto");
        return new TaskFailedNotification(workItem.Object, new EventMessages());
    }
}
