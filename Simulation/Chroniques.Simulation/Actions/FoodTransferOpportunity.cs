namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Opportunité de transfert volontaire minimale de VERB-004 / ENGINE-014.
///
/// Elle décrit le destinataire, les deux stocks et la quantité. La volonté
/// et l'autorisation appartiennent au resolver contextuel qui fournit cette
/// donnée ; ce record n'exécute aucune logique.
/// </summary>
public sealed record FoodTransferOpportunity
{
    public EntityId RecipientId { get; }
    public EntityId SourceFoodProductId { get; }
    public EntityId DestinationFoodProductId { get; }
    public int Portions { get; }

    public FoodTransferOpportunity(
        EntityId recipientId,
        EntityId sourceFoodProductId,
        EntityId destinationFoodProductId,
        int portions)
    {
        if (portions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(portions),
                portions,
                "Le nombre de portions à transférer doit être strictement positif.");
        }

        RecipientId = recipientId;
        SourceFoodProductId = sourceFoodProductId;
        DestinationFoodProductId = destinationFoodProductId;
        Portions = portions;
    }
}
