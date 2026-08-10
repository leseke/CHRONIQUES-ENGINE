namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Fait évoluer <see cref="RelationComponent"/> à chaque Tick, conformément
/// à GDB-004C (Les Relations Sociales). Toute la logique vit ici, jamais
/// dans le Component (CORE-003-C).
///
/// Deux responsabilités distinctes (ENGINE-008, section 3) :
///   1. Érosion naturelle à chaque Tick (Update).
///   2. Enregistrement d'une interaction qualifiante (EnregistrerInteraction),
///      appelé exclusivement depuis le résolveur d'Effects (ENGINE-008,
///      section 5.1) --- jamais depuis Update.
///
/// Invariants (ENGINE-008, section 6) :
///   - La Force reste toujours bornée entre 0 et 100.
///   - Une relation Familiale ne descend pas sous son plancher par la seule
///     érosion. Un effet d'interaction négatif suffisant peut l'y amener ---
///     une relation Familiale peut donc disparaître par acte délibéré, pas
///     par le seul écoulement du temps (GDB-009C, ENGINE-008 v1.3).
///   - Un Épisode n'est créé que si |impact| ≥ seuilImportance.
///   - Au-delà de la capacité, le plus ancien Épisode est retiré en
///     priorité, jamais le plus marquant.
/// </summary>
public sealed class RelationSystem : ISystem
{
    private readonly double _erosionParTick;
    private readonly double _plancherFamilial;
    private readonly double _seuilImportanceEpisode;
    private readonly int _capaciteEpisodes;
    private readonly double _forceInitiale;

    public RelationSystem(
        double erosionParTick = 0.5,
        double plancherFamilial = 10.0,
        double seuilImportanceEpisode = 10.0,
        int capaciteEpisodes = 10,
        double forceInitiale = 40.0)
    {
        _erosionParTick = erosionParTick;
        _plancherFamilial = plancherFamilial;
        _seuilImportanceEpisode = seuilImportanceEpisode;
        _capaciteEpisodes = capaciteEpisodes;
        _forceInitiale = forceInitiale;
    }

    /// <summary>
    /// Érosion naturelle de toutes les relations actives à chaque Tick.
    /// </summary>
    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGet<RelationComponent>(out var rc))
                continue;

            var aRetirer = new List<Relation>();

            foreach (var relation in rc.Relations)
            {
                var plancher = relation.Type == TypeRelation.Familiale
                    ? _plancherFamilial
                    : 0.0;

                relation.Force = Math.Max(plancher, relation.Force - _erosionParTick);

                if (relation.Force <= 0)
                    aRetirer.Add(relation);
            }

            foreach (var r in aRetirer)
                rc.Retirer(r);
        }
    }

    /// <summary>
    /// Enregistre une interaction qualifiante entre deux habitants.
    /// Crée la relation si elle n'existe pas encore. Applique l'impact.
    /// Crée un Épisode si |impact| ≥ seuilImportance.
    /// Supprime la relation si la Force tombe à 0 après interaction négative.
    ///
    /// Appelé exclusivement depuis le résolveur d'Effects (ENGINE-008,
    /// section 5.1) --- jamais depuis Update.
    /// </summary>
    public void EnregistrerInteraction(
        World world,
        Tick tick,
        EntityId source,
        EntityId cible,
        TypeRelation type,
        double impact,
        string description)
    {
        if (!world.TryGetEntity(source, out var entitySource))
            return;

        if (!entitySource.TryGet<RelationComponent>(out var rc))
            return;

        var relation = rc.Relations.FirstOrDefault(r => r.Cible == cible && r.Type == type);

        if (relation is null)
        {
            relation = new Relation(cible, type, _forceInitiale, tick);
            rc.Ajouter(relation);
        }

        relation.Force = Math.Clamp(relation.Force + impact, 0, 100);

        if (Math.Abs(impact) >= _seuilImportanceEpisode)
        {
            var episode = new Episode(tick, description, impact);
            relation.AjouterEpisode(episode, _capaciteEpisodes);
        }

        // Suppression si Force atteint 0 après interaction négative
        // (pas de protection absolue, même pour le type Familial --- ENGINE-008 v1.3)
        if (relation.Force <= 0)
            rc.Retirer(relation);
    }
}
