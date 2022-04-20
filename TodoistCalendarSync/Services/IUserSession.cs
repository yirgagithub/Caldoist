using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoistCalendarSync.Services
{
    public interface IUserSession
    {
        Task<IEnumerable<bool>> SaveAccessCode(string accessCode, string email, string provider);
        Task<IEnumerable<string>> GetTodoistAccesscode(string email, string provider);
    }
}
