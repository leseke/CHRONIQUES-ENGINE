namespace Chroniques.Simulation.Tests;

using System.Linq;
using Xunit;
using Chroniques.Simulation.Actions;
using Chroniques.Simulation.Actions.Exemples;
using Chroniques.Simulation.Autonomy;
using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Persistence;
using Chroniques.Simulation.Systems;

public sealed class Engine012FoodTests
{
    [Fact]
    public void FoodProductComponent_SurvitSauvegardeEtRechargement()
    {
        var world = new World(seed: 42);
        var food = world.Spawn();
        food.Set(new FoodProductComponent
        {
            FaimRestauree = 25,
            PortionsDisponibles = 3
        });

        var json = WorldRepository.Save(world);
        var restored = WorldRepository.Load(json);

        var restoredFood = restored.Entities.Single(e => e.Id == food.Id);
        Assert.True(restoredFood.TryGet<FoodProductComponent>(out var component));
        Assert.Equal(25d, component.FaimRestauree);
        Assert.Equal(3, component.PortionsDisponibles);
    }

    [Fact]
    public void PlanStep_PeutPorterCiblePrincipaleEtSecondaire()
    {
        var actorId = EntityId.New();
        var foodId = EntityId.New();

        var step = new PlanStep(
            MangerDefinition.Definition,
            PlanStepMode.Sequentiel,
            Array.Empty<PlanStep>())
        {
            Cibles = new[]
            {
                new CibleRef(foodId, RoleCible.Principale),
                new CibleRef(actorId, RoleCible.Secondaire)
            }
        };

        Assert.Equal(2, step.Cibles.Count);
        Assert.Contains(step.Cibles, c => c.Cible == foodId && c.Role == RoleCible.Principale);
        Assert.Contains(step.Cibles, c => c.Cible == actorId && c.Role == RoleCible.Secondaire);
    }

    [Fact]
    public void NeedsPlanner_SeReposer_CibleActeurPrincipal()
    {
        var world = new World(seed: 42);
        var actor = world.Spawn();
        var planner = new NeedsPlanner(new ExplicitFoodResolver());
        var intent = new Intent(actor.Id, NeedsIntentSource.RestObjective, 1);

        var plan = planner.CreatePlan(intent, world);

        var step = Assert.Single(plan.Steps);
        Assert.Same(SeReposerDefinition.Definition, step.Definition);
        var target = Assert.Single(step.Cibles);
        Assert.Equal(actor.Id, target.Cible);
        Assert.Equal(RoleCible.Principale, target.Role);
    }

