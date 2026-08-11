namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Applique les Effects métier d'une Action déjà résolue avec succès.
///
/// ENGINE-012 introduit cette frontière pour éviter que PipelineRunner
/// accumule des règles propres à chaque Verbe réel.
/// </summary>
public interface IActionEffectApplicator
{
    bool CanApply(ActionInstance instance);

    void Apply(ActionInstance instance, World world);
}
