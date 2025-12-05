using System.Security.Cryptography;
using System.Text;

namespace SafeTool.Application.Services;

/// <summary>
/// 证据校验服务（策略模式）
/// </summary>
public class EvidenceValidationService
{
    private readonly EvidenceService _evidenceService;

    public EvidenceValidationService(EvidenceService evidenceService)
    {
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// 验证证据文件完整性
    /// </summary>
    public EvidenceValidationResult ValidateEvidence(string evidenceId)
    {
        var evidence = _evidenceService.Get(evidenceId);
        if (evidence == null)
        {
            return new EvidenceValidationResult
            {
                EvidenceId = evidenceId,
                IsValid = false,
                Errors = new List<string> { "证据不存在" }
            };
        }

        var result = new EvidenceValidationResult
        {
            EvidenceId = evidenceId,
            EvidenceName = evidence.Name,
            ValidationDate = DateTime.UtcNow,
            Checks = new List<ValidationCheck>()
        };

        // 检查文件是否存在
        if (string.IsNullOrWhiteSpace(evidence.FilePath))
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "FileExistence",
                Passed = false,
                Message = "证据文件路径为空"
            });
        }
        else if (!File.Exists(evidence.FilePath))
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "FileExistence",
                Passed = false,
                Message = "证据文件不存在"
            });
        }
        else
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "FileExistence",
                Passed = true,
                Message = "文件存在"
            });

            // 计算文件哈希值
            try
            {
                var fileHash = CalculateFileHash(evidence.FilePath);
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "FileIntegrity",
                    Passed = true,
                    Message = $"文件哈希值: {fileHash}",
                    Details = fileHash
                });
            }
            catch (Exception ex)
            {
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "FileIntegrity",
                    Passed = false,
                    Message = $"计算文件哈希失败: {ex.Message}"
                });
            }

            // 检查文件大小
            try
            {
                var fileInfo = new FileInfo(evidence.FilePath);
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "FileSize",
                    Passed = true,
                    Message = $"文件大小: {fileInfo.Length} 字节",
                    Details = fileInfo.Length.ToString()
                });

                // 检查文件是否过大（超过100MB）
                if (fileInfo.Length > 100 * 1024 * 1024)
                {
                    result.Checks.Add(new ValidationCheck
                    {
                        CheckType = "FileSize",
                        Passed = false,
                        Message = "文件大小超过100MB，建议压缩或拆分"
                    });
                }
            }
            catch (Exception ex)
            {
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "FileSize",
                    Passed = false,
                    Message = $"检查文件大小失败: {ex.Message}"
                });
            }
        }

        // 检查有效期
        if (evidence.ValidUntil.HasValue)
        {
            if (evidence.ValidUntil.Value < DateTime.UtcNow)
            {
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "Validity",
                    Passed = false,
                    Message = $"证据已过期（过期日期: {evidence.ValidUntil.Value:yyyy-MM-dd}）"
                });
            }
            else if (evidence.ValidUntil.Value < DateTime.UtcNow.AddDays(30))
            {
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "Validity",
                    Passed = true,
                    Message = $"证据即将在30天内过期（过期日期: {evidence.ValidUntil.Value:yyyy-MM-dd}）"
                });
            }
            else
            {
                result.Checks.Add(new ValidationCheck
                {
                    CheckType = "Validity",
                    Passed = true,
                    Message = $"证据有效（过期日期: {evidence.ValidUntil.Value:yyyy-MM-dd}）"
                });
            }
        }
        else
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "Validity",
                Passed = true,
                Message = "证据无有效期限制"
            });
        }

        // 检查元数据完整性
        if (string.IsNullOrWhiteSpace(evidence.Name))
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "Metadata",
                Passed = false,
                Message = "证据名称缺失"
            });
        }
        else
        {
            result.Checks.Add(new ValidationCheck
            {
                CheckType = "Metadata",
                Passed = true,
                Message = "元数据完整"
            });
        }

        // 汇总结果
        result.IsValid = result.Checks.All(c => c.Passed);
        result.Errors = result.Checks.Where(c => !c.Passed).Select(c => c.Message).ToList();
        result.Warnings = result.Checks.Where(c => c.Passed && c.Message.Contains("即将")).Select(c => c.Message).ToList();

        return result;
    }

    /// <summary>
    /// 计算文件SHA256哈希值
    /// </summary>
    private string CalculateFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 批量验证证据
    /// </summary>
    public List<EvidenceValidationResult> ValidateEvidences(IEnumerable<string> evidenceIds)
    {
        return evidenceIds.Select(id => ValidateEvidence(id)).ToList();
    }

    /// <summary>
    /// 检查证据链完整性
    /// </summary>
    public EvidenceChainValidationResult ValidateEvidenceChain(string projectId, ComplianceMatrixService matrixService)
    {
        var result = new EvidenceChainValidationResult
        {
            ProjectId = projectId,
            Issues = new List<EvidenceChainIssue>()
        };

        var entries = matrixService.Get(projectId).ToList();
        var evidenceIds = entries.Select(e => e.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

        foreach (var evidenceId in evidenceIds)
        {
            var validation = ValidateEvidence(evidenceId!);
            if (!validation.IsValid)
            {
                result.Issues.Add(new EvidenceChainIssue
                {
                    EvidenceId = evidenceId!,
                    IssueType = "InvalidEvidence",
                    Severity = "High",
                    Message = $"证据 {evidenceId} 验证失败: {string.Join(", ", validation.Errors)}"
                });
            }

            // 检查证据是否被多个条目引用
            var referenceCount = entries.Count(e => e.EvidenceId == evidenceId);
            if (referenceCount > 5)
            {
                result.Issues.Add(new EvidenceChainIssue
                {
                    EvidenceId = evidenceId!,
                    IssueType = "OverReferenced",
                    Severity = "Medium",
                    Message = $"证据 {evidenceId} 被 {referenceCount} 个条目引用，建议检查是否合理"
                });
            }
        }

        // 检查缺失证据
        var entriesWithoutEvidence = entries.Where(e => string.IsNullOrWhiteSpace(e.EvidenceId) && e.Result == "符合").ToList();
        foreach (var entry in entriesWithoutEvidence)
        {
            result.Issues.Add(new EvidenceChainIssue
            {
                EvidenceId = null,
                IssueType = "MissingEvidence",
                Severity = "High",
                Message = $"条款 {entry.Clause} 的要求 {entry.Requirement} 标记为符合但缺少证据"
            });
        }

        result.IsValid = result.Issues.Count == 0;
        return result;
    }
}

public class EvidenceValidationResult
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? EvidenceName { get; set; }
    public DateTime ValidationDate { get; set; }
    public bool IsValid { get; set; }
    public List<ValidationCheck> Checks { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ValidationCheck
{
    public string CheckType { get; set; } = string.Empty; // FileExistence/FileIntegrity/FileSize/Validity/Metadata
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class EvidenceChainValidationResult
{
    public string ProjectId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<EvidenceChainIssue> Issues { get; set; } = new();
}

public class EvidenceChainIssue
{
    public string? EvidenceId { get; set; }
    public string IssueType { get; set; } = string.Empty; // InvalidEvidence/OverReferenced/MissingEvidence
    public string Severity { get; set; } = string.Empty; // Low/Medium/High/Critical
    public string Message { get; set; } = string.Empty;
}

