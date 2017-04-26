using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Budget.Controllers
{
    [Authorize]
    public partial class SiteController : Controller
    {
        IMongoClient client;
        IMongoDatabase db;

        public SiteController(MongoDbSettings settings)
        {
            client = new MongoClient(settings.ConnectionString);
            db = client.GetDatabase(settings.DatabaseName);
        }
    }
}
