using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class ArtifactImportingTriggerTests
{
    private readonly ArtifactImportingTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification("document");

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.artifactImporting");
    }

    [Fact]
    public void MapEvent_MapsArtifactUdiAndType()
    {
        var notification = BuildNotification("document");

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<ArtifactImportingTriggerOutput>)events[0]).Output;
        output.ArtifactType.ShouldBe("document");
        output.ArtifactUdi.ShouldStartWith("umb://document/");
    }

    [Fact]
    public void MapEvent_MapsMediaArtifactType()
    {
        var notification = BuildNotification("media");

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<ArtifactImportingTriggerOutput>)events[0]).Output;
        output.ArtifactType.ShouldBe("media");
    }

    private static ArtifactImportingNotification BuildNotification(string entityType)
    {
        var udi = new GuidUdi(entityType, Guid.NewGuid());
        var artifact = new Mock<IArtifactSignature>();
        artifact.Setup(x => x.Udi).Returns(udi);
        return new ArtifactImportingNotification(artifact.Object, new EventMessages());
    }
}
