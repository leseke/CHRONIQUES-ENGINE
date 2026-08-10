namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

// Désambiguïsation explicite entre :
// - Kernel.Relation : primitive CORE-006
// - Components.Relation : relation sociale GDB-004C
using SocialRelation = Chroniques.Simulation.Components.Relation;

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
///     érosion.
///   - Si une interaction négative l'a déjà fait descendre sous le plancher,
///     l'érosion naturelle ne doit jamais la faire remonter artificiellement.
///   - Un effet d'interaction négatif suffisant peut rompre une relation
///     Familiale.
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
    /// Applique l'érosion naturelle de toutes les relations actives.
    ///
    /// Règle particulière des relations Familiales :
    /// - si la Force est au-dessus du plancher, l'érosion peut la faire
    ///   descendre jusqu'au plancher mais jamais en dessous ;
    /// - si la Force est déjà sous le plancher à la suite d'une interaction,
    ///   l'érosion ne la modifie pas et surtout ne la remonte pas.
    /// </summary>
    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGet<RelationComponent>(out var rc))
                continue;

            var aRetirer = new List<SocialRelation>();

            foreach (var relation in rc.Relations)
            {
                if (relation.Type == TypeRelation.Familiale)
                {
                    /*
                     * Le plancher familial est uniquement une protection
                     * contre l'érosion naturelle.
                     *
                     * Exemple :
                     * Force = 15, plancher = 10, érosion = 10
                     * => Force devient 10.
                     *
                     * Exemple :
                     * Force = 5 après une interaction négative,
                     * plancher = 10
                     * => Force reste 5.
                     *
                     * On ne doit jamais faire :
                     * Math.Max(10, 5 - erosion)
                     * car cela remonterait artificiellement la relation.
                     */
                    if (relation.Force > _plancherFamilial)
                    {
                        relation.Force = Math.Max(
                            _plancherFamilial,
                            relation.Force - _erosionParTick);
                    }
                }
                else
                {
                    relation.Force = Math.Max(
                        0.0,
                        relation.Force - _erosionParTick);
                }

                if (relation.Force <= 0)
                {
                    aRetirer.Add(relation);
                }
            }

            foreach (var relation in aRetirer)
            {
                rc.Retirer(relation);
            }
        }
    }

    /// <summary>
    /// Enregistre une interaction qualifiante entre deux habitants.
    /// Crée la relation si elle n'existe pas encore.
    /// Applique l'impact à la Force.
    /// Crée un Épisode si |impact| ≥ seuilImportance.
    /// Supprime la relation si la Force atteint 0.
    ///
    /// Appelé depuis le résolveur d'Effects (ENGINE-008, section 5.1),
    /// jamais depuis Update.
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

        var relation = rc.Relations.FirstOrDefault(
            r => r.Cible == cible && r.Type == type);

        if (relation is null)
        {
            relation = new SocialRelation(
                cible,
                type,
                _forceInitiale,
                tick);

            rc.Ajouter(relation);
        }

        relation.Force = Math.Clamp(
            relation.Force + impact,
            0.0,
            100.0);

        if (Math.Abs(impact) >= _seuilImportanceEpisode)
        {
            var episode = new Episode(
                tick,
                description,
                impact);

            relation.AjouterEpisode(
                episode,
                _capaciteEpisodes);
        }

        // ENGINE-008 v1.3 :
        // le plancher familial protège uniquement contre l'érosion naturelle.
        // Une interaction négative peut rompre totalement la relation.
        if (relation.Force <= 0)
        {
            rc.Retirer(relation);
        }
    }
}
