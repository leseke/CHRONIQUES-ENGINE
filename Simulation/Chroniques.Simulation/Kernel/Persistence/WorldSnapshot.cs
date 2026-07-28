namespace Chroniques.Simulation.Kernel.Persistence;

/// <summary>
/// Représentation sérialisable d'un <see cref="World"/>.
///
/// Ce DTO couvre exactement ce qu'exige le critère de sortie v0.1 de
/// MASTER-005 (graine, Tick courant, identités des Entity, Events publiés).
///
/// Les Components concrets n'existent pas encore à ce stade du projet ---
/// aucun n'est défini au-delà de l'interface <see cref="IComponent"/>. Leur
/// sérialisation polymorphe (nécessitant des JsonDerivedType ou un
/// convertisseur dédié) sera traitée quand les premiers Components métier
/// seront écrits, jamais par anticipation (MASTER-008, section 4 : pas de
/// mise en conformité mécanique isolée sans motif réel).
/// </summary>
public sealed record WorldSnapshot(
    long Seed,
    long CurrentTick,
    IReadOnlyList<Guid> EntityIds,
    IReadOnlyList<GameEvent> Events);
