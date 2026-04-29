using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Disk;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class FilesDeletedTriggerTests
{
    private readonly FilesDeletedTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification("admin@example.com", "Admin", 2);

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.filesDeleted");
    }

    [Fact]
    public void MapEvent_MapsUserAndFileCount()
    {
        var notification = BuildNotification("admin@example.com", "Site Admin", 2);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<FilesDeletedTriggerOutput>)events[0]).Output;
        output.UserEmail.ShouldBe("admin@example.com");
        output.UserName.ShouldBe("Site Admin");
        output.FileCount.ShouldBe(2);
    }

    private static FilesDeletedNotification BuildNotification(string email, string name, int fileCount)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(_ => Mock.Of<DiskFileReference>())
            .ToList();
        return new FilesDeletedNotification(files, email, name, new EventMessages());
    }
}
