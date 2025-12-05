namespace SafeTool.Application.Services;

/// <summary>
/// ISO 13849-2 验证清单增强服务
/// </summary>
public class Iso13849VerificationEnhancementService
{
    private readonly VerificationChecklistService _checklistService;
    private readonly EvidenceService _evidenceService;

    public Iso13849VerificationEnhancementService(
        VerificationChecklistService checklistService,
        EvidenceService evidenceService)
    {
        _checklistService = checklistService;
        _evidenceService = evidenceService;
    }

    /// <summary>
    /// 创建故障排除清单
    /// </summary>
    public IEnumerable<FaultExclusionItem> CreateFaultExclusionChecklist(string projectId)
    {
        var items = new List<FaultExclusionItem>
        {
            new FaultExclusionItem
            {
                Code = "FE-001",
                Title = "短路故障排除",
                Description = "评估短路故障是否可被合理排除",
                Clause = "ISO 13849-2 5.2.1",
                FaultMode = "短路",
                ExclusionCriteria = "通过设计、制造和安装措施确保短路故障概率极低",
                EvidenceRequired = "设计文档、测试报告、安装记录"
            },
            new FaultExclusionItem
            {
                Code = "FE-002",
                Title = "开路故障排除",
                Description = "评估开路故障是否可被合理排除",
                Clause = "ISO 13849-2 5.2.2",
                FaultMode = "开路",
                ExclusionCriteria = "通过设计、制造和安装措施确保开路故障概率极低",
                EvidenceRequired = "设计文档、测试报告、安装记录"
            },
            new FaultExclusionItem
            {
                Code = "FE-003",
                Title = "参数漂移故障排除",
                Description = "评估参数漂移故障是否可被合理排除",
                Clause = "ISO 13849-2 5.2.3",
                FaultMode = "参数漂移",
                ExclusionCriteria = "通过设计、制造和测试措施确保参数漂移在可接受范围内",
                EvidenceRequired = "设计文档、测试报告、校准记录"
            },
            new FaultExclusionItem
            {
                Code = "FE-004",
                Title = "机械故障排除",
                Description = "评估机械故障是否可被合理排除",
                Clause = "ISO 13849-2 5.2.4",
                FaultMode = "机械故障",
                ExclusionCriteria = "通过设计、制造和安装措施确保机械故障概率极低",
                EvidenceRequired = "设计文档、测试报告、安装记录"
            }
        };

        // 保存到验证清单
        foreach (var item in items)
        {
            _checklistService.Upsert(projectId, "ISO13849-2", new VerificationChecklistService.Item
            {
                Code = item.Code,
                Title = item.Title,
                Clause = item.Clause,
                Description = item.Description
            });
        }

        return items;
    }

    /// <summary>
    /// 创建软件要求验证清单
    /// </summary>
    public IEnumerable<SoftwareRequirementItem> CreateSoftwareRequirementChecklist(string projectId)
    {
        var items = new List<SoftwareRequirementItem>
        {
            new SoftwareRequirementItem
            {
                Code = "SW-001",
                Title = "软件需求规格",
                Description = "验证软件需求规格是否完整",
                Clause = "ISO 13849-2 Annex A.1",
                Requirement = "软件需求应明确、可验证、可追溯",
                EvidenceRequired = "软件需求规格文档、需求追溯矩阵"
            },
            new SoftwareRequirementItem
            {
                Code = "SW-002",
                Title = "软件架构设计",
                Description = "验证软件架构设计是否符合要求",
                Clause = "ISO 13849-2 Annex A.2",
                Requirement = "软件架构应支持安全功能实现",
                EvidenceRequired = "软件架构设计文档、设计评审记录"
            },
            new SoftwareRequirementItem
            {
                Code = "SW-003",
                Title = "软件单元测试",
                Description = "验证软件单元测试覆盖率",
                Clause = "ISO 13849-2 Annex A.3",
                Requirement = "单元测试覆盖率应达到要求",
                EvidenceRequired = "单元测试报告、覆盖率报告"
            },
            new SoftwareRequirementItem
            {
                Code = "SW-004",
                Title = "软件集成测试",
                Description = "验证软件集成测试",
                Clause = "ISO 13849-2 Annex A.4",
                Requirement = "集成测试应验证模块间接口",
                EvidenceRequired = "集成测试报告、测试用例"
            },
            new SoftwareRequirementItem
            {
                Code = "SW-005",
                Title = "软件系统测试",
                Description = "验证软件系统测试",
                Clause = "ISO 13849-2 Annex A.5",
                Requirement = "系统测试应验证完整功能",
                EvidenceRequired = "系统测试报告、测试记录"
            },
            new SoftwareRequirementItem
            {
                Code = "SW-006",
                Title = "软件验证与确认",
                Description = "验证软件验证与确认活动",
                Clause = "ISO 13849-2 Annex A.6",
                Requirement = "软件应通过验证与确认",
                EvidenceRequired = "验证报告、确认记录"
            }
        };

        // 保存到验证清单
        foreach (var item in items)
        {
            _checklistService.Upsert(projectId, "ISO13849-2", new VerificationChecklistService.Item
            {
                Code = item.Code,
                Title = item.Title,
                Clause = item.Clause,
                Description = item.Description
            });
        }

        return items;
    }

