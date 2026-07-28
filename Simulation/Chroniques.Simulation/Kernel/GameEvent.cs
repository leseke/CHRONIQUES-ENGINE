namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Event (CORE-007).
///
/// CORE-000-G est explicite : « les Events sont immuables ». C'est pourquoi
/// ce type est un <c>record</c> --- toute Value.With(...) produit une
/// nouvelle instance, jamais une mutation de l'original.
///
/// Nommé <c>GameEvent</c> plutôt que <c>Event</c> pour éviter toute
/// confusion avec le mot-clé C# <c>event</c> ; le mapping conceptuel avec
/// CORE-007 reste explicite dans cette documentation XML.
/// </summary>
public sealed record GameEvent(Guid Id, Tick OccurredAt, string Kind, EntityId? Source, EntityId? Target)
{
    public static GameEvent Create(Tick tick, string kind, EntityId? source = null, EntityId? target = null)
        => new(Guid.NewGuid(), tick, kind, source, target);
}
