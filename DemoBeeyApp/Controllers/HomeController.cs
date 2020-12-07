using Beey.Proxy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DemoBeeyApp.Controllers
{
    [Route("/")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BeeyProxy beey;

        public HomeController(ILogger<HomeController> logger, BeeyProxy beey)
        {
            _logger = logger;
            this.beey = beey;
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return Json(beey);
        }
    }
}
