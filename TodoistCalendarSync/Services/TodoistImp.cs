using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.DataContext;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public class TodoistImp : TodoistInterface
    {
        private readonly ApplicationDbContext _context;

        public TodoistImp(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Add(TodoistProject todoistProject)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM TodoistProject where id = @id)
                               begin
                                    INSERT INTO TodoistProject(id, name, email) 
                                            values(@id,@name, @email)
                                end";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todoistProject.Id);
            parameters.Add("name", todoistProject.Name);
            parameters.Add("email", todoistProject.Email);

            
            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isSaved = queryResult.FirstOrDefault();

            }
            return isSaved;
        }

        public async Task<bool> AddLabel(TodoistLabel todoistLabel)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM TodoistLabel where id = @id)
                               begin
                                    INSERT INTO TodoistLabel(id, name, email) 
                                            values(@id,@name, @email)
                                end";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todoistLabel.Id);
            parameters.Add("name", todoistLabel.Name);
            parameters.Add("email", todoistLabel.Email);


            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isSaved = queryResult.FirstOrDefault();

            }
            return isSaved;
        }

        public async Task<bool> AddLabelRange(List<TodoistLabel> list)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM TodoistLabel where id = @id)
                               begin
                                    INSERT INTO TodoistLabel(id, name, email) 
                                            values(@id,@name, @email)
                                end";
            DynamicParameters parameters;

            using (var connection = _context.CreateConnection())
            {
                foreach (var element in list)
                {
                    parameters = new DynamicParameters();
                    parameters.Add("id", element.Id);
                    parameters.Add("name", element.Name);
                    parameters.Add("email", element.Email);

                    var queryResult = await connection.QueryAsync<bool>(query, parameters);
                    isSaved = queryResult.FirstOrDefault();
                }
            }

            return isSaved;
        }

        public async Task<bool> AddRange(List<TodoistProject> list)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM TodoistProject where id = @id and email = @email)
                               begin
                                    INSERT INTO TodoistProject(id, name, email) 
                                            values(@id,@name, @email)
                                end";
            DynamicParameters parameters;

            using (var connection = _context.CreateConnection())
            {
                foreach (var element in list)
                {
                    parameters = new DynamicParameters();
                    parameters.Add("id", element.Id);
                    parameters.Add("name", element.Name);
                    parameters.Add("email", element.Email);

                    var queryResult = await connection.QueryAsync<bool>(query, parameters);
                    isSaved = queryResult.FirstOrDefault();
                }
            }

            return isSaved;
        }

        public async Task<bool> Delete(TodoistProject todoistProject)
        {
            bool isDeleted = false;

            string query = @"DELETE FROM TodoistProject Where id = @id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todoistProject.Id);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isDeleted = queryResult.FirstOrDefault();
            }

            return isDeleted;
        }

        public async Task<bool> DeleteLabel(TodoistLabel todistLabel)
        {
            bool isDeleted = false;

            string query = @"DELETE FROM TodoistLabel Where id = @id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todistLabel.Id);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isDeleted = queryResult.FirstOrDefault();
            }

            return isDeleted;
        }

        public async Task<List<TodoistProject>> GetAllTodistProjectByEmail(string email)
        {
            List<TodoistProject> list = new List<TodoistProject>();
            string query = @"Select * from TodoistProject where email = @email";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("email", email);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<TodoistProject>(query, parameters);
                list = queryResult.ToList();
            }

            return list;
        }

        public async Task<List<TodoistLabel>> GetAllTodoistLabelByEmail(string email)
        {
            List<TodoistLabel> list = new List<TodoistLabel>();
            string query = @"Select * from TodoistLabel where email = @email";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("email", email);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<TodoistLabel>(query, parameters);
                list = queryResult.ToList();
            }

            return list;
        }

        public async Task<bool> Update(TodoistProject todoistProject)
        {
            bool isUpdated = false;

            string query = @"UPDATE TodoistProject SET name = @name Where id = @id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todoistProject.Id);
            parameters.Add("name", todoistProject.Name);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isUpdated = queryResult.FirstOrDefault();
            }

            return isUpdated;
        }

        public async Task<bool> UpdateLabel(TodoistLabel todistLabel)
        {
            bool isUpdated = false;

            string query = @"UPDATE TodoistLabel SET name = @name Where id = @id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("id", todistLabel.Id);
            parameters.Add("name", todistLabel.Name);

            using (var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isUpdated = queryResult.FirstOrDefault();
            }

            return isUpdated;
        }
    }
}
