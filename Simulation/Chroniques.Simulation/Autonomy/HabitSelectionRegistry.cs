namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Sélection runtime d'une Habitude. Cette donnée n'est jamais persistée et
/// n'enrichit pas ACT Intent avec une métadonnée de source.
/// </summary>
public sealed record HabitSelection(
    EntityId ActorId,
    string HabitTypeId,
    string IntentObjective,
    string FormationSignature,
    Tick SelectedAt);

/// <summary>
/// Pont runtime entre HabitIntentSource et HabitLearningObserver.
/// </summary>
public sealed class HabitSelectionRegistry
{
    private readonly Dictionary<EntityId, HabitSelection> _selections = new();

    public void Record(HabitSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        _selections[selection.ActorId] = selection;
    }

    public bool TryGet(EntityId actorId, out HabitSelection selection)
        => _selections.TryGetValue(actorId, out selection!);

    public bool TryTake(EntityId actorId, out HabitSelection selection)
    {
        if (_selections.TryGetValue(actorId, out selection!))
        {
            _selections.Remove(actorId);
            return true;
        }

        selection = default!;
        return false;
    }

    public void Clear(EntityId actorId) => _selections.Remove(actorId);
}
