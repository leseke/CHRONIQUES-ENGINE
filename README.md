# CHRONIQUES-ENGINE

> Moteur de simulation déterministe du projet **Chroniques**  
> État courant : **v0.4 — Autonomie par besoins consolidée**  
> Build : ✅  
> Tests : **178 / 178 réussis**

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

System / service moteur
= transformation ou orchestration
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
├── SkillComponent
└── FoodProductComponent

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
├── Planner / NeedsPlanner
├── Plan / PlanStep + Cibles
├── ActionDefinition
├── ActionInstance
├── Outcome
├── NeedsExecutionEngine
├── ActionEffectDispatcher
├── RestActionEffectApplicator
└── FoodActionEffectApplicator

Autonomy
│
├── IAutonomousIntentSource
├── IAutonomousIntentExecutor
├── AutonomousActionSystem
└── NeedsIntentSource

Session
│
├── LifeSession
└── LifeSessionState

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

Le Scheduler reste l'autorité temporelle :

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

Le pipeline supporte maintenant deux Verbes réellement validés :

```text
VERB-001 — Se reposer
VERB-002 — Manger
```

`PipelineRunner.Execute(...)` constitue l'entrée commune du bloc actuel.

Le runner ne contient pas les règles métier de repos ou d'alimentation ; il délègue leur application aux applicateurs d'Effects.

---

# 9. Population — ENGINE-008

Lot validé :

```text
Relations
Compétences
Héritage minimal
Effects de population
```

La mémoire individuelle, les Habitudes, les Ambitions, la Mémoire du Monde et l'économie autonome complète restent hors de ce lot.

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

Elle conserve le personnage actif tant qu'il vit, constate sa mort via `Lifecycle`, récupère une transmission observable valide, bascule vers l'héritier ou termine sans successeur.

Elle ne choisit pas elle-même l'héritier.

---

# 11. Orchestration autonome — ENGINE-010

Flux validé :

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

`AutonomousActionSystem` :

- traite uniquement les Acteurs explicitement enregistrés ;
- conserve leur ordre ;
- ignore une Entity absente ou morte ;
- accepte `null` comme absence de décision ;
- vérifie la cohérence `intent.Acteur` ;
- ne connaît aucun Verbe concret ;
- ne fait jamais avancer le Tick.

---

# 12. Décision autonome — ENGINE-011 / ENGINE-012

Le moteur possède maintenant une première politique métier concrète : `NeedsIntentSource`.

## Repos

```text
Fatigue < seuil configuré
↓
Intent "se_reposer"
```

## Alimentation

```text
Faim < seuil configuré
+
nourriture accessible
↓
Intent "manger"
```

Une Faim basse sans nourriture accessible ne crée pas de faux Intent.

---

# 13. Arbitrage des besoins

Lorsque Faim et Fatigue sont simultanément actionnables :

```text
satisfaction la plus basse
→ besoin retenu
```

En cas d'égalité stricte :

```text
Faim
avant
Fatigue
```

Ce tie-break est uniquement technique et déterministe.

Aucune personnalité, Habitude ou Ambition n'est encore pondérée dans cette décision.

---

# 14. FoodProductComponent

Le produit alimentaire minimal est représenté par :

```text
FaimRestauree
PortionsDisponibles
```

Une Action `Manger` réussie :

```text
PortionsDisponibles - 1
+
Faim de l'Acteur restaurée
```

La restauration reste bornée à `100`.

Le moteur ne crée pas gratuitement une nourriture au moment de la décision.

---

# 15. Accessibilité alimentaire

L'accès à une nourriture passe par :

```text
IAccessibleFoodResolver
```

Cette frontière permet de savoir si une Entity alimentaire est actuellement utilisable par un Acteur sans imposer encore :

- inventaire général ;
- propriété ;
- stockage ;
- distance universelle ;
- système commercial.

Une future implémentation d'inventaire ou de possession pourra se brancher derrière cette frontière.

---

# 16. Plan et Cibles

`PlanStep` peut désormais porter ses `Cibles`.

Pour le repos :

```text
Cible principale = Acteur
```

Pour l'alimentation :

```text
Cible principale = produit alimentaire
Cible secondaire = Acteur
```

La Cible concrète reste absente de l'Intent ; elle est matérialisée par le Planner.

---

# 17. NeedsPlanner

`NeedsPlanner` reconnaît exactement :

```text
se_reposer
manger
```

Il ne constitue pas un Planner universel de tous les futurs Verbes.

Pour `manger`, il demande une nourriture accessible au resolver puis produit le `PlanStep` correspondant.

---

# 18. NeedsExecutionEngine

Le moteur d'exécution du bloc valide les conditions techniques nécessaires avant réussite.

`Manger` échoue ou ne peut être planifié si la nourriture :

- n'existe plus ;
- n'est pas alimentaire ;
- est épuisée ;
- n'est plus accessible.

