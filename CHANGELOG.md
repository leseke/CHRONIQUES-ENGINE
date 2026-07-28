# Changelog --- Chroniques (moteur)

Ce fichier retrace l'historique des versions du **code**. Les décisions
d'architecture qui justifient ces versions vivent dans le registre ADR du
dépôt de documentation (`chroniques--main/ADR/`), pas ici.

Format : [Semantic Versioning](https://semver.org/lang/fr/). Les versions
suivent les jalons de `MASTER-005` (v0.1 « Le noyau », v0.2 « Une vie », etc.),
et non un calendrier.

---

## [Non publié]

### Ajouté --- v0.2 (en cours)

- Premier Component métier : `Components/NeedsComponent.cs` (GDB-004B ---
  faim, fatigue, santé, moral).
- `Systems/` : `ISystem`, `Scheduler` (Tick + invocation ordonnée),
  `NeedsDecaySystem` (déclin des besoins, GDB-004B section Évolution).
- `Persistence/` déplacé hors de `Kernel/` (la persistance connaît
  désormais les Components métier, ce n'est plus une responsabilité pure
  du Kernel) et étendu pour sérialiser `NeedsComponent`.
- Tests : `NeedsComponentTests`, `NeedsDecaySystemTests`, `SchedulerTests`,
  et extension de `WorldSerializationTests` pour couvrir le round-trip
  d'un Component métier.

### Ajouté --- v0.1

- Structure du dépôt (`Simulation/`, `Content/`, `Rendering/`, `Tools/`,
  `Documentation/`, `Tests/`), conforme à `PROD/FeuilleDeRoute.md`.
- Kernel minimal (v0.1) : les neuf primitives (`Entity`, `IComponent`,
  `Value<T>`, `State`, `Relation`, `GameEvent`, `Tick`, `SpaceRef`,
  `Lifecycle`), un `World` déterministe, la sauvegarde/rechargement JSON.
- Un test par invariant du Kernel (`Tests/Chroniques.Simulation.Tests/`).

### Validé

- v0.1 : Compilation et exécution des tests confirmées (`dotnet build && dotnet test`) : 18/18 tests réussis, 0 échec. Le critère de sortie v0.1 de MASTER-005 est atteint : le noyau tourne, tous les tests de lois passent, un World vide (et un World peuplé) se sauvegarde et se recharge à l'identique.
- v0.2 : **non encore compilé** --- rédigé sans SDK .NET disponible dans l'environnement de rédaction. À vérifier avec `dotnet build && dotnet test` avant de considérer ce lot comme acquis.
