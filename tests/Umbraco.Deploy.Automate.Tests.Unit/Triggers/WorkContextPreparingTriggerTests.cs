using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;
using Umbraco.Deploy.Core.Work;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class WorkContextPreparingTriggerTests
{
    private readonly WorkContextPreparingTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification();

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.workContextPreparing");
    }

    [Fact]
    public void MapEvent_MapsWorkItemProperties()
    {
        var workItemId = Guid.NewGuid();
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(workItemId);
        workItem.Setup(x => x.OwnerName).Returns("Automation User");
        workItem.Setup(x => x.OwnerEmail).Returns("auto@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("scheduled");
        var notification = new WorkContextPreparingNotification(
            Mock.Of<IWorkContext>(),
            new EventMessages(),
            workItem.Object,
            default);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<WorkContextPreparingTriggerOutput>)events[0]).Output;
        output.WorkItemId.ShouldBe(workItemId);
        output.OwnerName.ShouldBe("Automation User");
        output.OwnerEmail.ShouldBe("auto@example.com");
        output.EventTrigger.ShouldBe("scheduled");
    }

    private static WorkContextPreparingNotification BuildNotification()
    {
        var workItem = new Mock<IWorkItem>();
        workItem.Setup(x => x.Id).Returns(Guid.NewGuid());
        workItem.Setup(x => x.OwnerName).Returns("Test User");
        workItem.Setup(x => x.OwnerEmail).Returns("test@example.com");
        workItem.Setup(x => x.EventTrigger).Returns("auto");
        return new WorkContextPreparingNotification(
            Mock.Of<IWorkContext>(),
            new EventMessages(),
            workItem.Object,
            default);
    }
}