Aucun Effect n'est appliqué après un Outcome en échec.

---

# 19. Effects

L'application métier est séparée du runner :

```text
ActionEffectDispatcher
├── RestActionEffectApplicator
└── FoodActionEffectApplicator
```

Le dispatcher applique l'applicateur compatible avec l'Action réussie.

Pour l'alimentation, les faits observables sont :

```text
produit.alimentaire.consomme
besoin.faim.restauree
```

Pour le repos :

```text
besoin.fatigue.restauree
```

---

# 20. Persistence

`FoodProductComponent` survit aux sauvegardes/rechargements via `EntitySnapshot` et `WorldRepository`.

Le champ reste nullable afin de ne pas transformer toutes les Entities en produits alimentaires.

---

# 21. Boucles autonomes validées

Le moteur sait maintenant produire une régulation autonome réelle :

```text
NeedsDecaySystem
↓
Fatigue franchit son seuil
↓
se_reposer
↓
Fatigue restaurée
```

et :

```text
Faim franchit son seuil
+
nourriture accessible
↓
manger
↓
portion consommée
+
Faim restaurée
```

La première boucle a également été validée sur plusieurs Ticks sans entrée joueur.

---

# 22. World.Events

`World.Events` reste un journal d'observabilité.

Il ne constitue jamais :

- un EventBus ;
- une queue de messages ;
- un canal de coordination entre Systems.

---

# 23. Tests

État validé le **11 août 2026** :

```text
dotnet build
→ succès

dotnet test
→ 178 tests
→ 178 réussis
→ 0 échec
```

Le périmètre couvre notamment :

- orchestration autonome ;
- seuil strict de Fatigue ;
- régulation multi-Tick ;
- chaîne ACT `Entretien → Repos → SeReposer` ;
- seuil strict de Faim ;
- absence de faux Intent sans nourriture ;
- arbitrage déterministe Faim/Fatigue ;
- chaîne ACT `Entretien → Alimentation → Manger` ;
- Cibles portées par le Plan ;
- nourriture absente, épuisée ou inaccessible ;
- consommation réelle d'une portion ;
- restauration de Faim ;
- publication des Events ;
- persistance alimentaire ;
- compatibilité du comportement repos existant ;
- intégration Scheduler → autonomie → pipeline → World.

---

# 24. État des lots ENGINE récents

```text
ENGINE-006  Action Pipeline                    ✅ Maturité 4
ENGINE-008  Systems de population              ✅ Maturité 4
ENGINE-009  Boucle de vie minimale             ✅ Maturité 4
ENGINE-010  Orchestration habitants autonomes  ✅ Maturité 4
ENGINE-011  Décision autonome par besoins      ✅ Maturité 4
ENGINE-012  Alimentation autonome minimale     ✅ Maturité 4
```

---

# 25. Documentation technique

```text
TECH-001 — Systems de population
TECH-002 — Boucle de vie minimale
TECH-003 — Orchestration des habitants autonomes
TECH-004 — Décision autonome par besoins
```

TECH-004 consolide ENGINE-011 et ENGINE-012 comme une seule capacité technique cohérente.

---

# 26. Ce qui reste hors périmètre

```text
Inventaire général
Propriété
Travail autonome
Économie autonome complète
Production alimentaire autonome
Achat / vente autonomes
Habitudes pondérées
Ambitions pondérées
Personnalité décisionnelle pondérée
Mémoire individuelle avancée
Mémoire du Monde
Interactions sociales autonomes complètes
Perception
Croyances
Émotions
Catalogue complet de Verbes
```

---

# 27. Phase actuelle

Le moteur reste en **v0.4 — Le monde vivant**.

Le bloc suivant recommandé doit augmenter la capacité du monde à fonctionner réellement sans le joueur.

La prochaine frontière est donc :

```text
travail autonome
+
économie autonome minimale
```

avant d'ajouter mécaniquement un troisième besoin physiologique.

Les règles GDB correspondantes doivent être auditées avant toute nouvelle spécification ENGINE.

---

# 28. Workflow

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
Consolidation documentaire au jalon pertinent
↓
TECH / AUDIT / roadmap / README concernés
```

Une nouvelle règle métier ne doit pas apparaître uniquement dans le code.

---

# 29. Statut

```text
CHRONIQUES-ENGINE
Version de développement : v0.4
Build : ✅
Tests : ✅ 178 / 178
Architecture : active
Orchestration autonome : validée
Décision autonome repos : validée
Décision autonome alimentation : validée
Développement : en cours
```

Le moteur n'est pas terminé.

Il possède désormais une base déterministe capable d'orchestrer une vie contrôlée, de faire agir des habitants sans intervention du joueur, puis de choisir et exécuter deux réponses physiologiques réelles à travers le même pipeline d'Actions.
