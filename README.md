# CHRONIQUES-ENGINE

> Moteur de simulation déterministe du projet **Chroniques**  
> État courant : **v0.4 — Premier lot d'autonomie validé**  
> Build : ✅  
> Tests : **146 / 146 réussis**

---

# 1. Présentation

`CHRONIQUES-ENGINE` contient l'implémentation exécutable du moteur de simulation de Chroniques.

Le dépôt n'est pas l'autorité métier du projet.

Les règles et contrats sont définis en amont dans `CHRONIQUES` :

```text
MASTER
↓
CORE
↓
GDB
↓
ACT
↓
ENGINE
↓
CHRONIQUES-ENGINE
↓
Tests
↓
TECH
```

Le moteur traduit ces spécifications en C# déterministe, testable et indépendant du rendu.

---

# 2. Principes

## Déterminisme

À World, seed, entrées et ordre de Systems identiques, le résultat observable doit être identique.

## Séparation données / logique

```text
Component
= données

System
= transformation déterministe
```

## Documentation First

Une infrastructure structurante doit être spécifiée avant implémentation lorsqu'elle ne constitue pas la documentation rétroactive d'un code historique.

## Aucun couplage au rendu

La simulation reste indépendante de toute interface ou moteur graphique.

---

# 3. Solution

```text
Chroniques.sln
│
├── Simulation/
│   └── Chroniques.Simulation/
│
└── Tests/
    └── Chroniques.Simulation.Tests/
```

---

# 4. Architecture actuelle

```text
Kernel
│
├── World
├── Entity
├── Tick
├── State
├── Lifecycle
├── GameEvent
└── DeterministicRandom

Components
│
├── AgeComponent
├── NeedsComponent
├── RelationComponent
└── SkillComponent

Systems
│
├── Scheduler
├── AgingSystem
├── NeedsDecaySystem
├── RelationSystem
├── SkillSystem
└── HeritageSystem

Actions
│
├── Intent
├── Planner
├── Plan
├── ActionDefinition
├── ActionInstance
├── Outcome
├── Effects
└── PopulationEffectApplicator

Session
│
├── LifeSession
└── LifeSessionState

Autonomy
│
├── IAutonomousIntentSource
├── IAutonomousIntentExecutor
└── AutonomousActionSystem

Persistence
│
└── WorldRepository
```

---

# 5. Kernel et World

Le Kernel porte les primitives fondamentales.

`World` contient l'état simulé, les Entities, le Tick et le journal observable `World.Events`.

Une Entity porte des Components mais pas la logique métier.

---

# 6. Scheduler

Le Scheduler :

```text
World.Advance
↓
System 1
↓
System 2
↓
...
```

L'ordre d'enregistrement des Systems fait partie du comportement déterministe.

Aucun System ne doit avancer lui-même le Tick.

---

# 7. Systems fondamentaux

## NeedsDecaySystem

Fait évoluer les besoins au fil du temps.

## AgingSystem

Fait progresser l'âge et peut provoquer les transitions de fin de vie.

## RelationSystem

Gère l'évolution des relations sociales.

## SkillSystem

Gère pratique, progression et déclin des compétences.

## HeritageSystem

Détecte la mort via `Lifecycle` et porte la logique minimale de désignation d'un héritier.

---

# 8. Action Pipeline — ENGINE-006

```text
Intent
↓
Planner
↓
Plan
↓
Action Instance
↓
Execution Engine
↓
Outcome
↓
Effects
↓
World
```

Le moteur dispose d'un premier `PipelineRunner` de démonstration autour de « Se reposer ».

Ce runner n'est pas présenté comme un catalogue universel de Verbes.

---

# 9. Population — ENGINE-008

Lot validé :

```text
Relations
Compétences
Héritage minimal
Effects de population
```

La mémoire individuelle, les Habitudes, les Ambitions, la Mémoire du Monde et l'économie autonome restent hors de ce lot.

---

# 10. Boucle de vie — ENGINE-009

`LifeSession` orchestre le personnage contrôlé.

```text
LifeSession.AdvanceTime
↓
Scheduler.Tick(World)
↓
Systems
↓
Lifecycle / HeritageSystem
↓
LifeSession synchronise le contrôle
```

Elle :

- conserve le personnage actif tant qu'il vit ;
- constate sa mort via `Lifecycle` ;
- récupère une transmission observable valide ;
- bascule vers l'héritier ;
- termine sans successeur lorsqu'aucune continuité n'est possible.

Elle ne sélectionne jamais elle-même l'héritier et ne déclenche aucune Action automatiquement.

---

# 11. Autonomie — ENGINE-010

Le premier mécanisme d'Actions autonomes est désormais implémenté et validé.

Flux :

```text
Scheduler.Tick
↓
AutonomousActionSystem
↓
Acteur autonome enregistré
↓
IAutonomousIntentSource
↓
Intent?
↓
IAutonomousIntentExecutor
↓
ENGINE-006
↓
World
```

---

