namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Applique les Effects de VERB-001 — Se reposer.
/// </summary>
public sealed class RestActionEffectApplicator : IActionEffectApplicator
{
    public bool CanApply(ActionInstance instance)
        => string.Equals(
            instance.Definition.Verbe,
            SeReposerDefinition.Definition.Verbe,
            StringComparison.Ordinal);

    public void Apply(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var acteur)
            || !acteur.TryGet<NeedsComponent>(out var besoins))
        {
            return;
        }

        besoins.Fatigue = Math.Min(
            100.0,
            besoins.Fatigue + SeReposerDefinition.FatigueRestauree);

        foreach (var eventTemplate in instance.Definition.Contract.Events)
        {
            world.Publish(
                GameEvent.Create(
                    world.CurrentTick,
                    eventTemplate.Kind,
                    instance.Acteur));
        }
    }
}
