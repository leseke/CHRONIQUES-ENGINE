namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Résout la vitesse maximale de convergence d'un Trait vers son Poids de
/// référence. Les valeurs concrètes restent configurables et ne sont pas
/// inventées par ENGINE-018.
/// </summary>
public interface IPersonalityStabilizationParameterResolver
{
    double ResolveMaxConvergencePerTick(
        string traitName,
        Entity actor,
        World world,
        Tick currentTick);
}
