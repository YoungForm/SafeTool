using SafeTool.Domain.Standards;

namespace SafeTool.Application.Services;

public class IEC62061Evaluator
{
    public (IEC62061EvaluationResult result, SafetyFunction62061 input) Evaluate(SafetyFunction62061 input)
    {
        var pfhd = IEC62061Calculator.TotalPFHd(input);
        var achieved = IEC62061Calculator.AchievedSIL(pfhd);
        var warnings = IEC62061Calculator.ConsistencyWarnings(input).ToList();

        return (new IEC62061EvaluationResult
        {
            PFHd = pfhd,
            AchievedSIL = achieved,
            Warnings = warnings
        }, input);
    }
}

