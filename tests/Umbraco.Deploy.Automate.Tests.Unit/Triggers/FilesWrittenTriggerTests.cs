using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Disk;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class FilesWrittenTriggerTests
{
    private readonly FilesWrittenTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification("editor@example.com", "Editor", 1);

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.filesWritten");
    }

    [Fact]
    public void MapEvent_MapsUserAndFileCount()
    {
        var notification = BuildNotification("editor@example.com", "Jane Editor", 3);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<FilesWrittenTriggerOutput>)events[0]).Output;
        output.UserEmail.ShouldBe("editor@example.com");
        output.UserName.ShouldBe("Jane Editor");
        output.FileCount.ShouldBe(3);
    }

    [Fact]
    public void MapEvent_ReturnsUserInitiatorType()
    {
        var notification = BuildNotification("editor@example.com", "Editor", 1);

        var events = _trigger.MapEvent(notification).ToList();

        events[0].InitiatorType.ShouldBe("user");
    }

    private static FilesWrittenNotification BuildNotification(string email, string name, int fileCount)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(i => new DiskFileReference($"file{i}", "content", [$"/path/file{i}.cs"]))
            .ToList();
        return new FilesWrittenNotification(files, email, name, new EventMessages());
    }
}
