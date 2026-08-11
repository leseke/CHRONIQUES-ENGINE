namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Planner minimal de VERB-004 / ENGINE-014.
///
/// Le destinataire, la source, la destination et la quantité proviennent de
/// l'opportunité volontaire résolue dans le contexte, jamais de l'Intent.
/// </summary>
public sealed class FoodTransferPlanner : IPlanner
{
    private readonly IVoluntaryFoodTransferResolver _resolver;

    public FoodTransferPlanner(
        IVoluntaryFoodTransferResolver resolver)
    {
        _resolver = resolver
            ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Plan CreatePlan(Intent intent, World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.TryGetEntity(intent.Acteur, out var actor))
        {
            throw new InvalidOperationException(
                "Impossible de planifier un transfert pour un Acteur absent du World.");
        }

        if (!string.Equals(
                intent.Objectif,
                VoluntaryFoodTransferIntentSource.GiveFoodObjective,
                StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"FoodTransferPlanner ne reconnaît pas l'objectif \"{intent.Objectif}\".");
        }

        var opportunity = _resolver.FindExecutableTransfer(
            actor,
            world,
            world.CurrentTick)
            ?? throw new InvalidOperationException(
                "Aucune opportunité volontaire exécutable ne permet de planifier l'Intent donner_denree.");

        var step = new PlanStep(
            DonnerDenreeDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>())
        {
            Cibles = new[]
            {
                new CibleRef(
                    opportunity.DestinationFoodProductId,
                    RoleCible.Principale),
                new CibleRef(
                    opportunity.SourceFoodProductId,
                    RoleCible.Secondaire),
                new CibleRef(
                    opportunity.RecipientId,
                    RoleCible.Secondaire)
            }
        };

        return new Plan(intent, new[] { step });
    }
}
