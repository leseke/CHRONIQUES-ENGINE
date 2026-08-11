namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Execution Engine minimal de VERB-004 / ENGINE-014.
///
/// Il revalide l'opportunité et les stocks mais ne modifie jamais le World.
/// </summary>
public sealed class FoodTransferExecutionEngine : IExecutionEngine
{
    private readonly IVoluntaryFoodTransferResolver _resolver;

    public FoodTransferExecutionEngine(
        IVoluntaryFoodTransferResolver resolver)
    {
        _resolver = resolver
            ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Outcome Execute(ActionInstance instance, World world)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(world);

        if (!string.Equals(
                instance.Definition.Verbe,
                DonnerDenreeDefinition.Definition.Verbe,
                StringComparison.Ordinal)
            || !world.TryGetEntity(instance.Acteur, out var actor))
        {
            return new Outcome(OutcomeForme.Echec);
        }

        var opportunity = _resolver.FindExecutableTransfer(
            actor,
            world,
            world.CurrentTick);

        if (opportunity is null
            || opportunity.Portions <= 0
            || opportunity.RecipientId == actor.Id
            || opportunity.SourceFoodProductId == opportunity.DestinationFoodProductId
            || !world.TryGetEntity(opportunity.RecipientId, out _)
            || !HasExpectedTargets(instance, opportunity)
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
            return new Outcome(OutcomeForme.Echec);
        }

        return new Outcome(OutcomeForme.Reussite);
    }

    private static bool HasExpectedTargets(
        ActionInstance instance,
        FoodTransferOpportunity opportunity)
    {
        if (instance.Cibles.Count != 3)
        {
            return false;
        }

        var destinationMatches = instance.Cibles.Any(
            target => target.Role == RoleCible.Principale
                && target.Cible == opportunity.DestinationFoodProductId);

        var sourceMatches = instance.Cibles.Any(
            target => target.Role == RoleCible.Secondaire
                && target.Cible == opportunity.SourceFoodProductId);

        var recipientMatches = instance.Cibles.Any(
            target => target.Role == RoleCible.Secondaire
                && target.Cible == opportunity.RecipientId);

        return destinationMatches
            && sourceMatches
            && recipientMatches;
    }
}
