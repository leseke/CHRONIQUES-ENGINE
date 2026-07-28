namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Fait progresser <see cref="AgeComponent"/> et le <see cref="Lifecycle"/>
/// de chaque Entity à chaque Tick, conformément à GDB-008C (« Les Cycles de
/// Vie ») : enfance → adolescence → âge adulte → maturité → vieillesse →
/// mort. Toute la logique vit ici, jamais dans AgeComponent (CORE-003-C) ni
/// dans Lifecycle, qui doit rester strictement descriptif (CORE-010-C).
///
/// Complète le critère de sortie v0.2 de MASTER-005 : « un personnage naît,
/// vit ses besoins année après année, et meurt. Tout est observable sans
/// aucun rendu. »
///
/// --- Modèle de temps ---
///
/// Le rythme Tick → année simulée (un habitant vieillit d'un an tous les
/// <see cref="CalendrierSimule.MoisParAn"/> Ticks) vient entièrement de
/// <see cref="CalendrierSimule"/>, qui reste l'unique source de vérité pour
/// la conversion Tick → calendrier --- ne pas dupliquer ces constantes ici
/// ni ailleurs.
///
/// Il reste une hypothèse de travail, non encore tranchée par la
/// documentation, à traiter comme provisoire jusqu'à confirmation par un
/// ADR avant la v0.3 : les seuils d'âge (adolescence, âge adulte,
/// maturité, vieillesse) et l'espérance de vie. GDB-008C nomme les étapes
/// de vie sans fixer l'âge auquel on bascule de l'une à l'autre, et aucun
/// document ne fixe d'espérance de vie. Plutôt que de choisir des
/// constantes internes invérifiables, ces seuils sont des paramètres du
/// constructeur (même approche que NeedsDecaySystem pour ses taux de
/// déclin) : des valeurs de travail plausibles, à corriger dès qu'un
/// document GDB les fixera explicitement. Ne pas les recopier ailleurs
/// comme s'il s'agissait de valeurs officielles.
/// </summary>
public sealed class AgingSystem : ISystem
{
    private const string EtatMort = "mort";

    private readonly int _seuilAdolescence;
    private readonly int _seuilAgeAdulte;
    private readonly int _seuilMaturite;
    private readonly int _seuilVieillesse;
    private readonly int _esperanceDeVie;

    public AgingSystem(
        int seuilAdolescence = 12,
        int seuilAgeAdulte = 18,
        int seuilMaturite = 40,
        int seuilVieillesse = 65,
        int esperanceDeVie = 80)
    {
        _seuilAdolescence = seuilAdolescence;
        _seuilAgeAdulte = seuilAgeAdulte;
        _seuilMaturite = seuilMaturite;
        _seuilVieillesse = seuilVieillesse;
        _esperanceDeVie = esperanceDeVie;
    }

    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGet<AgeComponent>(out var age))
            {
                continue;
            }

            // Une Entity déjà morte ne vieillit plus : la mort est un état
            // terminal du Lifecycle (GDB-008C, « fin de vie »), jamais un
            // point de passage parmi d'autres.
            if (entity.Lifecycle.CurrentState.Name == EtatMort)
            {
                continue;
            }

            // Incrémenter l'âge seulement tous les CalendrierSimule.MoisParAn
            // Ticks --- un habitant vieillit d'un an, jamais d'un mois.
            if (currentTick.Value % CalendrierSimule.MoisParAn == 0)
            {
                age.Annees++;
            }

            var etatCible = DeterminerEtape(age.Annees);

            if (etatCible == entity.Lifecycle.CurrentState.Name)
            {
                continue;
            }

            var kind = etatCible == EtatMort ? "vie.mort" : $"vie.etape.{etatCible}";
            var occurredEvent = GameEvent.Create(currentTick, kind, source: entity.Id);

            // Lifecycle.Record se contente d'enregistrer --- c'est bien
            // AgingSystem, pas Lifecycle, qui a décidé du changement d'état
            // (CORE-010-C).
            entity.Lifecycle.Record(occurredEvent, new State(etatCible));
            world.Publish(occurredEvent);
        }
    }

    private string DeterminerEtape(int annees)
    {
        if (annees >= _esperanceDeVie)
        {
            return EtatMort;
        }

        if (annees >= _seuilVieillesse)
        {
            return "vieillesse";
        }

        if (annees >= _seuilMaturite)
        {
            return "maturite";
        }

        if (annees >= _seuilAgeAdulte)
        {
            return "age_adulte";
        }

        if (annees >= _seuilAdolescence)
        {
            return "adolescence";
        }

        return "enfance";
    }
}
