namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Execution Engine minimal pour VERB-001 et VERB-002.
///
/// Il valide l'état nécessaire à l'exécution mais ne modifie jamais le World.
/// Les Effects restent appliqués après l'Outcome par les applicateurs dédiés.
/// </summary>
public sealed class NeedsExecutionEngine : IExecutionEngine
{
    private readonly IAccessibleFoodResolver _foodResolver;

    public NeedsExecutionEngine(IAccessibleFoodResolver foodResolver)
    {
        _foodResolver = foodResolver
            ?? throw new ArgumentNullException(nameof(foodResolver));
    }

    public Outcome Execute(ActionInstance instance, World world)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(world);

        if (!world.TryGetEntity(instance.Acteur, out var acteur)
            || !acteur.Has<NeedsComponent>())
        {
            return new Outcome(OutcomeForme.Echec);
        }

        if (string.Equals(
                instance.Definition.Verbe,
                SeReposerDefinition.Definition.Verbe,
                StringComparison.Ordinal))
        {
            return new Outcome(OutcomeForme.Reussite);
        }

        if (!string.Equals(
                instance.Definition.Verbe,
                MangerDefinition.Definition.Verbe,
                StringComparison.Ordinal))
        {
            return new Outcome(OutcomeForme.Echec);
        }

        var ciblePrincipale = instance.Cibles.SingleOrDefault(
            cible => cible.Role == RoleCible.Principale);

        if (ciblePrincipale is null
            || !world.TryGetEntity(ciblePrincipale.Cible, out var nourriture)
            || !nourriture.TryGet<FoodProductComponent>(out var produit)
            || produit.PortionsDisponibles <= 0
            || produit.FaimRestauree <= 0
            || !_foodResolver.IsAccessible(
                acteur,
                ciblePrincipale.Cible,
                world,
                world.CurrentTick))
        {
            return new Outcome(OutcomeForme.Echec);
        }

        return new Outcome(OutcomeForme.Reussite);
    }
}
