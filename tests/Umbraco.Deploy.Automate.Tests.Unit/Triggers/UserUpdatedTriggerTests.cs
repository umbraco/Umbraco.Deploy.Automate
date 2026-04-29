using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Disk;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class UserUpdatedTriggerTests
{
    private readonly UserUpdatedTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification("user@example.com", "User", 1);

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.userUpdated");
    }

    [Fact]
    public void MapEvent_MapsUserDetails()
    {
        var notification = BuildNotification("author@example.com", "Content Author", 5);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<UserUpdatedTriggerOutput>)events[0]).Output;
        output.UserEmail.ShouldBe("author@example.com");
        output.UserName.ShouldBe("Content Author");
        output.FileCount.ShouldBe(5);
    }

    private static UserUpdatedNotification BuildNotification(string email, string name, int fileCount)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(_ => Mock.Of<DiskFileReference>())
            .ToList();
        return new UserUpdatedNotification(files, email, name, new EventMessages());
    }
}
