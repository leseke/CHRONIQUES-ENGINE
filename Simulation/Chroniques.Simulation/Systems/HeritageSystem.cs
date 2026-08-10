namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-004J (La Transmission) : déclenche la transmission au
/// moment où une Entity passe à l'état « mort » (ENGINE-008, section 3).
///
/// Détecte la mort en inspectant directement le <see cref="Lifecycle"/>
/// de chaque Entity --- jamais en lisant <see cref="World.Events"/> comme
/// canal de coordination (ENGINE-001 reste un journal d'observabilité,
/// ENGINE-008 section 7, invariant).
///
/// Doit être enregistré après <see cref="AgingSystem"/> dans le Scheduler
/// (ENGINE-008, section 5) : condition nécessaire pour que le Lifecycle
/// d'une Entity décédée ce même Tick soit déjà à l'état « mort » quand
/// Update s'exécute.
///
/// Invariants (ENGINE-008, section 6 et v1.3) :
///   - Une Entity déjà traitée n'est jamais retraitée (HashSet de garde).
///   - L'un des trois cas d'échec de GDB-004J s'applique systématiquement
///     quand la désignation ou la transmission n'aboutit pas normalement ---
///     jamais de sortie silencieuse.
///   - HeritageSystem ne modifie jamais RelationComponent directement ---
///     il le lit pour désigner l'héritier.
/// </summary>
public sealed class HeritageSystem : ISystem
{
    private const string EtatMort = "mort";

    private readonly HashSet<EntityId> _dejaTrait = new();

    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (entity.Lifecycle.CurrentState.Name != EtatMort)
                continue;

            if (_dejaTrait.Contains(entity.Id))
                continue;

            _dejaTrait.Add(entity.Id);
            TraiterTransmission(world, entity, currentTick);
        }
    }

    private void TraiterTransmission(World world, Entity defunt, Tick tick)
    {
        var heritier = DesignerHeritier(world, defunt);

        if (heritier is null)
        {
            // Cas d'échec 1 : absence de successeur (GDB-004J)
            world.Publish(GameEvent.Create(
                tick,
                "heritage.absence-successeur",
                source: defunt.Id));
            return;
        }

        // En Phase 1 : le refus est déclenché manuellement par le joueur.
        // En Phase 3 : il sera déclenché par un HeritageRefusalEffect
        // produit par un Intent de l'héritier via le pipeline (ENGINE-008 v1.3).
        // Ici, on publie simplement l'événement de transmission réussie.
        world.Publish(GameEvent.Create(
            tick,
            "heritage.transmission",
            source: defunt.Id,
            target: heritier.Id));
    }

    /// <summary>
    /// Désigne l'héritier selon l'algorithme déterministe de GDB-004J :
    ///   1. Priorité aux relations Familiales (Force la plus élevée).
    ///   2. En cas d'égalité de Force : relation créée au Tick le plus bas.
    ///   3. Si aucune relation Familiale : même règle sur toutes les relations.
    ///   4. Si aucune relation : null (absence de successeur).
    /// </summary>
    private static Entity? DesignerHeritier(World world, Entity defunt)
    {
        if (!defunt.TryGet<RelationComponent>(out var rc) || rc.Relations.Count == 0)
            return null;

        var candidates = rc.Relations
            .Where(r => r.Type == TypeRelation.Familiale)
            .ToList();

        if (candidates.Count == 0)
            candidates = rc.Relations.ToList();

        var meilleure = candidates
            .OrderByDescending(r => r.Force)
            .ThenBy(r => r.CreeeAu.Value)
            .First();

        return world.TryGetEntity(meilleure.Cible, out var heritier) ? heritier : null;
    }
}
