# Changelog --- Chroniques (moteur)

Ce fichier retrace l'historique des versions du **code**. Les décisions
d'architecture qui justifient ces versions vivent dans le registre ADR du
dépôt de documentation (`chroniques--main/ADR/`), pas ici.

Format : [Semantic Versioning](https://semver.org/lang/fr/). Les versions
suivent les jalons de `MASTER-005` (v0.1 « Le noyau », v0.2 « Une vie », etc.),
et non un calendrier.

---

## [Non publié]

### Ajouté --- v0.2, cycle de vie complet (en cours de vérification)

- `Entity` relié à `Lifecycle` dès sa création (CORE-002-H), écart présent
  depuis v0.1 --- `World.Spawn()` transmet désormais le Tick courant à
  `Entity.Create()`.
- Second Component métier : `Components/AgeComponent.cs` (GDB-008C ---
  l'âge en années simulées).
- `Systems/AgingSystem.cs` : fait progresser l'âge et le Lifecycle de
  chaque personnage (enfance → adolescence → âge adulte → maturité →
  vieillesse → mort), publie un Event `vie.mort` sur le World à la mort.
  Complète le critère de sortie v0.2 de MASTER-005.
- `Persistence/` étendu : `EntitySnapshot` couvre désormais l'état courant
  du Lifecycle (`LifecycleCreatedAt`, `LifecycleState`) et `AgeComponent`.
  L'historique complet du Lifecycle n'est pas encore persisté (limite
  assumée, documentée sur `Entity.Restore`).
- Tests : `AgingSystemTests`, extension d'`EntityTests` (CORE-002-H) et de
  `WorldSerializationTests` (round-trip du Lifecycle et de `AgeComponent`).
- Modèle de temps tranché en équipe : 3 Ticks par saison, 4 saisons par
  an, soit 12 Ticks par année simulée --- `AgingSystem` n'incrémente donc
  l'âge d'un habitant qu'un Tick sur douze. À formaliser par un ADR avant
  la v0.3 (GDB-008A ne fixe aucune conversion Tick → calendrier).
- **Une hypothèse de travail reste non tranchée par la documentation**,
  introduite par ce lot et à confirmer par un ADR avant la v0.3 : les
  seuils d'âge / l'espérance de vie (GDB-008C nomme les étapes de vie
  sans fixer d'âges de bascule). Détail dans le commentaire XML
  d'`AgingSystem`.
- **Non encore compilé dans cet environnement** --- rédigé sans SDK .NET
  disponible, comme le lot précédent. À vérifier avec
  `dotnet build && dotnet test` avant de considérer ce lot comme acquis.

### Ajouté --- v0.2, besoins et scheduler

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
- v0.2 (besoins et scheduler) : Compilation et exécution des tests confirmées (`dotnet build && dotnet test`) : 30/30 tests réussis, 0 échec.
- v0.2 (cycle de vie complet) : **non encore compilé** --- rédigé sans SDK .NET disponible dans l'environnement de rédaction. À vérifier avec `dotnet build && dotnet test` avant de considérer ce lot comme acquis, et avant de le considérer comme atteignant réellement le critère de sortie v0.2 (« un personnage naît, vit ses besoins année après année, et meurt »).
