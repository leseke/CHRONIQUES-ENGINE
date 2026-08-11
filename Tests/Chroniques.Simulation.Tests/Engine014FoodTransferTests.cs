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

public sealed class Engine014FoodTransferTests
{
    [Fact]
    public void FoodTransferOpportunity_PortionsNonPositives_SontRefusees()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FoodTransferOpportunity(
            EntityId.New(), EntityId.New(), EntityId.New(), 0));
    }

    [Fact]
    public void FoodProductKindId_SurvitSauvegardeEtRechargement()
    {
        var world = new World(42);
        var food = CreateFood(world, "pain", 25, 2);

        var restored = WorldRepository.Load(WorldRepository.Save(world));
        var restoredFood = restored.Entities.Single(entity => entity.Id == food.Id);

        Assert.True(restoredFood.TryGet<FoodProductComponent>(out var product));
        Assert.Equal("pain", product.ProductKindId);
        Assert.Equal(2, product.PortionsDisponibles);
    }

    [Fact]
    public void VoluntaryFoodTransferIntentSource_SansOpportunite_RetourneNull()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var source = new VoluntaryFoodTransferIntentSource(new FixedTransferResolver());

        Assert.Null(source.CreateIntent(actor, world, world.CurrentTick));
    }

    [Fact]
    public void VoluntaryFoodTransferIntentSource_AvecOpportunite_ProduitIntent()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var source = new VoluntaryFoodTransferIntentSource(
            new FixedTransferResolver(opportunity));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(VoluntaryFoodTransferIntentSource.GiveFoodObjective, intent!.Objectif);
        Assert.Equal(actor.Id, intent.Acteur);
    }

    [Fact]
    public void CompositeIntentSource_EntretienPasseAvantTransfert()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 40);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);

        var source = new CompositeAutonomousIntentSource(
            new NeedsIntentSource(60),
            new VoluntaryFoodTransferIntentSource(new FixedTransferResolver(opportunity)));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(NeedsIntentSource.RestObjective, intent!.Objectif);
    }

    [Fact]
    public void CompositeIntentSource_TransfertPasseAvantProduction()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var resource = CreateResource(world, 2);
        var operation = new ProductionOperation(
            "produire-pain", resource.Id, 1, sourceFood.Id, 1);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);

        var source = new CompositeAutonomousIntentSource(
            new NeedsIntentSource(60),
            new VoluntaryFoodTransferIntentSource(new FixedTransferResolver(opportunity)),
            new ProductiveActivityIntentSource(new FixedProductionResolver(operation)));

        var intent = source.CreateIntent(actor, world, world.CurrentTick);

        Assert.NotNull(intent);
        Assert.Equal(VoluntaryFoodTransferIntentSource.GiveFoodObjective, intent!.Objectif);
    }

    [Fact]
    public void FoodTransferPlanner_MaterialiseDestinationSourceEtDestinataire()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 2);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var planner = new FoodTransferPlanner(new FixedTransferResolver(opportunity));

        var plan = planner.CreatePlan(
            new Intent(actor.Id, VoluntaryFoodTransferIntentSource.GiveFoodObjective, 1),
            world);

        var step = Assert.Single(plan.Steps);
        Assert.Same(DonnerDenreeDefinition.Definition, step.Definition);
        Assert.Contains(step.Cibles, c => c.Cible == destinationFood.Id && c.Role == RoleCible.Principale);
        Assert.Contains(step.Cibles, c => c.Cible == sourceFood.Id && c.Role == RoleCible.Secondaire);
        Assert.Contains(step.Cibles, c => c.Cible == recipient.Id && c.Role == RoleCible.Secondaire);
    }

    [Fact]
    public void FoodTransferPlanner_SansOpportunite_RefuseLaPlanification()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var planner = new FoodTransferPlanner(new FixedTransferResolver());
        var intent = new Intent(actor.Id, VoluntaryFoodTransferIntentSource.GiveFoodObjective, 1);

        Assert.Throws<InvalidOperationException>(() => planner.CreatePlan(intent, world));
    }

    [Fact]
    public void FoodTransferExecutionEngine_DestinataireAbsent_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            EntityId.New(), sourceFood.Id, destinationFood.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_DestinataireIdentiqueAuDonneur_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            actor.Id, sourceFood.Id, destinationFood.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_SourceEtDestinationIdentiques_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var food = CreateFood(world, "pain", 25, 2);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, food.Id, food.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_SourceInsuffisante_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 2);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_IdentiteProduitAbsente_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, null, 25, 1);
        var destinationFood = CreateFood(world, null, 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_IdentitesProduitDifferentes_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "riz", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_ValeursAlimentairesIncompatibles_Echoue()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 30, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        Assert.Equal(
            OutcomeForme.Echec,
            engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world).Forme);
    }

    [Fact]
    public void FoodTransferExecutionEngine_Reussite_NeMutePasLeWorld()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 3);
        var destinationFood = CreateFood(world, "pain", 25, 1);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 2);
        var engine = new FoodTransferExecutionEngine(new FixedTransferResolver(opportunity));

        var outcome = engine.Execute(CreateTransferInstance(world, actor.Id, opportunity), world);

        Assert.Equal(OutcomeForme.Reussite, outcome.Forme);
        Assert.True(sourceFood.TryGet<FoodProductComponent>(out var source));
        Assert.True(destinationFood.TryGet<FoodProductComponent>(out var destination));
        Assert.Equal(3, source.PortionsDisponibles);
        Assert.Equal(1, destination.PortionsDisponibles);
    }

    [Fact]
    public void FoodTransferActionEffectApplicator_TransfereEtConserveExactementLesPortions()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 4);
        var destinationFood = CreateFood(world, "pain", 25, 1);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 2);
        var applicator = new FoodTransferActionEffectApplicator(new FixedTransferResolver(opportunity));

        applicator.Apply(CreateTransferInstance(world, actor.Id, opportunity), world);

        Assert.True(sourceFood.TryGet<FoodProductComponent>(out var source));
        Assert.True(destinationFood.TryGet<FoodProductComponent>(out var destination));
        Assert.Equal(2, source.PortionsDisponibles);
        Assert.Equal(3, destination.PortionsDisponibles);
        Assert.Equal(5, source.PortionsDisponibles + destination.PortionsDisponibles);
    }

    [Fact]
    public void FoodTransferActionEffectApplicator_PublieEventDeTransfert()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 1);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var opportunity = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var applicator = new FoodTransferActionEffectApplicator(new FixedTransferResolver(opportunity));

        applicator.Apply(CreateTransferInstance(world, actor.Id, opportunity), world);

        Assert.Contains(world.Events, e => e.Kind == "produit.alimentaire.transfere");
    }

    [Fact]
    public void PipelineComposite_ContinueDExecuterLesQuatreVerbes()
    {
        var world = new World(42);
        var actor = CreateActor(world, 40, 40);
        var recipient = CreateActor(world, 100, 100);
        var resource = CreateResource(world, 2);
        var sourceFood = CreateFood(world, "pain", 25, 0);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var operation = new ProductionOperation(
            "produire-pain", resource.Id, 1, sourceFood.Id, 1);
        var transfer = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var foodResolver = new ActorFoodResolver(
            (actor.Id, sourceFood.Id),
            (recipient.Id, destinationFood.Id));
        var productionResolver = new FixedProductionResolver(operation);
        var transferResolver = new FixedTransferResolver(transfer);
        var runner = CreateCompositeRunner(foodResolver, transferResolver, productionResolver);

        var rest = runner.Execute(new Intent(actor.Id, NeedsIntentSource.RestObjective, 1), world);
        var produce = runner.Execute(new Intent(actor.Id, ProductiveActivityIntentSource.ProduceFoodObjective, 1), world);
        var give = runner.Execute(new Intent(actor.Id, VoluntaryFoodTransferIntentSource.GiveFoodObjective, 1), world);
        var eat = runner.Execute(new Intent(recipient.Id, NeedsIntentSource.EatObjective, 1), world);

        Assert.Equal(OutcomeForme.Reussite, rest.Outcome?.Forme);
        Assert.Equal(OutcomeForme.Reussite, produce.Outcome?.Forme);
        Assert.Equal(OutcomeForme.Reussite, give.Outcome?.Forme);
        Assert.Equal(OutcomeForme.Reussite, eat.Outcome?.Forme);
    }

    [Fact]
    public void PipelineComposite_DonnerDenree_ArchiveActionEtConserveLeStockTotal()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 3);
        var destinationFood = CreateFood(world, "pain", 25, 1);
        var transfer = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 2);
        var runner = CreateCompositeRunner(
            new ActorFoodResolver(),
            new FixedTransferResolver(transfer),
            new FixedProductionResolver());

        var action = runner.Execute(
            new Intent(actor.Id, VoluntaryFoodTransferIntentSource.GiveFoodObjective, 1),
            world);

        Assert.Equal(ActionState.Archived, action.State);
        Assert.Equal(OutcomeForme.Reussite, action.Outcome?.Forme);
        Assert.True(sourceFood.TryGet<FoodProductComponent>(out var source));
        Assert.True(destinationFood.TryGet<FoodProductComponent>(out var destination));
        Assert.Equal(4, source.PortionsDisponibles + destination.PortionsDisponibles);
    }

    [Fact]
    public void Scheduler_DeuxHabitants_ProduitTransferePuisMangeSansEntreeJoueur()
    {
        var world = new World(42);
        var producer = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 40, 100);
        var resource = CreateResource(world, 2);
        var producerFood = CreateFood(world, "pain", 25, 0);
        var recipientFood = CreateFood(world, "pain", 25, 0);
        var operation = new ProductionOperation(
            "produire-pain", resource.Id, 1, producerFood.Id, 1);
        var transfer = new FoodTransferOpportunity(
            recipient.Id, producerFood.Id, recipientFood.Id, 1);

        var foodResolver = new ActorFoodResolver(
            (recipient.Id, recipientFood.Id));
        var productionResolver = new ActorProductionResolver(producer.Id, operation);
        var transferResolver = new ExecutableTransferResolver(producer.Id, transfer);

        var source = new CompositeAutonomousIntentSource(
            new NeedsIntentSource(60, 60, foodResolver),
            new VoluntaryFoodTransferIntentSource(transferResolver),
            new ProductiveActivityIntentSource(productionResolver));
        var runner = CreateCompositeRunner(foodResolver, transferResolver, productionResolver);
        var executor = new PipelineIntentExecutor(runner);
        var autonomy = new AutonomousActionSystem(source, executor);
        autonomy.RegisterActor(producer.Id);
        autonomy.RegisterActor(recipient.Id);

        var scheduler = new Scheduler();
        scheduler.Register(autonomy);

        scheduler.Tick(world);

        Assert.Equal(new Tick(1), world.CurrentTick);
        Assert.Single(executor.Intents);
        Assert.Equal(ProductiveActivityIntentSource.ProduceFoodObjective, executor.Intents[0].Objectif);
        Assert.True(producerFood.TryGet<FoodProductComponent>(out var afterProduction));
        Assert.Equal(1, afterProduction.PortionsDisponibles);

        scheduler.Tick(world);

        Assert.Equal(new Tick(2), world.CurrentTick);
        Assert.Equal(3, executor.Intents.Count);
        Assert.Equal(VoluntaryFoodTransferIntentSource.GiveFoodObjective, executor.Intents[1].Objectif);
        Assert.Equal(NeedsIntentSource.EatObjective, executor.Intents[2].Objectif);
        Assert.True(producerFood.TryGet<FoodProductComponent>(out var producerStock));
        Assert.True(recipientFood.TryGet<FoodProductComponent>(out var recipientStock));
        Assert.Equal(0, producerStock.PortionsDisponibles);
        Assert.Equal(0, recipientStock.PortionsDisponibles);
        Assert.True(recipient.TryGet<NeedsComponent>(out var needs));
        Assert.Equal(65d, needs.Faim);
        Assert.True(resource.TryGet<ResourceStockComponent>(out var remaining));
        Assert.Equal(1d, remaining.Quantity);
        Assert.Contains(world.Events, e => e.Kind == "production.denree.creee");
        Assert.Contains(world.Events, e => e.Kind == "produit.alimentaire.transfere");
        Assert.Contains(world.Events, e => e.Kind == "besoin.faim.restauree");
    }

    [Fact]
    public void MemeEtatEtOpportunite_ProduitMemeIntentEtMemesCibles()
    {
        var world = new World(42);
        var actor = CreateActor(world, 100, 100);
        var recipient = CreateActor(world, 100, 100);
        var sourceFood = CreateFood(world, "pain", 25, 2);
        var destinationFood = CreateFood(world, "pain", 25, 0);
        var transfer = new FoodTransferOpportunity(
            recipient.Id, sourceFood.Id, destinationFood.Id, 1);
        var resolver = new FixedTransferResolver(transfer);
        var source = new VoluntaryFoodTransferIntentSource(resolver);
        var planner = new FoodTransferPlanner(resolver);

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

    private static Entity CreateFood(
        World world,
        string? productKindId,
        double faimRestauree,
        int portions)
    {
        var food = world.Spawn();
        food.Set(new FoodProductComponent
        {
            ProductKindId = productKindId,
            FaimRestauree = faimRestauree,
            PortionsDisponibles = portions
        });
        return food;
    }

    private static Entity CreateResource(World world, double quantity)
    {
        var resource = world.Spawn();
        resource.Set(new ResourceStockComponent { Quantity = quantity });
        return resource;
    }

    private static ActionInstance CreateTransferInstance(
        World world,
        EntityId actorId,
        FoodTransferOpportunity opportunity)
        => new(
            world.CurrentTick,
            DonnerDenreeDefinition.Definition,
            actorId,
            new[]
            {
                new CibleRef(opportunity.DestinationFoodProductId, RoleCible.Principale),
                new CibleRef(opportunity.SourceFoodProductId, RoleCible.Secondaire),
                new CibleRef(opportunity.RecipientId, RoleCible.Secondaire)
            });

    private static PipelineRunner CreateCompositeRunner(
        IAccessibleFoodResolver foodResolver,
        IVoluntaryFoodTransferResolver transferResolver,
        IProductiveActivityResolver productionResolver)
    {
        var needsPlanner = new NeedsPlanner(foodResolver);
        var transferPlanner = new FoodTransferPlanner(transferResolver);
        var productionPlanner = new ProductionPlanner(productionResolver);
        var planner = new CompositePlanner(new Dictionary<string, IPlanner>
        {
            [NeedsIntentSource.RestObjective] = needsPlanner,
            [NeedsIntentSource.EatObjective] = needsPlanner,
            [VoluntaryFoodTransferIntentSource.GiveFoodObjective] = transferPlanner,
            [ProductiveActivityIntentSource.ProduceFoodObjective] = productionPlanner
        });

        var needsEngine = new NeedsExecutionEngine(foodResolver);
        var transferEngine = new FoodTransferExecutionEngine(transferResolver);
        var productionEngine = new ProductionExecutionEngine(productionResolver);
        var executionEngine = new CompositeExecutionEngine(new Dictionary<string, IExecutionEngine>
        {
            [SeReposerDefinition.Definition.Verbe] = needsEngine,
            [MangerDefinition.Definition.Verbe] = needsEngine,
            [DonnerDenreeDefinition.Definition.Verbe] = transferEngine,
            [ProduireDenreeDefinition.Definition.Verbe] = productionEngine
        });

        return new PipelineRunner(
            planner,
            executionEngine,
            new RestActionEffectApplicator(),
            new FoodActionEffectApplicator(),
            new FoodTransferActionEffectApplicator(transferResolver),
            new ProductionActionEffectApplicator(productionResolver));
    }

    private sealed class FixedTransferResolver : IVoluntaryFoodTransferResolver
    {
        private readonly FoodTransferOpportunity? _opportunity;

        public FixedTransferResolver(FoodTransferOpportunity? opportunity = null)
        {
            _opportunity = opportunity;
        }

        public FoodTransferOpportunity? FindExecutableTransfer(
            Entity actor,
            World world,
            Tick currentTick)
            => _opportunity;
    }

    private sealed class ExecutableTransferResolver : IVoluntaryFoodTransferResolver
    {
        private readonly EntityId _donorId;
        private readonly FoodTransferOpportunity _opportunity;

        public ExecutableTransferResolver(
            EntityId donorId,
            FoodTransferOpportunity opportunity)
        {
            _donorId = donorId;
            _opportunity = opportunity;
        }

        public FoodTransferOpportunity? FindExecutableTransfer(
            Entity actor,
            World world,
            Tick currentTick)
        {
            if (actor.Id != _donorId
                || !world.TryGetEntity(_opportunity.RecipientId, out _)
                || !world.TryGetEntity(_opportunity.SourceFoodProductId, out var sourceEntity)
                || !world.TryGetEntity(_opportunity.DestinationFoodProductId, out var destinationEntity)
                || !sourceEntity.TryGet<FoodProductComponent>(out var source)
                || !destinationEntity.TryGet<FoodProductComponent>(out var destination)
                || source.PortionsDisponibles < _opportunity.Portions
                || string.IsNullOrWhiteSpace(source.ProductKindId)
                || !string.Equals(source.ProductKindId, destination.ProductKindId, StringComparison.Ordinal)
                || source.FaimRestauree != destination.FaimRestauree)
            {
                return null;
            }

            return _opportunity;
        }
    }

    private sealed class ActorFoodResolver : IAccessibleFoodResolver
    {
        private readonly Dictionary<EntityId, IReadOnlyList<EntityId>> _foodsByActor = new();

        public ActorFoodResolver(params (EntityId ActorId, EntityId FoodId)[] mappings)
        {
            foreach (var group in mappings.GroupBy(mapping => mapping.ActorId))
            {
                _foodsByActor[group.Key] = group.Select(mapping => mapping.FoodId).ToArray();
            }
        }

        public EntityId? FindAccessibleFood(Entity actor, World world, Tick currentTick)
        {
            if (!_foodsByActor.TryGetValue(actor.Id, out var foodIds))
            {
                return null;
            }

            foreach (var foodId in foodIds)
            {
                if (world.TryGetEntity(foodId, out var food)
                    && food.TryGet<FoodProductComponent>(out var product)
                    && product.PortionsDisponibles > 0
                    && product.FaimRestauree > 0)
                {
                    return foodId;
                }
            }

            return null;
        }

        public bool IsAccessible(
            Entity actor,
            EntityId foodId,
            World world,
            Tick currentTick)
            => _foodsByActor.TryGetValue(actor.Id, out var foodIds)
                && foodIds.Contains(foodId);
    }

    private sealed class FixedProductionResolver : IProductiveActivityResolver
    {
        private readonly ProductionOperation? _operation;

        public FixedProductionResolver(ProductionOperation? operation = null)
        {
            _operation = operation;
        }

        public ProductionOperation? FindExecutableOperation(
            Entity actor,
            World world,
            Tick currentTick)
            => _operation;
    }

    private sealed class ActorProductionResolver : IProductiveActivityResolver
    {
        private readonly EntityId _producerId;
        private readonly ProductionOperation _operation;

        public ActorProductionResolver(
            EntityId producerId,
            ProductionOperation operation)
        {
            _producerId = producerId;
            _operation = operation;
        }

        public ProductionOperation? FindExecutableOperation(
            Entity actor,
            World world,
            Tick currentTick)
        {
            if (actor.Id != _producerId
                || !world.TryGetEntity(_operation.InputResourceId, out var input)
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
