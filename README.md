# CHRONIQUES-ENGINE

> Moteur de simulation déterministe du projet **Chroniques**  
> État courant : **v0.4 — Autonomie productive et cognitive consolidée**  
> Build : ✅  
> Tests : **291 / 291 réussis**

---

# 1. Présentation

`CHRONIQUES-ENGINE` contient l'implémentation C#/.NET exécutable du moteur de simulation de Chroniques.

Le dépôt moteur n'est pas l'autorité métier.

Ordre documentaire :

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

---

# 2. Principes

## Déterminisme

À World, seed, configuration, entrées et ordre de Systems identiques, le résultat observable doit être identique.

## Données / logique

```text
Component
= données persistables

System / resolver / rule / planner / engine / applicator
= logique
```

## Action Pipeline unique

Les nouvelles capacités continuent de passer par :

```text
Intent
↓
Planner
↓
Plan
↓
ActionInstance
↓
Execution Engine
↓
Outcome
↓
Effects
↓
World
```

Aucun second pipeline cognitif ou économique n'a été créé.

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
├── World
├── Entity
├── Tick
├── State
├── Lifecycle
├── GameEvent
└── DeterministicRandom

Components
├── AgeComponent
├── NeedsComponent
├── RelationComponent
├── SkillComponent
├── FoodProductComponent
├── ResourceStockComponent
├── ProductionProvenanceComponent
├── HabitComponent
└── AmbitionComponent

Systems
├── Scheduler
├── AgingSystem
├── NeedsDecaySystem
├── RelationSystem
├── SkillSystem
├── HeritageSystem
├── HabitEvolutionSystem
└── AmbitionEvolutionSystem

Actions
├── Intent
├── Plan / PlanStep / Cibles
├── ActionDefinition / ActionInstance
├── Outcome
├── CompositePlanner
├── CompositeExecutionEngine
├── NeedsPlanner
├── ProductionPlanner
├── FoodTransferPlanner
├── NeedsExecutionEngine
├── ProductionExecutionEngine
├── FoodTransferExecutionEngine
└── IActionEffectApplicator + applicateurs

Autonomy
├── IAutonomousIntentSource
├── IAutonomousIntentExecutor
├── AutonomousActionSystem
├── CompositeAutonomousIntentSource
├── NeedsIntentSource
├── VoluntaryFoodTransferIntentSource
├── ProductiveActivityIntentSource
├── HabitIntentSource
├── AmbitionIntentSource
├── IAutonomousIntentExecutionObserver
├── PipelineAutonomousIntentExecutor
├── HabitLearningObserver
├── IHabitRule
├── IHabitFormationParameterResolver
├── IHabitStrengthPolicy
└── IAmbitionRule

Session
├── LifeSession
└── LifeSessionState

Persistence
└── WorldRepository / WorldSnapshot
```

---

# 5. Ordre autonome courant

Le moteur est compatible avec l'ordre GDB-004A v1.3 :

```text
NeedsIntentSource
↓ sinon
VoluntaryFoodTransferIntentSource
↓ sinon
ProductiveActivityIntentSource
↓ sinon
HabitIntentSource
↓ sinon
AmbitionIntentSource
↓ sinon
aucun Intent
```

`CompositeAutonomousIntentSource` retourne la première source non-null.

Aucun score universel n'est calculé entre les familles.

---

# 6. Besoins autonomes

Le moteur exécute notamment :

```text
Fatigue → se_reposer
Faim + nourriture accessible → manger
```

Les besoins utilisent `NeedsIntentSource`, `NeedsPlanner`, `NeedsExecutionEngine` et les applicateurs d'Effects appropriés.

---

# 7. Production autonome — ENGINE-013

Une activité productive disponible peut produire :

```text
Intent produire_denree
↓
ProductionOperation
↓
entrée ResourceStock consommée
+
sortie FoodProduct augmentée
+
ProductionTrace persistée
```

Principales briques :

```text
ResourceStockComponent
ProductionOperation
IProductiveActivityResolver
ProductiveActivityIntentSource
ProductionPlanner
ProductionExecutionEngine
ProductionActionEffectApplicator
ProductionProvenanceComponent
```

Le moteur ne choisit ni métier, employeur, salaire ni prix.

---

# 8. Circulation autonome — ENGINE-014

Le moteur peut transférer volontairement une denrée entre deux habitants :

```text
stock source
↓
donner_denree
↓
stock destination
```

Contraintes principales :

- donneur ≠ destinataire ;
- stock source ≠ stock destination ;
- identité de produit compatible ;
- quantité positive et disponible ;
- conservation stricte des portions.

Principales briques :

```text
FoodProductComponent.ProductKindId
FoodTransferOpportunity
IVoluntaryFoodTransferResolver
VoluntaryFoodTransferIntentSource
FoodTransferPlanner
FoodTransferExecutionEngine
FoodTransferActionEffectApplicator
```

Le transfert n'implique aucun paiement.

---

# 9. Scénario économique multi-habitants validé

```text
Tick N
A produit une denrée
↓
stock A = 1

