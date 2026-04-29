using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class ArtifactExportingTriggerTests
{
    private readonly ArtifactExportingTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification("document", "home", "Home");

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.artifactExporting");
    }

    [Fact]
    public void MapEvent_MapsArtifactSignatureProperties()
    {
        var notification = BuildNotification("document", "home", "Home");

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<ArtifactExportingTriggerOutput>)events[0]).Output;
        output.ArtifactType.ShouldBe("document");
        output.ArtifactAlias.ShouldBe("home");
        output.ArtifactName.ShouldBe("Home");
    }

    private static ArtifactExportingNotification BuildNotification(string entityType, string alias, string name)
    {
        var udi = new GuidUdi(entityType, Guid.NewGuid());
        var artifact = new Mock<IArtifact>();
        artifact.Setup(x => x.Udi).Returns(udi);
        artifact.Setup(x => x.Alias).Returns(alias);
        artifact.Setup(x => x.Name).Returns(name);
        return new ArtifactExportingNotification(artifact.Object, new EventMessages());
    }
}
