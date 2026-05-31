using OolamaCommunication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OolamaCommunication.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id);
}