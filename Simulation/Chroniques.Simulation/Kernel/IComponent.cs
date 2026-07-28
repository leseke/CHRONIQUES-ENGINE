namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Marqueur pour toute donnée conceptuelle attachée à une <see cref="Entity"/>
/// (CORE-003).
///
/// Un Component ne contient jamais de logique, d'algorithme ni de règle de
/// décision (CORE-003-C, section 4 --- responsabilités interdites). Ces
/// responsabilités appartiennent aux Systems, qui vivent hors du Kernel et
/// n'existent pas encore à ce stade du projet (v0.1 : le noyau, sans aucune
/// règle de jeu --- voir MASTER-005).
///
/// Un même type de Component ne peut être présent qu'une seule fois par
/// Entity (CORE-003-C, section 5 : responsabilité unique).
/// </summary>
public interface IComponent
{
}
