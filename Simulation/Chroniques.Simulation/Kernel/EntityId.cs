namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Identité unique et immuable d'une <see cref="Entity"/>.
///
/// Implémente CORE-002-C : l'identité ne porte aucune signification métier
/// (ni type, ni rôle, ni propriété) --- elle sert uniquement à distinguer
/// une Entity de toutes les autres durant toute son existence.
/// </summary>
public readonly record struct EntityId(Guid Value)
{
    /// <summary>
    /// Crée une nouvelle identité, garantie unique (CORE-002-C, section 4).
    /// </summary>
    public static EntityId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
