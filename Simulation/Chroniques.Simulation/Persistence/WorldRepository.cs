namespace Chroniques.Simulation.Persistence;

using System.Text.Json;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Sauvegarde et recharge un <see cref="World"/> en JSON.
///
/// Format retenu pour v0.1/v0.2 : JSON, conformément à
/// PROD/FeuilleDeRoute.md. Un format binaire plus compact pourra être
/// introduit plus tard sans changer cette interface --- ce serait alors une
/// décision à tracer par un ADR, pas un changement silencieux.
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
                return new EntitySnapshot(entity.Id.Value, needs);
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
            // Entity.Restore est internal : accessible ici car WorldRepository
            // vit dans le même assembly que Entity, précisément pour que ce
            // mécanisme de reconstruction reste réservé à la persistance et
            // ne devienne jamais une API publique de création d'Entity.
            var entity = Entity.Restore(new EntityId(entitySnapshot.Id));

            if (entitySnapshot.Needs is not null)
            {
                entity.Set(entitySnapshot.Needs);
            }

            world.Reintroduce(entity);
        }

        world.ReplayEvents(snapshot.Events);

        return world;
    }
}
