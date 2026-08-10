namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Fait évoluer <see cref="SkillComponent"/> à chaque Tick, conformément
/// à GDB-004H (Les Compétences). Toute la logique vit ici, jamais dans
/// le Component (CORE-003-C).
///
/// Deux responsabilités distinctes (ENGINE-008, section 3) :
///   1. Déclin par inactivité à chaque Tick (Update).
///   2. Enregistrement d'une pratique qualifiante (Pratiquer), appelé
///      exclusivement depuis le résolveur d'Effects (ENGINE-008, section
///      5.1) --- jamais depuis Update.
///
/// Invariants (ENGINE-008, section 6 et v1.3) :
///   - Le Niveau reste toujours borné entre 0 et 100.
///   - Le gain est strictement décroissant avec le Niveau : à Niveau 0,
///     gain maximal ; à Niveau 100, gain nul. La forme retenue est
///     proportionnelle à (1 - Niveau/100), ce qui donne une courbe
///     linéairement décroissante, compatible avec le comportement
///     qualitatif fixé par ENGINE-008 v1.3 (chaque point supplémentaire
///     rend la progression marginalement plus difficile).
///   - Le déclin ne s'applique qu'après le seuil d'inactivité.
/// </summary>
public sealed class SkillSystem : ISystem
{
    private readonly double _facteurGain;
    private readonly int _seuilInactiviteTicks;
    private readonly double _declinParTickInactif;

    public SkillSystem(
        double facteurGain = 5.0,
        int seuilInactiviteTicks = 30,
        double declinParTickInactif = 0.2)
    {
        _facteurGain = facteurGain;
        _seuilInactiviteTicks = seuilInactiviteTicks;
        _declinParTickInactif = declinParTickInactif;
    }

    /// <summary>
    /// Déclin par inactivité de toutes les compétences non pratiquées
    /// au-delà du seuil.
    /// </summary>
    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGet<SkillComponent>(out var sc))
                continue;

            foreach (var competence in sc.Competences.Values)
            {
                var ticksInactif = currentTick.Value - competence.DernierePratique.Value;

                if (ticksInactif > _seuilInactiviteTicks)
                {
                    competence.Niveau = Math.Max(0, competence.Niveau - _declinParTickInactif);
                }
            }
        }
    }

    /// <summary>
    /// Enregistre une pratique qualifiante : gain de Niveau décroissant
    /// avec le Niveau actuel (ENGINE-008 v1.3, cas limite verrouillé).
    ///
    /// Appelé exclusivement depuis le résolveur d'Effects (ENGINE-008,
    /// section 5.1) --- jamais depuis Update.
    /// </summary>
    public void Pratiquer(World world, Tick tick, EntityId entityId, string nomCompetence)
    {
        if (!world.TryGetEntity(entityId, out var entity))
            return;

        if (!entity.TryGet<SkillComponent>(out var sc))
            return;

        var competence = sc.ObtenirOuCreer(nomCompetence, tick);

        // Gain linéairement décroissant : maximal à Niveau 0, nul à Niveau 100.
        // Comportement qualitatif fixé par ENGINE-008 v1.3, forme exacte
        // laissée libre (paramètre d'implémentation).
        var gain = _facteurGain * (1.0 - competence.Niveau / 100.0);
        competence.Niveau = Math.Clamp(competence.Niveau + gain, 0, 100);
        competence.DernierePratique = tick;
    }
}
