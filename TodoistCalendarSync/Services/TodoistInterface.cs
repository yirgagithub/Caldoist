using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public interface TodoistInterface
    {
        public Task<bool> Add(TodoistProject todoistProject);
        public Task<bool> AddRange(List<TodoistProject> list);
        public Task<List<TodoistProject>> GetAllTodistProjectByEmail(string email);
        public Task<bool> AddLabel(TodoistLabel todistLabel);
        public Task<bool> UpdateLabel(TodoistLabel todistLabel);
        public Task<bool> DeleteLabel(TodoistLabel todistLabel);
        public Task<bool> AddLabelRange(List<TodoistLabel> todistLabel);
        public Task<List<TodoistLabel>> GetAllTodoistLabelByEmail(string email);
        public Task<bool> Update(TodoistProject todoistProject);
        public Task<bool> Delete(TodoistProject todoistProject);
    }
}
