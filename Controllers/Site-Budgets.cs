using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Budget.Controllers
{
    // ViewModel для добавления/редактирования бюджета
    public class BudgetAddViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sum { get; set; }
        public List<string> Categories { get; set; }
        public Dictionary<string, string> Errors { get; set; }
    }
    // Модель для отображения бюджета
    public class BudgetViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Sum { get; set; }
        public double Current { get; set; }
        //public List<string> Categories { get; set; }
    }

    public partial class SiteController : Controller
    {
        //
        // GET: /Site/Budgets
        public async Task<IActionResult> Budgets()
        {
            string user_id = User.FindFirst(x => x.Type == "id").Value;
            var budgets = await db.GetCollection<BsonDocument>(user_id)
                                    .Find(new BsonDocument("type", $"budget"))
                                    .ToListAsync();

            List<BudgetViewModel> Budgets = new List<BudgetViewModel>();

            if (budgets != null)
            {
                //По-умолчанию ставим текущий месяц
                DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 00, 00, 00);
                DateTime lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 59);

                //Получаем данные по категориям и подкатегориям
                var builder = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = builder.Eq("type", "out") & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
                var result = await db.GetCollection<BsonDocument>(user_id).Aggregate().Match(filter)
                                        .Group(new BsonDocument {
                                            {"_id", new BsonDocument{
                                                {"category", "$category"},
                                                {"subcategory", "$subcategory"}}},
                                            {"sum", new BsonDocument("$sum", "$sum")}})
                                        .ToListAsync();

                foreach (var budget in budgets)
                {
                    double counter = 0;
                    foreach (BsonDocument category in result)
                    {
                        foreach (BsonDocument line in budget["categories"].AsBsonArray)
                        {
                            if (line["category"] == category["_id"]["category"] && (!line.Contains("subcategory") || line["subcategory"] == category["_id"]["subcategory"])) counter += category["sum"].ToDouble();
                        }
                    }
                    Budgets.Add(new BudgetViewModel { Id = budget["_id"].AsString, Name = budget["name"].AsString, Sum = budget["sum"].AsDouble, Current = counter });
                }
            }

            ViewBag.Budgets = Budgets.OrderBy(x => x.Name).ToList();
            return View();
        }

        //
        // GET: /Site/BudgetAdd
        [HttpGet]
        public async Task<IActionResult> BudgetAdd()
        {
            BudgetAddViewModel model = new BudgetAddViewModel
            {
                //Name = "",
                //Sum = "",
                //Categories = new List<string>(),
                Errors = new Dictionary<string, string>()
            };

            string user_id = User.FindFirst(x => x.Type == "id").Value;
            var categories = await db.GetCollection<BsonDocument>(user_id)
                                    .Find(new BsonDocument("_id", $"{user_id}_out"))
                                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                    .FirstOrDefaultAsync();
            var dict = categories.ToDictionary();
            if (dict != null)
            {
                ViewBag.Dict = dict.Values.First();
                ViewBag.Parrent = dict.Keys.First();
                return View(model);
            }
            else return NotFound();
        }
        //
        // POST: /Site/BudgetAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetAdd(BudgetAddViewModel model)
        {
            string user_id = User.FindFirst(x => x.Type == "id").Value;
            int errors_counter = 0;
            model.Errors = new Dictionary<string, string>();
            if (ModelState.IsValid && !String.IsNullOrEmpty(model.Sum) && !String.IsNullOrEmpty(model.Name) && model.Categories != null && model.Categories.Count > 0)
            {
                //Валидация суммы
                double sum = 0;
                model.Sum = model.Sum.Replace(".", ",");
                model.Sum = model.Sum.Replace(" ", "");
                if (!Double.TryParse(model.Sum, out sum))
                {
                    model.Errors.Add("Sum", "Необходимо ввести число");
                    errors_counter++;
                }
                sum = Math.Round(sum, 2);
                //Валидация названия
                if (model.Name.Length > 70)
                {
                    model.Errors.Add("Name", "Максимальная длина строки: 70 символов");
                    errors_counter++;
                }
                //Если ошибок нет, то пишем в базу и идем к списку бюджетов
                if (errors_counter == 0)
                {
                    //Собираем массив для хранения категорий
                    BsonArray array = new BsonArray();
                    foreach (string line in model.Categories)
                    {
                        string category = "";
                        string subcategory = "";
                        int index = line.IndexOf('.'); //Ищем точку как разделитель
                        if (index >= 0) //Если точка есть
                        {
                            category = line.Substring(0, index);
                            subcategory = line.Substring(index + 1);
                            if (array.Contains(new BsonDocument("category", category))) continue; //Если есть вся категория, то подкатегории не вносим
                            array.Add(new BsonDocument { { "category", category }, { "subcategory", subcategory } });
                        }
                        else //Если точки нет
                        {
                            category = line;
                            array.Add(new BsonDocument { { "category", category } });
                        }
                    }
                    if (String.IsNullOrEmpty(model.Id))
                    {
                        //Записываем в базу новый бюджет
                        string new_id = ObjectId.GenerateNewId().ToString();
                        var budget = new BsonDocument
                        {
                        { "_id" , new_id },
                        { "type" , "budget" },
                        { "name", model.Name },
                        { "sum", sum },
                        { "categories", array }
                        };
                        await db.GetCollection<BsonDocument>(user_id).InsertOneAsync(budget);
                    }
                    else
                    {
                        //Редактируем бюджет, если он уже есть
                        var budget_finder = db.GetCollection<BsonDocument>(user_id).Find(new BsonDocument("_id", model.Id)).Count();
                        if (budget_finder > 0)
                        {
                            var filter = new BsonDocument { { "_id", model.Id } };
                            var budget = new BsonDocument
                                {
                                { "_id" , model.Id },
                                { "type" , "budget" },
                                { "name", model.Name },
                                { "sum", sum },
                                { "categories", array }
                                };
                            var result = await db.GetCollection<BsonDocument>(user_id).ReplaceOneAsync(filter, budget);
                        }
                    }
                    return Redirect("/Site/Budgets/");
                }
            }
            else model.Errors.Add("All", "Необходимо выбрать категории, указать название и сумму");

            var categories = await db.GetCollection<BsonDocument>(user_id)
                                .Find(new BsonDocument("_id", $"{user_id}_out"))
                                .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                .FirstOrDefaultAsync();
            var dict = categories.ToDictionary();
            if (dict != null)
            {
                ViewBag.Dict = dict.Values.First();
                ViewBag.Parrent = dict.Keys.First();
                return View(model);
            }
            else return NotFound();
        }

        //
        // GET: /Site/BudgetDelForm
        public IActionResult BudgetDelForm(string id, string text)
        {
            ViewData["id"] = id;
            ViewData["text"] = text;
            return PartialView();
        }
        //
        // POST: /Site/BudgetDel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetDel(string id)
        {
            if (ModelState.IsValid && !String.IsNullOrEmpty(id))
            {
                string user_id = User.FindFirst(x => x.Type == "id").Value;
                var result = await db.GetCollection<BsonDocument>(user_id).DeleteOneAsync(new BsonDocument("_id", id));
                if (result.DeletedCount > 0) return Redirect($"~/Site/Budgets");
            }
            return Redirect($"~/Site/Budgets");
        }

        //
        // GET: /Site/BudgetEdit
        public async Task<IActionResult> BudgetEdit(string id)
        {
            string user_id = User.FindFirst(x => x.Type == "id").Value;
            var budget = await db.GetCollection<BsonDocument>(user_id).Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();
            if (budget != null)
            {
                //Собираем категории в массив строк
                List<string> list = new List<string>();
                foreach (BsonDocument line in budget["categories"].AsBsonArray)
                {
                    if (line.Contains("subcategory")) list.Add($"{line["category"]}.{line["subcategory"]}");
                    else list.Add($"{line["category"]}");
                }
                //Готовим модель
                BudgetAddViewModel model = new BudgetAddViewModel
                {
                    Id = budget["_id"].ToString(),
                    Name = budget["name"].ToString(),
                    Sum = ((double)budget["sum"]).ToString("### ### ### ###.##").Trim(),
                    Categories = list,
                    Errors = new Dictionary<string, string> { }
                };
                //Готовим список категорий
                var categories = await db.GetCollection<BsonDocument>(user_id)
                    .Find(new BsonDocument("_id", $"{user_id}_out"))
                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                    .FirstOrDefaultAsync();
                var dict = categories.ToDictionary();

                ViewBag.Dict = dict.Values.First();
                ViewBag.Parrent = dict.Keys.First();
                return View("~/Views/Site/BudgetAdd.cshtml", model);
            }
            else
            {
                ViewData["text"] = "Не найдено такой записи.";
                return View("~/Views/Shared/_Fail.cshtml");
            }
        }
        // Класс для сериализации
        [DataContract]
        public class Budget_Data
        {
            [DataMember]
            public string period;
            [DataMember]
            public double budget = 0;
            [DataMember]
            public double excess = 0;

            public Budget_Data(string period, double budget, double excess)
            {
                this.period = period;
                this.budget = budget;
                this.excess = excess;
            }
            public Budget_Data(string period)
            {
                this.period = period;
            }

        }

        //
        // GET: /Site/BudgetEdit
        public async Task<IActionResult> BudgetAnnual(string id)
        {
            int Year = DateTime.Today.Year; // Год ставим текущий
            string user_id = User.FindFirst(x => x.Type == "id").Value;
            var budget = await db.GetCollection<BsonDocument>(user_id).Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();

            if (budget != null)
            {
                //По-умолчанию ставим текущий год
                DateTime firstDay = new DateTime(Year, 1, 1, 00, 00, 00);
                DateTime lastDay = new DateTime(Year, 12, 31, 23, 59, 59);
                //Получаем данные за год
                FilterDefinition<BsonDocument> filter = new BsonDocument
                                                        {
                                                            {"type", "out"},
                                                            {"date", new BsonDocument
                                                            {   {"$gte", firstDay},
                                                                {"$lte", lastDay}
                                                            } },
                                                            {"$or", budget["categories"] }
                                                        };
                var result = await db.GetCollection<BsonDocument>(user_id).Aggregate().Match(filter)
                    .Group(new BsonDocument{
                    { "_id", new BsonDocument("$month", "$date") },
                    { "sum", new BsonDocument("$sum", "$sum") }
                    }).ToListAsync();

                Budget_Data[] list = new Budget_Data[13];
                string[] monthes_ru = new string[] { "ЯНВ", "ФЕВ", "МАР", "АПР", "МАЙ", "ИЮН", "ИЮЛ", "АВГ", "СЕН", "ОКТ", "НОЯ", "ДЕК", Year.ToString() };
                //Инициализация массива данных значениями по-умолчанию
                for (int i = 0; i <= 12; i++)
                {
                    list[i] = new Budget_Data(monthes_ru[i]);
                }

                //Счетчики для вычисления средних значений
                int sum_count = 0;
                double sum_sum = 0;
                //Перебор данных из базы
                foreach (var month in result)
                {
                    int i = month["_id"].AsInt32;
                    if (month["sum"].AsDouble != 0)
                    {
                        if (month["sum"].AsDouble >= budget["sum"].AsDouble)
                        {
                            list[i - 1].excess += month["sum"].AsDouble - budget["sum"].AsDouble;
                            list[i - 1].budget += budget["sum"].AsDouble;
                        }
                        else
                        {
                            list[i - 1].budget += month["sum"].AsDouble;
                        }
                        sum_count++;
                        sum_sum += month["sum"].AsDouble;
                    }
                }
                //Выводим средние значения за год
                double sum_avg = Math.Round(sum_sum / sum_count, 2);
                if (sum_avg >= budget["sum"].AsDouble)
                {
                    list[12].excess += sum_avg - budget["sum"].AsDouble;
                    list[12].budget += budget["sum"].AsDouble;
                }
                else
                {
                    list[12].budget = sum_avg;
                }

                ViewBag.Id = id;
                ViewBag.Year = Year;
                ViewBag.Data = JsonConvert.SerializeObject(list);
                return PartialView();
            }
            else return NotFound();
        }
        //////////////////////////////////////////////
        //////////////////////////////////////////////
        //////////////////////////////////////////////
        //////////////////////////////////////////////
        [HttpPost]
        public async Task<IActionResult> BudgetAdd_2(string sum, string[] categories)
        {
            BsonArray array = new BsonArray();

            foreach (string line in categories)
            {
                string category = "";
                string subcategory = "";
                int index = line.IndexOf('.');
                if (index >= 0)
                {
                    category = line.Substring(0, index);
                    subcategory = line.Substring(index + 1);
                    if(array.Contains(new BsonDocument ("category", category))) continue;
                    array.Add(new BsonDocument
                    {
                        {"category", category},
                        {"subcategory", subcategory}
                    });
                }
                else
                {
                    category = line;
                    array.Add(new BsonDocument
                    {
                        {"category", category}
                    });
                }
            }

            //Пытаемся посмотреть результат на текущем годе
            //По-умолчанию ставим текущий год
            DateTime firstDay = new DateTime(DateTime.Today.Year, 1, 1, 00, 00, 00);
            DateTime lastDay = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);
            //Получаем данные
            string user_id = User.FindFirst(x => x.Type == "id").Value;
            
            //var builder = Builders<BsonDocument>.Filter;
            //FilterDefinition<BsonDocument> filter1 = builder.Eq("type", "out") & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
            //var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
            //var temp = filter1.Render(documentSerializer, BsonSerializer.SerializerRegistry);
            //FilterDefinition<BsonDocument> filter2 = "{ type: \"out\", date : { $gte : ISODate(\"2017-01-01T21:00:00Z\"), $lte : ISODate(\"2017-04-29T21:00:00Z\")}}";
            FilterDefinition<BsonDocument> filter = new BsonDocument
            {
                {"type", "out"},
                {"date", new BsonDocument
                {   {"$gte", firstDay},
                    {"$lte", lastDay}
                } },
                {"$or", array }

            };

            List<string> list = new List<string>();
            foreach ( BsonDocument line in array)
            {
                if (line.Contains("subcategory")) list.Add($"{line["category"]}.{line["subcategory"]}");
                else list.Add($"{line["category"]}");
            }

            var result = await db.GetCollection<BsonDocument>(user_id).Aggregate().Match(filter)
                .Group(new BsonDocument{
                    { "_id", new BsonDocument("$month", "$date") },
                    { "sum", new BsonDocument("$sum", "$sum") }
                }).ToListAsync();

            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter2;
            filter2 = builder.Eq("type", "out") & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
            var pie_result = await db.GetCollection<BsonDocument>(user_id).Aggregate().Match(filter2)
            .Group(new BsonDocument {
                    { "_id", new BsonDocument{
                        {"category", "$category"},
                        {"subcategory", "$subcategory"}
                                              }},
                    { "sum", new BsonDocument("$sum", "$sum") }
            }).ToListAsync();

            double counter = 0;
            foreach(BsonDocument element in pie_result)
            {
                foreach(BsonDocument line in array)
                {
                    if (line["category"] == element["_id"]["category"] && (!line.Contains("subcategory") || line["subcategory"]== element["_id"]["subcategory"])) counter += element["sum"].ToDouble();
                }
            }


            var categories2 = await db.GetCollection<BsonDocument>(user_id)
                                    .Find(new BsonDocument("_id", $"{user_id}_out"))
                                    .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                                    .FirstOrDefaultAsync();
            var dict = categories2.ToDictionary();

            ViewBag.Dict = dict.Values.First();
            ViewBag.Parrent = dict.Keys.First();
            ViewBag.Filters = list;
            ViewBag.Result = result;
            ViewBag.Sum = sum;
            return View();
        }







    }
}
