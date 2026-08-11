namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Politique de décision autonome minimale des besoins (ENGINE-011/012).
///
/// Deux réponses sont actuellement documentées et exécutables :
/// - Fatigue → « se_reposer » ;
/// - Faim + nourriture accessible → « manger ».
///
/// Cette source ne modifie jamais le World et ne produit aucun Intent pour
/// Santé ou Moral tant qu'aucune réponse exécutable documentée n'existe.
/// </summary>
public sealed class NeedsIntentSource : IAutonomousIntentSource
{
    public const string RestObjective = "se_reposer";
    public const string EatObjective = "manger";

    private readonly IAccessibleFoodResolver? _foodResolver;

    public double FatigueActivationThreshold { get; }
    public double? FaimActivationThreshold { get; }

    /// <summary>
    /// Constructeur historique ENGINE-011 : autonomie repos uniquement.
    /// </summary>
    public NeedsIntentSource(double fatigueActivationThreshold)
    {
        ValidateThreshold(
            fatigueActivationThreshold,
            nameof(fatigueActivationThreshold));

        FatigueActivationThreshold = fatigueActivationThreshold;
    }

    /// <summary>
    /// Configuration ENGINE-012 : repos + alimentation autonome.
    /// </summary>
    public NeedsIntentSource(
        double fatigueActivationThreshold,
        double faimActivationThreshold,
        IAccessibleFoodResolver foodResolver)
    {
        ValidateThreshold(
            fatigueActivationThreshold,
            nameof(fatigueActivationThreshold));
        ValidateThreshold(
            faimActivationThreshold,
            nameof(faimActivationThreshold));

        FatigueActivationThreshold = fatigueActivationThreshold;
        FaimActivationThreshold = faimActivationThreshold;
        _foodResolver = foodResolver
            ?? throw new ArgumentNullException(nameof(foodResolver));
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

        var reposActionnable =
            needs.Fatigue < FatigueActivationThreshold;

        var faimActionnable =
            FaimActivationThreshold.HasValue
            && _foodResolver is not null
            && needs.Faim < FaimActivationThreshold.Value
            && _foodResolver.FindAccessibleFood(
                actor,
                world,
                currentTick) is not null;

        if (!reposActionnable && !faimActionnable)
        {
            return null;
        }

        if (reposActionnable && faimActionnable)
        {
            // GDB-004B v1.2 : satisfaction la plus basse = urgence de base
            // la plus forte. En égalité stricte, ENGINE-012 fixe Faim avant
            // Fatigue comme ordre technique stable, sans portée narrative.
            if (needs.Faim <= needs.Fatigue)
            {
                return CreateIntent(actor.Id, EatObjective);
            }

            return CreateIntent(actor.Id, RestObjective);
        }

        return faimActionnable
            ? CreateIntent(actor.Id, EatObjective)
            : CreateIntent(actor.Id, RestObjective);
    }

    private static Intent CreateIntent(
        EntityId actorId,
        string objective)
        => new(
            actorId,
            objective,
            Priorite: 1);

    private static void ValidateThreshold(
        double threshold,
        string parameterName)
    {
        if (double.IsNaN(threshold)
            || double.IsInfinity(threshold)
            || threshold < 0.0
            || threshold > 100.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                threshold,
                "Le seuil d'un besoin doit être compris entre 0 et 100 (GDB-004B v1.2).");
        }
    }
}
