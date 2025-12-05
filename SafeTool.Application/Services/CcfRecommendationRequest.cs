namespace SafeTool.Application.Services;

public class CcfRecommendationRequest
{
    public int CurrentScore { get; set; }
    public IEnumerable<string>? SelectedCodes { get; set; }
}

