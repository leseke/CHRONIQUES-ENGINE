Documentation technique

Statut : ActifDépôt : CHRONIQUES-ENGINESource documentaire canonique : dépôt CHRONIQUES, bibliothèque TECH

Objectif

Ce dossier sert de point d'entrée documentaire depuis le dépôt moteur.

La documentation TECH canonique n'est pas définie par anticipation dans CHRONIQUES-ENGINE.

Elle est produite à partir :

Spécification validée
↓
Code réellement implémenté
↓
Build
↓
Tests
↓
Validation
↓
TECH

Les documents TECH de référence vivent dans le dépôt documentaire :

CHRONIQUES/TECH/

État actuel

La bibliothèque TECH n'est plus vide.

Le premier document numéroté est :

TECH-001 — Systems de population

Il documente l'implémentation correspondant à :

ENGINE-008 — Systems de population

et couvre notamment :

RelationComponent ;

RelationSystem ;

SkillComponent ;

SkillSystem ;

HeritageSystem ;

RelationInteractionEffect ;

SkillPracticeEffect ;

HeritageRefusalEffect ;

PopulationEffectApplicator.

État de validation associé :

dotnet build
→ succès

dotnet test
→ 122 / 122 tests réussis
→ 0 échec

Sources d'autorité

La chaîne documentaire actuelle est :

MASTER / CORE / GDB / ACT
↓
ENGINE
↓
CHRONIQUES-ENGINE
↓
Tests
↓
TECH

Pour le lot de population :

GDB-004C / GDB-004H / GDB-004J
↓
ENGINE-008
↓
Code de Simulation/
↓
Tests/
↓
TECH-001

Règle

Ce dossier ne doit pas devenir une seconde source de vérité concurrente de CHRONIQUES/TECH.

Il peut contenir :

des pointeurs vers la documentation canonique ;

des notes spécifiques au dépôt moteur ;

des instructions de navigation technique.

Il ne doit pas recopier intégralement les documents TECH sauf besoin explicite de distribution autonome du dépôt moteur.

Documentation future

Lorsqu'un nouveau lot du moteur est implémenté et validé :

ENGINE
↓
Code
↓
Tests
↓
TECH

le document correspondant doit être créé ou mis à jour dans :

CHRONIQUES/TECH/

Ce README est ensuite mis à jour uniquement si l'état général de la documentation technique du moteur change.
