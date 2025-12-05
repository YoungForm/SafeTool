namespace SafeTool.Application.Services;

/// <summary>
/// 图形化建模服务（为前端提供图形化建模数据支持）
/// </summary>
public class GraphicalModelingService
{
    private readonly ProjectModelService _projectModel;
    private readonly ComponentLibraryService _componentLibrary;

    public GraphicalModelingService(
        ProjectModelService projectModel,
        ComponentLibraryService componentLibrary)
    {
        _projectModel = projectModel;
        _componentLibrary = componentLibrary;
    }

    /// <summary>
    /// 获取图形化建模数据（节点和连接）
    /// </summary>
    public GraphicalModelData GetGraphicalModel(string functionId)
    {
        var function = _projectModel.List().FirstOrDefault(f => f.Id == functionId);
        if (function == null)
            throw new KeyNotFoundException($"功能 {functionId} 不存在");

        var nodes = new List<GraphNode>();
        var connections = new List<GraphConnection>();

        // 创建输入节点
        var inputNodes = CreateChannelNodes(function.Model.I, "I", 0, nodes);
        
        // 创建逻辑节点
        var logicNodes = CreateChannelNodes(function.Model.L, "L", 1, nodes);
        
        // 创建输出节点
        var outputNodes = CreateChannelNodes(function.Model.O, "O", 2, nodes);

        // 创建连接
        CreateConnections(inputNodes, logicNodes, connections);
        CreateConnections(logicNodes, outputNodes, connections);

        return new GraphicalModelData
        {
            FunctionId = functionId,
            FunctionName = function.Name,
            Nodes = nodes,
            Connections = connections,
            Layout = GenerateLayout(nodes)
        };
    }

    /// <summary>
    /// 保存图形化建模数据
    /// </summary>
    public void SaveGraphicalModel(string functionId, GraphicalModelData modelData)
    {
        var function = _projectModel.List().FirstOrDefault(f => f.Id == functionId);
        if (function == null)
            throw new KeyNotFoundException($"功能 {functionId} 不存在");

        // 从图形化数据重建模型
        var inputDevices = modelData.Nodes
            .Where(n => n.Type == "I")
            .Select(n => new ProjectModelService.DeviceRef { Id = n.DeviceId ?? n.Id })
            .ToList();

        var logicDevices = modelData.Nodes
            .Where(n => n.Type == "L")
            .Select(n => new ProjectModelService.DeviceRef { Id = n.DeviceId ?? n.Id })
            .ToList();

        var outputDevices = modelData.Nodes
            .Where(n => n.Type == "O")
            .Select(n => new ProjectModelService.DeviceRef { Id = n.DeviceId ?? n.Id })
            .ToList();

        function.Model.I = inputDevices;
        function.Model.L = logicDevices;
        function.Model.O = outputDevices;

        // 保存布局信息
        if (function.Options == null)
            function.Options = new Dictionary<string, string>();
        
        function.Options["graphicalLayout"] = System.Text.Json.JsonSerializer.Serialize(modelData.Layout);

        _projectModel.Upsert(function);
    }

    private List<GraphNode> CreateChannelNodes(
        List<ProjectModelService.DeviceRef> devices,
        string channelType,
        int layer,
        List<GraphNode> nodes)
    {
        var channelNodes = new List<GraphNode>();
        
        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var component = _componentLibrary.Get(device.Id);
            
            var node = new GraphNode
            {
                Id = $"{channelType}-{i}",
                Type = channelType,
                DeviceId = device.Id,
                Label = component?.Model ?? device.Id,
                Layer = layer,
                Position = new NodePosition { X = layer * 200, Y = i * 100 },
                Properties = new Dictionary<string, object>
                {
                    ["manufacturer"] = component?.Manufacturer ?? "",
                    ["category"] = component?.Category ?? "",
                    ["mttfd"] = component?.Parameters?.GetValueOrDefault("MTTFd") ?? "",
                    ["dcavg"] = component?.Parameters?.GetValueOrDefault("DCavg") ?? "",
                    ["pfhd"] = component?.Parameters?.GetValueOrDefault("PFHd") ?? ""
                }
            };

            nodes.Add(node);
            channelNodes.Add(node);
        }

        return channelNodes;
    }

    private void CreateConnections(
        List<GraphNode> sourceNodes,
        List<GraphNode> targetNodes,
        List<GraphConnection> connections)
    {
        if (sourceNodes.Count == 0 || targetNodes.Count == 0)
            return;

        // 创建连接：每个源节点连接到每个目标节点（简化连接策略）
        foreach (var source in sourceNodes)
        {
            foreach (var target in targetNodes)
            {
                connections.Add(new GraphConnection
                {
                    Id = $"{source.Id}-{target.Id}",
                    SourceId = source.Id,
                    TargetId = target.Id,
                    Type = "data-flow"
                });
            }
        }
    }

    private GraphLayout GenerateLayout(List<GraphNode> nodes)
    {
        return new GraphLayout
        {
            Width = 800,
            Height = 600,
            NodeSpacing = new NodeSpacing { X = 200, Y = 100 },
            Layers = new List<LayerLayout>
            {
                new LayerLayout { Type = "I", X = 0, Width = 150 },
                new LayerLayout { Type = "L", X = 200, Width = 150 },
                new LayerLayout { Type = "O", X = 400, Width = 150 }
            }
        };
    }
}

public class GraphicalModelData
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphConnection> Connections { get; set; } = new();
    public GraphLayout Layout { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // I/L/O
    public string? DeviceId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Layer { get; set; }
    public NodePosition Position { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class GraphConnection
{
    public string Id { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = "data-flow"; // data-flow/control-flow
}

public class GraphLayout
{
    public double Width { get; set; }
    public double Height { get; set; }
    public NodeSpacing NodeSpacing { get; set; } = new();
    public List<LayerLayout> Layers { get; set; } = new();
}

public class NodeSpacing
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class LayerLayout
{
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Width { get; set; }
}

