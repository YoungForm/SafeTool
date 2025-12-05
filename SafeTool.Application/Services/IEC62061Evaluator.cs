using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public class IEC62061Evaluator
{
    public (IEC62061EvaluationResult result, SafetyFunction62061 input) Evaluate(SafetyFunction62061 input)
    {
        var pfhd = IEC62061Calculator.TotalPFHd(input);
        var achieved = IEC62061Calculator.AchievedSIL(pfhd);
        var warnings = IEC62061Calculator.ConsistencyWarnings(input).ToList();

        // 增强T1/T10D一致性校核
        if (input.ProofTestIntervalT1.HasValue && input.MissionTimeT10D.HasValue)
        {
            var t1 = input.ProofTestIntervalT1.Value;
            var t10d = input.MissionTimeT10D.Value;
            
            if (t1 > t10d)
            {
                warnings.Add($"⚠️ 严重：T1（{t1}小时）大于T10D（{t10d}小时），证明试验间隔超过有用寿命，存在严重超期风险");
                warnings.Add("建议：缩短证明试验间隔或延长有用寿命，否则PFHd计算可能失真");
            }
            else if (t1 > t10d * 0.8)
            {
                warnings.Add($"⚠️ 警告：T1（{t1}小时）接近T10D（{t10d}小时），建议缩短证明试验间隔以确保安全裕量");
            }

            // 检查证明试验覆盖率
            if (t1 > 0 && t10d > 0)
            {
                var coverageRatio = t1 / t10d;
                if (coverageRatio > 0.5)
                {
                    warnings.Add($"证明试验间隔占比 {coverageRatio:P0}，建议覆盖率应小于50%以确保有效性");
                }
            }
        }

        // 检查使用寿命超期风险
        if (input.MissionTimeT10D.HasValue)
        {
            var t10d = input.MissionTimeT10D.Value;
            var typicalLifetime = 87600; // 10年，约87600小时
            
            if (t10d > typicalLifetime * 1.5)
            {
                warnings.Add($"⚠️ 警告：T10D（{t10d}小时，约{t10d / 8760:F1}年）超过典型使用寿命，需确认设备实际寿命与维护策略");
            }
        }

        return (new IEC62061EvaluationResult
        {
            PFHd = pfhd,
            AchievedSIL = achieved,
            Warnings = warnings
        }, input);
    }
}

