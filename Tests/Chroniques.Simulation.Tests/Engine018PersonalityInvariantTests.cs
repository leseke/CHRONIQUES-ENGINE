namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed class Engine018PersonalityInvariantTests
{
    [Fact]
    public void System_ReglesDupliquees_SontRejetees()
    {
        Assert.Throws<ArgumentException>(() => new PersonalityEvolutionSystem(
            new IPersonalityTraitRule[]
            {
                new TestTraitRule("test.alpha"),
                new TestTraitRule("test.alpha"),
            },
            new FixedResolver(5d)));
    }

    [Fact]
    public void System_NomDeRegleVide_EstRejete()
    {
        Assert.Throws<ArgumentException>(() => new PersonalityEvolutionSystem(
            new[] { new TestTraitRule(" ") },
            new FixedResolver(5d)));
    }

    [Fact]
    public void System_ResolverNull_EstRejete()
    {
        Assert.Throws<ArgumentNullException>(() => new PersonalityEvolutionSystem(
            Array.Empty<IPersonalityTraitRule>(),
            null!));
    }

    [Fact]
    public void Formation_NomDifferentDeLaRegle_EstRejete()
    {
        var world = new World(42);
        world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.beta",
                20d,
                50d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, Tick.Zero));
    }

    [Fact]
    public void Formation_ValeurInitialeInvalide_EstRejetee()
    {
        var world = new World(42);
        world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha",
                double.NaN,
                50d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, Tick.Zero));
    }

    [Fact]
    public void Formation_PoidsReferenceInvalide_EstRejete()
    {
        var world = new World(42);
        world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha",
                20d,
                101d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, Tick.Zero));
    }

    [Fact]
    public void Stabilisation_VitesseNulle_EstRejetee()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 20d, 50d);
        var system = CreateSystem(new TestTraitRule("test.alpha"), 0d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Stabilisation_VitesseNonFinie_EstRejetee()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 20d, 50d);
        var system = CreateSystem(new TestTraitRule("test.alpha"), double.PositiveInfinity);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void InflexionLegere_NouveauPoids_EstRejete()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 40d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                10d,
                60d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void InflexionProfonde_SansNouveauPoids_EstRejetee()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 40d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Deep,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void InflexionProfonde_PoidsIdentique_EstRejete()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 40d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Deep,
                10d,
                50d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Inflexion_CauseVide_EstRejetee()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 40d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                " ",
                PersonalityInflexionKind.Light,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Inflexion_DeltaNul_EstRejete()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 40d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                0d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Inflexion_SansDeplacementReelApresClamp_EstRejetee()
    {
        var world = new World(42);
        CreateActorWithTrait(world, 100d, 50d);
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Component_TraitsDupliques_SontRejetes()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero),
                new PersonalityTraitState("test.alpha", 30d, 60d, Tick.Zero),
            },
        });
        var system = CreateSystem(new TestTraitRule("test.alpha"), 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Component_ValeurPersistanteInvalide_EstRejetee()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState("test.alpha", -1d, 50d, Tick.Zero),
            },
        });
        var system = CreateSystem(new TestTraitRule("test.alpha"), 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    [Fact]
    public void Component_TraceLegereQuiChangeReference_EstRejetee()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState("test.alpha", 40d, 50d, Tick.Zero),
            },
            Inflexions =
            {
                new PersonalityInflexionTrace(
                    "test.alpha",
                    "cause-1",
                    PersonalityInflexionKind.Light,
                    10d,
                    50d,
                    60d,
                    Tick.Zero),
            },
        });
        var system = CreateSystem(new TestTraitRule("test.alpha"), 5d);

        Assert.Throws<InvalidOperationException>(() =>
            system.Update(world, new Tick(1)));
    }

    private static PersonalityEvolutionSystem CreateSystem(
        IPersonalityTraitRule rule,
        double step)
        => new(
            new[] { rule },
            new FixedResolver(step));

    private static void CreateActorWithTrait(
        World world,
        double value,
        double reference)
    {
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState(
                    "test.alpha",
                    value,
                    reference,
                    Tick.Zero),
            },
        });
    }

    private sealed class TestTraitRule : IPersonalityTraitRule
    {
        public string TraitName { get; }

        public Func<Entity, World, Tick, PersonalityTraitCreationCandidate?> Creation { get; set; }
            = (_, _, _) => null;

        public Func<PersonalityTraitState, Entity, World, Tick, PersonalityInflexion?> Inflexion { get; set; }
            = (_, _, _, _) => null;

        public TestTraitRule(string traitName)
        {
            TraitName = traitName;
        }

        public PersonalityTraitCreationCandidate? FindCreationCandidate(
            Entity actor,
            World world,
            Tick currentTick)
            => Creation(actor, world, currentTick);

        public PersonalityInflexion? FindInflexion(
            PersonalityTraitState trait,
            Entity actor,
            World world,
            Tick currentTick)
            => Inflexion(trait, actor, world, currentTick);
    }

    private sealed class FixedResolver : IPersonalityStabilizationParameterResolver
    {
        private readonly double _step;

        public FixedResolver(double step)
        {
            _step = step;
        }

        public double ResolveMaxConvergencePerTick(
            string traitName,
            Entity actor,
            World world,
            Tick currentTick)
            => _step;
    }
}
