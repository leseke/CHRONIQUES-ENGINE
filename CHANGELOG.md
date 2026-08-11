# CHANGELOG — CHRONIQUES-ENGINE

Toutes les évolutions significatives du moteur Chroniques sont consignées dans ce document.

Le format suit l'évolution réelle du moteur et non uniquement les commits Git.

---

# [Unreleased]

## À venir

Les prochains lots seront définis par les documents d'autorité du dépôt `CHRONIQUES`.

Prochain besoin identifié pour v0.3 :

- assemblage d'une **boucle de vie minimale complète**, permettant de relier les briques déjà présentes jusqu'à la continuité avec un héritier.

Candidats identifiés pour des phases ultérieures, sans engagement d'implémentation :

- perception ;
- croyances ;
- émotions ;
- réputation ;
- patrimoine matériel ;
- transmission complète ;
- progression de la simulation sociale ;
- Mémoire du Monde, ciblée par v0.4 ;
- généralisation éventuelle du mécanisme d'application des Effects.

La notion ambiguë de « mémoire des habitants » n'est plus utilisée comme cible v0.3. Toute mémoire simulée future doit être définie par sa bibliothèque d'autorité avant implémentation.

Aucun de ces éléments n'est considéré comme engagé tant que la spécification correspondante n'est pas validée.

---

# [0.3.0-dev] — Systems de population

## État

```text
Build : réussi
Tests : 122 / 122 réussis
Échecs : 0
```

Cette phase introduit les premières briques concrètes de population au-dessus du Kernel et du pipeline d'Actions.

---

## Ajout — RelationComponent

Ajout de la représentation des relations sociales d'une Entity.

Une relation peut contenir notamment :

- cible ;
- type ;
- Force ;
- Tick de création ;
- Épisodes significatifs.

Types de relations introduits :

- Familiale ;
- Amicale ;
- Professionnelle ;
- Commerciale ;
- Politique ;
- Conflictuelle ;
- Sentimentale.

---

## Ajout — RelationSystem

Ajout du System responsable de l'évolution des relations.

Fonctionnalités :

- érosion naturelle ;
- plancher familial ;
- création de relation ;
- interactions positives et négatives ;
- suppression à Force `0` ;
- création d'Épisodes significatifs ;
- capacité limitée d'Épisodes ;
- éviction du plus ancien.

---

## Correction — Plancher familial

Correction d'un bug logique dans lequel une relation Familiale passée sous son plancher à la suite d'une interaction négative pouvait être remontée artificiellement au plancher par l'érosion du Tick suivant.

Ancien comportement incorrect :

```text
Plancher = 10
Force après interaction = 5

Tick suivant
↓
Force = 10
```

Nouveau comportement :

```text
Plancher = 10
Force après interaction = 5

Tick suivant
↓
Force = 5
```

La règle devient :

```text
Force > plancher
→ érosion jusqu'au plancher

Force <= plancher
→ aucune érosion naturelle supplémentaire
→ aucune remontée artificielle
```

Une interaction négative suffisamment forte peut toujours atteindre `0` et rompre la relation Familiale.

---

## Tests — Plancher familial

Ajout des tests de non-régression :

```text
Erosion_RelationFamiliale_AuDessusDuPlancher_DescendJusquAuPlancher
```

et :

```text
Erosion_RelationFamiliale_DejaSousPlancher_NeRemontePas
```

Le nombre total de tests est alors passé de :

```text
116
```

à :

```text
118
```

avec :

```text
118 / 118 réussis
```

---

## Ajout — SkillComponent

Ajout de la représentation des Compétences d'une Entity.

Chaque Compétence contient notamment :

```text
Niveau
DernierePratique
```

---

## Ajout — SkillSystem

Ajout de la logique de Compétences.

Fonctionnalités :

- création à la première pratique ;
- progression ;
- gain marginal décroissant ;
- borne `0..100` ;
- suivi de la dernière pratique ;
- déclin après inactivité.

---

## Tests — SkillSystem

Ajout des validations concernant :

- gain maximal depuis Niveau `0` ;
- gain décroissant ;
- saturation à proximité du Niveau `100` ;
- absence de déclin avant seuil ;
- déclin après seuil ;
- application de `SkillPracticeEffect`.

---

## Ajout — HeritageSystem

Ajout du premier système minimal de transmission.

`HeritageSystem` :

- inspecte directement le `Lifecycle` ;
- détecte les Entities à l'état `"mort"` ;
- ne lit jamais `World.Events` pour décider d'agir ;
- désigne un héritier ;
- évite tout retraitement d'une Entity morte ;
- publie des événements observables.

---

## Ajout — Algorithme de désignation

Ordre déterministe :

```text
Relations Familiales prioritaires
↓
Force la plus élevée
↓
égalité
↓
relation la plus ancienne
```

En l'absence de successeur valide :

```text
heritage.absence-successeur
```

est publié dans le journal observable.

---

## Ajout — Effects de population

Ajout de :

```text
RelationInteractionEffect
SkillPracticeEffect
HeritageRefusalEffect
```

Ces Effects constituent des données décrivant les conséquences produites par des Actions.

---

## Ajout — PopulationEffectApplicator

Introduction d'un résolveur spécialisé pour les Effects de population.

Flux :

```text
Action
↓
Effect
↓
PopulationEffectApplicator
↓
System responsable
↓
World
```

Dispatch actuel :

```text
RelationInteractionEffect
→ RelationSystem
```

```text
SkillPracticeEffect
→ SkillSystem
```

