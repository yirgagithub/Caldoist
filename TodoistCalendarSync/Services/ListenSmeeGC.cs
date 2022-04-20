using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smee.IO.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TodoistCalendarSync.DataContext;

namespace TodoistCalendarSync.Services
{
    public class ListenSmeeGC : BackgroundService
    {
        private readonly ILogger<ListenSmeeGC> _logger;
        private readonly IGoogleAuthProvider _auth;
        private readonly IServiceProvider _serviceProvider;
        private IUserSession _userSession;
        public ListenSmeeGC(ILogger<ListenSmeeGC> logger, IGoogleAuthProvider auth, IServiceProvider serviceProvider, IUserSession userSession)
        {
            _logger = logger;
            _auth = auth;
            _serviceProvider = serviceProvider;
            _userSession = userSession;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            HttpClient httpClient = new HttpClient();
           
            _logger.LogWarning("Start my smee GC");
            var smeeCli = new SmeeClient(new Uri("https://smee.io/xfVKl3FOgNPtcF"));

            smeeCli.OnConnect += (sender, args) => _logger.LogInformation("Connected to SMEE.io GC");
            smeeCli.OnMessage += async (sender, smeeEvent) => {

                var responseStr = JsonConvert.SerializeObject(smeeEvent).Replace("x-goog-resource-uri", "x_googgle_resource_uri");
                var ananmousObj = new { Data =  new { Headers = new { x_googgle_resource_uri = "" } } };

                var resourceURI = JsonConvert.DeserializeAnonymousType(responseStr, ananmousObj).Data.Headers.x_googgle_resource_uri;
                
                using(IServiceScope scope = _serviceProvider.CreateScope())
                {

                    IUserSession auth = scope.ServiceProvider.GetRequiredService<IUserSession>();

                    
                }

                //var cred1 = await this._auth.GetCredentialAsync();
                //httpClient.SetBearerToken(cred1.UnderlyingCredential.GetAccessTokenForRequestAsync().Result);
                //var f = await httpClient.GetAsync("https://www.googleapis.com/calendar/v3/calendars/saytoyirga@gmail.com/events?alt=json?key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ");
                //var d = f.Content.ReadAsStringAsync();

                //var newChange = httpClient.GetAsync("https://www.googleapis.com/calendar/v3/calendars/saytoyirga@gmail.com/events?alt=json?key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ&syncToken=CJDO1fHz7_YCEJDO1fHz7_YCGAUgmP7s0AE=").Result.Content.ReadAsStringAsync();

                //var cred = await this._auth.GetCredentialAsync();
                //var service = new CalendarService(new BaseClientService.Initializer
                //{
                //    HttpClientInitializer = cred
                //});

                //httpClient.SetBearerToken("AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ");

                var response = await httpClient.GetAsync(resourceURI+"&key=AIzaSyCpAYo3AZBhCdTZCXDHdWDzivu8PYAKDqQ");
                var result = response.Content.ReadAsStringAsync();
                
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(JsonConvert.SerializeObject(smeeEvent)); // This is a typed object.
                Console.ResetColor();
                Console.WriteLine();
            };

            smeeCli.OnPing += (sender, a) => Console.WriteLine("Ping from Smee GC");
            smeeCli.OnError += (sender, e) => Console.WriteLine("Error was raised (Disconnect/Anything else GC: " + e.Message);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {

                eventArgs.Cancel = true;
            };


            await smeeCli.StartAsync(stoppingToken);

        }

    }
}
