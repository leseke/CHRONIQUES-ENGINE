namespace Chroniques.Simulation.Persistence;

using System.Text.Json;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Sauvegarde et recharge un <see cref="World"/> en JSON.
///
/// Format retenu : JSON. Les Components métier actuellement persistés sont
/// explicitement projetés dans <see cref="EntitySnapshot"/>.
/// </summary>
public static class WorldRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Save(World world)
    {
        var entities = world.Entities
            .Select(entity =>
            {
                entity.TryGet<Components.NeedsComponent>(out var needs);
                entity.TryGet<Components.AgeComponent>(out var age);
                entity.TryGet<Components.FoodProductComponent>(out var foodProduct);

                return new EntitySnapshot(
                    entity.Id.Value,
                    entity.Lifecycle.CreatedAt.Value,
                    entity.Lifecycle.CurrentState.Name,
                    needs,
                    age,
                    foodProduct);
            })
            .ToList();

        var snapshot = new WorldSnapshot(
            world.Seed,
            world.CurrentTick.Value,
            entities,
            world.Events.ToList());

        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static World Load(string json)
    {
        var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, Options)
            ?? throw new InvalidOperationException("Sauvegarde invalide : le JSON ne contient pas de WorldSnapshot exploitable.");

        var world = World.Restore(snapshot.Seed, new Tick(snapshot.CurrentTick));

        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entity = Entity.Restore(
                new EntityId(entitySnapshot.Id),
                new Tick(entitySnapshot.LifecycleCreatedAt),
                entitySnapshot.LifecycleState);

            if (entitySnapshot.Needs is not null)
            {
                entity.Set(entitySnapshot.Needs);
            }

            if (entitySnapshot.Age is not null)
            {
                entity.Set(entitySnapshot.Age);
            }

            if (entitySnapshot.FoodProduct is not null)
            {
                entity.Set(entitySnapshot.FoodProduct);
            }

            world.Reintroduce(entity);
        }

        world.ReplayEvents(snapshot.Events);

        return world;
    }
}
