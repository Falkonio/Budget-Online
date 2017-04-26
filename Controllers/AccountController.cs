using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Budget.Controllers
{
    public partial class AccountController : Controller
    {
        IMongoClient client;
        IMongoDatabase db;

        public AccountController(MongoDbSettings settings)
        {
            client = new MongoClient(settings.ConnectionString);
            db = client.GetDatabase(settings.DatabaseName);
        }

        private bool CheckEmail(string email)
        {
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = new BsonDocument("email", email.ToLower());
            if (collection.Find(filter).Count()>0)
                return false;
            return true;
        }



        
    }
}
