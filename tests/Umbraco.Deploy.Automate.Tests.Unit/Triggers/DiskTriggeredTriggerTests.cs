using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Disk;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class DiskTriggeredTriggerTests
{
    private readonly DiskTriggeredTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = new DiskTriggeredNotification(DiskTriggeredResult.Succeeded);

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.diskTriggered");
    }

    [Fact]
    public void MapEvent_MapsResultAsString()
    {
        var notification = new DiskTriggeredNotification(DiskTriggeredResult.Succeeded);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<DiskTriggeredTriggerOutput>)events[0]).Output;
        output.Result.ShouldBe("Succeeded");
        output.ExceptionType.ShouldBeEmpty();
    }

    [Fact]
    public void MapEvent_MapsExceptionType_WhenFailed()
    {
        var notification = new DiskTriggeredNotification(DiskTriggeredResult.Failed, "TimeoutException");

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<DiskTriggeredTriggerOutput>)events[0]).Output;
        output.Result.ShouldBe("Failed");
        output.ExceptionType.ShouldBe("TimeoutException");
    }
}
