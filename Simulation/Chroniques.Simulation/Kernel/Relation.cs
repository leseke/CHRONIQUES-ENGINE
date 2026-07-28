namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Relation (CORE-006) : un lien qualifié entre deux
/// <see cref="Entity"/>, jamais entre une Entity et une donnée arbitraire
/// (CORE-000-D : Relation → Entity, Relation → State).
///
/// Une Relation n'existe jamais sans les deux Entity qu'elle relie.
/// </summary>
public sealed class Relation
{
    public EntityId Source { get; }
    public EntityId Target { get; }
    public string Kind { get; }
    public State State { get; }

    public Relation(EntityId source, EntityId target, string kind, State state)
    {
        Source = source;
        Target = target;
        Kind = kind;
        State = state;
    }
}