# 12. IAutonomousIntentSource

Contrat :

```csharp
Intent? CreateIntent(
    Entity actor,
    World world,
    Tick currentTick);
```

La source propose éventuellement un Intent.

Elle ne mute pas directement le World.

Aucune politique métier universelle de décision n'est actuellement intégrée au moteur.

---

# 13. IAutonomousIntentExecutor

Contrat :

```csharp
void Execute(Intent intent, World world);
```

Cette frontière permet au System d'autonomie de ne connaître :

- ni Planner concret ;
- ni Execution Engine concret ;
- ni Verbe concret.

Elle n'est pas un second Action Pipeline.

---

# 14. AutonomousActionSystem

Le System :

- traite uniquement les Acteurs explicitement enregistrés ;
- conserve leur ordre d'enregistrement ;
- ignore une Entity absente ;
- ignore une Entity morte ;
- accepte qu'aucun Intent ne soit produit ;
- rejette un Intent attribué à un autre Acteur ;
- transmet un Intent valide à l'exécuteur ;
- ne connaît aucun Verbe concret ;
- ne modifie pas directement un Component métier ;
- ne fait jamais avancer le Tick lui-même.

L'enregistrement d'un même Acteur est idempotent.

---

# 15. Intégration autonome validée

Le test d'intégration de référence démontre :

```text
Scheduler.Tick
↓
AutonomousActionSystem
↓
Intent "se_reposer"
↓
adaptateur IAutonomousIntentExecutor
↓
PipelineRunner
↓
Action Archived
↓
Outcome réussi
↓
Fatigue restaurée
↓
besoin.fatigue.restauree dans World.Events
```

Il prouve le raccordement réel avec ENGINE-006 sans prétendre résoudre tous les futurs Verbes.

---

# 16. World.Events

`World.Events` reste un journal d'observabilité.

Il ne constitue jamais :

- un EventBus ;
- une queue de messages ;
- un canal de coordination entre Systems.

---

# 17. Persistence

Le moteur dispose d'une première infrastructure de snapshot, sérialisation et restauration du World.

Le déterminisme doit être préservé après restauration.

---

# 18. Tests

État validé le **11 août 2026** :

```text
dotnet build
→ succès

dotnet test
→ 146 tests
→ 146 réussis
→ 0 échec
```

Les 12 tests ajoutés pour ENGINE-010 couvrent notamment :

- Acteur enregistré vivant ;
- Acteur non enregistré ;
- Entity absente ;
- Entity morte ;
- absence d'Intent ;
- exécution unique d'un Intent valide ;
- rejet d'un Intent attribué à un autre Acteur ;
- ordre déterministe des Acteurs ;
- idempotence de l'enregistrement ;
- déterminisme de la séquence ;
- absence d'avancement direct du Tick ;
- intégration Scheduler → autonomie → PipelineRunner → World.

---

# 19. État des lots ENGINE récents

```text
ENGINE-006  Action Pipeline                    ✅ Maturité 4
ENGINE-008  Systems de population              ✅ Maturité 4
ENGINE-009  Boucle de vie minimale             ✅ Maturité 4
ENGINE-010  Orchestration habitants autonomes  ✅ Maturité 4
```

---

# 20. ENGINE-C06

La lacune historique d'orchestration entre Scheduler et Actions autonomes est considérée résolue après validation d'ENGINE-010.

```text
ENGINE-C06
→ Clos
```

Cette clôture ne signifie pas que la politique de décision des PNJ est terminée.

---

# 21. Ce qui reste hors périmètre

```text
Politique métier de décision PNJ
Habitudes
Ambitions
Personnalité décisionnelle
Mémoire individuelle
Mémoire du Monde
Économie autonome complète
Métiers autonomes
Perception
Croyances
Émotions
Catalogue complet de Verbes
Simulation sociale avancée
```

---

# 22. Phase actuelle

Le moteur est entré dans **v0.4 — Le monde vivant**.

Le premier raccordement d'autonomie est validé.

La prochaine brique ne doit pas être codée avant d'avoir vérifié que les autorités GDB définissent suffisamment précisément la politique qu'elle doit traduire.

Candidats documentaires prioritaires :

```text
GDB-004B — Besoins
GDB-004D — Personnalités
GDB-004E — Habitudes
GDB-004F — Ambitions
GDB-002E — Opportunités
```

---

# 23. Workflow

```text
GDB / ACT
↓
ENGINE
↓
Implémentation
↓
Tests
↓
Validation
↓
TECH
```

Une nouvelle règle métier ne doit pas apparaître uniquement dans le code.

---

# 24. Statut

```text
CHRONIQUES-ENGINE
Version de développement : v0.4
Build : ✅
Tests : ✅ 146 / 146
Architecture : active
Autonomie orchestration : validée
Développement : en cours
```

Le moteur n'est pas terminé.

Il possède désormais une base déterministe capable d'orchestrer une vie contrôlée et de raccorder les premières Actions d'habitants autonomes au même pipeline d'exécution.
