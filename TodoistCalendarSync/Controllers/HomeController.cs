using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Hopesoftware.TodoistCalendarSync.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RoundTheCode.GoogleAuthentication.Models;
using Smee.IO.Client;
using Todoist.Net;
using TodoistCalendarSync.Models;
using TodoistCalendarSync.Services;
using static System.Net.Mime.MediaTypeNames;

namespace RoundTheCode.GoogleAuthentication.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserSession _userSession;
        private readonly IIntegrationInterface _integrationInterface;
        private readonly IGoogleAuthProvider _googleAuth;
        private readonly GoogleCalendarInterface _googleCalendar;
        private readonly TodoistInterface _todoistInterface;

        static readonly object _object = new object();

        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor, IUserSession userSession,
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

        [AllowAnonymous]
        [Route("connect")]
        public async Task<IActionResult> ConnectTodoist()
        {
            var client = new HttpClient();

            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = "https://todoist.com/oauth/authorize",
                ClientId = "283608a9cc824fb2a61c49fbbe7e5e24",
                ClientSecret = "e039cafdd25448e8ae7a0e4b424d1f16",
                Scope = "data:read,data:read_write,data:delete",

            });

            return (IActionResult)response;
        }

        [AllowAnonymous]
        public async Task<IActionResult> IndexAsync()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index");
        }

        public IActionResult IntegrationHome()
        {


            return View("ConnectHome");
        }

        [GoogleScopedAuthorize(CalendarService.ScopeConstants.Calendar)]
        public async Task<IActionResult> Privacy([FromServices] IGoogleAuthProvider _auth)
        {

            var routeData = _httpContextAccessor.HttpContext.Response;

            var code = "";
            if (!String.IsNullOrEmpty(HttpContext.Request.Query["code"]))
                code = HttpContext.Request.Query["code"];

            var state = "";
            if (!String.IsNullOrEmpty(HttpContext.Request.Query["state"]))
                state = HttpContext.Request.Query["state"];

            var cred = await _auth.GetCredentialAsync();

            HttpClient httpClient = new HttpClient();

            string accessCode = "";
            var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = auth.Principal.Identities.FirstOrDefault()
                .Claims.Select(claim => new { claim.Type, claim.Value });

            string email = "";
            email = claims.ToList().Where(x => x.Type.Contains("emailaddress")).Select(x => x.Value).ToList()[0];

            var googleAccessToken = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync();

            //Listen to events in google calendar
            await this.ListenToCalendarEvents(email, googleAccessToken);
            await this.ListenToTodistEvent(email, _auth);

            //get access code for todoist
            var ac = (await _userSession.GetTodoistAccesscode(email, "Todist")).ToArray();
            accessCode = ac != null && ac.Length > 0 ? ac[0] : null;

            //get auth code for todoist
            var savedAuthCodeList = (await _userSession.GetTodoistAccesscode(email, "Todist.Auth")).ToArray();
            var savedAuthCode = savedAuthCodeList != null && savedAuthCodeList.Length > 0 ? savedAuthCodeList[0] : null;

            //if thre is a new auth code generate a new accesscode and save it
            if ((code != null && !code.Equals("") && accessCode == null) || (code != null && !code.Equals(savedAuthCode)))
            {

                await _userSession.SaveAccessCode(code, email, "Todist.Auth");

                var baseUri = new Uri($"https://todoist.com/oauth/access_token");
                TodoistAccessRequest todoistAccessRequest = new TodoistAccessRequest
                {
                    client_id = "283608a9cc824fb2a61c49fbbe7e5e24",
                    client_secret = "e039cafdd25448e8ae7a0e4b424d1f16",
                    code = code,
                    redirect_uri = "https://localhost:8888/Home/Privacy"
                };

                var requestBody = System.Text.Json.JsonSerializer.Serialize(todoistAccessRequest);

                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
                var content = new ByteArrayContent(messageBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var result = await httpClient.PostAsync("https://todoist.com/oauth/access_token", content);

                var contents = result.Content.ReadAsStringAsync().Result;

                TodoistAccessCode obj = JsonConvert.DeserializeObject<TodoistAccessCode>(contents);
                accessCode = obj.access_token;

                //save the new acccesscode
                await _userSession.SaveAccessCode(accessCode, email, "Todist");
            }

           
            //check if google calendar is beging watched if not request watch
            var isWatched = await _googleCalendar.IsGoogleCalendarWatched(email);


             if (!isWatched)
            {
                await WatchGoogleCalendar(email); 
                await this.SaveTodoistItem(email);
            }

            var integrationList = await _integrationInterface.GetAll(email);
            return View(integrationList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        //In Production this will be moved to its own call back url
        private async Task ListenToCalendarEvents(string email, string googleAccessToken)
        {
            _logger.LogInformation("Inside calendar event listener");
            //Listen to Google calendar Events 
            var smeeCli = new SmeeClient(new Uri("https://smee.io/xfVKl3FOgNPtcF"));
            var cred = await this._googleAuth.GetCredentialAsync();

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred
            });
            smeeCli.OnConnect += (sender, args) => _logger.LogInformation("Connected to SMEE.io GC");
            smeeCli.OnMessage += async (sender, smeeEvent) =>
            {
                
                //serialize the response messge and extract the google resource uri to load changed events
                var responseStr = JsonConvert.SerializeObject(smeeEvent).Replace("x-goog-resource-uri", "x_googgle_resource_uri");
                _logger.LogInformation("Inside calendar message");
                _logger.LogInformation("Message Received:"+responseStr);

                var ananmousObj = new { Data = new { Headers = new { x_googgle_resource_uri = "" } } };
                var resourceURI = JsonConvert.DeserializeAnonymousType(responseStr, ananmousObj).Data.Headers.x_googgle_resource_uri;

                HttpClient httpClient = new HttpClient();
                httpClient.SetBearerToken(googleAccessToken);

                var syncTokenList = (await _userSession.GetTodoistAccesscode(email, "Google")).ToList();
                var syncToken = syncTokenList.Count > 0 ? syncTokenList.ToList()[0] : null;
                _logger.LogInformation("Synch Token: "+syncToken);

                var url = resourceURI + "&key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ" +(syncToken != null && !syncToken.Equals("") ? "&syncToken=" + syncToken : "");
                var newChangeRes = await httpClient.GetAsync(url);
                var newChangeCon = await newChangeRes.Content.ReadAsStringAsync();
               
                var syncTokenAnan = new { nextSyncToken = "", nextPageToken = "" };
                var newSyncTokens = JsonConvert.DeserializeAnonymousType(newChangeCon, syncTokenAnan);
                var nextPageToken = newSyncTokens.nextPageToken;
                var nextSyncToken = newSyncTokens.nextSyncToken;

                //check if the synch token is valid
                var errorResult = JsonConvert.DeserializeAnonymousType(newChangeCon, new { error = new { errors = new[] { new { reason = "" } } } });
                if(errorResult != null && errorResult.error != null && errorResult.error.errors != null && errorResult.error.errors != null){

                    foreach (var element in errorResult.error.errors)
                    {
                        if(element.reason != null && element.reason.Equals("fullSyncRequired"))
                        {
                            url = resourceURI + "&key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ";
                            newChangeRes = await httpClient.GetAsync(url);
                            newChangeCon = await newChangeRes.Content.ReadAsStringAsync();

                            newSyncTokens = JsonConvert.DeserializeAnonymousType(newChangeCon, syncTokenAnan);
                            nextPageToken = newSyncTokens.nextPageToken;
                            nextSyncToken = newSyncTokens.nextSyncToken;
                        }
                        
                    }
                }

                //if we have a lot of changes the api will return request by paging
                if (nextSyncToken == null)
                {
                    while(nextPageToken != null)
                    {
                        url = resourceURI + "&key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ" + (nextPageToken != null && !nextPageToken.Equals("") ? "&pageToken=" + nextPageToken : "");
                        newChangeRes = await httpClient.GetAsync(url);
                        newChangeCon = await newChangeRes.Content.ReadAsStringAsync();

                        syncTokenAnan = new { nextSyncToken = "", nextPageToken = "" };
                        newSyncTokens = JsonConvert.DeserializeAnonymousType(newChangeCon, syncTokenAnan);

                        nextPageToken = newSyncTokens.nextPageToken;
                        nextSyncToken = newSyncTokens.nextSyncToken;
                    }
                }

                _logger.LogInformation("New changed in calendar: " + newChangeCon);
                _logger.LogInformation("Next synch toekn: " + nextSyncToken != null ? nextSyncToken : "next token null");

                //store calendar token code into table
                var isTokenNew = syncToken == null || (nextSyncToken != null && !nextSyncToken.Equals(syncToken));
                if (isTokenNew && nextSyncToken != null)
                {
                    await _userSession.SaveAccessCode(nextSyncToken, email, "Google");
                }

                //serialize response to update todoist
                var changesAnanObj = new { kind = "", summary = "", items = new[] { new {id="", kind="", summary = "", status = "", start = new { dateTime = "" }, end = new { dateTime = "" }, created = "", updated = "" } } };
                var changes = newChangeCon != null ? JsonConvert.DeserializeAnonymousType(newChangeCon, changesAnanObj) : null;

                if (changes != null && changes.kind != null && changes.kind.Equals("calendar#events"))
                {
                    var integrationList = changes != null ? await _integrationInterface.FindIntegrationByCalendar(changes.summary) : null;
                    _logger.LogInformation("Integrated count: " + integrationList.Count);
                    if (integrationList != null && integrationList.Count > 0)
                    {

                        foreach (var element in integrationList)
                        {
                            foreach (var proj in changes.items)
                            {

                                var isExist = await _integrationInterface.IsChangedCalDoist("Calendar", element.GoogleCalendarId, proj.summary, email);

                                _logger.LogInformation("Loop through changed items");
                                _logger.LogInformation("Value of isExist: " + isExist);
                                _logger.LogInformation("Changed status: " + proj.status);
                                //get the todoist access code
                                var ac = (await _userSession.GetTodoistAccesscode(email, "Todist")).ToArray();
                                var accessCode = ac != null && ac.Length > 0 ? ac[0] : null;

                                //event deleted in google calendar
                                if (proj.status.Equals("cancelled"))
                                {

                                }
                                else if (proj.status.Equals("confirmed") && proj.summary != null)
                                {

                                    //event modified or created in google calendar

                                    var taskIds = await _integrationInterface.GetTaskEventHistoryById("Event", email, proj.id);
                                    string taskId = "";

                                    if (taskIds.Count == 0)
                                    {
                                        
                                        //save the change in todoist to google calendar
                                        string dueDate = proj.start != null && proj.start.dateTime != null ? proj.start.dateTime.Substring(0,10) : DateTime.Now.ToString("yyyy-MM-dd").Substring(0,10);
                                        var postBody = "{ \"content\":\"" + proj.summary+"\", \"due_date\":\""+ dueDate+"\", \"X-Request-Id\": \"123456789\", \"project_id\": " + long.Parse(element.TodoistItemId) + "}";
                                        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(postBody);
                                        var content = new ByteArrayContent(messageBytes);
                                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                                        httpClient.SetBearerToken(accessCode);

                                        if (!taskId.Equals(""))
                                            await _integrationInterface.SaveTaskEventHistory(taskId, proj.id, email);

                                        _logger.LogInformation("before post request to add changes to todoist ");
                                        var result = await httpClient.PostAsync("https://api.todoist.com/rest/v1/tasks", content);

                                        var contents = await result.Content.ReadAsStringAsync();
                                        var responseAnan = new { id = "", name = "", content = "", due = new { date = "" }, date_added = "", date_completed = "", description = "", project_id = "" } ;
                                        var todoistRes = JsonConvert.DeserializeAnonymousType(contents, responseAnan);

                                        //check the event type
                                        var todoistTask = todoistRes;
                                        taskId = todoistTask.id;

                                        _logger.LogInformation("after post and result: " + contents);

                                        await _integrationInterface.SaveHistory("Todoist", element.TodoistItemId, proj.summary, email);

                                      
                                    }
                                    else
                                    {
                                        //update if needed
                                    }

                                }
                            }
                        }
                    }
                }else if(changes != null && changes.items != null && changes.items.Length > 0 && changes.kind !=null && changes.kind.Equals("calendar#calendarList"))
                {
                    //the change is in calendar so sync the calendar and watch for further changes
                    var calendar = changes.items[0];
                    GoogleCalendar googleCalendar = new GoogleCalendar { Id = calendar.id, Kind = calendar.kind, Email = email };
                    await _googleCalendar.Add(googleCalendar);

                    var googleWatch = "{\"id\": \"01234567-yyyyy-cdef-0123456789ab\", \"type\": \"web_hook\", \"address\": \"https://smee.io/xfVKl3FOgNPtcF\"}";
                    var googleWatchByte = System.Text.Encoding.UTF8.GetBytes(googleWatch);
                    var googleWatchcontent = new ByteArrayContent(googleWatchByte);
                    googleWatchcontent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    var googleCalResult = await httpClient.PostAsync("https://www.googleapis.com/calendar/v3/calendars/" + (googleCalendar.isPrimary ? "primary" : googleCalendar.Id) + "/events/watch", googleWatchcontent);


                }

            };

            var tasResult = smeeCli.StartAsync();
        }


        //this will be moved to its own url call back in production
        private async Task ListenToTodistEvent(string email, IGoogleAuthProvider _auth)
        {

               var smeeCli = new SmeeClient(new Uri("https://smee.io/tKAHsCCIJcGgado"));
                var cred = await this._googleAuth.GetCredentialAsync();

                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = cred
                });

                smeeCli.OnConnect += (sender, args) => _logger.LogInformation("Connected to SMEE.io");
                smeeCli.OnMessage += async (sender, smeeEvent) =>
                {

                 
                 var responseSerialized = JsonConvert.SerializeObject(smeeEvent);

                _logger.LogInformation("Inside todoist message");
                _logger.LogInformation("Received Data: " + responseSerialized);

                 //parse the incoming data using this template
                var responseAnan = new { Data = new { Body = new { event_name = "", event_data = new {id="", name="", content = "", due = new { date = "" }, date_added = "", date_completed = "", description = "", project_id = "" } } } };
                var todoistRes = JsonConvert.DeserializeAnonymousType(responseSerialized, responseAnan);

                //check the event type
                var todoistTask = todoistRes.Data.Body.event_data != null ? todoistRes.Data.Body.event_data : null;
                var eventName = todoistRes.Data.Body.event_name != null ? todoistRes.Data.Body.event_name : null;

                    if (eventName != null && (eventName.Contains("project") || eventName.Contains("label")))
                    {
                        if (eventName.Contains("project"))
                        {
                            TodoistProject todoistProject = new TodoistProject { Id = todoistTask.id, Name = todoistTask.name, Email = email };
                            if (eventName.Equals("project:added"))
                            {
                                await _todoistInterface.Add(todoistProject);
                            }
                            else if (eventName.Equals("project:updated"))
                            {
                                await _todoistInterface.Update(todoistProject);
                            }
                            else if (eventName.Equals("project:deleted"))
                            {
                                await _todoistInterface.Delete(todoistProject);
                            }
                        }else if (eventName.Contains("label"))
                        {
                            TodoistLabel todoistLabel = new TodoistLabel { Id = todoistTask.id, Name = todoistTask.name, Email = email };
                            if (eventName.Equals("label:added"))
                            {
                                await _todoistInterface.AddLabel(todoistLabel);
                            }
                            else if (eventName.Equals("label:updated"))
                            {
                                await _todoistInterface.UpdateLabel(todoistLabel);
                            }
                            else if (eventName.Equals("label:deleted"))
                            {
                                await _todoistInterface.DeleteLabel(todoistLabel);
                            }
                        }
                    }
                    else
                    {

                        //check todoist history to block infinite circular listening
                        var isExist = await _integrationInterface.IsChangedCalDoist("Todoist", todoistTask.project_id, todoistTask.content, email);
                        _logger.LogInformation("Exist result: " + isExist);

                        if (!isExist && todoistTask != null && eventName != null)
                        {
                            var integrationList = await _integrationInterface.FindIntegrationByTodoist(todoistTask.project_id);

                            if (integrationList != null && integrationList.Count > 0)
                            {
                                if (eventName.Equals("item:added"))
                                {

                                    foreach (var element in integrationList)
                                    {
                                        _logger.LogInformation("interation for integration list");

                                        Event eventD = new Event
                                        {
                                            Summary = todoistTask.content,
                                            Description = todoistTask.description,
                                        };

                                        var startTime = todoistTask.due != null && todoistTask.due.date != null ? todoistTask.due.date : todoistTask.date_added;
                                        DateTime startDateTime = Convert.ToDateTime(startTime);
                                        EventDateTime start = new EventDateTime { DateTime = startDateTime };
                                        eventD.Start = start;

                                        var endTime = todoistTask.due != null && todoistTask.due != null ? Convert.ToDateTime(todoistTask.due.date) : Convert.ToDateTime(todoistTask.date_added);
                                        var duration = element.Duration != null ? int.Parse(element.Duration) : 1440;
                                        endTime = endTime.AddMinutes(duration);

                                        DateTime endDateTime = Convert.ToDateTime(endTime);
                                        EventDateTime end = new EventDateTime { DateTime = endDateTime };
                                        eventD.End = end;

                                        eventD.EndTimeUnspecified = todoistTask.date_completed != null;
                                        _logger.LogInformation("Log before insertion");
                                        var insertRes = service.Events.Insert(eventD, element.GoogleCalendarId).Execute();
                                        _logger.LogInformation("Log after insertion");
                                        //save in history
                                        await _integrationInterface.SaveHistory("Calendar", element.GoogleCalendarId, todoistTask.content, email);
                                        await _integrationInterface.SaveTaskEventHistory(todoistTask.id, insertRes.Id, email);
                                    }

                                }
                                else if (eventName.Equals("item:updated"))
                                {
                                    var events = await _integrationInterface.GetTaskEventHistoryById("Task", email, todoistTask.id);

                                    if (events != null && (events[0].Createdat - DateTime.Now).TotalMinutes > 2)
                                    {
                                        foreach (var calendar in integrationList)
                                        {
                                            foreach (var eventI in events)
                                            {
                                                Event eventD = service.Events.Get(calendar.GoogleCalendarId, eventI.EventId).Execute();

                                                eventD.Summary = todoistTask.content;
                                                eventD.Description = todoistTask.description;

                                                var startTime = todoistTask.due != null && todoistTask.due.date != null ? todoistTask.due.date : todoistTask.date_added;
                                                DateTime startDateTime = Convert.ToDateTime(startTime);
                                                EventDateTime start = new EventDateTime { DateTime = startDateTime };
                                                eventD.Start = start;

                                                var endTime = todoistTask.due != null && todoistTask.due != null ? Convert.ToDateTime(todoistTask.due.date).AddSeconds(86400) : Convert.ToDateTime(todoistTask.date_added).AddSeconds(86400);
                                                DateTime endDateTime = Convert.ToDateTime(endTime);
                                                EventDateTime end = new EventDateTime { DateTime = endDateTime };
                                                eventD.End = end;

                                                eventD.EndTimeUnspecified = todoistTask.date_completed != null;

                                                var updateRes = service.Events.Update(eventD, calendar.GoogleCalendarId, eventI.EventId).Execute();

                                            }
                                        }
                                    }
                                }
                                else if (eventName.Equals("item:deleted"))
                                {
                                    var events = await _integrationInterface.GetTaskEventHistoryById("Task", email, todoistTask.id);
                                    foreach (var calendar in integrationList)
                                    {
                                        foreach (var eventI in events)
                                        {
                                            var deleteRes = service.Events.Delete(calendar.GoogleCalendarId, eventI.EventId).Execute();
                                        }
                                    }
                                }
                                else if (eventName.Equals("item:completed"))
                                {
                                    var eventIs = await _integrationInterface.GetTaskEventHistoryById("Task", email, todoistTask.id);

                                    foreach (var calendar in integrationList)
                                    {
                                        var completedOptions = calendar.CompletedOptions;

                                        foreach (var eventI in eventIs)
                                        {
                                            if (completedOptions != null && completedOptions.Equals("1"))
                                            {
                                                //Delete on complete                                           
                                                var deleteRes = service.Events.Delete(calendar.GoogleCalendarId, eventI.EventId).Execute();
                                            }
                                            //else if (completedOptions != null && completedOptions.Equals("2"))
                                            //{
                                            //    //Keep on complete so do nothing
                                            //}
                                            else if (completedOptions != null && completedOptions.Equals("3"))
                                            {
                                                //Change color on complete
                                                Event eventD = service.Events.Get(calendar.GoogleCalendarId, eventI.EventId).Execute();
                                                eventD.ColorId = calendar.ColorChange;
                                                Event updateRes = service.Events.Update(eventD, calendar.GoogleCalendarId, eventI.EventId).Execute();
    
                                            }

                                            else if (completedOptions != null && completedOptions.Equals("4"))
                                            {
                                                //move to other calendar on complete
                                                Event eventD = service.Events.Get(calendar.GoogleCalendarId, eventI.EventId).Execute();
                                                var deleteRes = service.Events.Delete(calendar.GoogleCalendarId, eventI.EventId).Execute();
                                                eventD = service.Events.Insert(eventD, calendar.MoveCalendarId).Execute();
                                            }
                                        }
                                    }
                                }


                            }
                        }
                        else
                        {
                            //clear todoist history
                            //_integrationInterface.ClearHistory("Todoist", todoistTask.content, todoistTask.project_id, email);
                        }
                    }

                };

                smeeCli.OnPing += (sender, a) => Console.WriteLine("Ping from Smee");
                smeeCli.OnError += (sender, e) => Console.WriteLine("Error was raised (Disconnect/Anything else: " + e.Message);

                Console.CancelKeyPress += (sender, eventArgs) =>
                {

                    eventArgs.Cancel = true;
                };


                var result = smeeCli.StartAsync();

        }


        public async Task WatchGoogleCalendar(string email)
        {
            var cred = await this._googleAuth.GetCredentialAsync();
            var googleAccessToken = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync();

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred
            });

            HttpClient watchHttpClient = new HttpClient();
            watchHttpClient.SetBearerToken(googleAccessToken);


            //watch calndar list
            string calendarListUri = "https://www.googleapis.com/calendar/v3/users/me/calendarList/watch";
            var calendarListWatchJ = "{\"id\": \"01234567-zzzz-cdef-0123456789ab\", \"type\": \"web_hook\", \"address\": \"https://smee.io/xfVKl3FOgNPtcF\"}";
            var calendarListWatchByte = System.Text.Encoding.UTF8.GetBytes(calendarListWatchJ);
            var calendarListWatchcontent = new ByteArrayContent(calendarListWatchByte);
            var calendarListWatch = await watchHttpClient.PostAsync(calendarListUri, calendarListWatchcontent);
            var calendarListResult = await calendarListWatch.Content.ReadAsStringAsync();


            List<GoogleCalendar> projectList = new List<GoogleCalendar>();
            
            var response = await service.CalendarList.List().ExecuteAsync();
            var responseList = response.Items.Select(x => new { x.Id, x.Summary, x.Kind, x.Primary, email, x.BackgroundColor, x.ColorId});
           
            foreach(var element in responseList)
            {
                GoogleCalendar googleCalendar = new GoogleCalendar { Id = element.Id, Summary = element.Summary, Kind = element.Kind,
                    isPrimary = element.Primary != null ? element.Primary.Value : false, Background = element.BackgroundColor, ColorId = element.ColorId, Email = email };
                projectList.Add(googleCalendar);
            }

            await _googleCalendar.AddRange(projectList);

            //calendar list and watch the changes
            var index = 0;
            foreach (var element in projectList)
            {
                var googleWatch = "{\"id\": \"01234567-zzzz-cdef-0123456789ab" + index + "\", \"type\": \"web_hook\", \"address\": \"https://smee.io/xfVKl3FOgNPtcF\"}";
                var googleWatchByte = System.Text.Encoding.UTF8.GetBytes(googleWatch);
                var googleWatchcontent = new ByteArrayContent(googleWatchByte);
                googleWatchcontent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var googleCalResult = await watchHttpClient.PostAsync("https://www.googleapis.com/calendar/v3/calendars/" + (element.isPrimary ? "primary" : element.Id) + "/events/watch", googleWatchcontent);
                var googleCalResultStr = googleCalResult.Content.ReadAsStringAsync().Result;

                index++;
            }

            await _googleCalendar.AddLookup(email, "Google_CALENDAR_API_WATCH", "ON");
        }

        public async Task SaveTodoistItem(string email)
        {
            
            var ac = (await _userSession.GetTodoistAccesscode(email, "Todist")).ToArray();
            var accessCode = ac != null && ac.Length > 0 ? ac[0] : null;

            var projectsUrl = $"https://api.todoist.com/sync/v8/sync";

            var projectJson = "sync_token=*&resource_types=[\"projects\", \"labels\"]";
            var projectMessageBytes = System.Text.Encoding.UTF8.GetBytes(projectJson);
            var projectContent = new ByteArrayContent(projectMessageBytes);
            projectContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            HttpClient httpClient = new HttpClient();

            httpClient.SetBearerToken(accessCode);

            var projectsResult = await httpClient.PostAsync(projectsUrl, projectContent);
            var projectsContent = projectsResult.Content.ReadAsStringAsync().Result;

            var projectListAnanomous = new {  projects = new[] { new { Name = "", Id = "" } }, labels = new[] { new { Name = "", Id = "" } } };
            var projectList = JsonConvert.DeserializeAnonymousType(projectsContent, projectListAnanomous);

            List<TodoistProject> todoistProjects = new List<TodoistProject>();
            TodoistProject todoistProject;
            foreach (var element in projectList.projects)
            {
                todoistProject  = new TodoistProject { Id = element.Id, Name = element.Name, Email = email};
                todoistProjects.Add(todoistProject);
            }

            await _todoistInterface.AddRange(todoistProjects);

            List<TodoistLabel> todoistLabels = new List<TodoistLabel>();
            TodoistLabel todoistLabel;
            foreach (var element in projectList.labels)
            {
                todoistLabel = new TodoistLabel { Id = element.Id, Name = element.Name, Email = email };
                todoistLabels.Add(todoistLabel);
            }

            await _todoistInterface.AddLabelRange(todoistLabels);

        }
    }
}
