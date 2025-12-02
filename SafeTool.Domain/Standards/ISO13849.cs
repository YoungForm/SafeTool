namespace SafeTool.Domain.Standards;

public enum Category { B, Cat1, Cat2, Cat3, Cat4 }
public enum PerformanceLevel { PLa, PLb, PLc, PLd, PLe }

public class ISO13849Assessment
{
    public PerformanceLevel RequiredPL { get; set; } = PerformanceLevel.PLc;
    public Category Architecture { get; set; } = Category.Cat3;
    public double DCavg { get; set; } = 0.9; // Diagnostic coverage average
    public double MTTFd { get; set; } = 10e6; // Mean time to dangerous failure (hours)
    public int CCFScore { get; set; } = 65; // Common cause failure score
    public bool ValidationPerformed { get; set; }
}

public static class ISO13849Calculator
{
    public static PerformanceLevel AchievedPL(ISO13849Assessment a)
    {
        // Simplified mapping consistent with ISO 13849-1 qualitative guidance
        var mttfClass = a.MTTFd switch
        {
            < 3e6 => 0,          // Low
            < 10e6 => 1,         // Medium
            _ => 2               // High
        };

        var dcClass = a.DCavg switch
        {
            < 0.6 => 0,          // Low
            < 0.99 => 1,         // Medium
            _ => 2               // High
        };

        var archClass = a.Architecture switch
        {
            Category.B or Category.Cat1 => 0,
            Category.Cat2 => 1,
            Category.Cat3 => 2,
            Category.Cat4 => 3,
            _ => 0
        };

        var score = mttfClass + dcClass + archClass + (a.CCFScore >= 65 ? 1 : 0);

        return score switch
        {
            <= 2 => PerformanceLevel.PLb,
            3 => PerformanceLevel.PLc,
            4 => PerformanceLevel.PLd,
            _ => PerformanceLevel.PLe
        };
    }

    public static bool MeetsRequirement(ISO13849Assessment a)
    {
        var achieved = AchievedPL(a);
        return achieved >= a.RequiredPL && a.ValidationPerformed && a.CCFScore >= 65;
    }
}

