namespace SafeTool.Domain.Standards;

public enum SafetyIntegrityLevel { SIL1, SIL2, SIL3 }

public class IEC62061Component
{
    public string Id { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double PFHd { get; set; } = 1e-7;
    public double? Beta { get; set; }
}

public class IEC62061Subsystem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Architecture { get; set; } = "1oo1";
    public List<IEC62061Component> Components { get; set; } = new();
    public double PFHdCalculated { get; set; }
}

public class SafetyFunction62061
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SafetyIntegrityLevel TargetSIL { get; set; } = SafetyIntegrityLevel.SIL2;
    public List<IEC62061Subsystem> Subsystems { get; set; } = new();
    public double? ProofTestIntervalT1 { get; set; }
    public double? MissionTimeT10D { get; set; }
}

public class IEC62061EvaluationResult
{
    public double PFHd { get; set; }
    public SafetyIntegrityLevel AchievedSIL { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public static class IEC62061Calculator
{
    public static double SubsystemPFHd(IEC62061Subsystem s)
    {
        var sum = s.Components.Sum(c => c.PFHd);
        if (s.Architecture.Equals("1oo2", StringComparison.OrdinalIgnoreCase) && s.Components.Count >= 2)
        {
            var beta = s.Components.Select(c => c.Beta ?? 0.05).DefaultIfEmpty(0.05).Average();
            sum *= (0.5 * (1 - Math.Clamp(beta, 0, 1)));
        }
        else if (s.Architecture.Equals("2oo3", StringComparison.OrdinalIgnoreCase) && s.Components.Count >= 3)
        {
            var beta = s.Components.Select(c => c.Beta ?? 0.05).DefaultIfEmpty(0.05).Average();
            sum *= (0.33 * (1 - Math.Clamp(beta, 0, 1)));
        }
        else if (s.Architecture.Equals("1oo3", StringComparison.OrdinalIgnoreCase) && s.Components.Count >= 3)
        {
            var beta = s.Components.Select(c => c.Beta ?? 0.05).DefaultIfEmpty(0.05).Average();
            sum *= (0.5 * (1 - Math.Clamp(beta, 0, 1)));
        }
        else if (s.Architecture.Equals("2oo2", StringComparison.OrdinalIgnoreCase) && s.Components.Count >= 2)
        {
            var beta = s.Components.Select(c => c.Beta ?? 0.05).DefaultIfEmpty(0.05).Average();
            sum *= (0.25 * (1 - Math.Clamp(beta, 0, 1)));
        }
        s.PFHdCalculated = sum;
        return sum;
    }

    public static double TotalPFHd(SafetyFunction62061 f) => f.Subsystems.Sum(SubsystemPFHd);

    public static SafetyIntegrityLevel AchievedSIL(double pfhd)
    {
        if (pfhd < 1e-7) return SafetyIntegrityLevel.SIL3;
        if (pfhd < 1e-6) return SafetyIntegrityLevel.SIL2;
        return SafetyIntegrityLevel.SIL1;
    }

    public static IEnumerable<string> ConsistencyWarnings(SafetyFunction62061 f)
    {
        var list = new List<string>();
        if (f.ProofTestIntervalT1.HasValue && f.MissionTimeT10D.HasValue && f.ProofTestIntervalT1.Value > f.MissionTimeT10D.Value)
            list.Add("T1（证明试验间隔）大于T10D（有用寿命），存在超期风险");
        if (!f.Subsystems.Any()) list.Add("未定义任何子系统");
        foreach (var s in f.Subsystems)
        {
            if (!s.Components.Any()) list.Add($"子系统 {s.Name} 未定义组件");
        }
        return list;
    }
}
