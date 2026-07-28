namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Lifecycle (CORE-010) : représente la continuité
/// d'une <see cref="Entity"/> dans le temps, au travers d'une suite ordonnée
/// de <see cref="GameEvent"/> (CORE-000-G : « Lifecycle représente une
/// continuité » ; CORE-000-D : Lifecycle → Event, State, Time).
/// </summary>
public sealed class Lifecycle
{
    private readonly List<GameEvent> _history = new();

    public Tick CreatedAt { get; }
    public State CurrentState { get; private set; }

    public Lifecycle(Tick createdAt, State initialState)
    {
        CreatedAt = createdAt;
        CurrentState = initialState;
    }

    public IReadOnlyList<GameEvent> History => _history;

    /// <summary>
    /// Enregistre un Event dans l'historique et, le cas échéant, fait
    /// progresser le State courant. L'Event lui-même reste immuable ; seule
    /// la référence au State courant change.
    /// </summary>
    public void Record(GameEvent occurredEvent, State? newState = null)
    {
        _history.Add(occurredEvent);

        if (newState is not null)
        {
            CurrentState = newState;
        }
    }
}
