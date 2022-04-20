using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoistCalendarSync.DataContext;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Services
{
    public class GoogleCalendarImp : GoogleCalendarInterface
    {
        private readonly ApplicationDbContext _context;
        public GoogleCalendarImp(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Add(GoogleCalendar googleCalendar)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM GoogleCalendar where id = @id)
                               begin
                                    INSERT INTO GoogleCalendar(id, kind, summary, isPrimary, colorId, background, email) 
                                            values(@id, @kind, @summary, @isPrimary, @colorId, @background, @email)
                                end";
            DynamicParameters parameters = new DynamicParameters();
            
            parameters.Add("id", googleCalendar.Id);
            parameters.Add("kind", googleCalendar.Kind);
            parameters.Add("summary", googleCalendar.Summary);
            parameters.Add("isPrimary", googleCalendar.isPrimary);
            parameters.Add("colorId", googleCalendar.ColorId);
            parameters.Add("background", googleCalendar.Background);
            parameters.Add("email", googleCalendar.Email);

            using(var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isSaved = queryResult.FirstOrDefault();
            }

            return isSaved;
        }

        public async Task<bool> AddLookup(string email, string type, string value)
        {
            bool isSaved = false;

            string query = @"IF NOT EXISTS (SELECT * FROM LookupType where email = @email and type = 'Google_CALENDAR_API_WATCH')
                                begin
                                    INSERT INTO LookupType(type, value, email) values(@type, @value, @email)
                                end
                             ELSE 
                                begin
                                   UPDATE LookupType SET value = @value where email = @email and type = 'Google_CALENDAR_API_WATCH'
                                end";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("email", email);
            parameters.Add("value", value);
            parameters.Add("type", type);

            using(var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<bool>(query, parameters);
                isSaved = queryResult.FirstOrDefault();
            }

            return isSaved;
        }

        public async Task<bool> AddRange(List<GoogleCalendar> list)
        {
            bool isSaved = false;
            string query = @"IF NOT EXISTS(SELECT * FROM GoogleCalendar where id = @id)
                               begin
                                    INSERT INTO GoogleCalendar(id, kind, summary, isPrimary, colorId, background, email) 
                                            values(@id, @kind, @summary, @isPrimary, @colorId, @background, @email)
                                end";
            DynamicParameters parameters;

            using (var connection = _context.CreateConnection())
            {
                foreach (var element in list)
                {
                    parameters = new DynamicParameters();
                    parameters.Add("id", element.Id);
                    parameters.Add("kind", element.Kind);
                    parameters.Add("summary", element.Summary);
                    parameters.Add("isPrimary", element.isPrimary);
                    parameters.Add("colorId", element.ColorId);
                    parameters.Add("background", element.Background);
                    parameters.Add("email", element.Email);

                    var queryResult = await connection.QueryAsync<bool>(query, parameters);
                    isSaved = queryResult.FirstOrDefault();
                }
            }

            return isSaved;
        }

        public async Task<List<GoogleCalendar>> GetAllGoogleCalendarByEmail(string email)
        {
            List<GoogleCalendar> list = new List<GoogleCalendar>();
            string query = @"Select * from GoogleCalendar where email = @email";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("email", email);

            using(var connection = _context.CreateConnection())
            {
                var queryResult = await connection.QueryAsync<GoogleCalendar>(query, parameters);
                list = queryResult.ToList();
            }

            return list;
        }

        public async Task<bool> IsGoogleCalendarWatched(string email)
        {
            string value = "OFF";
            using(var connection = _context.CreateConnection())
            {
                string query = "Select value from LookupType where type = 'Google_CALENDAR_API_WATCH' and email = @email";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("email", email);

                var queryResult = await connection.QueryAsync<string>(query, parameters);
                value = queryResult.FirstOrDefault();
            }

            return value != null && value.Equals("ON");
        }

        
    }
}