    /// <summary>
    /// 创建验证计划模板
    /// </summary>
    public VerificationPlanTemplate CreateVerificationPlanTemplate(string projectId, string safetyFunctionName)
    {
        var template = new VerificationPlanTemplate
        {
            ProjectId = projectId,
            SafetyFunctionName = safetyFunctionName,
            CreatedAt = DateTime.UtcNow,
            Activities = new List<VerificationActivity>
            {
                new VerificationActivity
                {
                    Code = "VP-001",
                    Title = "设计评审",
                    Description = "评审安全功能设计文档",
                    Type = VerificationActivityType.Review,
                    Responsible = "设计工程师",
                    PlannedDate = DateTime.UtcNow.AddDays(7),
                    Status = "Planned"
                },
                new VerificationActivity
                {
                    Code = "VP-002",
                    Title = "计算验证",
                    Description = "验证PL/SIL计算",
                    Type = VerificationActivityType.Calculation,
                    Responsible = "安全工程师",
                    PlannedDate = DateTime.UtcNow.AddDays(14),
                    Status = "Planned"
                },
                new VerificationActivity
                {
                    Code = "VP-003",
                    Title = "硬件测试",
                    Description = "执行硬件功能测试",
                    Type = VerificationActivityType.Test,
                    Responsible = "测试工程师",
                    PlannedDate = DateTime.UtcNow.AddDays(21),
                    Status = "Planned"
                },
                new VerificationActivity
                {
                    Code = "VP-004",
                    Title = "软件测试",
                    Description = "执行软件功能测试",
                    Type = VerificationActivityType.Test,
                    Responsible = "测试工程师",
                    PlannedDate = DateTime.UtcNow.AddDays(28),
                    Status = "Planned"
                },
                new VerificationActivity
                {
                    Code = "VP-005",
                    Title = "集成测试",
                    Description = "执行系统集成测试",
                    Type = VerificationActivityType.Test,
                    Responsible = "测试工程师",
                    PlannedDate = DateTime.UtcNow.AddDays(35),
                    Status = "Planned"
                },
                new VerificationActivity
                {
                    Code = "VP-006",
                    Title = "见证测试",
                    Description = "第三方见证测试",
                    Type = VerificationActivityType.Witness,
                    Responsible = "认证机构",
                    PlannedDate = DateTime.UtcNow.AddDays(42),
                    Status = "Planned"
                }
            }
        };

        return template;
    }
}

public class FaultExclusionItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Clause { get; set; } = string.Empty;
    public string FaultMode { get; set; } = string.Empty;
    public string ExclusionCriteria { get; set; } = string.Empty;
    public string EvidenceRequired { get; set; } = string.Empty;
}

public class SoftwareRequirementItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Clause { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string EvidenceRequired { get; set; } = string.Empty;
}

public class VerificationPlanTemplate
{
    public string ProjectId { get; set; } = string.Empty;
    public string SafetyFunctionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<VerificationActivity> Activities { get; set; } = new();
}

public class VerificationActivity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VerificationActivityType Type { get; set; }
    public string Responsible { get; set; } = string.Empty;
    public DateTime? PlannedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = "Planned"; // Planned/InProgress/Completed/Cancelled
    public string? EvidenceId { get; set; }
    public string? Notes { get; set; }
}

public enum VerificationActivityType
{
    Review,      // 评审
    Calculation, // 计算
    Test,        // 测试
    Witness      // 见证
}

