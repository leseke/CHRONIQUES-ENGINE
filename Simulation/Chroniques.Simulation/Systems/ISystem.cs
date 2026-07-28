namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Un System applique une logique aux Entity d'un World à chaque Tick.
///
/// CORE-003-C est explicite : décider, exécuter une logique ou déclencher
/// un Event sont des responsabilités interdites à un Component --- « ces
/// responsabilités appartiennent aux Systems ». C'est pourquoi les Systems
/// vivent dans leur propre dossier, jamais dans Kernel/.
/// </summary>
public interface ISystem
{
    void Update(World world, Tick currentTick);
}
