using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Automate.Triggers;
using Umbraco.Deploy.Automate.Triggers.Outputs;
using Umbraco.Deploy.Core.Events;

namespace Umbraco.Deploy.Automate.Tests.Unit.Triggers;

public class ValidateArtifactImportTriggerTests
{
    private readonly ValidateArtifactImportTrigger _trigger = new(
        new TriggerInfrastructure(Mock.Of<IEditableModelResolver>()));

    [Fact]
    public void MapEvent_ReturnsCorrectAlias()
    {
        var notification = BuildNotification(1);

        var events = _trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        events[0].TriggerAlias.ShouldBe("umbracodeploy.validateArtifactImport");
    }

    [Fact]
    public void MapEvent_MapsArtifactCount()
    {
        var notification = BuildNotification(3);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<ValidateArtifactImportTriggerOutput>)events[0]).Output;
        output.ArtifactCount.ShouldBe(3);
    }

    [Fact]
    public void MapEvent_MapsArtifactUdis()
    {
        var notification = BuildNotification(2);

        var events = _trigger.MapEvent(notification).ToList();

        var output = ((TriggerEvent<ValidateArtifactImportTriggerOutput>)events[0]).Output;
        output.ArtifactUdis.Count().ShouldBe(2);
        output.ArtifactUdis.ShouldAllBe(u => u.StartsWith("umb://document/"));
    }

    private static ValidateArtifactImportNotification BuildNotification(int artifactCount)
    {
        var artifacts = Enumerable.Range(0, artifactCount).Select(_ =>
        {
            var udi = new GuidUdi("document", Guid.NewGuid());
            var artifact = new Mock<IArtifactSignature>();
            artifact.Setup(x => x.Udi).Returns(udi);
            return artifact.Object;
        }).ToList();
        return new ValidateArtifactImportNotification(artifacts, new EventMessages());
    }
}
