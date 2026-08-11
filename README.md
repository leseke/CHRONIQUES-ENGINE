# CHRONIQUES-ENGINE

> Moteur de simulation déterministe du projet **Chroniques**  
> État courant : **v0.3 — Boucle de vie minimale validée**  
> Build : ✅  
> Tests : **134 / 134 réussis**

---

# 1. Présentation

`CHRONIQUES-ENGINE` contient l'implémentation exécutable du moteur de simulation de Chroniques.

Le dépôt n'est pas l'autorité métier du projet.

Les règles et contrats sont définis en amont dans le dépôt documentaire `CHRONIQUES`, notamment par :

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

Le moteur traduit ces spécifications en code C# déterministe, testable et indépendant du rendu.

---

# 2. Principes

Le moteur respecte plusieurs principes fondamentaux.

## Déterminisme

À :

```text
World identique
+
Seed identique
+
Entrées identiques
+
Ordre de Systems identique
```

le moteur doit produire :

```text
Résultat identique
```

Le hasard passe par les primitives déterministes du Kernel.

---

## Séparation données / logique

Les Components portent l'état.

Les Systems portent la logique.

```text
Component
=
données

System
=
transformation déterministe des données
```

Un Component ne doit pas devenir un objet métier autonome contenant sa propre logique de simulation.

---

## Documentation First

Lorsqu'une nouvelle infrastructure ou règle structurante est requise :

```text
Spécification
↓
Validation
↓
Implémentation
↓
Tests
↓
Documentation TECH
```

Le code ne doit pas inventer silencieusement des règles absentes des documents d'autorité.

---

## Aucun couplage au rendu

Le moteur ne dépend pas :

- d'une interface graphique ;
- d'un moteur 3D ;
- d'une plateforme particulière ;
- d'une technologie de présentation.

Il doit pouvoir fonctionner sans rendu.

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

Le projet principal contient le moteur.

Le projet Tests contient les tests unitaires et d'intégration du moteur.

---

# 4. Architecture actuelle

L'architecture principale est organisée autour de plusieurs couches.

```text
Kernel
│
├── World
├── Entity
├── Component
├── Tick
├── State
├── Value
├── Relation
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

Persistence
│
└── WorldRepository
```

L'arborescence exacte peut évoluer sans modifier les responsabilités décrites ci-dessus.

---

# 5. Kernel

Le Kernel constitue la couche fondamentale du moteur.

Il expose les primitives génériques nécessaires à la simulation.

## World

`World` constitue le conteneur principal de l'état simulé.

Il gère notamment :

- les Entities ;
- le Tick ;
- les événements observables ;
- les primitives nécessaires à la simulation déterministe.

---

## Entity

Une Entity représente une identité dans le World.

Elle peut porter plusieurs Components.

La logique métier ne vit pas directement dans l'Entity.

---

## Components

Les Components représentent des fragments d'état.

Exemples actuels :

```text
AgeComponent
NeedsComponent
RelationComponent
SkillComponent
```

Une Entity ne possédant pas un Component donné peut simplement être ignorée par le System correspondant lorsque le contrat le prévoit.

---

## Lifecycle

Le `Lifecycle` représente l'évolution d'état d'une Entity.

Il est notamment utilisé pour détecter l'état :

```text
mort
```

par `HeritageSystem` et par la couche de session lorsqu'elle synchronise le personnage contrôlé après un Tick.

La mort n'est pas déduite uniquement par lecture de `World.Events`.

---

# 6. Temps et Scheduler

Le moteur utilise un temps simulé discret basé sur `Tick`.

Le Scheduler garantit un ordre déterministe d'exécution des Systems.

Conceptuellement :

```text
Tick
↓
System 1
↓
System 2
↓
System 3
↓
...
↓
Tick suivant
```

L'ordre d'enregistrement des Systems constitue donc une partie du comportement déterministe de la simulation.

---

# 7. Systems fondamentaux

## NeedsDecaySystem

