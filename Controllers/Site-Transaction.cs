using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Budget.Controllers
{
    public class TransactionAddViewModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Text { get; set; }
        public string Sum { get; set; }
        public string Date { get; set; }
        public Dictionary<string, string> Errors { get; set; }
    }

    public partial class SiteController : Controller
    {
        //
        // GET: /Site/TransactionAdd
        public IActionResult TransactionAdd(string type = "out", string category = "", string subcategory = "", string text = "", string sum = "")
        {
            TransactionAddViewModel model = new TransactionAddViewModel
            {
                Id = "",
                Type = type,
                Category = category,
                Subcategory = subcategory,
                Text = text,
                Sum = sum,
                Date = DateTime.Now.ToString("dd MMMMM yyyy", new CultureInfo("en-US")),
                Errors = new Dictionary<string, string> { }
            };

            return PartialView(model);
        }

        //
        // POST: /Site/TransactionAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransactionAdd(TransactionAddViewModel model)
        {
            model.Errors = new Dictionary<string, string> { };
            if (ModelState.IsValid)
            {
                int errors_counter = 0;
                DateTime date = DateTime.Now;
                double sum = 0;
                //Валидация даты
                if (String.IsNullOrEmpty(model.Date) || !DateTime.TryParseExact(model.Date, "dd MMMMM yyyy", new CultureInfo("en-US"), DateTimeStyles.AssumeUniversal, out date))
                {
                    model.Date = DateTime.Now.ToString("dd MMMMM yyyy", new CultureInfo("en-US"));
                    model.Errors.Add("Date", "Нераспознанный формат даты");
                    errors_counter++;
                }
                //Валидация суммы
                if (!String.IsNullOrEmpty(model.Sum))
                {
                    model.Sum = model.Sum.Replace(".", ",");
                    model.Sum = model.Sum.Replace(" ", "");
                    if (!Double.TryParse(model.Sum, out sum))
                    {
                        model.Errors.Add("Sum", "Необходимо ввести число");
                        errors_counter++;
                    }
                    sum = Math.Round(sum, 2);
                    //model.Sum = sum.ToString();
                }
                else
                {
                    model.Errors.Add("Sum", "Необходимо ввести число");
                    errors_counter++;
                }
                //Валидация описания
                if (String.IsNullOrEmpty(model.Text)) model.Text = "-";
                else if (model.Text.Length > 70)
                {
                    model.Errors.Add("Text", "Максимальная длина строки: 70 символов");
                    errors_counter++;
                }
                //Валидация категорий
                if (String.IsNullOrEmpty(model.Subcategory)) model.Subcategory = "-";
                if (String.IsNullOrEmpty(model.Category) || model.Category.Length > 70 || model.Subcategory.Length > 70)
                {
                    model.Errors.Add("Category", "Неверно указаны категории");
                    errors_counter++;
                }

                //Валидация типа
                if (model.Type != "in" && model.Type != "out")
                {
                    model.Errors.Add("All", "Некорректные данные");
                    errors_counter++;
                }

                if (errors_counter == 0)
                {
                    string id = "";
                    string user_collection = User.FindFirst(x => x.Type == "id").Value;
                    var collection = db.GetCollection<BsonDocument>(user_collection);

                    if (String.IsNullOrEmpty(model.Id))
                    {
                        id = ObjectId.GenerateNewId().ToString();
                        var transaction = new BsonDocument
                        {
                            { "_id", id},
                            { "type", model.Type},
                            { "sum", sum},
                            { "text", model.Text},
                            { "category", model.Category},
                            { "subcategory", model.Subcategory},
                            { "date", date}
                        };
                        await collection.InsertOneAsync(transaction);
                        //ViewData["text"] = "Запись добавлена. Чтобы её увидеть, необходимо обновить страницу.";
                        //return PartialView("~/Views/Shared/_Success.cshtml");
                        return Content("<script>location = location.href='?error=add_success'</script>");
                    }
                    else
                    {
                        id = model.Id;
                        var transaction_old = await collection.Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();
                        if (transaction_old != null)
                        {
                            var filter = new BsonDocument { { "_id", id } };
                            var transaction = new BsonDocument
                                {
                                    { "_id", id},
                                    { "type", model.Type},
                                    { "sum", sum},
                                    { "text", model.Text},
                                    { "category", model.Category},
                                    { "subcategory", model.Subcategory},
                                    { "date", date}
                                };
                            var result = await collection.ReplaceOneAsync(filter, transaction);
                            if (result.ModifiedCount > 0)
                            {
                                //ViewData["text"] = "Запись обновлена. Чтобы её увидеть, необходимо обновить страницу.";
                                //return PartialView("~/Views/Shared/_Success.cshtml");
                                return Content("<script>location = location.href='?error=edit_success'</script>");
                            }
                            else
                            {
                                ViewData["text"] = "Запись не обновлена.";
                                return PartialView("~/Views/Shared/_Fail.cshtml");
                            }
                        }
                        else
                        {
                            ViewData["text"] = "Не найдено записи для редактирования.";
                            return PartialView("~/Views/Shared/_Fail.cshtml");
                        }
                    }
                }
            }
            else model.Errors.Add("All", "Некорректные данные");
            return PartialView(model);
        }

        //
        // GET: /Site/TransactionCategoryEditForm
        [HttpGet]
        public async Task<IActionResult> TransactionCategoryEditForm(string type)
        {
            string user_collection = User.FindFirst(x => x.Type == "id").Value;
            var collection = db.GetCollection<BsonDocument>(user_collection);
            var categories = await collection
                                    .Find(new BsonDocument("_id", $"{user_collection}_{type}"))
                                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                    .FirstOrDefaultAsync();
            var dict = categories.ToDictionary();
            if (dict != null)
            {
                ViewBag.Dict = dict.Values.First();
                ViewBag.Parrent = dict.Keys.First();
                return PartialView();
            }
            else return NotFound();
        }

        //
        // GET: /Site/TransactionEdit
        public async Task<IActionResult> TransactionEdit(string id)
        {
            string user_collection = User.FindFirst(x => x.Type == "id").Value;
            var collection = db.GetCollection<BsonDocument>(user_collection);
            var transaction = await collection.Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();
            if (transaction != null)
            {
                TransactionAddViewModel model = new TransactionAddViewModel
                {
                    Id = transaction["_id"].ToString(),
                    Type = transaction["type"].ToString(),
                    Category = transaction["category"].ToString(),
                    Subcategory = transaction["subcategory"].ToString(),
                    Text = transaction["text"].ToString(),
                    Sum = ((double)transaction["sum"]).ToString("### ### ### ###.##").Trim(),
                    Date = (Convert.ToDateTime(transaction["date"].ToString())).ToString("dd MMMMM yyyy", new CultureInfo("en-US")),
                    Errors = new Dictionary<string, string> { }
                };
                return PartialView("~/Views/Site/TransactionAdd.cshtml", model);
            }
            else
            {
                ViewData["text"] = "Не найдено такой записи.";
                return PartialView("~/Views/Shared/_Fail.cshtml");
            }
        }

        //
        // GET: /Site/TransactionDelForm
        public IActionResult TransactionDelForm(string id, string text)
        {
            ViewData["id"] = id;
            ViewData["text"] = text;
            return PartialView();
        }

        //
        // POST: /Site/TransactionDel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransactionDel(string id)
        {
            if (ModelState.IsValid && !String.IsNullOrEmpty(id))
            {
                string user_collection = User.FindFirst(x => x.Type == "id").Value;
                var collection = db.GetCollection<BsonDocument>(user_collection);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var result = await collection.DeleteOneAsync(filter);
                if(result.DeletedCount>0) return Redirect($"~/Site/?error=del_success");
            }
            return Redirect($"~/Site/?error=del_fail");
        }










    }
}
