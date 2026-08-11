namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Première politique de décision autonome concrète de v0.4 (ENGINE-011).
///
/// Traduit uniquement le contrat GDB-004B v1.1 actuellement implémentable :
/// une satisfaction de Fatigue strictement inférieure au seuil configuré
/// produit l'Intent « se_reposer ».
///
/// Cette source ne modifie jamais le World et n'invente aucun Intent pour
/// les autres besoins tant qu'aucune réponse exécutable documentée n'existe.
/// </summary>
public sealed class NeedsIntentSource : IAutonomousIntentSource
{
    public const string RestObjective = "se_reposer";

    public double FatigueActivationThreshold { get; }

    public NeedsIntentSource(double fatigueActivationThreshold)
    {
        if (double.IsNaN(fatigueActivationThreshold)
            || double.IsInfinity(fatigueActivationThreshold)
            || fatigueActivationThreshold < 0.0
            || fatigueActivationThreshold > 100.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fatigueActivationThreshold),
                fatigueActivationThreshold,
                "Le seuil de Fatigue doit être compris entre 0 et 100 (GDB-004B v1.1).");
        }

        FatigueActivationThreshold = fatigueActivationThreshold;
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        if (!actor.TryGet<NeedsComponent>(out var needs))
        {
            return null;
        }

        if (needs.Fatigue >= FatigueActivationThreshold)
        {
            return null;
        }

        return new Intent(
            actor.Id,
            RestObjective,
            Priorite: 1);
    }
}
