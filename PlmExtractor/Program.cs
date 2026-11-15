// PLM Extractor (C#)
// Lit les exports PLM, applique des filtres, calcule des indicateurs par article
// et produit un rapport CSV prêt pour Excel / Power BI.
//
// Usage : dotnet run -- <dossier_donnees> [--site Annecy] [--statut Validé] [--sortie rapport.csv]

using System.Globalization;
using System.Text;

namespace PlmExtractor;

public record Article(string Code, string Designation, string Famille, string Statut, string Site, string Responsable);

public record ChangeOrder(string Id, string CodeArticle, string Priorite, DateTime Ouverture, DateTime? Cloture, bool EnRetard);

public record LigneRapport(Article Article, int NbEco, int NbOuvertes, int NbRetard, double? DelaiMoyen)
{
    public double TauxRetard => NbEco == 0 ? 0 : 100.0 * NbRetard / NbEco;
}

public static class CsvReader
{
    // Lit un CSV séparé par ';' et renvoie une liste de dictionnaires colonne -> valeur
    public static List<Dictionary<string, string>> Read(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var headers = lines[0].TrimStart('﻿').Split(';');
        return lines.Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l =>
            {
                var values = l.Split(';');
                return headers.Select((h, i) => (h, v: i < values.Length ? values[i] : ""))
                              .ToDictionary(x => x.h, x => x.v);
            })
            .ToList();
    }

    public static DateTime? ParseDate(string s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage : dotnet run -- <dossier_donnees> [--site X] [--statut X] [--sortie fichier.csv]");
            return 1;
        }

        string dossier = args[0];
        string? site = Option(args, "--site");
        string? statut = Option(args, "--statut");
        string sortie = Option(args, "--sortie") ?? "rapport_plm.csv";

        // 1. Extraction
        var articles = CsvReader.Read(Path.Combine(dossier, "articles_clean.csv"))
            .Select(r => new Article(r["code_article"], r["designation"], r["famille"], r["statut"], r["site"], r["responsable"]))
            .ToList();

        var ecos = CsvReader.Read(Path.Combine(dossier, "change_orders_clean.csv"))
            .Select(r => new ChangeOrder(
                r["id_eco"], r["code_article"], r["priorite"],
                CsvReader.ParseDate(r["date_ouverture"]) ?? DateTime.MinValue,
                CsvReader.ParseDate(r["date_cloture"]),
                r["en_retard"] == "1"))
            .ToList();

        Console.WriteLine($"[extraction] {articles.Count} articles, {ecos.Count} demandes de modification");

        // 2. Filtres
        var filtres = articles
            .Where(a => site is null || a.Site.Equals(site, StringComparison.OrdinalIgnoreCase))
            .Where(a => statut is null || a.Statut.Equals(statut, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Console.WriteLine($"[filtre] site={site ?? "tous"}, statut={statut ?? "tous"} -> {filtres.Count} articles");

        // 3. Calcul des indicateurs par article
        var ecosParArticle = ecos.ToLookup(e => e.CodeArticle);
        var rapport = filtres.Select(a =>
        {
            var liste = ecosParArticle[a.Code].ToList();
            var delais = liste.Where(e => e.Cloture.HasValue)
                              .Select(e => (e.Cloture!.Value - e.Ouverture).TotalDays)
                              .ToList();
            return new LigneRapport(
                a,
                liste.Count,
                liste.Count(e => e.Cloture is null),
                liste.Count(e => e.EnRetard),
                delais.Count > 0 ? Math.Round(delais.Average(), 1) : null);
        })
        .OrderByDescending(l => l.NbRetard)
        .ThenByDescending(l => l.NbEco)
        .ToList();

        // 4. Export CSV
        var sb = new StringBuilder("code_article;designation;famille;statut;site;responsable;nb_eco;nb_ouvertes;nb_retard;taux_retard_pct;delai_moyen_jours\n");
        foreach (var l in rapport)
        {
            sb.AppendLine(string.Join(';',
                l.Article.Code, l.Article.Designation, l.Article.Famille, l.Article.Statut, l.Article.Site, l.Article.Responsable,
                l.NbEco, l.NbOuvertes, l.NbRetard,
                l.TauxRetard.ToString("0.0", CultureInfo.InvariantCulture),
                l.DelaiMoyen?.ToString("0.0", CultureInfo.InvariantCulture) ?? ""));
        }
        File.WriteAllText(sortie, sb.ToString(), new UTF8Encoding(true));

        // 5. Synthèse console
        int totalEco = rapport.Sum(l => l.NbEco);
        int totalRetard = rapport.Sum(l => l.NbRetard);
        Console.WriteLine();
        Console.WriteLine("=== Synthèse ===");
        Console.WriteLine($"Demandes de modification : {totalEco}");
        Console.WriteLine($"En retard                : {totalRetard} ({(totalEco == 0 ? 0 : 100.0 * totalRetard / totalEco):0.0} %)");
        Console.WriteLine("Top 5 des articles à surveiller :");
        foreach (var l in rapport.Take(5))
            Console.WriteLine($"  {l.Article.Code}  {l.Article.Designation,-28} {l.NbRetard} retard(s) / {l.NbEco} ECO");
        Console.WriteLine($"\nRapport exporté : {Path.GetFullPath(sortie)}");
        return 0;
    }

    private static string? Option(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
