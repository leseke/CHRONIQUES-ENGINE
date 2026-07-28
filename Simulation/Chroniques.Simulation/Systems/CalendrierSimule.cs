namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Traduit un Tick en repères de calendrier simulé (saison, année),
/// conformément à GDB-008A : « la conversion vers un temps de jeu habité
/// (jours, saisons, années) relève de la GDB, pas du Kernel » --- c'est
/// pourquoi cette traduction vit dans Systems, jamais dans
/// <see cref="Kernel.Tick"/>, qui reste un simple rang sans signification
/// calendaire.
///
/// Convention retenue (décision d'équipe, à formaliser par un ADR avant la
/// v0.3) : un Tick représente un mois simulé. Trois mois font une saison,
/// quatre saisons une année --- exactement la structure du calendrier réel,
/// pas une invention propre à Chroniques. C'est ce qui permet à
/// <see cref="AgingSystem"/> de faire vieillir un habitant d'un an tous les
/// douze Ticks, et à n'importe quel autre System de savoir quelle saison il
/// traverse (GDB-008A : « le passage du temps influence... les saisons »).
///
/// Les semaines ne sont volontairement pas représentées : à raison d'un
/// Tick par mois, une semaine (~1/4 de mois) tomberait toujours entre deux
/// Ticks --- aucune valeur entière de Ticks ne peut la représenter
/// proprement (12 Ticks / 52 semaines ≈ 0,23, une valeur inexploitable).
/// Introduire une granularité hebdomadaire demanderait de redéfinir Tick
/// lui-même (ex. un Tick par semaine, avec le mois et la saison recalculés
/// à partir de lui), pas d'ajouter une constante arrondie ici --- une
/// décision d'architecture à part entière, à ne prendre que si un System
/// futur en a réellement besoin (MASTER-006 : pas d'anticipation sans motif
/// réel).
/// </summary>
public static class CalendrierSimule
{
    public const int MoisParSaison = 3;
    public const int SaisonsParAn = 4;
    public const int MoisParAn = MoisParSaison * SaisonsParAn;

    private static readonly string[] Saisons = { "printemps", "ete", "automne", "hiver" };

    /// <summary>
    /// Saison en cours à ce Tick. Convention : Tick(1) correspond à la fin
    /// du premier mois simulé (printemps), ..., Tick(12) à la fin du
    /// douzième (hiver) ; Tick(13) entame le printemps de l'année suivante.
    /// Avant tout Tick (<see cref="Tick.Zero"/> ou antérieur), la convention
    /// est le début du printemps de l'année 0 --- un World tout juste créé
    /// n'a encore traversé aucune saison.
    /// </summary>
    public static string SaisonAu(Tick tick)
    {
        if (tick.Value <= 0)
        {
            return Saisons[0];
        }

        var moisDansLAnnee = (tick.Value - 1) % MoisParAn;
        var indexSaison = moisDansLAnnee / MoisParSaison;
        return Saisons[indexSaison];
    }

    /// <summary>
    /// Nombre d'années simulées complètes écoulées à ce Tick. Coïncide
    /// exactement avec le rythme de vieillissement d'<see cref="AgingSystem"/> :
    /// les deux atteignent 1 à Tick(12), 2 à Tick(24), etc.
    /// </summary>
    public static int AnneeAu(Tick tick) => tick.Value <= 0 ? 0 : (int)(tick.Value / MoisParAn);
}
