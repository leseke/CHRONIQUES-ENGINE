namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-004H (Les Compétences) : l'ensemble des compétences
/// d'un habitant.
///
/// Pure donnée, conformément à CORE-003-C : aucune logique n'est exécutée
/// ici. C'est <see cref="Chroniques.Simulation.Systems.SkillSystem"/>
/// qui fait évoluer les niveaux à chaque Tick (déclin par inactivité) et
/// qui enregistre les pratiques qualifiantes (gain de niveau).
/// </summary>
public sealed class SkillComponent : IComponent
{
    private readonly Dictionary<string, Competence> _competences = new();

    public IReadOnlyDictionary<string, Competence> Competences => _competences;

    internal Competence ObtenirOuCreer(string nom, Tick tick)
    {
        if (!_competences.TryGetValue(nom, out var competence))
        {
            competence = new Competence(tick);
            _competences[nom] = competence;
        }

        return competence;
    }

    internal bool TryGet(string nom, out Competence competence) =>
        _competences.TryGetValue(nom, out competence!);
}

/// <summary>
/// Une compétence d'un habitant (GDB-004H, MODÈLE DE PROGRESSION).
/// Le Niveau va de 0 à 100. Une Compétence non encore pratiquée n'existe
/// pas dans <see cref="SkillComponent"/> --- il n'y a pas de Niveau 0 par
/// défaut pour toute compétence imaginable.
///
/// Mutable uniquement par <see cref="Chroniques.Simulation.Systems.SkillSystem"/>.
/// </summary>
public sealed class Competence
{
    public double Niveau { get; internal set; } = 0;
    public Tick DernierePratique { get; internal set; }

    public Competence(Tick tick)
    {
        DernierePratique = tick;
    }
}
