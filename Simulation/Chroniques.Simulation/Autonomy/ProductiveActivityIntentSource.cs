namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Source d'Intent productive minimale d'ENGINE-013.
///
/// Elle ne crée un objectif productif que lorsqu'une opération réellement
/// exécutable est fournie par le contexte.
/// </summary>
public sealed class ProductiveActivityIntentSource : IAutonomousIntentSource
{
    public const string ProduceFoodObjective = "produire_denree";

    private readonly IProductiveActivityResolver _resolver;

    public ProductiveActivityIntentSource(IProductiveActivityResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        var operation = _resolver.FindExecutableOperation(
            actor,
            world,
            currentTick);

        if (operation is null)
        {
            return null;
        }

        return new Intent(
            actor.Id,
            ProduceFoodObjective,
            Priorite: 1);
    }
}