    [Fact]
    public void NeedsPlanner_Manger_CibleNourriturePrincipaleEtActeurSecondaire()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 2);
        var resolver = new ExplicitFoodResolver(food.Id);
        var planner = new NeedsPlanner(resolver);
        var intent = new Intent(actor.Id, NeedsIntentSource.EatObjective, 1);

        var plan = planner.CreatePlan(intent, world);

        var step = Assert.Single(plan.Steps);
        Assert.Same(MangerDefinition.Definition, step.Definition);
        Assert.Contains(step.Cibles, c => c.Cible == food.Id && c.Role == RoleCible.Principale);
        Assert.Contains(step.Cibles, c => c.Cible == actor.Id && c.Role == RoleCible.Secondaire);
    }

    [Fact]
    public void NeedsIntentSource_FaimSousSeuilSansNourriture_NeProduitPasManger()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var source = new NeedsIntentSource(
            fatigueActivationThreshold: 60,
            faimActivationThreshold: 60,
            foodResolver: new ExplicitFoodResolver());

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.Null(intent);
    }

    [Fact]
    public void NeedsIntentSource_FaimSousSeuilAvecNourriture_ProduitManger()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var source = new NeedsIntentSource(
            fatigueActivationThreshold: 60,
            faimActivationThreshold: 60,
            foodResolver: new ExplicitFoodResolver(food.Id));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(NeedsIntentSource.EatObjective, intent!.Objectif);
        Assert.Equal(actor.Id, intent.Acteur);
    }

    [Fact]
    public void NeedsIntentSource_DeuxBesoinsActionnables_FaimPlusBasseChoisitManger()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 30, fatigue: 40);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var source = new NeedsIntentSource(60, 60, new ExplicitFoodResolver(food.Id));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(NeedsIntentSource.EatObjective, intent!.Objectif);
    }

    [Fact]
    public void NeedsIntentSource_DeuxBesoinsActionnables_FatiguePlusBasseChoisitRepos()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 30);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var source = new NeedsIntentSource(60, 60, new ExplicitFoodResolver(food.Id));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(NeedsIntentSource.RestObjective, intent!.Objectif);
    }

    [Fact]
    public void NeedsIntentSource_EgaliteFaimFatigue_ChoisitFaimDeManiereStable()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 40);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var source = new NeedsIntentSource(60, 60, new ExplicitFoodResolver(food.Id));

        var first = source.CreateIntent(actor, world, new Tick(5));
        var second = source.CreateIntent(actor, world, new Tick(5));

        Assert.Equal(first, second);
        Assert.NotNull(first);
        Assert.Equal(NeedsIntentSource.EatObjective, first!.Objectif);
    }

    [Fact]
    public void NeedsExecutionEngine_MangerCibleAbsente_EchoueSansMutation()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var resolver = new ExplicitFoodResolver();
        var engine = new NeedsExecutionEngine(resolver);
        var missingFood = EntityId.New();
        var instance = CreateEatInstance(world, actor.Id, missingFood);

        var outcome = engine.Execute(instance, world);

        Assert.Equal(OutcomeForme.Echec, outcome.Forme);
        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(40d, needs.Faim);
    }

    [Fact]
    public void NeedsExecutionEngine_NourritureEpuisee_Echoue()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 0);
        var resolver = new ExplicitFoodResolver(food.Id);
        var engine = new NeedsExecutionEngine(resolver);
        var instance = CreateEatInstance(world, actor.Id, food.Id);

        var outcome = engine.Execute(instance, world);

        Assert.Equal(OutcomeForme.Echec, outcome.Forme);
    }

    [Fact]
    public void NeedsExecutionEngine_NourritureInaccessible_Echoue()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var engine = new NeedsExecutionEngine(new ExplicitFoodResolver());
        var instance = CreateEatInstance(world, actor.Id, food.Id);

        var outcome = engine.Execute(instance, world);

        Assert.Equal(OutcomeForme.Echec, outcome.Forme);
    }

    [Fact]
    public void PipelineGenerique_SeReposer_ResteCompatible()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 100, fatigue: 50);
        var resolver = new ExplicitFoodResolver();
        var runner = CreateRunner(resolver);
        var intent = new Intent(actor.Id, NeedsIntentSource.RestObjective, 1);

        var action = runner.Execute(intent, world);

        Assert.Equal(ActionState.Archived, action.State);
        Assert.Equal(OutcomeForme.Reussite, action.Outcome?.Forme);
        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(70d, needs.Fatigue);
        Assert.Contains(world.Events, e => e.Kind == "besoin.fatigue.restauree");
    }

    [Fact]
    public void PipelineGenerique_Manger_ConsommePortionRestaureFaimEtPublieEvents()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 2);
        var resolver = new ExplicitFoodResolver(food.Id);
        var runner = CreateRunner(resolver);
        var intent = new Intent(actor.Id, NeedsIntentSource.EatObjective, 1);

        var action = runner.Execute(intent, world);

        Assert.Equal(ActionState.Archived, action.State);
        Assert.Equal(OutcomeForme.Reussite, action.Outcome?.Forme);
        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(65d, needs.Faim);
        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(1, product.PortionsDisponibles);
        Assert.Contains(world.Events, e => e.Kind == "produit.alimentaire.consomme");
        Assert.Contains(world.Events, e => e.Kind == "besoin.faim.restauree");
    }

    [Fact]
    public void PipelineGenerique_Manger_BorneFaimACent()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 90, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var resolver = new ExplicitFoodResolver(food.Id);
        var runner = CreateRunner(resolver);
        var intent = new Intent(actor.Id, NeedsIntentSource.EatObjective, 1);

        _ = runner.Execute(intent, world);

        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(100d, needs.Faim);
    }

    [Fact]
    public void NeedsPlanner_MemesEntrees_SelectionneMemeNourriture()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food1 = CreateFood(world, faimRestauree: 20, portions: 1);
        var food2 = CreateFood(world, faimRestauree: 30, portions: 1);
        var resolver = new ExplicitFoodResolver(food1.Id, food2.Id);
        var planner = new NeedsPlanner(resolver);
        var intent = new Intent(actor.Id, NeedsIntentSource.EatObjective, 1);

        var first = planner.CreatePlan(intent, world);
        var second = planner.CreatePlan(intent, world);

        var firstTarget = first.Steps.Single().Cibles.Single(c => c.Role == RoleCible.Principale).Cible;
        var secondTarget = second.Steps.Single().Cibles.Single(c => c.Role == RoleCible.Principale).Cible;

        Assert.Equal(food1.Id, firstTarget);
        Assert.Equal(firstTarget, secondTarget);
    }

    [Fact]
    public void Scheduler_AutonomieFaim_TraverseIntentPlanActionEtWorld()
    {
        var world = new World(seed: 42);
        var actor = CreateActor(world, faim: 40, fatigue: 100);
        var food = CreateFood(world, faimRestauree: 25, portions: 1);
        var resolver = new ExplicitFoodResolver(food.Id);

        var source = new NeedsIntentSource(
            fatigueActivationThreshold: 60,
            faimActivationThreshold: 60,
            foodResolver: resolver);
        var runner = CreateRunner(resolver);
        var executor = new PipelineIntentExecutor(runner);
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(actor.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.NotNull(executor.LastAction);
        Assert.Equal(ActionState.Archived, executor.LastAction!.State);
        Assert.Equal(OutcomeForme.Reussite, executor.LastAction.Outcome?.Forme);
        Assert.True(actor.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(65d, needs.Faim);
        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(0, product.PortionsDisponibles);
        Assert.Contains(world.Events, e => e.Kind == "produit.alimentaire.consomme");
        Assert.Contains(world.Events, e => e.Kind == "besoin.faim.restauree");
    }

    private static Entity CreateActor(
        World world,
        double faim,
        double fatigue)
    {
        var actor = world.Spawn();
        actor.Set(new NeedsComponent
        {
            Faim = faim,
            Fatigue = fatigue,
            Sante = 100,
            Moral = 100
        });
        return actor;
    }

    private static Entity CreateFood(
        World world,
        double faimRestauree,
        int portions)
    {
        var food = world.Spawn();
        food.Set(new FoodProductComponent
        {
            FaimRestauree = faimRestauree,
            PortionsDisponibles = portions
        });
        return food;
    }

    private static ActionInstance CreateEatInstance(
        World world,
        EntityId actorId,
        EntityId foodId)
        => new(
            world.CurrentTick,
            MangerDefinition.Definition,
            actorId,
            new[]
            {
                new CibleRef(foodId, RoleCible.Principale),
                new CibleRef(actorId, RoleCible.Secondaire)
            });

    private static PipelineRunner CreateRunner(
        IAccessibleFoodResolver resolver)
        => new(
            new NeedsPlanner(resolver),
            new NeedsExecutionEngine(resolver),
            new RestActionEffectApplicator(),
            new FoodActionEffectApplicator());

    private sealed class ExplicitFoodResolver : IAccessibleFoodResolver
    {
        private readonly IReadOnlyList<EntityId> _accessibleFoodIds;

        public ExplicitFoodResolver(params EntityId[] accessibleFoodIds)
        {
            _accessibleFoodIds = accessibleFoodIds;
        }

        public EntityId? FindAccessibleFood(
            Entity actor,
            World world,
            Tick currentTick)
        {
            foreach (var foodId in _accessibleFoodIds)
            {
                if (!world.TryGetEntity(foodId, out var food)
                    || !food.TryGet<FoodProductComponent>(out var product)
                    || product.PortionsDisponibles <= 0
                    || product.FaimRestauree <= 0)
                {
                    continue;
                }

                return foodId;
            }

            return null;
        }

        public bool IsAccessible(
            Entity actor,
            EntityId foodId,
            World world,
            Tick currentTick)
            => _accessibleFoodIds.Contains(foodId);
    }

    private sealed class PipelineIntentExecutor : IAutonomousIntentExecutor
    {
        private readonly PipelineRunner _runner;

        public ActionInstance? LastAction { get; private set; }

        public PipelineIntentExecutor(PipelineRunner runner)
        {
            _runner = runner;
        }

        public void Execute(Intent intent, World world)
        {
            LastAction = _runner.Execute(intent, world);
        }
    }
}
