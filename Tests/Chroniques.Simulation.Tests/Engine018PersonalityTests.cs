namespace Chroniques.Simulation.Tests;

using Xunit;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Chroniques.Simulation.Systems;

public sealed class Engine018PersonalityTests
{
    [Fact]
    public void Persistence_PreserveTraitsEtInflexions()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState("test.alpha", 65d, 70d, new Tick(3)),
            },
            Inflexions =
            {
                new PersonalityInflexionTrace(
                    "test.alpha",
                    "cause-1",
                    PersonalityInflexionKind.Deep,
                    15d,
                    50d,
                    70d,
                    new Tick(5)),
            },
        });

        var json = WorldRepository.Save(world);
        var loaded = WorldRepository.Load(json);

        Assert.True(loaded.TryGetEntity(actor.Id, out var restored));
        Assert.True(restored.TryGet<PersonalityComponent>(out var personality));

        var trait = Assert.Single(personality.Traits);
        Assert.Equal("test.alpha", trait.Name);
        Assert.Equal(65d, trait.Value);
        Assert.Equal(70d, trait.ReferenceWeight);
        Assert.Equal(new Tick(3), trait.CreatedAt);

        var trace = Assert.Single(personality.Inflexions);
        Assert.Equal("cause-1", trace.CauseId);
        Assert.Equal(PersonalityInflexionKind.Deep, trace.Kind);
        Assert.Equal(15d, trace.ValueDelta);
        Assert.Equal(new Tick(5), trace.AppliedAt);
    }

    [Fact]
    public void Persistence_SansPersonalityComponent_OmetLeChamp()
    {
        var world = new World(42);
        world.Spawn();

        var json = WorldRepository.Save(world);

        Assert.DoesNotContain("\"Personality\":", json);
    }

    [Fact]
    public void Evolution_SansRegle_NeCreePasDeComponent()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var system = new PersonalityEvolutionSystem(
            Array.Empty<IPersonalityTraitRule>(),
            new FixedStabilizationResolver(5d));

        system.Update(world, Tick.Zero);

        Assert.False(actor.Has<PersonalityComponent>());
    }

    [Fact]
    public void Formation_CandidateValide_CreeTraitExact()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha",
                20d,
                80d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(4));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        var trait = Assert.Single(personality.Traits);
        Assert.Equal("test.alpha", trait.Name);
        Assert.Equal(20d, trait.Value);
        Assert.Equal(80d, trait.ReferenceWeight);
        Assert.Equal(new Tick(4), trait.CreatedAt);
    }

    [Fact]
    public void Formation_NeStabilisePasAuTickDeCreation()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha",
                20d,
                80d),
        };
        var system = CreateSystem(rule, 10d);

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(20d, Assert.Single(personality.Traits).Value);
    }

    [Fact]
    public void Formation_CandidateRepetee_NeDupliquePasTrait()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha",
                20d,
                80d),
        };
        var system = CreateSystem(rule, 10d);

        system.Update(world, Tick.Zero);
        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Single(personality.Traits);
    }

    [Fact]
    public void Evolution_TraitSansRegle_ResteInchange()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.absent", 20d, 80d, Tick.Zero));
        var system = new PersonalityEvolutionSystem(
            Array.Empty<IPersonalityTraitRule>(),
            new FixedStabilizationResolver(10d));

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        var trait = Assert.Single(personality.Traits);
        Assert.Equal(20d, trait.Value);
        Assert.Equal(80d, trait.ReferenceWeight);
    }

    [Fact]
    public void Stabilisation_ConvergeVersLeHaut()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero));
        var system = CreateSystem(new TestTraitRule("test.alpha"), 7d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(27d, Assert.Single(personality.Traits).Value);
    }

    [Fact]
    public void Stabilisation_ConvergeVersLeBas()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 80d, 50d, Tick.Zero));
        var system = CreateSystem(new TestTraitRule("test.alpha"), 7d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(73d, Assert.Single(personality.Traits).Value);
    }

    [Fact]
    public void Stabilisation_NeDepasseJamaisLaReference()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 48d, 50d, Tick.Zero));
        var system = CreateSystem(new TestTraitRule("test.alpha"), 10d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(50d, Assert.Single(personality.Traits).Value);
    }

    [Fact]
    public void InflexionLegere_DeplaceValeurSansChangerReference()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 40d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-light",
                PersonalityInflexionKind.Light,
                15d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        var trait = Assert.Single(personality.Traits);
        Assert.Equal(55d, trait.Value);
        Assert.Equal(50d, trait.ReferenceWeight);

        var trace = Assert.Single(personality.Inflexions);
        Assert.Equal("cause-light", trace.CauseId);
        Assert.Equal(PersonalityInflexionKind.Light, trace.Kind);
        Assert.Equal(50d, trace.PreviousReferenceWeight);
        Assert.Equal(50d, trace.NewReferenceWeight);
    }

    [Fact]
    public void InflexionProfonde_DeplaceValeurEtReference()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 40d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, tick) => tick.Value == 1
                ? new PersonalityInflexion(
                    "cause-deep",
                    PersonalityInflexionKind.Deep,
                    20d,
                    80d)
                : null,
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        var trait = Assert.Single(personality.Traits);
        Assert.Equal(65d, trait.Value);
        Assert.Equal(80d, trait.ReferenceWeight);

        var trace = Assert.Single(personality.Inflexions);
        Assert.Equal(50d, trace.PreviousReferenceWeight);
        Assert.Equal(80d, trace.NewReferenceWeight);
    }

    [Fact]
    public void Inflexion_ClampValeurEtTraceLeDeplacementReel()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 95d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-clamp",
                PersonalityInflexionKind.Light,
                20d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(100d, Assert.Single(personality.Traits).Value);
        Assert.Equal(5d, Assert.Single(personality.Inflexions).ValueDelta);
    }

    [Fact]
    public void Inflexion_MemeCause_NEstPasReappliqueeEtLaStabilisationReprend()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 30d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-unique",
                PersonalityInflexionKind.Light,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(45d, Assert.Single(personality.Traits).Value);
        Assert.Single(personality.Inflexions);
    }

    [Fact]
    public void Inflexion_CausesDistinctes_PeuventSEnchainer()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 30d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, tick) => new PersonalityInflexion(
                $"cause-{tick.Value}",
                PersonalityInflexionKind.Light,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));
        system.Update(world, new Tick(2));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(50d, Assert.Single(personality.Traits).Value);
        Assert.Equal(2, personality.Inflexions.Count);
    }

    [Fact]
    public void TraitsOpposes_RestentDeuxTraitsDistincts()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var system = new PersonalityEvolutionSystem(
            new IPersonalityTraitRule[]
            {
                new TestTraitRule("test.alpha")
                {
                    Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                        "test.alpha", 25d, 40d),
                },
                new TestTraitRule("test.beta")
                {
                    Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                        "test.beta", 75d, 60d),
                },
            },
            new FixedStabilizationResolver(5d));

        system.Update(world, Tick.Zero);

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(2, personality.Traits.Count);
        Assert.Contains(personality.Traits, trait => trait.Name == "test.alpha");
        Assert.Contains(personality.Traits, trait => trait.Name == "test.beta");
    }

    [Fact]
    public void PlusieursTraits_EvoluentIndependamment()
    {
        var world = new World(42);
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits =
            {
                new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero),
                new PersonalityTraitState("test.beta", 80d, 60d, Tick.Zero),
            },
        });
        var system = new PersonalityEvolutionSystem(
            new IPersonalityTraitRule[]
            {
                new TestTraitRule("test.alpha"),
                new TestTraitRule("test.beta"),
            },
            new FixedStabilizationResolver(5d));

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<PersonalityComponent>(out var personality));
        Assert.Equal(25d, personality.Traits.Single(x => x.Name == "test.alpha").Value);
        Assert.Equal(75d, personality.Traits.Single(x => x.Name == "test.beta").Value);
    }

    [Fact]
    public void Scheduler_AnimeFormationPuisStabilisation()
    {
        var world = new World(42);
        var actor = world.Spawn();
        var rule = new TestTraitRule("test.alpha")
        {
            Creation = (_, _, _) => new PersonalityTraitCreationCandidate(
                "test.alpha", 20d, 80d),
        };
        var system = CreateSystem(rule, 5d);
        var scheduler = new Scheduler();
        scheduler.Register(system);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.True(actor.TryGet<PersonalityComponent>(out var created));
        Assert.Equal(20d, Assert.Single(created.Traits).Value);
        Assert.Equal(new Tick(1), Assert.Single(created.Traits).CreatedAt);

        scheduler.Tick(world);

        Assert.Equal(new Tick(2), world.CurrentTick);
        Assert.True(actor.TryGet<PersonalityComponent>(out var evolved));
        Assert.Equal(25d, Assert.Single(evolved.Traits).Value);
    }

    [Fact]
    public void EvolutionSystem_NAvanceJamaisLeTick()
    {
        var world = new World(42);
        CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero));
        var system = CreateSystem(new TestTraitRule("test.alpha"), 5d);

        system.Update(world, new Tick(12));

        Assert.Equal(Tick.Zero, world.CurrentTick);
    }

    [Fact]
    public void EvolutionSystem_NePubliePasDEventImplicitement()
    {
        var world = new World(42);
        CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero));
        var rule = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                10d),
        };
        var system = CreateSystem(rule, 5d);

        system.Update(world, new Tick(1));

        Assert.Empty(world.Events);
    }

    [Fact]
    public void Personality_NeModifiePasHabitudesOuAmbitionsImplicitement()
    {
        var world = new World(42);
        var actor = CreateActorWithTrait(
            world,
            new PersonalityTraitState("test.alpha", 20d, 50d, Tick.Zero));
        actor.Set(new HabitComponent
        {
            Habits =
            {
                new HabitState("test.habit", "agir", "ctx", 40d, null, Tick.Zero),
            },
        });
        actor.Set(new AmbitionComponent
        {
            Ambitions =
            {
                new AmbitionState(
                    "test.ambition",
                    "a-1",
                    "payload",
                    "agir",
                    60d,
                    25d,
                    false,
                    Tick.Zero),
            },
        });
        var system = CreateSystem(new TestTraitRule("test.alpha"), 5d);

        system.Update(world, new Tick(1));

        Assert.True(actor.TryGet<HabitComponent>(out var habits));
        Assert.Equal(40d, Assert.Single(habits.Habits).Force);
        Assert.True(actor.TryGet<AmbitionComponent>(out var ambitions));
        Assert.Equal(60d, Assert.Single(ambitions.Ambitions).Intensity);
    }

    [Fact]
    public void MemeEtatEtRegles_ProduisentMemeEvolution()
    {
        var worldA = new World(42);
        var worldB = new World(42);
        var actorA = CreateActorWithTrait(
            worldA,
            new PersonalityTraitState("test.alpha", 30d, 50d, Tick.Zero));
        var actorB = CreateActorWithTrait(
            worldB,
            new PersonalityTraitState("test.alpha", 30d, 50d, Tick.Zero));

        var ruleA = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                10d),
        };
        var ruleB = new TestTraitRule("test.alpha")
        {
            Inflexion = (_, _, _, _) => new PersonalityInflexion(
                "cause-1",
                PersonalityInflexionKind.Light,
                10d),
        };

        CreateSystem(ruleA, 5d).Update(worldA, new Tick(1));
        CreateSystem(ruleB, 5d).Update(worldB, new Tick(1));

        Assert.True(actorA.TryGet<PersonalityComponent>(out var personalityA));
        Assert.True(actorB.TryGet<PersonalityComponent>(out var personalityB));
        Assert.Equal(personalityA.Traits, personalityB.Traits);
        Assert.Equal(personalityA.Inflexions, personalityB.Inflexions);
    }

    private static PersonalityEvolutionSystem CreateSystem(
        IPersonalityTraitRule rule,
        double convergenceStep)
        => new(
            new[] { rule },
            new FixedStabilizationResolver(convergenceStep));

    private static Entity CreateActorWithTrait(
        World world,
        PersonalityTraitState trait)
    {
        var actor = world.Spawn();
        actor.Set(new PersonalityComponent
        {
            Traits = { trait },
        });
        return actor;
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

    private sealed class FixedStabilizationResolver : IPersonalityStabilizationParameterResolver
    {
        private readonly double _step;

        public FixedStabilizationResolver(double step)
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
