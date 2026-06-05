namespace OolamaCommunication.Models;

public class QueryRequest
{
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public bool? Stream { get; set; }
}
