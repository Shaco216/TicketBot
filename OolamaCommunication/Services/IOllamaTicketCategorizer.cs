
namespace OolamaCommunication.Services
{
    public interface IOllamaTicketCategorizer
    {
        Task<string> CategorizeAsync(string subject, string body, IEnumerable<string> categories);
    }
}