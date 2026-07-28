namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente le critère de sortie v0.2 de MASTER-005 : « Scheduler et tick
/// temporel ». Fait avancer un <see cref="World"/> d'un Tick et invoque
/// chaque System enregistré, toujours dans l'ordre de leur enregistrement
/// --- condition du déterminisme exigé par le Principe 10 (MASTER-002).
/// </summary>
public sealed class Scheduler
{
    private readonly List<ISystem> _systems = new();

    public IReadOnlyList<ISystem> Systems => _systems;

    public void Register(ISystem system) => _systems.Add(system);

    public void Tick(World world)
    {
        world.Advance();

        foreach (var system in _systems)
        {
            system.Update(world, world.CurrentTick);
        }
    }
}
