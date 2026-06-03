
using OolamaCommunication.Models;

namespace OolamaCommunication.Services
{
    public interface IOllamaTicketCategorizer
    {
        Task<Category> CategorizeAsync(Guid emailId, string subject, string body);
    }
}