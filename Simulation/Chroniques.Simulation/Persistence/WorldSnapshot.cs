namespace Chroniques.Simulation.Persistence;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Représentation sérialisable d'une Entity : son identité, plus chaque
/// Component métier concret qui lui est attaché.
///
/// Approche volontairement explicite plutôt que polymorphe : System.Text.Json
/// ne sérialise pas nativement un <c>Dictionary&lt;Type, IComponent&gt;</c>
/// sans convertisseur dédié. Tant qu'un petit nombre de Components existe,
/// un champ nullable par type reste plus simple et plus lisible qu'un
/// mécanisme générique. Ce choix sera reconsidéré --- et tracé par un ADR
/// --- le jour où le nombre de Components rendra cette liste manuelle
/// pénible à maintenir (MASTER-006 : pas d'anticipation sans motif réel).
///
/// <paramref name="LifecycleCreatedAt"/> et <paramref name="LifecycleState"/>
/// couvrent la partie du Lifecycle nécessaire à v0.2 : savoir qu'un
/// personnage rechargé est toujours vivant, en vieillesse, ou déjà mort.
/// L'historique complet des Events qui ont mené à cet état n'est pas encore
/// persisté --- limite assumée, documentée sur <see cref="Kernel.Entity.Restore"/>.
/// </summary>
public sealed record EntitySnapshot(
    Guid Id,
    long LifecycleCreatedAt,
    string LifecycleState,
    NeedsComponent? Needs,
    AgeComponent? Age);

/// <summary>
/// Représentation sérialisable d'un <see cref="World"/>.
///
/// Couvre le critère de sortie v0.1 de MASTER-005 (graine, Tick courant,
/// Entity, Events) et, depuis v0.2, les Components métier attachés à
/// chaque Entity (<see cref="EntitySnapshot"/>).
/// </summary>
public sealed record WorldSnapshot(
    long Seed,
    long CurrentTick,
    IReadOnlyList<EntitySnapshot> Entities,
    IReadOnlyList<GameEvent> Events);
