using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

/// <summary>
/// PL↔SIL 对照服务，提供 ISO 13849-1 与 IEC 62061 之间的映射关系
/// </summary>
public class PlSilMappingService
{
    /// <summary>
    /// PL 到 SIL 的区间映射
    /// </summary>
    public static SafetyIntegrityLevel[] MapPlToSil(PerformanceLevel pl)
    {
        return pl switch
        {
            PerformanceLevel.PLa => new[] { SafetyIntegrityLevel.SIL1 },
            PerformanceLevel.PLb => new[] { SafetyIntegrityLevel.SIL1 },
            PerformanceLevel.PLc => new[] { SafetyIntegrityLevel.SIL1, SafetyIntegrityLevel.SIL2 },
            PerformanceLevel.PLd => new[] { SafetyIntegrityLevel.SIL2, SafetyIntegrityLevel.SIL3 },
            PerformanceLevel.PLe => new[] { SafetyIntegrityLevel.SIL3 },
            _ => Array.Empty<SafetyIntegrityLevel>()
        };
    }

    /// <summary>
    /// SIL 到 PL 的区间映射
    /// </summary>
    public static PerformanceLevel[] MapSilToPl(SafetyIntegrityLevel sil)
    {
        return sil switch
        {
            SafetyIntegrityLevel.SIL1 => new[] { PerformanceLevel.PLa, PerformanceLevel.PLb, PerformanceLevel.PLc },
            SafetyIntegrityLevel.SIL2 => new[] { PerformanceLevel.PLc, PerformanceLevel.PLd },
            SafetyIntegrityLevel.SIL3 => new[] { PerformanceLevel.PLd, PerformanceLevel.PLe },
            _ => Array.Empty<PerformanceLevel>()
        };
    }

    /// <summary>
    /// 获取对照说明与注意事项
    /// </summary>
    public static MappingNotes GetMappingNotes(PerformanceLevel? pl = null, SafetyIntegrityLevel? sil = null)
    {
        var notes = new MappingNotes
        {
            GeneralNotes = new List<string>
            {
                "PL 与 SIL 的映射关系为近似对应，实际评估需考虑具体应用场景",
                "ISO 13849-1 基于定性方法（Category/MTTFd/DCavg），IEC 62061 基于定量方法（PFHd）",
                "两种方法在假设、边界条件与不确定性方面存在差异",
                "建议在关键应用中同时使用两种方法进行验证"
            },
            BoundaryConditions = new List<string>
            {
                "映射关系基于标准指南，非强制性对应",
                "实际对应关系受系统架构、应用环境与需求率影响",
                "高需求率系统可能需要更严格的对应关系"
            },
            Recommendations = new List<string>()
        };

        if (pl.HasValue)
        {
            var mappedSils = MapPlToSil(pl.Value);
            notes.Recommendations.Add($"PL {pl.Value} 通常对应 {string.Join(" 或 ", mappedSils)}");
            if (pl.Value == PerformanceLevel.PLc)
                notes.Recommendations.Add("PLc 可能对应 SIL1 或 SIL2，需根据具体PFHd值判断");
            if (pl.Value == PerformanceLevel.PLd)
                notes.Recommendations.Add("PLd 通常对应 SIL2 或 SIL3，建议进行PFHd计算验证");
        }

        if (sil.HasValue)
        {
            var mappedPls = MapSilToPl(sil.Value);
            notes.Recommendations.Add($"SIL {sil.Value} 通常对应 {string.Join(" 或 ", mappedPls)}");
            if (sil.Value == SafetyIntegrityLevel.SIL2)
                notes.Recommendations.Add("SIL2 可能对应 PLc 或 PLd，需根据Category与DCavg判断");
        }

        return notes;
    }

    /// <summary>
    /// 检查 PL 与 SIL 的一致性
    /// </summary>
    public static ConsistencyCheckResult CheckConsistency(PerformanceLevel pl, SafetyIntegrityLevel sil)
    {
        var mappedSils = MapPlToSil(pl);
        var mappedPls = MapSilToPl(sil);
        var isConsistent = mappedSils.Contains(sil) || mappedPls.Contains(pl);

        var warnings = new List<string>();
        if (!isConsistent)
        {
            warnings.Add($"PL {pl} 与 SIL {sil} 的对应关系不一致");
            warnings.Add($"建议的对应关系：PL {pl} → {string.Join(" 或 ", mappedSils)}");
            warnings.Add($"或 SIL {sil} → {string.Join(" 或 ", mappedPls)}");
        }
        else
        {
            warnings.Add("PL 与 SIL 对应关系一致，但建议进行详细计算验证");
        }

        return new ConsistencyCheckResult
        {
            IsConsistent = isConsistent,
            Warnings = warnings,
            RecommendedActions = isConsistent
                ? new List<string> { "进行PFHd计算验证", "检查Category与DCavg是否满足要求" }
                : new List<string> { "重新评估PL或SIL目标", "进行双标准并行评估", "咨询认证机构" }
        };
    }

    public class MappingNotes
    {
        public List<string> GeneralNotes { get; set; } = new();
        public List<string> BoundaryConditions { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ConsistencyCheckResult
    {
        public bool IsConsistent { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// 执行PL↔SIL对照
    /// </summary>
    public PlSilMappingResult Map(string achievedPL, string achievedSIL)
    {
        // 实现映射逻辑
        var result = new PlSilMappingResult
        {
            AchievedPL = achievedPL,
            AchievedSIL = achievedSIL,
            IsConsistent = true, // 默认一致，实际逻辑需要根据映射规则判断
            Warnings = new List<string>(),
            Notes = new List<string>()
        };

        return result;
    }
}

public class PlSilMappingResult
{
    public string AchievedPL { get; set; } = string.Empty;
    public string AchievedSIL { get; set; } = string.Empty;
    public bool IsConsistent { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

