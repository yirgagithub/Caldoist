using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smee.IO.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TodoistCalendarSync.Services
{
    public class ListenSmee : BackgroundService
    {
        private readonly ILogger<ListenSmee> _logger;
        public ListenSmee(ILogger<ListenSmee> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("Start my smee");
            var smeeCli = new SmeeClient(new Uri("https://smee.io/tKAHsCCIJcGgado"));

            smeeCli.OnConnect += (sender, args) => _logger.LogInformation("Connected to SMEE.io");
            smeeCli.OnMessage += (sender, smeeEvent) => {
                _logger.LogInformation($"Message: {JsonConvert.SerializeObject(smeeEvent)}");
                Console.Write("Message received: ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(JsonConvert.SerializeObject(smeeEvent)); // This is a typed object.
                Console.ResetColor();
                Console.WriteLine();
            };

            smeeCli.OnPing += (sender, a) => Console.WriteLine("Ping from Smee");
            smeeCli.OnError += (sender, e) => Console.WriteLine("Error was raised (Disconnect/Anything else: " + e.Message);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
               
                eventArgs.Cancel = true;
            };

           
            await smeeCli.StartAsync(stoppingToken); 
        }
    }
}
