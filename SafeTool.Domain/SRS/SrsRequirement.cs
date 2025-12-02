namespace SafeTool.Domain.SRS;

public class SrsRequirement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClauseRef { get; set; } = string.Empty; // ISO 13849-1 条款或附录编号
    public string Category { get; set; } = string.Empty; // 输入/逻辑/输出/诊断/环境
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public bool Mandatory { get; set; } = true;
}

