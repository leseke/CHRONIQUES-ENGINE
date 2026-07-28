namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Entity définie par CORE-002.
///
/// Une Entity est un point d'ancrage identifiable et neutre : elle ne
/// possède aucune signification métier intrinsèque (CORE-002-B, section 3)
/// --- sa nature est entièrement déterminée par les <see cref="IComponent"/>
/// qui lui sont associés. La composition est toujours privilégiée à
/// l'héritage (CORE-002-B, section 6 ; CORE-000-E).
///
/// CORE-002-H est explicite : « une Entity... possède un Lifecycle ». Toute
/// Entity porte donc désormais, dès sa création, un <see cref="Kernel.Lifecycle"/>
/// qui trace sa continuité --- primitive qui existait dans le Kernel depuis
/// v0.1 sans qu'aucune Entity n'y soit reliée (écart CORE-002-H comblé en
/// v0.2).
/// </summary>
public sealed class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public EntityId Id { get; }

    /// <summary>
    /// Continuité de cette Entity dans le temps (CORE-002-H). L'état initial
    /// « vivant » n'est qu'un point de départ neutre --- seul un System (ex.
    /// <see cref="Systems.AgingSystem"/>) peut le faire progresser, jamais
    /// l'Entity ni le Lifecycle lui-même (CORE-010-C : Lifecycle reste
    /// strictement descriptif).
    /// </summary>
    public Lifecycle Lifecycle { get; }

    private Entity(EntityId id, Lifecycle lifecycle)
    {
        Id = id;
        Lifecycle = lifecycle;
    }

    /// <summary>
    /// Crée une nouvelle Entity avec une identité fraîche, née au Tick donné.
    /// <paramref name="createdAt"/> vaut <see cref="Tick.Zero"/> par défaut,
    /// pour les usages hors <see cref="World"/> (ex. tests unitaires du
    /// Kernel qui n'ont pas de Tick courant à fournir).
    /// </summary>
    public static Entity Create(Tick? createdAt = null) =>
        new(EntityId.New(), new Lifecycle(createdAt ?? Tick.Zero, new State("vivant")));

    /// <summary>
    /// Reconstruit une Entity à partir d'une identité et d'une continuité
    /// déjà connues --- utilisé exclusivement par la couche de persistance
    /// (<see cref="Persistence.WorldRepository"/>) pour préserver l'identité
    /// stable exigée par CORE-002-C, section 3.
    ///
    /// Limite connue, assumée : seuls l'instant de création et l'état courant
    /// du Lifecycle survivent au rechargement, pas l'historique complet des
    /// Events qui y ont mené. Couvrir cet historique appartient à une future
    /// extension de <see cref="Persistence.WorldSnapshot"/>, à construire
    /// quand un besoin réel l'exigera, pas par anticipation (MASTER-006).
    /// </summary>
    internal static Entity Restore(EntityId id, Tick lifecycleCreatedAt, string lifecycleState) =>
        new(id, new Lifecycle(lifecycleCreatedAt, new State(lifecycleState)));

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
