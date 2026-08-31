namespace MontrealFoodViolations.Application.Options;

public class MontrealDatasetOptions
{
    public string ViolationsUrl { get; set; } = "https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0/resource/7f939a08-be8a-45e1-b208-d8744dca8fc6/download/violations.csv";
    public int TimeoutSeconds { get; set; } = 60;
}
