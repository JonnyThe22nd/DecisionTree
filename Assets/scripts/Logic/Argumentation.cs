using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ArgumentationKind
{
    Observation,
    BeliefAdoption,
    InferenceMaking,
    ProvenanceAssessment
}

public enum BeliefValue
{
    Refuted = 0,
    Unknown = 1,
    Uncertain = 2,
    Probable = 3,
    Certain = 4
}

public static class ArgumentationInfo
{
    public static readonly ArgumentationKind[] Kinds =
    {
        ArgumentationKind.Observation,
        ArgumentationKind.BeliefAdoption,
        ArgumentationKind.InferenceMaking,
        ArgumentationKind.ProvenanceAssessment
    };

    public static readonly BeliefValue[] Beliefs =
    {
        BeliefValue.Certain,
        BeliefValue.Probable,
        BeliefValue.Uncertain,
        BeliefValue.Unknown,
        BeliefValue.Refuted
    };

    public static string Name(ArgumentationKind kind)
    {
        switch (kind)
        {
            case ArgumentationKind.Observation: return "Beobachtung";
            case ArgumentationKind.BeliefAdoption: return "Übernahme aus Quelle";
            case ArgumentationKind.InferenceMaking: return "Schlussfolgerung";
            default: return "Provenienzbewertung";
        }
    }

    public static string CrmClass(ArgumentationKind kind)
    {
        switch (kind)
        {
            case ArgumentationKind.Observation: return "S27 Observation";
            case ArgumentationKind.BeliefAdoption: return "I7 Belief Adoption";
            case ArgumentationKind.InferenceMaking: return "I5 Inference Making";
            default: return "I15 Provenance Assessment";
        }
    }

    public static string Letter(ArgumentationKind kind)
    {
        switch (kind)
        {
            case ArgumentationKind.Observation: return "B";
            case ArgumentationKind.BeliefAdoption: return "Ü";
            case ArgumentationKind.InferenceMaking: return "F";
            default: return "P";
        }
    }

    public static Color Color(ArgumentationKind kind)
    {
        switch (kind)
        {
            case ArgumentationKind.Observation: return new Color(0.35f, 0.75f, 0.45f);
            case ArgumentationKind.BeliefAdoption: return new Color(0.35f, 0.6f, 0.95f);
            case ArgumentationKind.InferenceMaking: return new Color(0.95f, 0.65f, 0.25f);
            default: return new Color(0.7f, 0.5f, 0.9f);
        }
    }

    public static string Name(BeliefValue belief)
    {
        switch (belief)
        {
            case BeliefValue.Certain: return "gesichert";
            case BeliefValue.Probable: return "wahrscheinlich";
            case BeliefValue.Uncertain: return "unsicher";
            case BeliefValue.Unknown: return "unbekannt";
            default: return "widerlegt";
        }
    }

    public static Color Color(BeliefValue belief)
    {
        switch (belief)
        {
            case BeliefValue.Certain: return new Color(0.3f, 0.75f, 0.4f);
            case BeliefValue.Probable: return new Color(0.6f, 0.8f, 0.3f);
            case BeliefValue.Uncertain: return new Color(0.95f, 0.8f, 0.25f);
            case BeliefValue.Unknown: return new Color(0.6f, 0.6f, 0.6f);
            default: return new Color(0.85f, 0.3f, 0.3f);
        }
    }

    public static string Describe(MenuNode node)
    {
        var sb = new StringBuilder();

        if (node.IsRoot)
        {
            sb.Append("Baum angelegt von ").Append(Author(node)).Append(" am ").Append(node.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
            return sb.ToString();
        }

        sb.Append("Grundlage: ").Append(Name(node.Kind)).Append(" (CRMinf ").Append(CrmClass(node.Kind)).AppendLine(")");
        sb.Append("Sicherheit: ").AppendLine(Name(node.Belief));
        if (!string.IsNullOrEmpty(node.Reasoning))
            sb.Append("Begründung: ").AppendLine(node.Reasoning);

        var path = node.GetPath();
        var labels = new List<string>();
        foreach (var step in path) labels.Add(step.Label);
        sb.Append("Pfad: ").AppendLine(string.Join(" › ", labels));

        if (node.Depth >= 2)
            sb.Append("Schwächste Sicherheit im Pfad: ").AppendLine(Name(node.WeakestBelief()));

        if (!string.IsNullOrEmpty(node.ModelFile))
            sb.Append("Modell: ").AppendLine(System.IO.Path.GetFileName(node.ModelFile));

        sb.Append("Angelegt von ").Append(Author(node)).Append(" am ").Append(node.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
        return sb.ToString();
    }

    static string Author(MenuNode node)
    {
        return string.IsNullOrEmpty(node.Author) ? "unbekannt" : node.Author;
    }
}
