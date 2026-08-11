namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Opération productive minimale de VERB-003 / ENGINE-013.
///
/// Elle décrit une transformation configurée ; elle n'exécute aucune logique.
/// </summary>
public sealed record ProductionOperation
{
    public string OperationId { get; }
    public EntityId InputResourceId { get; }
    public double InputQuantity { get; }
    public EntityId OutputFoodProductId { get; }
    public int OutputPortions { get; }

    public ProductionOperation(
        string operationId,
        EntityId inputResourceId,
        double inputQuantity,
        EntityId outputFoodProductId,
        int outputPortions)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new ArgumentException(
                "L'identifiant d'une opération de production ne peut pas être vide.",
                nameof(operationId));
        }

        if (double.IsNaN(inputQuantity)
            || double.IsInfinity(inputQuantity)
            || inputQuantity <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputQuantity),
                inputQuantity,
                "La quantité d'entrée doit être strictement positive.");
        }

        if (outputPortions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outputPortions),
                outputPortions,
                "Le nombre de portions produites doit être strictement positif.");
        }

        OperationId = operationId;
        InputResourceId = inputResourceId;
        InputQuantity = inputQuantity;
        OutputFoodProductId = outputFoodProductId;
        OutputPortions = outputPortions;
    }
}
