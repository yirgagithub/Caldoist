using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoistCalendarSync.Controllers
{
    public class TodoistController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
