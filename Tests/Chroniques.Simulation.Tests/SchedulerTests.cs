using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie le critère de sortie v0.2 de MASTER-005 : le Scheduler fait
/// avancer le Tick du World et invoque les Systems enregistrés, dans
/// l'ordre --- condition du déterminisme (Principe 10, MASTER-002).
/// </summary>
public class SchedulerTests
{
    [Fact]
    public void Tick_fait_avancer_le_world_dun_cran()
    {
        var world = new World(seed: 1);
        var scheduler = new Scheduler();

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
    }

    [Fact]
    public void Tick_invoque_chaque_system_enregistre()
    {
        var world = new World(seed: 1);
        var habitant = world.Spawn();
        habitant.Set(new NeedsComponent());
        var scheduler = new Scheduler();
        scheduler.Register(new NeedsDecaySystem(faimDeclinParTick: 10));

        scheduler.Tick(world);

        habitant.TryGet<NeedsComponent>(out var needs);
        Assert.Equal(90, needs.Faim);
    }

    [Fact]
    public void Les_systems_sinvoquent_toujours_dans_lordre_denregistrement()
    {
        var world = new World(seed: 1);
        var scheduler = new Scheduler();
        var ordreExecution = new List<int>();

        scheduler.Register(new OrdreTraceur(1, ordreExecution));
        scheduler.Register(new OrdreTraceur(2, ordreExecution));
        scheduler.Register(new OrdreTraceur(3, ordreExecution));

        scheduler.Tick(world);

        Assert.Equal(new[] { 1, 2, 3 }, ordreExecution);
    }

    private sealed class OrdreTraceur : ISystem
    {
        private readonly int _identifiant;
        private readonly List<int> _journal;

        public OrdreTraceur(int identifiant, List<int> journal)
        {
            _identifiant = identifiant;
            _journal = journal;
        }

        public void Update(World world, Tick currentTick) => _journal.Add(_identifiant);
    }
}
