# Chroniques --- Moteur

> *Chaque vie raconte une Chronique.*

Ce dépôt contient le **code** du moteur de Chroniques. Il est distinct du
dépôt de **documentation** (`chroniques--main`), qui reste l'unique source
de vérité pour la conception : vision, principes, spécifications GDB et ACT,
architecture, ADR.

Toute divergence entre ce code et la documentation ouvre un constat, selon
la règle de divergence de MASTER-004 : soit la conception était fausse et le
document est corrigé, soit l'implémentation s'est écartée et c'est le code
qui est corrigé --- jamais un arbitrage silencieux.

---

## Où trouver quoi

| Question | Réponse |
|---|---|
| Pourquoi ce projet existe | `chroniques--main/MASTER/MASTER-001.md` |
| Quelles sont les primitives du Kernel | `chroniques--main/CORE/CORE-000` à `CORE-010` |
| Quel est le pipeline d'une action | `chroniques--main/ACT/ACT-002` |
| Quel langage, quel moteur de rendu, pourquoi | `chroniques--main/ADR/ADR-002.md` |
| Dans quel ordre construire | `chroniques--main/MASTER/MASTER-005.md` et `PROD/FeuilleDeRoute.md` |

---

## Structure

```
Chroniques-Engine/
├── Simulation/     C# pur, aucune dépendance graphique (ADR-002)
├── Content/        données externes, aucune donnée codée en dur
├── Rendering/      adaptateur Godot --- lit l'état, l'affiche
├── Tools/          éditeur, débogueur
├── Documentation/  documents TECH, écrits uniquement à partir du code existant
└── Tests/          un test par loi du Kernel (MASTER-005, critère de sortie v0.1)
```

Cette structure reprend exactement celle décrite par `PROD/FeuilleDeRoute.md`.
`Content/`, `Rendering/`, `Tools/` et `Documentation/` sont volontairement
vides à ce stade : ils ne seront remplis qu'au moment où une phase réelle de
MASTER-005 l'exigera, jamais par anticipation (MASTER-006).

---

## État actuel --- v0.2, Un être vivant (cycle de vie ajouté, à valider)

Critère de sortie v0.1 (atteint et vérifié) : *« le noyau tourne, tous les
tests de lois passent, un World vide se sauvegarde et se recharge à
l'identique. »*

Critère de sortie v0.2 (MASTER-005) : *« un personnage naît, vit ses
besoins année après année, et meurt. Tout est observable sans aucun
rendu. »*

Implémenté :

- les neuf primitives du Kernel (`Simulation/Chroniques.Simulation/Kernel/`) :
  `Entity`, `IComponent`, `Value<T>`, `State`, `Relation`, `GameEvent`,
  `Tick` (Time), `SpaceRef` (Space), `Lifecycle` ;
- un `World` déterministe (`DeterministicRandom` à graine) ;
- `Entity` ↔ `Lifecycle`, désormais reliés dès la création (CORE-002-H :
  « une Entity possède un Lifecycle ») --- écart comblé en v0.2, la
  primitive existait depuis v0.1 sans qu'aucune Entity n'y soit rattachée ;
- `Components/NeedsComponent.cs` --- premier Component métier (GDB-004B) :
  faim, fatigue, santé, moral ;
- `Components/AgeComponent.cs` --- second Component métier (GDB-008C) :
  l'âge en années simulées ;
- `Systems/` --- `ISystem`, `Scheduler` (Tick + invocation ordonnée des
  Systems), `NeedsDecaySystem` (déclin des besoins par Tick),
  `AgingSystem` (cycle de vie complet : enfance → adolescence → âge
  adulte → maturité → vieillesse → mort, avec Event `vie.mort` publié sur
  le World à la mort) ;
- `Persistence/` --- sauvegarde/rechargement JSON, capable de sérialiser
  `NeedsComponent`, `AgeComponent`, et l'état courant du `Lifecycle`
  (approche explicite, pas encore générique --- voir le commentaire XML de
  `WorldSnapshot.cs`) ;
- un test par invariant du Kernel, par règle de `NeedsDecaySystem`/
  `Scheduler`/`AgingSystem`, et par cas de rechargement
  (`Tests/Chroniques.Simulation.Tests/`).

**Modèle de temps désormais tranché** (décision d'équipe, à formaliser par
un ADR avant la v0.3, GDB-008A ne fixant aucune conversion Tick →
calendrier) : 3 Ticks par saison, 4 saisons par an, soit 12 Ticks par
année simulée. `AgingSystem` n'incrémente donc l'âge d'un habitant qu'un
Tick sur douze, pas à chaque Tick.

**Une hypothèse de travail reste non tranchée par la documentation**,
introduite par `AgingSystem` et documentée dans son commentaire XML --- à
confirmer ou corriger par un ADR avant la v0.3 :

- Les seuils d'âge (adolescence, âge adulte, maturité, vieillesse) et
  l'espérance de vie --- GDB-008C nomme les étapes de vie sans fixer les
  âges de bascule ; aucun document ne fixe d'espérance de vie. Ces
  valeurs sont des paramètres du constructeur d'`AgingSystem` (mêmes
  valeurs par défaut que le code, à ne pas recopier ailleurs comme si
  elles étaient officielles).

Pas encore implémenté, volontairement --- appartient à v0.3 (MASTER-005) :

- les actions du joueur (moteur ACT : Intent → Plan → Action → Outcome) ;
- la transmission de lignée à la mort ;
- la couche `Rendering/` (Godot) ;
- l'historique complet du Lifecycle à travers la persistance (seul l'état
  courant survit au rechargement pour l'instant --- voir le commentaire XML
  d'`Entity.Restore`) ;
- une sérialisation générique des Components (n'a de sens que lorsque leur
  nombre rendra la liste explicite actuelle pénible à maintenir).

---

## Compiler et tester

```bash
dotnet build
dotnet test
```

Prérequis : SDK .NET 10 (LTS). Le projet `Rendering/` (à venir) devra vérifier
la version de .NET supportée par la version de Godot retenue au moment de sa
création --- ADR-002 ne fixe que Godot comme moteur de rendu, pas sa version.

---

## Convention de code

Aucune convention de code formelle n'existe encore (namespaces, style,
organisation des fichiers). Une telle convention sera écrite au moment où
un premier désaccord réel se présentera, pas avant --- conformément à
MASTER-006 (« une décision qui ne peut être tranchée est explicitement
ajournée ») et au principe directeur de MASTER-004 (« une règle n'existe que
si elle fait économiser plus d'efforts qu'elle n'en demande »).

En attendant, ce dépôt suit les conventions .NET standard (PascalCase pour
les types et méthodes publiques, un fichier par type public, dossiers
reflétant les namespaces).
