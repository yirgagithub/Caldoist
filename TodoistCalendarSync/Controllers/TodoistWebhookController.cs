using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TodoistCalendarSync.Models;

namespace TodoistCalendarSync.Controllers
{
    public class TodoistWebhookController: Controller
    {
        [HttpPost]
        public Task index([FromBody]TodoistWebhookModel model)
        {
            return null;
        }
    }
}
