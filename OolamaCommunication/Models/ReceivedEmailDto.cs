namespace OolamaCommunication.Models;

public class ReceivedEmailDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
}
