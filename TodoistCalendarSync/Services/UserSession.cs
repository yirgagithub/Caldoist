using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.DataContext;

namespace TodoistCalendarSync.Services
{
    public class UserSession: IUserSession
    {
        private readonly ApplicationDbContext _context;

        public UserSession(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<string>> GetTodoistAccesscode(string email, string provider)
        {

            var query = "Select accesscode from TodoistAccessCode where email = @email and provider = @provider";
            var parameters = new DynamicParameters();
            parameters.Add("email", email, DbType.String);
            parameters.Add("provider", provider, DbType.String);

            using (var connection = _context.CreateConnection())
            {
                var accessCode = await connection.QueryAsync<string>(query,parameters);
                return accessCode;
            }
        }

        public async Task<IEnumerable<bool>> SaveAccessCode(string accessCode, string email, string provider)
        {
            string query = @"IF NOT EXISTS(SELECT * FROM TodoistAccessCode where email= @email and provider = @provider) 
                                    begin
                                        Insert INTO TodoistAccessCode(accesscode, email, provider) values(@accessCode,@email, @provider )
                                    end
                                begin 
                                    UPDATE TodoistAccessCode set accesscode = @accessCode WHERE email= @email and provider = @provider
                                end";
            var parameters = new DynamicParameters();
            parameters.Add("accesscode", accessCode, DbType.String);
            parameters.Add("email",email, DbType.String);
            parameters.Add("provider", provider, DbType.String);

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync<bool>(query, parameters);
                return result;
            }
        }
    }
}
