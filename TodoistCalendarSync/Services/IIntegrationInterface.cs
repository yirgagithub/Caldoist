using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public interface IIntegrationInterface
    {
        Task<List<IntegrationModel>> GetAll(string email);
        Task<List<IntegrationModel>> FindIntegrationByTodoist(string todoistItemId);
        Task<List<IntegrationModel>> FindIntegrationByCalendar(string calendarName);
        Task<IEnumerable<bool>> SaveIntegration(IntegrationModel integrationModel);
        Task<bool> SaveTaskEventHistory(string taskId, string eventId, string email);
        Task<bool> IsChangedCalDoist(string type, string param1, string param2, string email);
        Task<bool> SaveHistory(string type, string param1, string param2, string email);
        Task<bool> ClearHistory(string type, string param1, string param2, string email);
        Task<List<TaskEventHistory>> GetTaskEventHistoryById(string type, string email, string id);
    }
}
