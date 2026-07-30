using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Interprète un <see cref="Intent"/> et produit un <see cref="Plan"/>
/// (ACT-002-H, section Production : « Un Intent est interprété par un
/// Planner. Le Planner choisit une ou plusieurs Action Definitions. »).
///
/// Un Planner est un service sans état : il ne conserve aucune référence
/// entre deux appels --- c'est le <see cref="Plan"/> qu'il produit qui
/// porte l'état à réévaluer (ACT-002-I, section 7 ; ENGINE-006, section
/// 4.2).
/// </summary>
public interface IPlanner
{
    Plan CreatePlan(Intent intent, World world);
}