Fait évoluer les besoins au fil du temps.

Il constitue l'une des premières briques de simulation autonome du moteur.

---

## AgingSystem

Fait progresser l'âge des Entities concernées.

Il peut également provoquer les transitions de Lifecycle liées à la fin de vie.

---

# 8. Action Pipeline

Le moteur possède maintenant un premier pipeline d'Actions conforme aux contrats ACT et ENGINE-006.

Le modèle conceptuel est :

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

## Intent

Exprime ce qu'un acteur cherche à accomplir.

Il ne décrit pas comment l'objectif sera réalisé.

---

## Planner

Transforme un Intent en Plan lorsque les conditions nécessaires sont réunies.

---

## Plan

Décrit une séquence d'Actions permettant de tenter d'atteindre l'Intent.

---

## Action Instance

Représente l'exécution concrète d'une Action dans un contexte donné.

---

## Outcome

Décrit le résultat d'une Action résolue.

Le résultat peut ensuite produire des Effects.

---

# 9. Effects et résolution

Le moteur sépare explicitement :

```text
résolution d'une Action
```

de :

```text
mutation métier du World
```

Le flux utilisé par les Effects de population est :

```text
Action Instance
↓
Execution Engine
↓
Effects typés
↓
PopulationEffectApplicator
↓
System responsable
↓
World
```

Le pipeline ne dépend donc pas directement de :

- `RelationSystem` ;
- `SkillSystem` ;
- `HeritageSystem`.

`PopulationEffectApplicator` assure actuellement le dispatch spécialisé des Effects de population.

---

# 10. Population — ENGINE-008

Le premier lot des Systems de population est implémenté et validé.

Il couvre :

```text
Relations
Compétences
Héritage minimal
```

La mémoire, la cognition et les autres couches psychologiques ne font pas partie de ce lot.

---

# 11. Relations sociales

`RelationComponent` représente les relations actives d'une Entity.

Une relation possède notamment :

- une cible ;
- un type ;
- une Force ;
- une date de création ;
- des Épisodes significatifs.

Les types actuellement représentés comprennent :

```text
Familiale
Amicale
Professionnelle
Commerciale
Politique
Conflictuelle
Sentimentale
```

---

## RelationSystem

`RelationSystem` constitue l'unique source de vérité pour l'évolution des relations sociales actuellement implémentées.

Il gère :

- l'érosion naturelle ;
- le plancher familial ;
- les interactions ;
- la création des relations ;
- leur disparition ;
- les Épisodes.

---

## Plancher familial

Une relation Familiale possède une protection contre la seule érosion naturelle.

```text
Force > plancher
↓
l'érosion peut descendre jusqu'au plancher
```

Mais :

```text
interaction négative
↓
Force peut descendre sous le plancher
```

Si la Force est déjà sous le plancher après une interaction :

```text
érosion naturelle
↓
la Force reste inchangée
```

Le temps ne doit donc jamais produire artificiellement :

```text
5 → 10
```

pour un plancher familial fixé à `10`.

Une interaction suffisamment négative peut atteindre `0` et rompre la relation.

Ce comportement est verrouillé par des tests de non-régression.

---

# 12. Compétences

`SkillComponent` représente les Compétences connues par une Entity.

Chaque Compétence possède notamment :

```text
Niveau
DernierePratique
```

---

## SkillSystem

`SkillSystem` gère :

- création lors de la première pratique ;
- progression ;
- gain décroissant ;
- dernière pratique ;
- déclin par inactivité.

Le Niveau reste borné entre :

```text
0
et
100
```

Le gain marginal diminue à mesure que le Niveau augmente.

---

# 13. Héritage minimal

`HeritageSystem` implémente actuellement la partie de GDB-004J pouvant être représentée sans système complet de patrimoine.

Il détecte une Entity morte directement via :

```text
Lifecycle.CurrentState.Name == "mort"
```

Il ne lit pas `World.Events` pour décider d'agir.

---

## Désignation

