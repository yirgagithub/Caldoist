using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TodoistCalendarSync.Models;
using TodoistCalendarSync.Services;

namespace TodoistCalendarSync.Controllers
{
    public class IntegrationController : Controller
    {

        private readonly ILogger<IntegrationController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserSession _userSession;
        private readonly IIntegrationInterface _integrationInterface;
        private readonly IGoogleAuthProvider _googleAuth;
        private readonly GoogleCalendarInterface _googleCalendar;
        private readonly TodoistInterface _todoistInterface;

        public IntegrationController(ILogger<IntegrationController> logger, IHttpContextAccessor httpContextAccessor, IUserSession userSession,
            IIntegrationInterface integrationInterface, IGoogleAuthProvider auth, GoogleCalendarInterface googleCalendar,
            TodoistInterface todoistInterface)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userSession = userSession;
            _integrationInterface = integrationInterface;
            _googleAuth = auth;
            _googleCalendar = googleCalendar;
            _todoistInterface = todoistInterface;
        }
            public async Task<ActionResult> integrate([FromServices] IGoogleAuthProvider _auth)
        {

            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = auth.Principal.Identities.FirstOrDefault()
                .Claims.Select(claim => new { claim.Type, claim.Value });

            string email = "";
            email = claims.ToList().Where(x => x.Type.Contains("emailaddress")).Select(x => x.Value).ToList()[0];

            List<TodoistProject> todoistProjects = await _todoistInterface.GetAllTodistProjectByEmail(email);
            List<GoogleCalendar> calendarList = await _googleCalendar.GetAllGoogleCalendarByEmail(email);
            List<TodoistLabel> todoistLabels = await _todoistInterface.GetAllTodoistLabelByEmail(email);

            ViewData["googleCalendarList"] = new SelectList(calendarList, "Id", "Summary");
            ViewData["todoistItemList"] = new SelectList(todoistProjects, "Id", "Name");
            ViewData["todoistLabels"] = new SelectList(todoistLabels, "Id", "Name");
            ViewData["integrationModel"] = new IntegrationModel();

            return PartialView("CreateEditIntegration", new IntegrationModel());

        }

