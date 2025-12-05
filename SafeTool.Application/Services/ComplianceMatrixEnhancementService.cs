using System.Text.RegularExpressions;

namespace SafeTool.Application.Services;

/// <summary>
/// 合规映射矩阵增强服务（策略模式）
/// </summary>
public class ComplianceMatrixEnhancementService
{
    private readonly ComplianceMatrixService _matrixService;
    private readonly EvidenceService _evidenceService;
    private readonly Dictionary<string, List<string>> _standardClauses = new();

    public ComplianceMatrixEnhancementService(ComplianceMatrixService matrixService, EvidenceService evidenceService)
    {
        _matrixService = matrixService;
        _evidenceService = evidenceService;
        InitializeStandardClauses();
    }

    private void InitializeStandardClauses()
    {
        // ISO 13849-1 标准条款
        _standardClauses["ISO13849-1"] = new List<string>
        {
            "4.1", "4.2", "4.3", "4.4", "5.1", "5.2", "6.1", "6.2", "6.3", "6.4",
            "Annex A", "Annex B", "Annex C", "Annex D", "Annex E", "Annex F", "Annex K"
        };

        // IEC 62061 标准条款
        _standardClauses["IEC62061"] = new List<string>
        {
            "4.1", "4.2", "4.3", "5.1", "5.2", "6.1", "6.2", "7.1", "7.2",
            "Annex A", "Annex B", "Annex C"
        };

        // ISO 12100 标准条款
        _standardClauses["ISO12100"] = new List<string>
        {
            "4.1", "4.2", "5.1", "5.2", "5.3", "5.4", "6.1", "6.2", "6.3"
        };

        // IEC 60204-1 标准条款
        _standardClauses["IEC60204-1"] = new List<string>
        {
            "4.1", "4.2", "4.3", "4.4", "5.1", "5.2", "5.3", "7.1", "7.2",
            "8.1", "8.2", "10.1", "10.2", "10.3", "13.1", "13.2", "17.1", "18.1"
        };
    }

    /// <summary>
    /// 获取标准条款索引
    /// </summary>
    public StandardClauseIndex GetClauseIndex(string projectId, string standard)
    {
        var entries = _matrixService.Get(projectId).Where(e => e.Standard == standard).ToList();
        var clauses = entries.Select(e => e.Clause).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();
        
        var standardClauses = _standardClauses.TryGetValue(standard, out var sc) ? sc : new List<string>();
        var coveredClauses = clauses.Intersect(standardClauses).ToList();
        var missingClauses = standardClauses.Except(clauses).ToList();

        return new StandardClauseIndex
        {
            Standard = standard,
            TotalClauses = standardClauses.Count,
            CoveredClauses = coveredClauses,
            MissingClauses = missingClauses,
            CoverageRate = standardClauses.Count > 0 ? (double)coveredClauses.Count / standardClauses.Count : 0
        };
    }

