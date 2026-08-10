namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

// ─────────────────────────────────────────────────────────────────────────────
// Effects typés — ENGINE-008, section 5.1
//
// Le pipeline d'Actions produit des Effects (données).
//
// Il ne connaît jamais :
// - RelationSystem ;
// - SkillSystem ;
// - HeritageSystem.
//
// PopulationEffectApplicator assure uniquement le dispatch.
// La logique métier reste exclusivement dans le System responsable.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Effect représentant une interaction sociale entre deux habitants.
/// </summary>
public sealed record RelationInteractionEffect(
    EntityId Source,
    EntityId Cible,
    TypeRelation Type,
    double Impact,
    string Description) : IEffect;

/// <summary>
/// Effect représentant la pratique d'une compétence.
/// </summary>
public sealed record SkillPracticeEffect(
    EntityId EntityId,
    string NomCompetence) : IEffect;

/// <summary>
/// Effect représentant le refus d'un héritage par un héritier.
///
/// Le résolveur ne traite pas lui-même le refus.
/// Il transmet cet Effect à HeritageSystem, qui constitue l'unique
/// source de vérité pour la logique d'héritage.
/// </summary>
public sealed record HeritageRefusalEffect(
    EntityId Heritier,
    EntityId Defunt) : IEffect;

/// <summary>
/// Contrat commun des Effects de population introduits par ENGINE-008.
/// </summary>
public interface IEffect
{
}

// ─────────────────────────────────────────────────────────────────────────────
// PopulationEffectApplicator
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Résout les Effects de population produits par le pipeline d'Actions.
///
/// Responsabilité unique :
///
///     Effect
///       ↓
///     identifier le System responsable
///       ↓
///     lui transmettre les données
///
/// Il ne contient aucune logique métier propre aux relations,
/// compétences ou héritages.
/// </summary>
public sealed class PopulationEffectApplicator
{
    private readonly RelationSystem _relationSystem;
    private readonly SkillSystem _skillSystem;
    private readonly HeritageSystem _heritageSystem;

    /// <summary>
    /// Constructeur complet recommandé.
    ///
    /// Les trois Systems sont explicitement injectés.
    /// </summary>
    public PopulationEffectApplicator(
        RelationSystem relationSystem,
        SkillSystem skillSystem,
        HeritageSystem heritageSystem)
    {
        ArgumentNullException.ThrowIfNull(relationSystem);
        ArgumentNullException.ThrowIfNull(skillSystem);
        ArgumentNullException.ThrowIfNull(heritageSystem);

        _relationSystem = relationSystem;
        _skillSystem = skillSystem;
        _heritageSystem = heritageSystem;
    }

    /// <summary>
    /// Constructeur de compatibilité avec les appels existants.
    ///
    /// Il permet aux tests et usages ne manipulant pas encore l'héritage
    /// de continuer à construire l'applicator avec RelationSystem et
    /// SkillSystem uniquement.
    ///
    /// Les nouveaux usages impliquant l'héritage doivent préférer
    /// le constructeur à trois arguments.
    /// </summary>
    public PopulationEffectApplicator(
        RelationSystem relationSystem,
        SkillSystem skillSystem)
        : this(
            relationSystem,
            skillSystem,
            new HeritageSystem())
    {
    }

    /// <summary>
    /// Applique un Effect en le dispatchant vers le System responsable.
    /// </summary>
    public void Appliquer(
        IEffect effect,
        World world,
        Tick tick)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(world);

        switch (effect)
        {
            case RelationInteractionEffect relationEffect:

                _relationSystem.EnregistrerInteraction(
                    world,
                    tick,
                    relationEffect.Source,
                    relationEffect.Cible,
                    relationEffect.Type,
                    relationEffect.Impact,
                    relationEffect.Description);

                break;

            case SkillPracticeEffect skillEffect:

                _skillSystem.Pratiquer(
                    world,
                    tick,
                    skillEffect.EntityId,
                    skillEffect.NomCompetence);

                break;

            case HeritageRefusalEffect heritageEffect:

                _heritageSystem.RefuserHeritage(
                    world,
                    tick,
                    heritageEffect.Heritier,
                    heritageEffect.Defunt);

                break;
        }
    }
}
