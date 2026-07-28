using System.Text.Json;

namespace Chroniques.Simulation.Kernel.Persistence;

/// <summary>
/// Sauvegarde et recharge un <see cref="World"/> en JSON.
///
/// Format de sauvegarde retenu pour v0.1 : JSON, conformément à
/// PROD/FeuilleDeRoute.md (« Sérialisation JSON »). Un format binaire plus
/// compact pourra être introduit plus tard sans changer cette interface ---
/// ce serait alors une décision à tracer par un ADR, pas un changement
/// silencieux.
/// </summary>
public static class WorldRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Save(World world)
    {
        var snapshot = new WorldSnapshot(
            world.Seed,
            world.CurrentTick.Value,
            world.Entities.Select(e => e.Id.Value).ToList(),
            world.Events.ToList());

        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static World Load(string json)
    {
        var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, Options)
            ?? throw new InvalidOperationException("Sauvegarde invalide : le JSON ne contient pas de WorldSnapshot exploitable.");

        var world = World.Restore(snapshot.Seed, new Tick(snapshot.CurrentTick));

        foreach (var entityId in snapshot.EntityIds)
        {
            // Entity.Restore est internal : accessible ici car WorldRepository
            // vit dans le même assembly que Entity, précisément pour que ce
            // mécanisme de reconstruction reste réservé à la persistance et
            // ne devienne jamais une API publique de création d'Entity.
            world.Reintroduce(Entity.Restore(new EntityId(entityId)));
        }

        world.ReplayEvents(snapshot.Events);

        return world;
    }
}