    /// <summary>
    /// 检查缺失和不一致
    /// </summary>
    public ComplianceCheckResult CheckCompliance(string projectId)
    {
        var result = new ComplianceCheckResult
        {
            ProjectId = projectId,
            Issues = new List<ComplianceIssue>()
        };

        var entries = _matrixService.Get(projectId).ToList();
        var standards = entries.Select(e => e.Standard).Distinct().ToList();

        foreach (var standard in standards)
        {
            // 检查缺失的条款
            var index = GetClauseIndex(projectId, standard);
            foreach (var missingClause in index.MissingClauses)
            {
                result.Issues.Add(new ComplianceIssue
                {
                    Type = ComplianceIssueType.MissingClause,
                    Standard = standard,
                    Clause = missingClause,
                    Severity = ComplianceIssueSeverity.Medium,
                    Message = $"标准 {standard} 的条款 {missingClause} 未在合规矩阵中"
                });
            }

            // 检查缺失证据
            var entriesWithoutEvidence = entries.Where(e => e.Standard == standard && string.IsNullOrWhiteSpace(e.EvidenceId)).ToList();
            foreach (var entry in entriesWithoutEvidence)
            {
                result.Issues.Add(new ComplianceIssue
                {
                    Type = ComplianceIssueType.MissingEvidence,
                    Standard = standard,
                    Clause = entry.Clause,
                    Severity = ComplianceIssueSeverity.High,
                    Message = $"条款 {entry.Clause} 的要求 {entry.Requirement} 缺少证据"
                });
            }

            // 检查结果不一致
            var entriesWithFail = entries.Where(e => e.Standard == standard && e.Result == "不符合").ToList();
            foreach (var entry in entriesWithFail)
            {
                if (string.IsNullOrWhiteSpace(entry.Owner) || string.IsNullOrWhiteSpace(entry.Due))
                {
                    result.Issues.Add(new ComplianceIssue
                    {
                        Type = ComplianceIssueType.IncompleteRemediation,
                        Standard = standard,
                        Clause = entry.Clause,
                        Severity = ComplianceIssueSeverity.High,
                        Message = $"不符合项 {entry.Requirement} 缺少责任人或整改期限"
                    });
                }
            }
        }

        // 检查证据链完整性
        var evidenceIds = entries.Select(e => e.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        foreach (var evidenceId in evidenceIds)
        {
            var evidence = _evidenceService.Get(evidenceId!);
            if (evidence == null)
            {
                result.Issues.Add(new ComplianceIssue
                {
                    Type = ComplianceIssueType.InvalidEvidence,
                    Severity = ComplianceIssueSeverity.High,
                    Message = $"证据 {evidenceId} 不存在或已被删除"
                });
            }
            else if (evidence.ValidUntil.HasValue && evidence.ValidUntil.Value < DateTime.UtcNow)
            {
                result.Issues.Add(new ComplianceIssue
                {
                    Type = ComplianceIssueType.ExpiredEvidence,
                    Severity = ComplianceIssueSeverity.Critical,
                    Message = $"证据 {evidence.Name} (ID: {evidenceId}) 已过期"
                });
            }
        }

        result.IssueCount = result.Issues.Count;
        result.CriticalIssues = result.Issues.Count(i => i.Severity == ComplianceIssueSeverity.Critical);
        result.HighIssues = result.Issues.Count(i => i.Severity == ComplianceIssueSeverity.High);

        return result;
    }

    /// <summary>
    /// 生成追溯链报告
    /// </summary>
    public TraceabilityChain GenerateTraceabilityChain(string projectId)
    {
        var entries = _matrixService.Get(projectId).ToList();
        var chain = new TraceabilityChain
        {
            ProjectId = projectId,
            Chains = new List<TraceabilityLink>()
        };

        foreach (var entry in entries)
        {
            var link = new TraceabilityLink
            {
                Requirement = entry.Requirement,
                Standard = entry.Standard,
                Clause = entry.Clause,
                Reference = entry.Reference,
                EvidenceId = entry.EvidenceId,
                EvidenceName = !string.IsNullOrWhiteSpace(entry.EvidenceId) ? _evidenceService.Get(entry.EvidenceId)?.Name : null,
                Result = entry.Result,
                Owner = entry.Owner,
                Due = entry.Due
            };

            chain.Chains.Add(link);
        }

        return chain;
    }
}

public class StandardClauseIndex
{
    public string Standard { get; set; } = string.Empty;
    public int TotalClauses { get; set; }
    public List<string> CoveredClauses { get; set; } = new();
    public List<string> MissingClauses { get; set; } = new();
    public double CoverageRate { get; set; }
}

public class ComplianceCheckResult
{
    public string ProjectId { get; set; } = string.Empty;
    public List<ComplianceIssue> Issues { get; set; } = new();
    public int IssueCount { get; set; }
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
}

public class ComplianceIssue
{
    public ComplianceIssueType Type { get; set; }
    public string? Standard { get; set; }
    public string? Clause { get; set; }
    public ComplianceIssueSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum ComplianceIssueType
{
    MissingClause,          // 缺失条款
    MissingEvidence,         // 缺失证据
    IncompleteRemediation,   // 整改不完整
    InvalidEvidence,        // 无效证据
    ExpiredEvidence         // 过期证据
}

public enum ComplianceIssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class TraceabilityChain
{
    public string ProjectId { get; set; } = string.Empty;
    public List<TraceabilityLink> Chains { get; set; } = new();
}

public class TraceabilityLink
{
    public string Requirement { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string Clause { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? EvidenceId { get; set; }
    public string? EvidenceName { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string? Due { get; set; }
}

