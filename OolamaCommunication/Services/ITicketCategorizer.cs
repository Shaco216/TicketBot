using System.Collections.Generic;
using System.Threading.Tasks;

namespace OolamaCommunication.Services;

public interface ITicketCategorizer
{
    /// <summary>
    /// Gibt den Namen der passenden Kategorie zurück (exakter Eintrag aus <paramref name="categories"/>).
    /// </summary>
    Task<string> CategorizeAsync(string subject, string body, IEnumerable<string> categories);
}