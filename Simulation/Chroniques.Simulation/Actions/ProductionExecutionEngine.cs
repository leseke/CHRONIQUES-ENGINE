namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Valide l'exécution de VERB-003 sans modifier le World.
/// </summary>
public sealed class ProductionExecutionEngine : IExecutionEngine
{
    private readonly IProductiveActivityResolver _resolver;

    public ProductionExecutionEngine(IProductiveActivityResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Outcome Execute(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var actor))
        {
            return new Outcome(OutcomeForme.Echec);
        }

        var operation = _resolver.FindExecutableOperation(actor, world, world.CurrentTick);
        if (operation is null)
        {
            return new Outcome(OutcomeForme.Echec);
        }

        var principale = instance.Cibles.SingleOrDefault(c => c.Role == RoleCible.Principale);
        var secondaire = instance.Cibles.SingleOrDefault(c => c.Role == RoleCible.Secondaire);

        if (principale is null
            || secondaire is null
            || !principale.Cible.Equals(operation.OutputFoodProductId)
            || !secondaire.Cible.Equals(operation.InputResourceId)
            || !world.TryGetEntity(operation.InputResourceId, out var input)
            || !input.TryGet<ResourceStockComponent>(out var stock)
            || stock.Quantity < operation.InputQuantity
            || !world.TryGetEntity(operation.OutputFoodProductId, out var output)
            || !output.TryGet<FoodProductComponent>(out var food)
            || food.FaimRestauree <= 0)
        {
            return new Outcome(OutcomeForme.Echec);
        }

        return new Outcome(OutcomeForme.Reussite);
    }
}
