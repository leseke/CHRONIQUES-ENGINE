namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Applique les Effects de VERB-004 — Donner une denrée après un Outcome
/// réussi. Le transfert est conservatif : la même quantité quitte la source
/// et entre dans la destination.
/// </summary>
public sealed class FoodTransferActionEffectApplicator : IActionEffectApplicator
{
    private readonly IVoluntaryFoodTransferResolver _resolver;

    public FoodTransferActionEffectApplicator(
        IVoluntaryFoodTransferResolver resolver)
    {
        _resolver = resolver
            ?? throw new ArgumentNullException(nameof(resolver));
    }

    public bool CanApply(ActionInstance instance)
        => string.Equals(
            instance.Definition.Verbe,
            DonnerDenreeDefinition.Definition.Verbe,
            StringComparison.Ordinal);

    public void Apply(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var actor))
        {
            throw new InvalidOperationException(
                "VERB-004 ne peut pas appliquer ses Effects sans Acteur existant.");
        }

        var opportunity = _resolver.FindExecutableTransfer(
            actor,
            world,
            world.CurrentTick)
            ?? throw new InvalidOperationException(
                "L'opportunité de transfert n'est plus exécutable au moment d'appliquer les Effects.");

        if (opportunity.RecipientId == actor.Id
            || opportunity.SourceFoodProductId == opportunity.DestinationFoodProductId
            || opportunity.Portions <= 0
            || !HasExpectedTargets(instance, opportunity)
            || !world.TryGetEntity(opportunity.RecipientId, out _)
            || !world.TryGetEntity(opportunity.SourceFoodProductId, out var sourceEntity)
            || !world.TryGetEntity(opportunity.DestinationFoodProductId, out var destinationEntity)
            || !sourceEntity.TryGet<FoodProductComponent>(out var source)
            || !destinationEntity.TryGet<FoodProductComponent>(out var destination)
            || source.PortionsDisponibles < opportunity.Portions
            || string.IsNullOrWhiteSpace(source.ProductKindId)
            || string.IsNullOrWhiteSpace(destination.ProductKindId)
            || !string.Equals(
                source.ProductKindId,
                destination.ProductKindId,
                StringComparison.Ordinal)
            || source.FaimRestauree != destination.FaimRestauree)
        {
            throw new InvalidOperationException(
                "Les Cibles, parties ou stocks ne correspondent plus au transfert validé.");
        }

        source.PortionsDisponibles -= opportunity.Portions;
        destination.PortionsDisponibles = checked(
            destination.PortionsDisponibles + opportunity.Portions);

        foreach (var eventTemplate in instance.Definition.Contract.Events)
        {
            world.Publish(
                GameEvent.Create(
                    world.CurrentTick,
                    eventTemplate.Kind,
                    instance.Acteur));
        }
    }

    private static bool HasExpectedTargets(
        ActionInstance instance,
        FoodTransferOpportunity opportunity)
    {
        if (instance.Cibles.Count != 3)
        {
            return false;
        }

        return instance.Cibles.Any(
                target => target.Role == RoleCible.Principale
                    && target.Cible == opportunity.DestinationFoodProductId)
            && instance.Cibles.Any(
                target => target.Role == RoleCible.Secondaire
                    && target.Cible == opportunity.SourceFoodProductId)
            && instance.Cibles.Any(
                target => target.Role == RoleCible.Secondaire
                    && target.Cible == opportunity.RecipientId);
    }
}
