namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Conteneur de la simulation. Le World assemble les primitives du Kernel
/// mais ne contient lui-même aucune donnée métier (CORE-000-G).
///
/// Critère de sortie v0.1 (MASTER-005) : « le noyau tourne, tous les tests
/// de lois passent, un World vide se sauvegarde et se recharge à
/// l'identique. » Voir <see cref="Chroniques.Simulation.Persistence.WorldRepository"/> pour la
/// sauvegarde/rechargement.
/// </summary>
public sealed class World
{
    private readonly Dictionary<EntityId, Entity> _entities = new();
    private readonly List<GameEvent> _events = new();

    public long Seed { get; }
    public Tick CurrentTick { get; private set; }
    public DeterministicRandom Random { get; }

    public World(long seed)
        : this(seed, Tick.Zero)
    {
    }

    private World(long seed, Tick currentTick)
    {
        Seed = seed;
        CurrentTick = currentTick;
        Random = new DeterministicRandom(seed);
    }

    /// <summary>
    /// Reconstruit un World à un Tick donné --- utilisé exclusivement par
    /// <see cref="Chroniques.Simulation.Persistence.WorldRepository"/> lors du rechargement.
    /// </summary>
    internal static World Restore(long seed, Tick currentTick) => new(seed, currentTick);

    public Entity Spawn()
    {
        var entity = Entity.Create();
        _entities[entity.Id] = entity;
        return entity;
    }

    /// <summary>
    /// Réintroduit une Entity existante --- utilisé exclusivement lors du
    /// rechargement, afin de préserver l'identité stable de chaque Entity
    /// (CORE-002-C, section 3).
    /// </summary>
    internal void Reintroduce(Entity entity)
    {
        _entities[entity.Id] = entity;
    }

    public bool TryGetEntity(EntityId id, out Entity entity) => _entities.TryGetValue(id, out entity!);

    public IReadOnlyCollection<Entity> Entities => _entities.Values;

    public void Publish(GameEvent occurredEvent) => _events.Add(occurredEvent);

    public IReadOnlyList<GameEvent> Events => _events;

    internal void ReplayEvents(IEnumerable<GameEvent> events) => _events.AddRange(events);

    public void Advance() => CurrentTick = CurrentTick.Next();
}
