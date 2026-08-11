namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Applique les Effects de VERB-002 — Manger après un Outcome réussi.
/// Une seule portion est consommée par Action dans ENGINE-012.
/// </summary>
public sealed class FoodActionEffectApplicator : IActionEffectApplicator
{
    public bool CanApply(ActionInstance instance)
        => string.Equals(
            instance.Definition.Verbe,
            MangerDefinition.Definition.Verbe,
            StringComparison.Ordinal);

    public void Apply(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var acteur)
            || !acteur.TryGet<NeedsComponent>(out var besoins))
        {
            throw new InvalidOperationException(
                "VERB-002 ne peut pas appliquer ses Effects sans Acteur porteur de NeedsComponent.");
        }

        var ciblePrincipale = instance.Cibles.Single(
            cible => cible.Role == RoleCible.Principale);

        if (!world.TryGetEntity(ciblePrincipale.Cible, out var nourriture)
            || !nourriture.TryGet<FoodProductComponent>(out var produit)
            || produit.PortionsDisponibles <= 0
            || produit.FaimRestauree <= 0)
        {
            throw new InvalidOperationException(
                "VERB-002 ne peut appliquer ses Effects sans produit alimentaire disponible et valide.");
        }

        produit.PortionsDisponibles -= 1;
        besoins.Faim = Math.Min(
            100.0,
            besoins.Faim + produit.FaimRestauree);

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
