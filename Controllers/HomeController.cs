using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Budget.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStringLocalizer<HomeController> localizer;
        private readonly IStringLocalizer<SharedResource> sharedLocalizer;
        public IMongoClient client;
        public IMongoDatabase database;

        public HomeController(MongoDbSettings settings, IStringLocalizer<HomeController> _localizer,
                    IStringLocalizer<SharedResource> _sharedLocalizer)
        {
            client = new MongoClient(settings.ConnectionString);
            database = client.GetDatabase(settings.DatabaseName);
            localizer = _localizer;
            sharedLocalizer = _sharedLocalizer;
        }

        public IActionResult Index()
        {
            ViewData["Message"] = localizer["Список ролей:"];
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
