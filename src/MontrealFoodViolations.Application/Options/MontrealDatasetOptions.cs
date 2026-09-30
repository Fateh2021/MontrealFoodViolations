namespace MontrealFoodViolations.Application.Options;

public class MontrealDatasetOptions
{
    public string ViolationsUrl { get; set; } = "https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0/resource/7f939a08-be8a-45e1-b208-d8744dca8fc6/download/violations.csv";
    public string ConvictionsUrl { get; set; } = "https://www.donneesquebec.ca/recherche/dataset/515374ee-ce34-464f-9875-7d1af3fa9b2a/resource/40105615-3abf-414b-bcba-182e8f2c5eb2/download/listecondamnation.csv";
    public int TimeoutSeconds { get; set; } = 60;
}
