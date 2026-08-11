namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions.Exemples;

public sealed class Engine014ActClassificationTests
{
    [Fact]
    public void DonnerDenreeDefinition_RespecteClassificationActCanonique()
    {
        Assert.Equal("Échange", DonnerDenreeDefinition.Definition.Principe);
        Assert.Equal("Transfert", DonnerDenreeDefinition.Definition.Pattern);
        Assert.Equal("DonnerDenree", DonnerDenreeDefinition.Definition.Verbe);
    }
}