```text
HeritageRefusalEffect
→ HeritageSystem
```

Le pipeline d'Actions ne dépend donc pas directement des Systems de population.

---

## Correction — Refus d'héritage

La première implémentation faisait porter une partie de la logique du refus directement par `PopulationEffectApplicator`.

Cette responsabilité a été corrigée.

Nouveau flux :

```text
HeritageRefusalEffect
↓
PopulationEffectApplicator
↓
HeritageSystem.RefuserHeritage
↓
World.Publish
```

`HeritageSystem` constitue désormais l'unique source de vérité pour cette logique.

---

## Tests — Refus d'héritage

Ajout de tests concernant :

- refus direct via `HeritageSystem` ;
- héritier inexistant ;
- défunt inexistant ;
- dispatch de `HeritageRefusalEffect` vers `HeritageSystem`.

Après ces tests :

```text
122 / 122 tests réussis
```

---

## Différé — Transmission incomplète

La transmission incomplète prévue conceptuellement par GDB-004J n'est pas implémentée.

Elle nécessite encore une représentation explicite de :

- patrimoine ;
- éléments transmissibles ;
- redistribution.

Elle reste volontairement différée plutôt que simulée avec des structures prématurées.

---

# [0.2.0] — Action Pipeline et infrastructure de simulation

## Ajout — Action Pipeline

Introduction des premières briques permettant de passer d'une intention à une action résolue.

Architecture :

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
```

Cette architecture traduit progressivement les contrats de la bibliothèque ACT dans le moteur.

---

## Ajout — Intent

Introduction de la représentation d'une intention d'acteur.

Un Intent exprime un objectif et non sa méthode d'exécution.

---

## Ajout — Planner

Ajout d'une première infrastructure de planification.

Le Planner traduit un Intent réalisable en Plan.

---

## Ajout — Plan

Introduction d'une représentation structurée du chemin d'exécution d'un Intent.

---

## Ajout — ActionDefinition / ActionInstance

Séparation entre :

```text
définition d'une Action
```

et :

```text
instance concrète d'exécution
```

afin de permettre plusieurs exécutions contextualisées d'une même Action.

---

## Ajout — Outcome

Ajout d'une représentation explicite du résultat d'une Action.

Cette couche prépare la séparation entre résolution d'une Action et application de ses conséquences.

---

## Ajout — Scheduler

Ajout de l'orchestrateur temporel des Systems.

Le Scheduler garantit :

- ordre déterministe ;
- progression du Tick ;
- exécution reproductible.

---

## Ajout — AgingSystem

Ajout de la progression d'âge et des premières transitions de Lifecycle.

---

## Ajout — NeedsDecaySystem

Ajout de l'évolution automatique des besoins au fil du temps.

---

## Ajout — CalendrierSimule

Ajout de la traduction du Tick en informations temporelles de simulation.

---

## Ajout — Tests

Couverture étendue pour :

- Scheduler ;
- AgingSystem ;
- NeedsDecaySystem ;
- calendrier ;
- Action Pipeline ;
- déterminisme.

---

# [0.1.0] — Fondations du moteur

## Ajout — Kernel

Création des premières primitives du moteur.

Comprend notamment :

- `Entity` ;
- `World` ;
- `Tick` ;
- `Value` ;
- `State` ;
- `Relation` ;
- `Lifecycle`.

---

## Ajout — Entity / Components

Introduction de la composition des Entities à partir de Components.

Le modèle repose sur :

```text
Entity
+
Components
+
Systems
```

plutôt que sur des objets métier fortement couplés.

---

## Ajout — Tick

Introduction du temps discret de simulation.

---

## Ajout — Lifecycle

Introduction du suivi de l'évolution d'état des Entities.

---

## Ajout — DeterministicRandom

Introduction d'un générateur aléatoire déterministe basé sur une seed.

Objectif :

```text
même seed
+
mêmes opérations
=
mêmes résultats
```

---

## Ajout — World.Events

Création du journal d'événements observable du World.

Le journal n'est pas conçu comme un EventBus Publish/Subscribe.

---

## Ajout — Persistence

Ajout des premières capacités de sauvegarde et de restauration du World.

---

## Ajout — Tests fondamentaux

Tests initiaux sur :

- Entity ;
- Components ;
- Tick ;
- Lifecycle ;
- RNG ;
- événements ;
- sérialisation ;
- restauration.

---

# Convention de validation

Une entrée de ce CHANGELOG correspondant à une fonctionnalité implémentée doit idéalement avoir parcouru :

```text
Spécification
↓
Implémentation
↓
Build
↓
Tests
↓
Validation
```

État courant du moteur :

```text
dotnet build
✅ succès

dotnet test
✅ 122 / 122
```

---

# Références documentaires

Les règles décrites dans ce CHANGELOG ne remplacent jamais les documents du dépôt `CHRONIQUES`.

Les principales autorités correspondantes sont actuellement :

```text
ENGINE-000
→ principes d'architecture

ENGINE-001
→ journal d'événements du World

ENGINE-002
→ Kernel

ENGINE-003
→ Scheduler et boucle de simulation

ENGINE-004
→ Systems

ENGINE-005
→ Persistence / Serialization

ENGINE-006
→ Action Pipeline

ENGINE-008
→ Relations / Compétences / Héritage
```

En cas de divergence :

```text
document d'autorité validé
>
CHANGELOG
```

Le CHANGELOG décrit l'histoire de l'implémentation ; il n'introduit pas de nouvelle règle métier.
