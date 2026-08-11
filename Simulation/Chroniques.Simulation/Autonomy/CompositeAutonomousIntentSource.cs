namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Compose plusieurs sources d'Intent dans un ordre déterministe.
///
/// La première source qui produit un Intent gagne ; les suivantes ne sont
/// pas consultées. ENGINE-013 utilise cet ordre pour appliquer GDB-004A :
/// entretien actionnable avant activité productive.
/// </summary>
public sealed class CompositeAutonomousIntentSource : IAutonomousIntentSource
{
    private readonly IReadOnlyList<IAutonomousIntentSource> _sources;

    public CompositeAutonomousIntentSource(
        params IAutonomousIntentSource[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (sources.Any(source => source is null))
        {
            throw new ArgumentException(
                "Une source d'Intent composite ne peut pas contenir de valeur null.",
                nameof(sources));
        }

        _sources = sources.ToArray();
    }

    public Intent? CreateIntent(
        Entity actor,
        World world,
        Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(world);

        foreach (var source in _sources)
        {
            var intent = source.CreateIntent(actor, world, currentTick);
            if (intent is not null)
            {
                return intent;
            }
        }

        return null;
    }
}
