using Chroniques.Simulation.Components;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie GDB-004B : un habitant nouvellement créé démarre avec des
/// besoins pleinement satisfaits, jamais en détresse par défaut.
/// </summary>
public class NeedsComponentTests
{
    [Fact]
    public void Un_needs_component_demarre_pleinement_satisfait()
    {
        var needs = new NeedsComponent();

        Assert.Equal(100, needs.Faim);
        Assert.Equal(100, needs.Fatigue);
        Assert.Equal(100, needs.Sante);
        Assert.Equal(100, needs.Moral);
    }
}
