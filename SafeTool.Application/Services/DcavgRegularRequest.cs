namespace SafeTool.Application.Services;

public class DcavgRegularRequest
{
    public List<DeviceDcavgInfo>? Devices { get; set; }
    public double DemandRate { get; set; }
    public int SeriesCount { get; set; }
}

