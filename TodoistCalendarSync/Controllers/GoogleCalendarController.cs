using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Calendar.v3.Data;

namespace TodoistCalendarSync.Controllers
{

    public class GoogleCalendarController: Controller
    {
        [GoogleScopedAuthorize(CalendarService.ScopeConstants.Calendar)]
        public async Task<CalendarList> Index([FromServices] IGoogleAuthProvider auth)
        {
            var cred = await auth.GetCredentialAsync();
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred
            });

            var response = await service.CalendarList.List().ExecuteAsync();

            var res = service.CalendarList.Watch(new Channel { Id = "123456789", Address = "https://smee.io/xfVKl3FOgNPtcF", Type = "web_hook" });

            return response;
        }
    }
}
