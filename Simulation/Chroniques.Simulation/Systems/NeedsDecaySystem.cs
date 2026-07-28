namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Fait évoluer <see cref="NeedsComponent"/> à chaque Tick, conformément à
/// GDB-004B : « les besoins évoluent avec le temps ». Toute la logique vit
/// ici, jamais dans le Component lui-même (CORE-003-C).
///
/// Les taux de déclin sont des paramètres du constructeur plutôt que des
/// constantes internes : c'est la première étape vers le principe
/// data-driven de PROD/FeuilleDeRoute.md, sans construire prématurément un
/// pipeline de contenu externe complet qu'aucun autre System ne justifie
/// encore.
/// </summary>
public sealed class NeedsDecaySystem : ISystem
{
    private readonly double _faimDeclinParTick;
    private readonly double _fatigueDeclinParTick;
    private readonly double _moralDeclinEnDetresse;
    private readonly double _seuilDetresse;

    public NeedsDecaySystem(
        double faimDeclinParTick = 1.0,
        double fatigueDeclinParTick = 0.75,
        double moralDeclinEnDetresse = 0.5,
        double seuilDetresse = 20.0)
    {
        _faimDeclinParTick = faimDeclinParTick;
        _fatigueDeclinParTick = fatigueDeclinParTick;
        _moralDeclinEnDetresse = moralDeclinEnDetresse;
        _seuilDetresse = seuilDetresse;
    }

    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.TryGet<NeedsComponent>(out var needs))
            {
                continue;
            }

            needs.Faim = Clamp(needs.Faim - _faimDeclinParTick);
            needs.Fatigue = Clamp(needs.Fatigue - _fatigueDeclinParTick);

            // GDB-004B, « Impact sur le monde » : les besoins s'influencent
            // mutuellement plutôt que d'évoluer chacun isolément.
            if (needs.Faim <= _seuilDetresse || needs.Fatigue <= _seuilDetresse)
            {
                needs.Moral = Clamp(needs.Moral - _moralDeclinEnDetresse);
            }

            // Sante n'est pas affectée par le simple écoulement du temps :
            // seule la maladie ou la blessure la fait varier (GDB-022),
            // pas encore implémentées. Ne pas anticiper (MASTER-006).
        }
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
