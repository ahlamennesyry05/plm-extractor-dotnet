' PLM Extractor (VB.NET)
' Version VB.NET : liste les demandes de modification (ECO) en retard
' et exporte un CSV de suivi.
'
' Usage : dotnet run -- <dossier_donnees> [--sortie eco_en_retard.csv]

Imports System.Globalization
Imports System.IO
Imports System.Text

Module Program

    Function LireCsv(chemin As String) As List(Of Dictionary(Of String, String))
        Dim lignes = File.ReadAllLines(chemin, Encoding.UTF8)
        Dim entetes = lignes(0).TrimStart(ChrW(&HFEFF)).Split(";"c)
        Dim resultat As New List(Of Dictionary(Of String, String))
        For Each ligne In lignes.Skip(1)
            If String.IsNullOrWhiteSpace(ligne) Then Continue For
            Dim valeurs = ligne.Split(";"c)
            Dim d As New Dictionary(Of String, String)
            For i = 0 To entetes.Length - 1
                d(entetes(i)) = If(i < valeurs.Length, valeurs(i), "")
            Next
            resultat.Add(d)
        Next
        Return resultat
    End Function

    Function Main(args As String()) As Integer
        If args.Length = 0 Then
            Console.WriteLine("Usage : dotnet run -- <dossier_donnees> [--sortie fichier.csv]")
            Return 1
        End If

        Dim dossier = args(0)
        Dim idx = Array.IndexOf(args, "--sortie")
        Dim sortie = If(idx >= 0 AndAlso idx + 1 < args.Length, args(idx + 1), "eco_en_retard.csv")

        ' 1. Extraction
        Dim articles = LireCsv(Path.Combine(dossier, "articles_clean.csv")).
            ToDictionary(Function(a) a("code_article"))
        Dim ecos = LireCsv(Path.Combine(dossier, "change_orders_clean.csv"))

        ' 2. Filtre : ECO en retard, triées par priorité
        Dim ordre As New Dictionary(Of String, Integer) From {
            {"Critique", 0}, {"Haute", 1}, {"Moyenne", 2}, {"Basse", 3}}

        Dim enRetard = ecos.
            Where(Function(e) e("en_retard") = "1").
            OrderBy(Function(e) If(ordre.ContainsKey(e("priorite")), ordre(e("priorite")), 9)).
            ThenBy(Function(e) e("date_echeance")).
            ToList()

        ' 3. Export
        Dim sb As New StringBuilder("id_eco;code_article;designation;responsable;priorite;statut;date_echeance;date_cloture" & vbLf)
        For Each e In enRetard
            Dim art = articles(e("code_article"))
            sb.AppendLine(String.Join(";", e("id_eco"), e("code_article"), art("designation"), art("responsable"),
                                      e("priorite"), e("statut"), e("date_echeance"), e("date_cloture")))
        Next
        File.WriteAllText(sortie, sb.ToString(), New UTF8Encoding(True))

        ' 4. Synthèse par priorité
        Console.WriteLine($"[extraction] {ecos.Count} ECO lues, {enRetard.Count} en retard")
        For Each g In enRetard.GroupBy(Function(e) e("priorite")).OrderBy(Function(g2) If(ordre.ContainsKey(g2.Key), ordre(g2.Key), 9))
            Console.WriteLine($"  {g.Key,-10} {g.Count()} en retard")
        Next
        Console.WriteLine($"Rapport exporté : {Path.GetFullPath(sortie)}")
        Return 0
    End Function

End Module