Tick N+1
A transfère à B
↓
stock A = 0
stock B = 1
↓
B mange
↓
stock B = 0
Faim B restaurée
```

Aucune entrée joueur n'est nécessaire.

---

# 10. Observation d'exécution — ENGINE-015

Le moteur expose désormais le chemin réel d'une exécution autonome à des observateurs :

```text
BeforeExecution
↓
PipelineRunner
├── exception → ExecutionAborted
└── Action terminée → AfterExecution
```

Briques :

```text
IAutonomousIntentExecutionObserver
PipelineAutonomousIntentExecutor
```

Les contrats historiques `IAutonomousIntentExecutor`, `AutonomousActionSystem`, `PipelineRunner` et ACT `Intent` restent inchangés.

---

# 11. Habitudes génériques — ENGINE-016

Le moteur peut, via des règles injectées :

```text
Intent répété
+
Signature de formation déterministe
↓
HabitState persistante
↓
Déclencheur
↓
HabitIntentSource
↓
Intent ACT
```

Le framework couvre :

- formation dans une fenêtre configurée ;
- persistance ;
- sélection Force puis ancienneté ;
- activation ;
- renforcement ;
- érosion ;
- suppression à Force 0.

Aucune Habitude narrative concrète n'est fournie par défaut.

---

# 12. Ambitions génériques — ENGINE-017

Le moteur peut, via des Types injectés :

```text
création
↓
AmbitionState persistante
↓
évaluation du Progrès
↓
accomplissement / abandon
↓
AmbitionIntentSource
↓
Intent ACT
```

Arbitrage interne :

```text
Intensité
↓ égalité
Progrès
↓ égalité
ancienneté
```

`ObjectivePayload` reste opaque pour le moteur générique.

Aucun Type concret de carrière, richesse, logement ou relation n'est fourni.

---

# 13. Persistance

`WorldRepository` conserve désormais notamment :

```text
FoodProductComponent
ResourceStockComponent
ProductionProvenanceComponent
HabitComponent
AmbitionComponent
```

Les services runtime restent injectés et ne sont pas sérialisés : resolvers, rules, policies et registres temporaires.

---

# 14. Action taxonomy concrète

Le moteur exécute actuellement quatre chaînes ACT canoniques :

```text
Entretien → Repos → SeReposer
Entretien → Alimentation → Manger
Transformation → Production → ProduireDenree
Échange → Transfert → DonnerDenree
```

Les systèmes cognitifs produisent des Intents mais n'introduisent pas de nouveau Verbe par eux-mêmes.

---

# 15. Tests

État validé le **12 août 2026** :

```text
dotnet build
→ succès

dotnet test
→ 291 tests
→ 291 réussis
→ 0 échec
```

Repères :

```text
178 → besoins autonomes consolidés
201 → production autonome
224 → circulation autonome
233 → observation d'exécution
260 → Habitudes génériques
291 → Ambitions génériques
```

---

# 16. Documentation TECH

```text
TECH-001 — Systems de population
TECH-002 — Boucle de vie minimale
TECH-003 — Orchestration des habitants autonomes
TECH-004 — Décision autonome par besoins
TECH-005 — Production et circulation autonomes
TECH-006 — Cognition autonome générique
```

Le jalon ENGINE-013 à 017 est consolidé par `TECH-005`, `TECH-006` et l'audit documentaire dédié dans le dépôt CHRONIQUES.

---

# 17. Frontières actuelles

Le moteur ne contient pas encore comme capacités validées :

```text
PersonalityComponent
mappings Trait/Habitude ou Trait/Ambition
Habitudes métier canoniques
Types d'Ambitions métier canoniques
Monnaie / prix / vente / marché
Mémoire du Monde opérationnelle
événements mondiaux autonomes complets
fairness inter-familles
simulation multi-générations crédible achevée
```

---

# 18. Prochaine frontière

La prochaine frontière cognitive est l'audit de `GDB-004D — Les Personnalités` avant toute spécification ENGINE correspondante.

Le moteur ne doit pas inventer de Trait concret ni de mapping psychologique universel.

---

# 19. Statut

```text
CHRONIQUES-ENGINE
Version de développement : v0.4
Build : ✅
Tests : ✅ 291 / 291
Architecture : active
Autonomie physiologique : validée
Substrat économique matériel : validé
Observation cognitive : validée
Habitudes génériques : validées
Ambitions génériques : validées
Développement : en cours
```

v0.4 reste ouverte jusqu'à démonstration d'un monde évoluant de façon crédible sur plusieurs générations sans intervention permanente du joueur.
