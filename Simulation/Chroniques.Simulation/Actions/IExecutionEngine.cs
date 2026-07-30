using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Interprète une <see cref="ActionInstance"/> et produit un
/// <see cref="Outcome"/> (ACT-002-F : « L'exécution est assurée par un
/// moteur spécialisé »).
///
/// <see cref="Execute"/> ne modifie jamais directement le World --- il ne
/// fait que produire un Outcome ; l'application des Effects et la
/// publication des Events sont des étapes séparées et postérieures
/// (ACT-002-G, section Production ; ENGINE-006, section 5).
/// </summary>
public interface IExecutionEngine
{
    Outcome Execute(ActionInstance instance, World world);
}
