using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.DataContext;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public class IntegrationService : IIntegrationInterface
    {
        private readonly ApplicationDbContext _context;

        public IntegrationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IntegrationModel>> FindIntegrationByCalendar(string calendarName)
        {

            var query = "Select * from Integration where calendarName = @calendarName";
            var parameters = new DynamicParameters();
            parameters.Add("calendarName", calendarName, DbType.String);

            using (var connection = _context.CreateConnection())
            {
                var integrationList = (await connection.QueryAsync<IntegrationModel>(query, parameters)).ToList();
                return integrationList;
            }
        }

        public async Task<List<IntegrationModel>> FindIntegrationByTodoist(string todoistItemId)
        {

            var query = "Select * from Integration where todoistItemId = @todoistItemId";
            var parameters = new DynamicParameters();
            parameters.Add("todoistItemId", todoistItemId, DbType.String);

            using (var connection = _context.CreateConnection())
            {
                var integrationList = (await connection.QueryAsync<IntegrationModel>(query, parameters)).ToList();
                return integrationList;
            }
        }

        public async Task<IEnumerable<bool>> SaveIntegration(IntegrationModel integrationModel)
        {
            string query = @"IF NOT EXISTS(SELECT * FROM Integration WHERE todoistItemId = @todoistItemId AND googleCalendarId = @calendarId)
                                    begin
                                        Insert INTO Integration(googleCalendarId, calendarName, todoistItemId, todoistItemName, email, completedoptions, colorChange, moveCalendarId, label)
                                            values(@calendarId, @calendarName, @todoistItemId,@todoistItemName,@email, @completedoptions, @colorChange, @moveCalendarId, @label)
                                    end
                            ELSE
                                begin 
                                    UPDATE Integration set calendarName = @calendarName, todoistItemName = @todoistItemName, label = @label,
                                                            completedoptions = @completedoptions, colorChange = @colorChange, moveCalendarId = @moveCalendarId
                                    WHERE id = @id
                                end";
            var parameters = new DynamicParameters();
            parameters.Add("calendarId", integrationModel.GoogleCalendarId, DbType.String);
            parameters.Add("calendarName", integrationModel.CalendarName, DbType.String);
            parameters.Add("todoistItemId", integrationModel.TodoistItemId, DbType.String);
            parameters.Add("todoistItemName", integrationModel.TodoistItemName, DbType.String);
            parameters.Add("email", integrationModel.Email, DbType.String);
            parameters.Add("id", integrationModel.Id, DbType.Int32);
            parameters.Add("completedoptions", integrationModel.CompletedOptions);
            parameters.Add("colorChange", integrationModel.ColorChange);
            parameters.Add("moveCalendarId", integrationModel.MoveCalendarId);
            parameters.Add("label", integrationModel.NewLabel);

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync<bool>(query, parameters);
                return result;
            }
        }

        public async Task<List<IntegrationModel>> GetAll(string email)
        {
            List<IntegrationModel> list = new List<IntegrationModel>();
            var query = "Select * from Integration where email = @email";
            var parameters = new DynamicParameters();
            parameters.Add("email", email, DbType.String);

            using (var connection = _context.CreateConnection())
            {
                list = (await connection.QueryAsync<IntegrationModel>(query, parameters)).ToList();
            }

            return list;
        }

        public async Task<bool> IsChangedCalDoist(string type, string param1, string param2, string email)
        {
            bool isFound = false;
            string query = "";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("param1", param1);
            parameters.Add("param2", param2);
            parameters.Add("email", email);

            if (type != null && type.Equals("calendar"))
            {
                query = "SELECT * FROM GoogleCalendarHistory WHERE calendarId = @param1 and eventName = @param2 and email = @email";
            }
            else
            {
                query = "SELECT * FROM TodoistHistory WHERE projectId = @param1 and todoistItemName = @param2 and email = @email";
            }

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync(query, parameters);
                isFound = result.ToList().Count > 0;
            }

            return isFound;
        }

        public async Task<bool> SaveHistory(string type, string param1, string param2, string email)
        {
            var isSaved = false;

            string query = "";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("param1", param1);
            parameters.Add("param2", param2);
            parameters.Add("email", email);

            if (type != null && type.Equals("Calendar"))
            {
                query = @" IF NOT EXISTS(SELECT * FROM GoogleCalendarHistory WHERE calendarId = @param1 and eventName = @param2 and email = @email)
                                    begin
                                        Insert into GoogleCalendarHistory(calendarId, eventName, email) values(@param1, @param2, @email)
                                    end;";
            }
            else
            {
                query = @" IF NOT EXISTS(SELECT * FROM TodoistHistory WHERE todoistItemName = @param2 and projectId = @param1 and email = @email)
                                begin
                                        Insert into TodoistHistory(todoistItemName, projectId, email) values(@param2, @param1, @email)
                                end;";
            }

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync<bool>(query, parameters);
                isSaved = result.FirstOrDefault();
            }

            return isSaved;
        }

        public async Task<bool> ClearHistory(string type, string param1, string param2, string email)
        {
            var isDeleted = false;

            string query = "";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("param1", param1);
            parameters.Add("param2", param2);
            parameters.Add("email", email);

            if (type != null && type.Equals("Calendar"))
            {
                query = @"DELETE FROM GoogleCalendarHistory WHERE calendarId = @param1 and eventName = @param2 and email = @email;";
            }
            else
            {
                query = @"DELETE FROM TodoistHistory WHERE todoistItemName = @param2 and projectId = @param1 and email = @email;";
            }

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync<bool>(query, parameters);
                isDeleted = result.FirstOrDefault();
            }

            return isDeleted;
        }

        public async Task<bool> SaveTaskEventHistory(string taskId, string eventId, string email)
        {
            bool isSaved = false;

            string query = @"IF NOT EXISTS(SELECT * FROM TaskEventHistory WHERE taskId = @taskId and eventId = @eventId)
                             BEGIN 
                                INSERT INTO TaskEventHistory(taskId, eventId, email, createdat) VALUES(@taskId, @eventId, @email, @createdat);
                            END";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("taskId", taskId);
            parameters.Add("eventId", eventId);
            parameters.Add("email", email);
            parameters.Add("createdat", DateTime.Now);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isSaved = queryResult.FirstOrDefault();
            }

            return isSaved;
        }

        public async Task<List<TaskEventHistory>> GetTaskEventHistoryById(string type, string email, string id)
        {
            List<TaskEventHistory> result = new List<TaskEventHistory>();
            string query = "";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", id);
            parameters.Add("email", email);
            if (type.Equals("Event"))
            {
                query = "SELECT * FROM TaskEventHistory WHERE eventId = @id and email=@email";
            }
            else if (type.Equals("Task"))
            {
                query = "SELECT * FROM TaskEventHistory WHERE taskId = @id and email=@email";
            }
            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<TaskEventHistory>(query, parameters);
                result = queryResult.ToList();
            }
            return result;
        }
    }
}
