using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;

namespace Budget.Controllers
{
    public partial class SiteController : Controller
    {
        //
        // GET: /Site/Categories
        public async Task<IActionResult> Categories(string error="")
        {
            string user_collection = User.FindFirst(x => x.Type == "id").Value;

            var collection = db.GetCollection<BsonDocument>(user_collection);
            var inputs = await collection
                                    .Find(new BsonDocument("_id", $"{user_collection}_in"))
                                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                    .FirstOrDefaultAsync();
            var outputs = await collection
                                    .Find(new BsonDocument("_id", $"{user_collection}_out"))
                                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                    .FirstOrDefaultAsync();

            var dict_inputs = inputs.ToDictionary();
            var dict_outputs = outputs.ToDictionary();
            if (dict_inputs != null && dict_outputs != null)
            {
                ViewBag.Error = error;
                ViewBag.Inputs_Dict = dict_inputs.Values.First();
                ViewBag.Inputs_Parrent = dict_inputs.Keys.First();
                ViewBag.Outputs_Dict = dict_outputs.Values.First();
                ViewBag.Outputs_Parrent = dict_outputs.Keys.First();
                return View();
            }
            else return NotFound();
        }

        public IActionResult CategoryAddForm(string address, string parent, string type)
        {
            ViewData["address"] = address;
            ViewData["parent"] = parent;
            ViewData["type"] = type;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryAdd(string address, string value, string type)
        {
            string error = "";
            if (ModelState.IsValid)
            {
                if (value.Length < 3 || value.Length > 70) error = "length";
                else
                {
                    value = value.Replace(".", "_");
                    string user_collection = User.FindFirst(x => x.Type == "id").Value;
                    var collection = db.GetCollection<BsonDocument>(user_collection);
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("_id", $"{user_collection}_{type}");
                    var update = Builders<BsonDocument>.Update
                                .Set($"{address}.{value}", new BsonDocument { });
                    var result = await collection.UpdateOneAsync(filter, update);
                    if (result.ModifiedCount > 0) error = "success";
                    else error = "fail";
                }
            }
            return Redirect($"~/Site/Categories/?error={error}");
        }

        public IActionResult CategoryDelForm(string address, string item, string type)
        {
            ViewData["address"] = address;
            ViewData["item"] = item;
            ViewData["type"] = type;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryDel(string address, string type)
        {
            string error = "";
            if (ModelState.IsValid)
            {
                string user_collection = User.FindFirst(x => x.Type == "id").Value;
                var collection = db.GetCollection<BsonDocument>(user_collection);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("_id", $"{user_collection}_{type}");
                var update = Builders<BsonDocument>.Update
                            .Unset($"{address}");
                var result = await collection.UpdateOneAsync(filter, update);
                if (result.ModifiedCount > 0) error = "success";
                else error = "fail";
            }
            return Redirect($"~/Site/Categories/?error={error}");
        }

        public IActionResult CategoryEditForm(string address, string item, string type)
        {
            ViewData["address"] = address;
            ViewData["item"] = item;
            ViewData["type"] = type;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryEdit(string address, string item, string value, string type)
        {
            string error = "";
            if (ModelState.IsValid)
            {
                if (value.Length < 3 || value.Length > 70) error = "length";
                else
                {
                    value = value.Replace(".", "_");
                    string user_collection = User.FindFirst(x => x.Type == "id").Value;
                    var collection = db.GetCollection<BsonDocument>(user_collection);
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("_id", $"{user_collection}_{type}");

                    int length = item.Length + 1;
                    int begin = address.LastIndexOf(item) - 1;
                    string new_address = address.Remove(begin, length);

                    var update = Builders<BsonDocument>.Update
                                .Rename(address, $"{new_address}.{value}");
                    var result = await collection.UpdateOneAsync(filter, update);
                    if (result.ModifiedCount > 0) error = "success";
                    else error = "fail";
                }
            }
            return Redirect($"~/Site/Categories/?error={error}");
        }

    }
}
