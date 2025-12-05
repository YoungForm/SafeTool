namespace SafeTool.Application.Services;

/// <summary>
/// 通道连接可视化服务
/// </summary>
public class ChannelVisualizationService
{
    private readonly ProjectModelService _projectModel;
    private readonly ComponentLibraryService _componentLibrary;
    private readonly ModelComputeService _computeService;

    public ChannelVisualizationService(
        ProjectModelService projectModel,
        ComponentLibraryService componentLibrary,
        ModelComputeService computeService)
    {
        _projectModel = projectModel;
        _componentLibrary = componentLibrary;
        _computeService = computeService;
    }

    /// <summary>
    /// 生成通道连接可视化数据
    /// </summary>
    public ChannelVisualizationData GenerateVisualization(string functionId)
    {
        var function = _projectModel.List().FirstOrDefault(f => f.Id == functionId);
        if (function == null)
            throw new KeyNotFoundException($"功能 {functionId} 不存在");

        var visualization = new ChannelVisualizationData
        {
            FunctionId = functionId,
            FunctionName = function.Name,
            Channels = new List<ChannelInfo>(),
            Connections = new List<ChannelConnection>(),
            Statistics = new ChannelStatistics()
        };

        // 处理输入通道
        var inputChannels = ProcessChannels(function.Model.I, "Input", visualization);
        
        // 处理逻辑通道
        var logicChannels = ProcessChannels(function.Model.L, "Logic", visualization);
        
        // 处理输出通道
        var outputChannels = ProcessChannels(function.Model.O, "Output", visualization);

        // 创建通道连接
        CreateChannelConnections(inputChannels, logicChannels, visualization);
        CreateChannelConnections(logicChannels, outputChannels, visualization);

        // 计算统计信息
        CalculateStatistics(function, visualization);

        return visualization;
    }

    private List<ChannelInfo> ProcessChannels(
        List<ProjectModelService.DeviceRef> devices,
        string channelType,
        ChannelVisualizationData visualization)
    {
        var channels = new List<ChannelInfo>();

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var component = _componentLibrary.Get(device.Id);

            var channel = new ChannelInfo
            {
                Id = $"{channelType}-{i}",
                Type = channelType,
                DeviceId = device.Id,
                DeviceName = component?.Model ?? device.Id,
                Manufacturer = component?.Manufacturer ?? "",
                Category = component?.Category ?? "",
                Index = i,
                Parameters = ExtractParameters(component, device)
            };

            channels.Add(channel);
            visualization.Channels.Add(channel);
        }

        return channels;
    }

    private Dictionary<string, object> ExtractParameters(
        ComponentLibraryService.ComponentRecord? component,
        ProjectModelService.DeviceRef device)
    {
        var parameters = new Dictionary<string, object>();

        if (component?.Parameters != null)
        {
            foreach (var param in component.Parameters)
            {
                if (double.TryParse(param.Value, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out var num))
                {
                    parameters[param.Key] = num;
                }
                else
                {
                    parameters[param.Key] = param.Value;
                }
            }
        }

        // 应用覆盖参数
        if (device.OverrideParams != null)
        {
            foreach (var param in device.OverrideParams)
            {
                if (double.TryParse(param.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var num))
                {
                    parameters[param.Key] = num;
                }
                else
                {
                    parameters[param.Key] = param.Value;
                }
            }
        }

        return parameters;
    }

    private void CreateChannelConnections(
        List<ChannelInfo> sourceChannels,
        List<ChannelInfo> targetChannels,
        ChannelVisualizationData visualization)
    {
        if (sourceChannels.Count == 0 || targetChannels.Count == 0)
            return;

        foreach (var source in sourceChannels)
        {
            foreach (var target in targetChannels)
            {
                visualization.Connections.Add(new ChannelConnection
                {
                    Id = $"{source.Id}-{target.Id}",
                    SourceChannelId = source.Id,
                    TargetChannelId = target.Id,
                    ConnectionType = "sequential",
                    Description = $"{source.Type} → {target.Type}"
                });
            }
        }
    }

    private void CalculateStatistics(
        ProjectModelService.Function function,
        ChannelVisualizationData visualization)
    {
        var stats = visualization.Statistics;
        
        stats.InputChannelCount = function.Model.I.Count;
        stats.LogicChannelCount = function.Model.L.Count;
        stats.OutputChannelCount = function.Model.O.Count;
        stats.TotalChannelCount = stats.InputChannelCount + stats.LogicChannelCount + stats.OutputChannelCount;
        
        stats.HasRedundancy = stats.InputChannelCount >= 2 || stats.LogicChannelCount >= 2 || stats.OutputChannelCount >= 2;
        stats.HasMonitoring = function.Options?.ContainsKey("I.monitor") == true ||
                             function.Options?.ContainsKey("L.monitor") == true ||
                             function.Options?.ContainsKey("O.monitor") == true;

        // 计算总连接数
        stats.TotalConnections = visualization.Connections.Count;

        // 计算平均参数
        var allParams = visualization.Channels
            .SelectMany(c => c.Parameters)
            .Where(p => p.Value is double)
            .GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => g.Average(p => (double)p.Value));

        stats.AverageParameters = allParams;
    }
}

public class ChannelVisualizationData
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public List<ChannelInfo> Channels { get; set; } = new();
    public List<ChannelConnection> Connections { get; set; } = new();
    public ChannelStatistics Statistics { get; set; } = new();
}

public class ChannelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Input/Logic/Output
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Index { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ChannelConnection
{
    public string Id { get; set; } = string.Empty;
    public string SourceChannelId { get; set; } = string.Empty;
    public string TargetChannelId { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "sequential"; // sequential/parallel/redundant
    public string Description { get; set; } = string.Empty;
}

public class ChannelStatistics
{
    public int InputChannelCount { get; set; }
    public int LogicChannelCount { get; set; }
    public int OutputChannelCount { get; set; }
    public int TotalChannelCount { get; set; }
    public int TotalConnections { get; set; }
    public bool HasRedundancy { get; set; }
    public bool HasMonitoring { get; set; }
    public Dictionary<string, double> AverageParameters { get; set; } = new();
}

