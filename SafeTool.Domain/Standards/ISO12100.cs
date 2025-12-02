namespace SafeTool.Domain.Standards;

public enum SeverityLevel { Negligible = 1, Minor = 2, Serious = 3, Critical = 4 }
public enum FrequencyLevel { Rare = 1, Occasional = 2, Frequent = 3, Continuous = 4 }
public enum AvoidanceLevel { Easy = 1, Possible = 2, Difficult = 3, Impossible = 4 }

public class ISO12100Assessment
{
    public List<string> IdentifiedHazards { get; set; } = new();
    public SeverityLevel Severity { get; set; } = SeverityLevel.Minor;
    public FrequencyLevel Frequency { get; set; } = FrequencyLevel.Occasional;
    public AvoidanceLevel Avoidance { get; set; } = AvoidanceLevel.Possible;
    public string RiskReductionMeasures { get; set; } = string.Empty;
}

public static class ISO12100Risk
{
    public static int RiskScore(SeverityLevel s, FrequencyLevel f, AvoidanceLevel a)
    {
        return (int)s * (int)f * (int)a;
    }

    public static string RiskLevel(int score)
    {
        if (score <= 6) return "Low";
        if (score <= 24) return "Medium";
        if (score <= 36) return "High";
        return "Extreme";
    }
}

