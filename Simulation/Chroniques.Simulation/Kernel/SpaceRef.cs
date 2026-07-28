namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Space (CORE-009) : une localisation, orthogonale
/// aux autres primitives (CORE-000-D --- Space n'a aucune dépendance
/// déclarée).
///
/// Pour le noyau minimal (v0.1 --- MASTER-005), une localisation référence
/// simplement l'Entity qui représente le lieu. La hiérarchie géographique
/// complète (Monde, Continent, Région, Zone, Lieu, Point d'intérêt et leurs
/// cardinalités) est définie par GDB-003 [réf: GDB-003A] et sera modélisée
/// avec des Entity + Component dédiés lorsque la Phase correspondante de
/// MASTER-005 sera atteinte --- pas avant.
/// </summary>
public readonly record struct SpaceRef(EntityId LocationEntity);