Le modèle actuellement implémenté privilégie :

```text
Relations Familiales
↓
Force la plus élevée
↓
égalité
↓
relation la plus ancienne
```

Si aucun successeur valide ne peut être déterminé, le System produit un événement observable d'absence de successeur.

---

## Non-retraitement

Une Entity morte déjà traitée par `HeritageSystem` n'est jamais retraitée.

Une même mort ne peut donc pas provoquer plusieurs transmissions.

---

## Refus

Le refus d'héritage utilise le flux :

```text
HeritageRefusalEffect
↓
PopulationEffectApplicator
↓
HeritageSystem.RefuserHeritage
↓
GameEvent observable
```

`PopulationEffectApplicator` ne contient aucune règle métier de refus.

`HeritageSystem` constitue l'unique source de vérité pour ce comportement.

---

## Transmission incomplète

Le cas conceptuel de transmission incomplète existe dans GDB-004J mais n'est pas encore implémenté.

Il dépend de futurs systèmes représentant notamment :

- le patrimoine matériel ;
- les éléments transmissibles ;
- les règles de redistribution.

Il ne doit pas être simulé artificiellement avant leur spécification.

---

# 14. Boucle de vie minimale — ENGINE-009

Le moteur possède désormais une couche `Session` dédiée à l'orchestration du personnage contrôlé.

`LifeSession` :

- connaît le personnage actif ;
- fait avancer exactement un Tick via le Scheduler lorsqu'on appelle `AdvanceTime()` ;
- constate la mort via le `Lifecycle` ;
- lit le résultat observable de `HeritageSystem` après le Tick ;
- bascule le contrôle vers l'héritier si la transmission est valide ;
- termine la session si aucun successeur valide n'existe.

Elle ne :

- sélectionne pas elle-même un héritier ;
- modifie pas directement les Components métier ;
- déclenche pas automatiquement une Action joueur ;
- déclenche pas automatiquement une Action PNJ ;
- transforme pas `World.Events` en EventBus.

Flux minimal :

```text
LifeSession.AdvanceTime
↓
Scheduler.Tick(World)
↓
AgingSystem / Systems
↓
HeritageSystem
↓
fin du Tick
↓
LifeSession synchronise le contrôle
```

La décision d'exécuter une Action et celle de faire avancer le temps restent séparées.

Aucune règle `1 Action = 1 Tick` n'est introduite.

---

# 15. World.Events

`World.Events` constitue le journal d'événements observable du World.

Il peut contenir des événements comme :

```text
vie.mort
heritage.transmission
heritage.absence-successeur
heritage.refus
```

Mais il ne constitue pas :

- un EventBus ;
- une queue de messages ;
- un système Publish/Subscribe entre Systems ;
- un canal de coordination.

Les Systems déterminent leurs actions à partir de l'état du World.

Les Events servent à observer et tracer les faits significatifs.

La couche `LifeSession` peut lire ces événements après un Tick pour constater un résultat déjà produit, sans faire communiquer les Systems entre eux.

---

# 16. Persistence

Le moteur dispose d'une première infrastructure de persistance.

Elle permet notamment :

```text
World
↓
Snapshot / Serialization
↓
Stockage
↓
Restauration
↓
World
```

Le déterminisme doit être préservé après restauration.

---

# 17. Tests

La règle actuelle est :

> une fonctionnalité structurante n'est considérée comme intégrée qu'après compilation et validation de ses tests.

État courant validé le 11 août 2026 :

```text
dotnet build
→ succès

dotnet test
→ 134 tests
→ 134 réussis
→ 0 échec
→ 0 ignoré critique
```

Les tests couvrent notamment :

