namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Entity définie par CORE-002.
///
/// Une Entity est un point d'ancrage identifiable et neutre : elle ne
/// possède aucune signification métier intrinsèque (CORE-002-B, section 3)
/// --- sa nature est entièrement déterminée par les <see cref="IComponent"/>
/// qui lui sont associés. La composition est toujours privilégiée à
/// l'héritage (CORE-002-B, section 6 ; CORE-000-E).
/// </summary>
public sealed class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public EntityId Id { get; }

    private Entity(EntityId id)
    {
        Id = id;
    }

    /// <summary>
    /// Crée une nouvelle Entity avec une identité fraîche.
    /// </summary>
    public static Entity Create() => new(EntityId.New());

    /// <summary>
    /// Reconstruit une Entity à partir d'une identité déjà connue --- utilisé
    /// exclusivement par la couche de persistance (<see cref="Persistence.WorldRepository"/>)
    /// pour préserver l'identité stable exigée par CORE-002-C, section 3.
    /// </summary>
    internal static Entity Restore(EntityId id) => new(id);

    /// <summary>
    /// Attache ou remplace un Component. Un même type de Component ne peut
    /// être présent qu'une seule fois (CORE-003-C, section 5).
    /// </summary>
    public void Set<T>(T component) where T : IComponent
    {
        _components[typeof(T)] = component;
    }

    public bool TryGet<T>(out T component) where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var raw) && raw is T typed)
        {
            component = typed;
            return true;
        }

        component = default!;
        return false;
    }

    public bool Has<T>() where T : IComponent => _components.ContainsKey(typeof(T));

    /// <summary>
    /// Retire un Component. L'absence d'un Component ne remet jamais en
    /// cause l'existence de l'Entity (CORE-002-B, section 4).
    /// </summary>
    public void Remove<T>() where T : IComponent => _components.Remove(typeof(T));

    public IReadOnlyCollection<IComponent> Components => _components.Values;
}
