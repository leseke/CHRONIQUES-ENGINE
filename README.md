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

## État actuel --- v0.1, Le noyau

Critère de sortie (MASTER-005) : *« le noyau tourne, tous les tests de lois
passent, un World vide se sauvegarde et se recharge à l'identique. »*

Implémenté :

- les neuf primitives du Kernel (`Simulation/Chroniques.Simulation/Kernel/`) :
  `Entity`, `IComponent`, `Value<T>`, `State`, `Relation`, `GameEvent`,
  `Tick` (Time), `SpaceRef` (Space), `Lifecycle` ;
- un `World` déterministe (`DeterministicRandom` à graine) ;
- la sauvegarde/rechargement JSON (`Kernel/Persistence/WorldRepository.cs`) ;
- un test par invariant du Kernel (`Tests/Chroniques.Simulation.Tests/`).

Pas encore implémenté, volontairement --- appartient à v0.2 et au-delà
(MASTER-005) :

- aucun Component métier concret (besoins, santé, compétences) ;
- aucun Scheduler ni System exécutant une logique de jeu ;
- la couche `Rendering/` (Godot) ;
- la sérialisation polymorphe des Components (n'a pas de sens tant qu'aucun
  Component concret n'existe).

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