- Entity ;
- Components ;
- Tick ;
- Lifecycle ;
- RNG déterministe ;
- Events ;
- Persistence ;
- Scheduler ;
- AgingSystem ;
- NeedsDecaySystem ;
- Action Pipeline ;
- Relations ;
- Compétences ;
- Héritage ;
- Effects de population ;
- `LifeSession` ;
- non-réutilisation d'une transmission ancienne ;
- cible d'héritage inexistante ;
- absence de sélection d'héritier par la session ;
- absence d'Action automatique ;
- déterminisme de la séquence de contrôle ;
- parcours d'intégration v0.3.

Le test d'intégration de référence démontre :

```text
Action joueur
↓
progression temporelle
↓
vieillissement
↓
mort
↓
HeritageSystem
↓
transmission
↓
continuité avec l'héritier
```

---

# 18. Commandes

Depuis la racine du dépôt :

```bash
dotnet build
```

Puis :

```bash
dotnet test
```

Une modification ne doit pas être considérée comme validée si :

```text
build != succès
```

ou :

```text
tests != 100 % réussis
```

---

# 19. État actuel

Le moteur a dépassé le stade du simple Kernel et possède désormais un premier assemblage de vie continue.

Les couches actuellement disponibles sont :

```text
Kernel                     ✅
World / Entity             ✅
Components génériques      ✅
Tick                        ✅
Lifecycle                   ✅
RNG déterministe           ✅
Journal d'événements       ✅
Persistence                ✅
Scheduler                   ✅
Needs                      ✅
Aging                      ✅
Action Pipeline             ✅
Relations                  ✅
Compétences                ✅
Héritage minimal           ✅
Effects population         ✅
LifeSession                 ✅
Continuité avec héritier   ✅
```

Restent notamment hors périmètre actuel :

```text
PNJ autonomes
Mémoire du Monde
Perception
Croyances
Émotions
Cognition avancée
Réputation complète
Économie autonome / complète
Patrimoine matériel
Transmission matérielle
Simulation sociale avancée
```

Ces systèmes ne doivent être ajoutés qu'après leurs spécifications correspondantes.

---

# 20. Phase actuelle

L'assemblage moteur minimal visé par **v0.3** est validé.

Le moteur peut désormais démontrer techniquement :

```text
Action
↓
Temps
↓
Vieillissement
↓
Mort
↓
Héritage minimal
↓
Continuité du personnage contrôlé
```

Cette validation ne signifie pas que toute la richesse finale d'une vie jouable est déjà implémentée.

Le prochain axe d'architecture prévu par la roadmap est **v0.4 — Le monde vivant**.

Il concerne notamment :

```text
PNJ autonomes
Économie autonome
Mémoire du Monde
Événements dynamiques
Comportements autonomes
```

Aucune de ces briques ne doit être ajoutée avant sa spécification documentaire.

---

# 21. Workflow

Pour les prochaines briques :

```text
GDB / ACT
↓
ENGINE
↓
Implémentation
↓
Tests rouges / verts
↓
Validation
↓
TECH
↓
Intégration
```

Une nouvelle règle métier ne doit pas apparaître uniquement dans le code.

---

# 22. Dépôt documentaire

Le moteur doit toujours être lu conjointement avec le dépôt :

```text
CHRONIQUES
```

Les documents d'autorité y définissent :

```text
MASTER → gouvernance
CORE   → primitives conceptuelles
GDB    → règles de simulation
ACT    → modèle d'actions
ENGINE → architecture moteur
TECH   → implémentations validées
QA     → validation
PROD   → feuille de route
```

Documents directement liés au dernier lot validé :

```text
ENGINE-009 — Boucle de vie minimale
TECH-002 — Boucle de vie minimale
```

---

# 23. Statut

```text
CHRONIQUES-ENGINE
Version de développement : v0.3
État : assemblage minimal v0.3 validé
Build : ✅
Tests : ✅ 134 / 134
Architecture : active
Prochain axe : préparation v0.4 — Le monde vivant
```

Le moteur n'est pas considéré comme terminé.

Il constitue désormais une base déterministe fonctionnelle avec continuité minimale de vie, sur laquelle les couches de monde autonome pourront être construites sans remettre en cause les responsabilités validées de v0.3.
