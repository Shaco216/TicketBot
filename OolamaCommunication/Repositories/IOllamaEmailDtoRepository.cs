using OolamaCommunication.Models;

namespace OolamaCommunication.Repositories
{
    public interface IOllamaEmailDtoRepository
    {
        Task CreateTable();
        Task<IEnumerable<ReceivedEmailDto>> GetAllAsync();
        Task<IEnumerable<ReceivedEmailDto>> GetByReceiverAsync(string receiver);
        Task<IEnumerable<ReceivedEmailDto>> GetBySenderAsync(string sender);
        Task<ReceivedEmailDto?> GetByIdAsync(Guid id);
        Task<bool> InsertAsync(Guid emailId,string from, string to, string subject, string body);
    }
}