        [HttpPost]
        public async Task<IntegrationModel> integrate([FromForm] IntegrationModel integrationModel)
        {
            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = auth.Principal.Identities.FirstOrDefault()
                .Claims.Select(claim => new { claim.Type, claim.Value });


            string email = "";
            email = claims.ToList().Where(x => x.Type.Contains("emailaddress")).Select(x => x.Value).ToList()[0];

            integrationModel.Email = email;

            var ac = (await _userSession.GetTodoistAccesscode(email, "Todist")).ToArray();
            var accessCode = ac != null && ac.Length > 0 ? ac[0] : null;

            HttpClient httpClient = new HttpClient();

            List<string> labels = integrationModel.LabelIds != null ? integrationModel.LabelIds.Split(',').ToList() : new List<string>();
            List<string> prioritys = integrationModel.PriorityIds != null ? integrationModel.PriorityIds.Split(',').ToList() : new List<string>();
            List<string> projects = integrationModel.TodoistItemId != null ? integrationModel.TodoistItemId.Split(',').ToList() : new List<string>();
            var param = "";
            var result = "";

            for (var i= 0; i < projects.Count; i++ )
             {
                param = "";
                httpClient.SetBearerToken(accessCode);
                _logger.LogInformation("before post request to add changes to todoist ");
                param = projects[i] != "" ? "?project_id="+projects[i] : "";

                result = (await httpClient.GetAsync("https://api.todoist.com/rest/v1/tasks"+param)).Content.ReadAsStringAsync().Result;
                if (result != null && !result.Equals("[]\n"))
                {
                    var responseAnan = new[] { new { id = "", name = "", content = "", assignee = "", due = new { date = "" }, date_added = "", date_completed = "", description = "", project_id = "", label_ids = new[] { "" }, priority = "" } };
                    var todoistResList = JsonConvert.DeserializeAnonymousType(result, responseAnan);

                    var cred = await this._googleAuth.GetCredentialAsync();

                    var service = new CalendarService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = cred
                    });

                    if (!integrationModel.IsFilter)
                    {
                        if (integrationModel.AssignedFilter != null && integrationModel.AssignedFilter.Equals("1"))
                        {
                            todoistResList = todoistResList.Where(x => x.assignee == null).ToArray();
                        }
                        else
                        {
                            var collaborators = await httpClient.GetAsync("https://api.todoist.com/rest/v1/projects/" + projects[i] + "/collaborators");
                            var collaboratorsRes = await collaborators.Content.ReadAsStringAsync();
                            var collaboratorsAna = new[] { new { id = "", name = "", email = "" } };
                            var collaboratorsResList = JsonConvert.DeserializeAnonymousType(collaboratorsRes, collaboratorsAna);

                            if (integrationModel.AssignedFilter != null && integrationModel.AssignedFilter.Equals("2"))
                            {
                                collaboratorsResList = collaboratorsResList.Where(x => x.email.Equals(email)).ToArray();
                            }
                            else if (integrationModel.AssignedFilter != null && integrationModel.AssignedFilter.Equals("3"))
                            {
                                collaboratorsResList = collaboratorsResList.Where(x => !x.email.Equals(email)).ToArray();
                            }

                            todoistResList = todoistResList.Where(x => collaboratorsResList.Select(y => y.id).ToArray().Contains(x.assignee)).ToArray();
                        }

                        if (integrationModel.LabelIds != null && !integrationModel.LabelIds.Equals(""))
                        {
                            // todoistResList = todoistResList.Where(x => x.label_ids.Contains(integrationModel.LabelIds))
                            var labelIds = integrationModel.LabelIds.Split(",");
                            foreach (var element in labelIds)
                            {
                                todoistResList = todoistResList.Where(x => x.label_ids.Contains(element)).ToArray();
                            }
                        }

                        if (integrationModel.PriorityIds != null && !integrationModel.PriorityIds.Equals(""))
                        {
                            var priorityIds = integrationModel.PriorityIds.Split(",");
                            todoistResList = todoistResList.Where(x => priorityIds.Contains(x.priority)).ToArray();
                        }
                    }
                    List<TodoistTask> todoistTasks = new List<TodoistTask>();
                    TodoistTask todoistTask;
                    foreach(var task in todoistResList)
                    {
                        todoistTask = new TodoistTask { Id = task.id, Assignee = task.assignee, Content = task.content,
                        Name = task.name, DateAdded = task.date_added, DateCompleted = task.date_completed, DueDate = task.due != null ? task.due.date: null, ProjectId = task.project_id,
                        LabelIds = task.label_ids, Priority = task.project_id, Description = task.description};
                        todoistTasks.Add(todoistTask);
                    }
                    if(integrationModel.OperationType.Equals("Assign"))
                    {
                        await AssignTasks(todoistTasks, integrationModel, httpClient, email, accessCode, service);
                    }else if (integrationModel.OperationType.Equals("UnAssign"))
                    {
                        await UnAssign(todoistTasks, integrationModel, email, service);
                    }
                    
                    
                 }
                    _logger.LogInformation("after post and result: " + result);
            }
            if (integrationModel.OperationType.Equals("Assign"))
            {
                await _integrationInterface.SaveIntegration(integrationModel);
            }

            return integrationModel;
        }


        private async Task AssignTasks(List<TodoistTask> todoistResList, IntegrationModel integrationModel
            , HttpClient httpClient, string email, string accessCode, CalendarService service)
        {
            foreach (var todoistTask in todoistResList)
            {
                Event eventD = new Event
                {
                    Summary = todoistTask.Content,
                    Description = todoistTask.Description,
                };

                var startTime = todoistTask.DueDate != null ? todoistTask.DueDate : todoistTask.DateAdded;
                if(startTime == null)
                {
                    startTime = DateTime.Now.ToString("yyyy-MM-dd");
                }
                DateTime startDateTime = Convert.ToDateTime(startTime);
                EventDateTime start = new EventDateTime { DateTime = startDateTime };
                eventD.Start = start;

                var endTime = todoistTask.DueDate != null ? todoistTask.DueDate : todoistTask.DateAdded;
                if(endTime == null)
                {
                    endTime = DateTime.Now.ToString("yyyy-MM-dd");
                }

                DateTime endDateTime = Convert.ToDateTime(endTime);
                var duration = integrationModel.Duration != null ? int.Parse(integrationModel.Duration) : 1440;
                endDateTime = endDateTime.AddMinutes(duration);
                EventDateTime end = new EventDateTime { DateTime = endDateTime };
                eventD.End = end;

                eventD.EndTimeUnspecified = todoistTask.DateCompleted != null;
                _logger.LogInformation("Log before insertion");
                var insertRes = service.Events.Insert(eventD, integrationModel.GoogleCalendarId).Execute();
                _logger.LogInformation("Log after insertion");

                await _integrationInterface.SaveTaskEventHistory(todoistTask.Id, insertRes.Id, email);

            }

            var postBody = "{ \"name\": \"" + integrationModel.NewLabel + "\"}";
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(postBody);
            var content = new ByteArrayContent(messageBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var labelId = "";
            if (todoistResList.Count > 0)
            {

                var labelRes = await httpClient.PostAsync("https://api.todoist.com/rest/v1/labels", content);
                var labelCon = await labelRes.Content.ReadAsStringAsync();
                var labelAnan = new { id = "", name = "" };
                labelId = JsonConvert.DeserializeAnonymousType(labelCon, labelAnan).id;

            }

            //update the tasks in todoist to use the new label
            foreach (var todoistTask in todoistResList)
            {
                //after inserting matching tasks form todoist to google calendar. Add the new label
                postBody = "{ \"label_ids\": [" + labelId + "] }";
                messageBytes = System.Text.Encoding.UTF8.GetBytes(postBody);
                content = new ByteArrayContent(messageBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                httpClient.SetBearerToken(accessCode);
                _logger.LogInformation("before post request to add changes to todoist ");
                var updateRes = await httpClient.PostAsync("https://api.todoist.com/rest/v1/tasks/" + todoistTask.Id, content);

                var updateCon = updateRes.Content.ReadAsStringAsync().Result;
                _logger.LogInformation("after post and result: " + updateCon);
            }
        }

        private async Task UnAssign(List<TodoistTask> todoistResList, IntegrationModel integrationModel,
            string email, CalendarService service)
        {
            foreach (var todoistTask in todoistResList)
            {
                var events = await _integrationInterface.GetTaskEventHistoryById("Task", email, todoistTask.Id);
                foreach (var eventD in events)
                {
                    var deleteRes = service.Events.Delete(integrationModel.GoogleCalendarId, eventD.EventId).Execute();
                }
            }


        }
    }
}
