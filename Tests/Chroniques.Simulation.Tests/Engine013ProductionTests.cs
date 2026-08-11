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

public sealed class Engine013ProductionTests
{
    [Fact]
    public void ProductionOperation_IdVide_EstRefuse()
    {
        Assert.Throws<ArgumentException>(() => new ProductionOperation(
            " ", EntityId.New(), 1, EntityId.New(), 1));
    }

    [Fact]
    public void ProductionOperation_QuantiteEntreeInvalide_EstRefuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProductionOperation(
            "op", EntityId.New(), 0, EntityId.New(), 1));
    }

    [Fact]
    public void ProductionOperation_PortionsSortieInvalides_SontRefusees()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProductionOperation(
            "op", EntityId.New(), 1, EntityId.New(), 0));
    }

    [Fact]
    public void ResourceStockComponent_SurvitSauvegardeEtRechargement()
    {
        var world = new World(42);
        var resource = world.Spawn();
        resource.Set(new ResourceStockComponent { Quantity = 7.5 });

        var restored = WorldRepository.Load(WorldRepository.Save(world));
        var restoredResource = restored.Entities.Single(e => e.Id == resource.Id);

        Assert.True(restoredResource.TryGet<ResourceStockComponent>(out var stock));
        Assert.Equal(7.5d, stock.Quantity);
    }

    [Fact]
    public void ProductionProvenanceComponent_SurvitSauvegardeEtRechargement()
    {
        var world = new World(42);
        var input = world.Spawn();
        var output = world.Spawn();
        output.Set(new ProductionProvenanceComponent
        {
            Traces = new List<ProductionTrace>
            {
                new("op-food", input.Id, new Tick(7))
            }
        });

        var restored = WorldRepository.Load(WorldRepository.Save(world));
        var restoredOutput = restored.Entities.Single(e => e.Id == output.Id);

        Assert.True(restoredOutput.TryGet<ProductionProvenanceComponent>(out var provenance));
        var trace = Assert.Single(provenance.Traces);
        Assert.Equal("op-food", trace.OperationId);
        Assert.Equal(input.Id, trace.InputResourceId);
        Assert.Equal(new Tick(7), trace.ProducedAt);
    }

    [Fact]
    public void ProductiveActivityIntentSource_SansOperation_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var source = new ProductiveActivityIntentSource(new FixedProductionResolver());

        Assert.Null(source.CreateIntent(actor, world, world.CurrentTick));
    }

    [Fact]
    public void ProductiveActivityIntentSource_AvecOperation_ProduitIntent()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, food, operation) = CreateProductionContext(world);
        var source = new ProductiveActivityIntentSource(new FixedProductionResolver(operation));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(ProductiveActivityIntentSource.ProduceFoodObjective, intent!.Objectif);
        Assert.Equal(actor.Id, intent.Acteur);
    }

    [Fact]
    public void CompositeIntentSource_BesoinActionnable_PasseAvantProduction()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 40);
        var (_, _, operation) = CreateProductionContext(world);
        var source = new CompositeAutonomousIntentSource(
            new NeedsIntentSource(60),
            new ProductiveActivityIntentSource(new FixedProductionResolver(operation)));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(NeedsIntentSource.RestObjective, intent!.Objectif);
    }

    [Fact]
    public void CompositeIntentSource_SansEntretienActionnable_UtiliseProduction()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (_, _, operation) = CreateProductionContext(world);
        var source = new CompositeAutonomousIntentSource(
            new NeedsIntentSource(60),
            new ProductiveActivityIntentSource(new FixedProductionResolver(operation)));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(ProductiveActivityIntentSource.ProduceFoodObjective, intent!.Objectif);
    }

    [Fact]
    public void ProductionPlanner_SelectionneSortiePrincipaleEtEntreeSecondaire()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, food, operation) = CreateProductionContext(world);
        var planner = new ProductionPlanner(new FixedProductionResolver(operation));
        var intent = new Intent(actor.Id, ProductiveActivityIntentSource.ProduceFoodObjective, 1);

        var plan = planner.CreatePlan(intent, world);
        var step = Assert.Single(plan.Steps);

        Assert.Same(ProduireDenreeDefinition.Definition, step.Definition);
        Assert.Contains(step.Cibles, c => c.Cible == food.Id && c.Role == RoleCible.Principale);
        Assert.Contains(step.Cibles, c => c.Cible == resource.Id && c.Role == RoleCible.Secondaire);
    }

    [Fact]
    public void ProductionPlanner_SansOperation_RefuseLaPlanification()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var planner = new ProductionPlanner(new FixedProductionResolver());
        var intent = new Intent(actor.Id, ProductiveActivityIntentSource.ProduceFoodObjective, 1);

        Assert.Throws<InvalidOperationException>(() => planner.CreatePlan(intent, world));
    }

    [Fact]
    public void ProductionExecutionEngine_EntreeAbsente_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var food = CreateFood(world, 20, 0);
        var missingInput = EntityId.New();
        var operation = new ProductionOperation("op", missingInput, 1, food.Id, 1);
        var engine = new ProductionExecutionEngine(new FixedProductionResolver(operation));
        var instance = CreateProductionInstance(world, actor.Id, operation);

        Assert.Equal(OutcomeForme.Echec, engine.Execute(instance, world).Forme);
    }

    [Fact]
    public void ProductionExecutionEngine_StockInsuffisant_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var resource = CreateResource(world, 0.5);
        var food = CreateFood(world, 20, 0);
        var operation = new ProductionOperation("op", resource.Id, 1, food.Id, 1);
        var engine = new ProductionExecutionEngine(new FixedProductionResolver(operation));
        var instance = CreateProductionInstance(world, actor.Id, operation);

        Assert.Equal(OutcomeForme.Echec, engine.Execute(instance, world).Forme);
    }

    [Fact]
    public void ProductionExecutionEngine_SortieNonAlimentaire_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var resource = CreateResource(world, 2);
        var invalidOutput = world.Spawn();
        var operation = new ProductionOperation("op", resource.Id, 1, invalidOutput.Id, 1);
        var engine = new ProductionExecutionEngine(new FixedProductionResolver(operation));
        var instance = CreateProductionInstance(world, actor.Id, operation);

        Assert.Equal(OutcomeForme.Echec, engine.Execute(instance, world).Forme);
    }

    [Fact]
    public void ProductionExecutionEngine_Reussite_NeMutePasLeWorld()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, food, operation) = CreateProductionContext(world, resourceQuantity: 2, outputPortions: 2);
        var engine = new ProductionExecutionEngine(new FixedProductionResolver(operation));
        var instance = CreateProductionInstance(world, actor.Id, operation);

        var outcome = engine.Execute(instance, world);

        Assert.Equal(OutcomeForme.Reussite, outcome.Forme);
        Assert.True(resource.TryGet<ResourceStockComponent>(out var stock));
        Assert.Equal(2d, stock.Quantity);
        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(0, product.PortionsDisponibles);
    }

    [Fact]
    public void ProductionEffectApplicator_ConsommeExactementEntreeConfiguree()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, _, operation) = CreateProductionContext(world, resourceQuantity: 3, inputQuantity: 1.25);
        var applicator = new ProductionActionEffectApplicator(new FixedProductionResolver(operation));

        applicator.Apply(CreateProductionInstance(world, actor.Id, operation), world);

        Assert.True(resource.TryGet<ResourceStockComponent>(out var stock));
        Assert.Equal(1.75d, stock.Quantity);
    }

    [Fact]
    public void ProductionEffectApplicator_AjouteExactementPortionsConfigurees()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (_, food, operation) = CreateProductionContext(world, outputPortions: 3);
        var applicator = new ProductionActionEffectApplicator(new FixedProductionResolver(operation));

        applicator.Apply(CreateProductionInstance(world, actor.Id, operation), world);

        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(3, product.PortionsDisponibles);
    }

    [Fact]
    public void ProductionEffectApplicator_AjouteProvenance()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, food, operation) = CreateProductionContext(world);
        var applicator = new ProductionActionEffectApplicator(new FixedProductionResolver(operation));

        applicator.Apply(CreateProductionInstance(world, actor.Id, operation), world);

        Assert.True(food.TryGet<ProductionProvenanceComponent>(out var provenance));
        var trace = Assert.Single(provenance.Traces);
        Assert.Equal(operation.OperationId, trace.OperationId);
        Assert.Equal(resource.Id, trace.InputResourceId);
        Assert.Equal(world.CurrentTick, trace.ProducedAt);
    }

    [Fact]
    public void ProductionEffectApplicator_PublieLesDeuxEvents()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (_, _, operation) = CreateProductionContext(world);
        var applicator = new ProductionActionEffectApplicator(new FixedProductionResolver(operation));

        applicator.Apply(CreateProductionInstance(world, actor.Id, operation), world);

        Assert.Contains(world.Events, e => e.Kind == "production.entree.consommee");
        Assert.Contains(world.Events, e => e.Kind == "production.denree.creee");
    }

    [Fact]
    public void PipelineComposite_ExecuteReposMangerEtProduire()
    {
        var world = new World(42);
        var actor = CreateActor(world, 40, 40);
        var (resource, food, operation) = CreateProductionContext(world, resourceQuantity: 2, outputPortions: 1);
        var foodResolver = new ExplicitFoodResolver(food.Id);
        var productionResolver = new FixedProductionResolver(operation);
        var runner = CreateCompositeRunner(foodResolver, productionResolver);

        var rest = runner.Execute(new Intent(actor.Id, NeedsIntentSource.RestObjective, 1), world);
        var produce = runner.Execute(new Intent(actor.Id, ProductiveActivityIntentSource.ProduceFoodObjective, 1), world);
        var eat = runner.Execute(new Intent(actor.Id, NeedsIntentSource.EatObjective, 1), world);

        Assert.Equal(OutcomeForme.Reussite, rest.Outcome?.Forme);
        Assert.Equal(OutcomeForme.Reussite, produce.Outcome?.Forme);
        Assert.Equal(OutcomeForme.Reussite, eat.Outcome?.Forme);
        Assert.True(resource.TryGet<ResourceStockComponent>(out var stock));
        Assert.Equal(1d, stock.Quantity);
        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(0, product.PortionsDisponibles);
    }

    [Fact]
    public void PipelineComposite_ProductionArchiveActionEtModifieStocks()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (resource, food, operation) = CreateProductionContext(world, resourceQuantity: 2, outputPortions: 2);
        var runner = CreateCompositeRunner(
            new ExplicitFoodResolver(food.Id),
            new FixedProductionResolver(operation));

        var action = runner.Execute(
            new Intent(actor.Id, ProductiveActivityIntentSource.ProduceFoodObjective, 1),
            world);

        Assert.Equal(ActionState.Archived, action.State);
        Assert.Equal(OutcomeForme.Reussite, action.Outcome?.Forme);
        Assert.True(resource.TryGet<ResourceStockComponent>(out var stock));
        Assert.Equal(1d, stock.Quantity);
        Assert.True(food.TryGet<FoodProductComponent>(out var product));
        Assert.Equal(2, product.PortionsDisponibles);
    }

    [Fact]
    public void Scheduler_DeuxTicks_ProduitPuisMangeSansEntreeJoueur()
    {
        var world = new World(42);
        var actor = CreateActor(world, 40, 100);
        var (resource, food, operation) = CreateProductionContext(world, resourceQuantity: 2, outputPortions: 1);
        var foodResolver = new ExplicitFoodResolver(food.Id);
        var productionResolver = new ExecutableProductionResolver(operation);

        var needs = new NeedsIntentSource(60, 60, foodResolver);
        var production = new ProductiveActivityIntentSource(productionResolver);
        var source = new CompositeAutonomousIntentSource(needs, production);
        var runner = CreateCompositeRunner(foodResolver, productionResolver);
        var executor = new PipelineIntentExecutor(runner);
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(actor.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.Equal(ProductiveActivityIntentSource.ProduceFoodObjective, executor.Intents.Single().Objectif);
        Assert.True(food.TryGet<FoodProductComponent>(out var afterProduction));
        Assert.Equal(1, afterProduction.PortionsDisponibles);

        scheduler.Tick(world);

        Assert.Equal(new Tick(2), world.CurrentTick);
        Assert.Equal(2, executor.Intents.Count);
        Assert.Equal(NeedsIntentSource.EatObjective, executor.Intents[1].Objectif);
        Assert.True(actor.TryGet<NeedsComponent>(out var actorNeeds));
        Assert.Equal(65d, actorNeeds.Faim);
        Assert.True(food.TryGet<FoodProductComponent>(out var afterEating));
        Assert.Equal(0, afterEating.PortionsDisponibles);
        Assert.True(resource.TryGet<ResourceStockComponent>(out var remaining));
        Assert.Equal(1d, remaining.Quantity);
        Assert.Contains(world.Events, e => e.Kind == "production.denree.creee");
        Assert.Contains(world.Events, e => e.Kind == "besoin.faim.restauree");
    }

    [Fact]
    public void MemeEtatEtConfiguration_ProduitMemeIntentEtMemesCibles()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var (_, _, operation) = CreateProductionContext(world);
        var resolver = new FixedProductionResolver(operation);
        var source = new ProductiveActivityIntentSource(resolver);
        var planner = new ProductionPlanner(resolver);

        var firstIntent = source.CreateIntent(actor, world, new Tick(5));
        var secondIntent = source.CreateIntent(actor, world, new Tick(5));
        Assert.Equal(firstIntent, secondIntent);

        var firstPlan = planner.CreatePlan(firstIntent!, world);
        var secondPlan = planner.CreatePlan(secondIntent!, world);

        Assert.Equal(
            firstPlan.Steps.Single().Cibles,
            secondPlan.Steps.Single().Cibles);
    }

    private static Entity CreateActor(World world, double faim, double fatigue)
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

    private static Entity CreateResource(World world, double quantity)
    {
        var resource = world.Spawn();
        resource.Set(new ResourceStockComponent { Quantity = quantity });
        return resource;
    }

    private static Entity CreateFood(World world, double faimRestauree, int portions)
    {
        var food = world.Spawn();
        food.Set(new FoodProductComponent
        {
            FaimRestauree = faimRestauree,
            PortionsDisponibles = portions
        });
        return food;
    }

    private static (Entity Resource, Entity Food, ProductionOperation Operation) CreateProductionContext(
        World world,
        double resourceQuantity = 2,
        double inputQuantity = 1,
        int outputPortions = 1)
    {
        var resource = CreateResource(world, resourceQuantity);
        var food = CreateFood(world, 25, 0);
        var operation = new ProductionOperation(
            "produire-denree-test",
            resource.Id,
            inputQuantity,
            food.Id,
            outputPortions);
        return (resource, food, operation);
    }

    private static ActionInstance CreateProductionInstance(
        World world,
        EntityId actorId,
        ProductionOperation operation)
        => new(
            world.CurrentTick,
            ProduireDenreeDefinition.Definition,
            actorId,
            new[]
            {
                new CibleRef(operation.OutputFoodProductId, RoleCible.Principale),
                new CibleRef(operation.InputResourceId, RoleCible.Secondaire)
            });

    private static PipelineRunner CreateCompositeRunner(
        IAccessibleFoodResolver foodResolver,
        IProductiveActivityResolver productionResolver)
    {
        var needsPlanner = new NeedsPlanner(foodResolver);
        var productionPlanner = new ProductionPlanner(productionResolver);
        var planner = new CompositePlanner(new Dictionary<string, IPlanner>
        {
            [NeedsIntentSource.RestObjective] = needsPlanner,
            [NeedsIntentSource.EatObjective] = needsPlanner,
            [ProductiveActivityIntentSource.ProduceFoodObjective] = productionPlanner
        });

        var needsEngine = new NeedsExecutionEngine(foodResolver);
        var productionEngine = new ProductionExecutionEngine(productionResolver);
        var executionEngine = new CompositeExecutionEngine(new Dictionary<string, IExecutionEngine>
        {
            [SeReposerDefinition.Definition.Verbe] = needsEngine,
            [MangerDefinition.Definition.Verbe] = needsEngine,
            [ProduireDenreeDefinition.Definition.Verbe] = productionEngine
        });

        return new PipelineRunner(
            planner,
            executionEngine,
            new RestActionEffectApplicator(),
            new FoodActionEffectApplicator(),
            new ProductionActionEffectApplicator(productionResolver));
    }

    private sealed class ExplicitFoodResolver : IAccessibleFoodResolver
    {
        private readonly IReadOnlyList<EntityId> _foodIds;

        public ExplicitFoodResolver(params EntityId[] foodIds)
        {
            _foodIds = foodIds;
        }

        public EntityId? FindAccessibleFood(Entity actor, World world, Tick currentTick)
        {
            foreach (var id in _foodIds)
            {
                if (world.TryGetEntity(id, out var food)
                    && food.TryGet<FoodProductComponent>(out var product)
                    && product.PortionsDisponibles > 0
                    && product.FaimRestauree > 0)
                {
                    return id;
                }
            }

            return null;
        }

        public bool IsAccessible(Entity actor, EntityId foodId, World world, Tick currentTick)
            => _foodIds.Contains(foodId);
    }

    private sealed class FixedProductionResolver : IProductiveActivityResolver
    {
        private readonly ProductionOperation? _operation;

        public FixedProductionResolver(ProductionOperation? operation = null)
        {
            _operation = operation;
        }

        public ProductionOperation? FindExecutableOperation(Entity actor, World world, Tick currentTick)
            => _operation;
    }

    private sealed class ExecutableProductionResolver : IProductiveActivityResolver
    {
        private readonly ProductionOperation _operation;

        public ExecutableProductionResolver(ProductionOperation operation)
        {
            _operation = operation;
        }

        public ProductionOperation? FindExecutableOperation(Entity actor, World world, Tick currentTick)
        {
            if (!world.TryGetEntity(_operation.InputResourceId, out var input)
                || !input.TryGet<ResourceStockComponent>(out var stock)
                || stock.Quantity < _operation.InputQuantity
                || !world.TryGetEntity(_operation.OutputFoodProductId, out var output)
                || !output.TryGet<FoodProductComponent>(out var food)
                || food.FaimRestauree <= 0)
            {
                return null;
            }

            return _operation;
        }
    }

    private sealed class PipelineIntentExecutor : IAutonomousIntentExecutor
    {
        private readonly PipelineRunner _runner;

        public List<Intent> Intents { get; } = new();

        public PipelineIntentExecutor(PipelineRunner runner)
        {
            _runner = runner;
        }

        public void Execute(Intent intent, World world)
        {
            Intents.Add(intent);
            _ = _runner.Execute(intent, world);
        }
    }
}
