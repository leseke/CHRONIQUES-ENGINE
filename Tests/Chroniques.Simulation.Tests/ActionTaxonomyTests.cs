namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Kernel;

public sealed class ActionTaxonomyTests
{
    [Fact]
    public void SeReposerDefinition_RespecteChaineCanoniqueAct()
    {
        var definition = SeReposerDefinition.Definition;

        Assert.Equal("Entretien", definition.Principe);
        Assert.Equal("Repos", definition.Pattern);
        Assert.Equal("SeReposer", definition.Verbe);
    }

    [Fact]
    public void SeReposerDefinition_RespecteStructureContractuelleVerb001()
    {
        var contract = SeReposerDefinition.Definition.Contract;

        Assert.Single(contract.Inputs);
        Assert.Empty(contract.Preconditions);
        Assert.Empty(contract.Constraints);
        Assert.Empty(contract.Costs);
        Assert.Single(contract.Effects);

        var eventTemplate = Assert.Single(contract.Events);
        Assert.Equal("besoin.fatigue.restauree", eventTemplate.Kind);
    }

    [Fact]
    public void IntentSeReposer_EstPlanifieVersVerb001()
    {
        var world = new World(seed: 42);
        var acteur = world.Spawn();
        var intent = new Intent(
            acteur.Id,
            NeedsIntentSource.RestObjective,
            Priorite: 1);
        var planner = new SimplePlanner();

        var plan = planner.CreatePlan(intent, world);

        var etape = Assert.Single(plan.Steps);
        Assert.Same(SeReposerDefinition.Definition, etape.Definition);
        Assert.Equal("SeReposer", etape.Definition.Verbe);
        Assert.Equal("Repos", etape.Definition.Pattern);
        Assert.Equal("Entretien", etape.Definition.Principe);
    }
}
