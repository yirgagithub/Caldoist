using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoistCalendarSync.Controllers
{
    public class GCWebhook: Controller
    {
        [HttpPost]
        public async Task Index(string model)
        {
            Console.WriteLine("Webhook");
        }

    }
}
