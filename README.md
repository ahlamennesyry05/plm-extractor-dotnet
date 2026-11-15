# ⚙️ PLM Extractor – Scripts d'extraction de données en C# et VB.NET

Deux applications console **.NET** qui extraient les données d'un système **PLM**, appliquent des filtres métier, calculent des **indicateurs** et produisent des **rapports CSV** prêts pour Excel ou Power BI.

> En entreprise, les données PLM sont souvent extraites par des scripts **.NET** connectés à l'API ou aux exports de l'outil PLM. Ce projet reproduit ce cas d'usage sur des données fictives.

## 🧩 Contenu

| Projet | Langage | Rôle |
|---|---|---|
| `PlmExtractor` | **C#** | Rapport par article : nb de demandes de modification (ECO), ECO ouvertes, retards, délai moyen. Filtres par **site** et **statut**. |
| `PlmExtractorVB` | **VB.NET** | Liste des ECO **en retard**, triées par priorité, avec synthèse console. |

## ▶️ Lancer

Prérequis : [.NET 8 SDK](https://dotnet.microsoft.com/download) et les CSV produits par le projet **plm-data-pipeline** (`output/`).

```bash
# C# – rapport des articles du site d'Annecy
cd PlmExtractor
dotnet run -- ../../plm-data-pipeline/output --site Annecy --sortie rapport_annecy.csv

# VB.NET – liste des demandes de modification en retard
cd ../PlmExtractorVB
dotnet run -- ../../plm-data-pipeline/output --sortie eco_en_retard.csv
```

Exemple de sortie (C#) :

```
[extraction] 300 articles, 900 demandes de modification
[filtre] site=Annecy, statut=tous -> 76 articles

=== Synthèse ===
Demandes de modification : 226
En retard                : 69 (30.5 %)
Top 5 des articles à surveiller :
  PN-10042  Bague type G40               3 retard(s) / 4 ECO
  ...
```

## 🧠 Notions utilisées

- Lecture et parsing de fichiers CSV
- `record`, LINQ (`Where`, `Select`, `ToLookup`, `GroupBy`, `OrderBy`)
- Gestion des dates et des valeurs manquantes (`DateTime?`)
- Arguments en ligne de commande
- Export CSV en UTF-8 (compatible Excel)

## 🛠️ Technologies

C# · VB.NET · .NET 8 · LINQ · Extraction de données · CSV
