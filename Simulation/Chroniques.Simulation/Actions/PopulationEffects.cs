namespace Chroniques.Simulation.Actions;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

// ──────────────────────────────────────────────────────────────────────────────
// Effects typés (ENGINE-008, section 5.1)
//
// Le pipeline d'Actions (ENGINE-006) produit des Effects (données). Il ne
// connaît jamais les Systems concrets d'ENGINE-008. Le résolveur ci-dessous
// dispatche chaque Effect typé vers le System responsable.
//
// Ajouter un nouveau type d'Effect ne modifie pas ENGINE-006.
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Déclenche <see cref="RelationSystem.EnregistrerInteraction"/> pour une
/// paire d'habitants. Produit par le pipeline d'Actions quand une Action
/// affecte une relation (ex. « Se lier d'amitié »).
/// </summary>
public sealed record RelationInteractionEffect(
    EntityId Source,
    EntityId Cible,
    TypeRelation Type,
    double Impact,
    string Description) : IEffect;

/// <summary>
/// Déclenche <see cref="SkillSystem.Pratiquer"/> pour un habitant. Produit
/// par le pipeline d'Actions quand une Action entraîne la pratique d'une
/// compétence (ex. « Travailler »).
/// </summary>
public sealed record SkillPracticeEffect(
    EntityId EntityId,
    string NomCompetence) : IEffect;

/// <summary>
/// Déclenche le refus d'un héritage par un héritier désigné. Produit par le
/// pipeline d'Actions quand l'héritier choisit de refuser (ENGINE-008 v1.3,
/// cas limite verrouillé --- en Phase 1, déclenché manuellement par le
/// joueur si son personnage est l'héritier désigné).
/// </summary>
public sealed record HeritageRefusalEffect(
    EntityId Heritier,
    EntityId Defunt) : IEffect;

/// <summary>
/// Marqueur commun des Effects de population introduits par ENGINE-008.
/// </summary>
public interface IEffect { }

// ──────────────────────────────────────────────────────────────────────────────
// Résolveur (EffectApplicator)
//
// Dispatche les Effects typés vers les Systems responsables.
// Les Systems sont injectés à la construction --- aucun couplage statique.
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Résout les Effects produits par le pipeline d'Actions (ENGINE-006) en
/// mutations concrètes du World, via les Systems appropriés (ENGINE-008,
/// section 5.1).
///
/// Garantit qu'ENGINE-006 ne connaît aucun System concret d'ENGINE-008 :
/// il produit des Effects (données), ce résolveur sait qui les traite.
/// </summary>
public sealed class PopulationEffectApplicator
{
    private readonly RelationSystem _relationSystem;
    private readonly SkillSystem _skillSystem;

    public PopulationEffectApplicator(
        RelationSystem relationSystem,
        SkillSystem skillSystem)
    {
        _relationSystem = relationSystem;
        _skillSystem = skillSystem;
    }

    public void Appliquer(IEffect effect, World world, Tick tick)
    {
        switch (effect)
        {
            case RelationInteractionEffect e:
                _relationSystem.EnregistrerInteraction(
                    world, tick,
                    e.Source, e.Cible, e.Type,
                    e.Impact, e.Description);
                break;

            case SkillPracticeEffect e:
                _skillSystem.Pratiquer(world, tick, e.EntityId, e.NomCompetence);
                break;

            case HeritageRefusalEffect e:
                // Le refus produit le même chemin que l'absence de successeur
                // pour la part refusée (GDB-004J, CAS D'ÉCHEC, Refus du successeur).
                world.Publish(GameEvent.Create(
                    tick,
                    "heritage.refus",
                    source: e.Heritier,
                    target: e.Defunt));
                break;
        }
    }
}
