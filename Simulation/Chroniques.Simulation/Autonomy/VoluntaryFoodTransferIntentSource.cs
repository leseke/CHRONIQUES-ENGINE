namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Source d'Intent minimale de VERB-004 / ENGINE-014.
///
/// Une opportunité volontaire exécutable produit l'objectif abstrait
/// "donner_denree". Le destinataire, les stocks et la quantité restent hors
/// de l'Intent et seront matérialisés par le Planner.
/// </summary>
public sealed class VoluntaryFoodTransferIntentSource : IAutonomousIntentSource
{
    public const string GiveFoodObjective = "donner_denree";

    private readonly IVoluntaryFoodTransferResolver _resolver;

    public VoluntaryFoodTransferIntentSource(
        IVoluntaryFoodTransferResolver resolver)
    {
        _resolver = resolver
            ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        var opportunity = _resolver.FindExecutableTransfer(
            actor,
            world,
            currentTick);

        return opportunity is null
            ? null
            : new Intent(actor.Id, GiveFoodObjective, Priorite: 1);
    }
}
