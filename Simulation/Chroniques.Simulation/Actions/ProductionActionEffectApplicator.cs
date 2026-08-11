namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Applique les Effects matériels de VERB-003 après Outcome réussi.
/// </summary>
public sealed class ProductionActionEffectApplicator : IActionEffectApplicator
{
    private readonly IProductiveActivityResolver _resolver;

    public ProductionActionEffectApplicator(IProductiveActivityResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public bool CanApply(ActionInstance instance) =>
        string.Equals(
            instance.Definition.Verbe,
            ProduireDenreeDefinition.Definition.Verbe,
            StringComparison.Ordinal);

    public void Apply(ActionInstance instance, World world)
    {
        if (!world.TryGetEntity(instance.Acteur, out var actor))
        {
            throw new InvalidOperationException("Acteur de production absent du World.");
        }

        var operation = _resolver.FindExecutableOperation(actor, world, world.CurrentTick)
            ?? throw new InvalidOperationException("L'opération productive n'est plus exécutable.");

        var principale = instance.Cibles.Single(c => c.Role == RoleCible.Principale);
        var secondaire = instance.Cibles.Single(c => c.Role == RoleCible.Secondaire);

        if (!principale.Cible.Equals(operation.OutputFoodProductId)
            || !secondaire.Cible.Equals(operation.InputResourceId)
            || !world.TryGetEntity(operation.InputResourceId, out var input)
            || !input.TryGet<ResourceStockComponent>(out var stock)
            || stock.Quantity < operation.InputQuantity
            || !world.TryGetEntity(operation.OutputFoodProductId, out var output)
            || !output.TryGet<FoodProductComponent>(out var food))
        {
            throw new InvalidOperationException(
                "Les Cibles ou stocks ne correspondent plus à l'opération validée.");
        }

        stock.Quantity -= operation.InputQuantity;
        food.PortionsDisponibles = checked(food.PortionsDisponibles + operation.OutputPortions);

        if (!output.TryGet<ProductionProvenanceComponent>(out var provenance))
        {
            provenance = new ProductionProvenanceComponent();
            output.Set(provenance);
        }

        provenance.Traces.Add(new ProductionTrace(
            operation.OperationId,
            operation.InputResourceId,
            world.CurrentTick));

        foreach (var eventTemplate in instance.Definition.Contract.Events)
        {
            world.Publish(GameEvent.Create(
                world.CurrentTick,
                eventTemplate.Kind,
                instance.Acteur));
        }
    }
}
