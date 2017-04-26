using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Budget.Controllers
{

    [DataContract]
    public class Days_Range
    {
        [DataMember]
        public string firstDay;
        [DataMember]
        public string lastDay;
        [DataMember]
        public string firstDay_Full;
        [DataMember]
        public string lastDay_Full;

        public Days_Range(DateTime firstDay, DateTime lastDay)
        {
            this.firstDay = firstDay.ToString("dd.MM.yyyy");
            this.lastDay = lastDay.ToString("dd.MM.yyyy");
            firstDay_Full = firstDay.ToString("D");
            lastDay_Full = lastDay.ToString("D");
        }
    }

    public partial class SiteController : Controller
    {
        //
        // GET: /Site/Reports_Pie
        public async Task<IActionResult> Reports_Pie(string type="out", string first_day = "", string last_day = "", string month = "", string year = "")
        {
            DateTime firstDay = DateTime.Today;
            DateTime lastDay = DateTime.Today;

            DaysValidator(out firstDay, out lastDay, first_day: first_day, last_day: last_day, month: month, year: year);
            ViewBag.Pie_Data = await Get_Pie_Data(type, first_day: first_day, last_day: last_day, month: month, year: year);
            ViewBag.First_Day_Full = firstDay.ToString("D");
            ViewBag.Last_Day_Full = lastDay.ToString("D");
            if (type == "in") ViewBag.Pie_Title = $"Структура доходов в период с {ViewBag.First_Day_Full} по {ViewBag.Last_Day_Full}";
            else if (ViewBag.Pie_Data == "[]" || String.IsNullOrEmpty(ViewBag.Pie_Data)) ViewBag.Pie_Title = "Нет данных за указанный период";
            else ViewBag.Pie_Title = ViewBag.Pie_Title = $"Структура расходов в период с {ViewBag.First_Day_Full} по {ViewBag.Last_Day_Full}";
            ViewBag.First_Day = firstDay.ToString("dd.MM.yyyy");
            ViewBag.Last_Day = lastDay.ToString("dd.MM.yyyy");
            return View();
        }

        public string Get_Days(string month = "", string year = "")
        {
            //По-умолчанию ставим текущий месяц
            DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
            //Пытаемся пропарсить месяц, если он получен
            if (!String.IsNullOrEmpty(month))
            {
                DateTime month_days;
                if (DateTime.TryParseExact(month, "MM.yyyy", null, DateTimeStyles.None, out month_days))
                {   //Удалось получить месяц и год
                    firstDay = new DateTime(month_days.Year, month_days.Month, 1);
                    lastDay = new DateTime(month_days.Year, month_days.Month, DateTime.DaysInMonth(month_days.Year, month_days.Month));
                }
            }
            //Пытаемся пропарсить год, если он получен
            else if (!String.IsNullOrEmpty(year))
            {
                DateTime year_days;
                if (DateTime.TryParseExact(year, "yyyy", null, DateTimeStyles.None, out year_days))
                {   //Удалось получить год
                    firstDay = new DateTime(year_days.Year, 1, 1);
                    lastDay = new DateTime(year_days.Year, 12, 31);
                }
            }
            Days_Range days_range = new Days_Range(firstDay, lastDay);
            return JsonConvert.SerializeObject(days_range);
        }

        public async Task<string> Get_Pie_Data(string type, string category="", string first_day="", string last_day="", string month = "", string year = "")
        {
            //По-умолчанию ставим текущий месяц
            DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 00, 00, 00);
            DateTime lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 59);
            //Отдаем дни на валидацию
            DaysValidator(out firstDay, out lastDay, first_day:first_day, last_day:last_day, month: month, year: year);
            //Проверяем тип
            if (type != "in" & type != "out") type = "out";
            //Получаем данные
            string user_collection = User.FindFirst(x => x.Type == "id").Value;
            var collection = db.GetCollection<BsonDocument>(user_collection);
            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter;
            List<BsonDocument> result;
            if (String.IsNullOrEmpty(category))
            {
                filter = builder.Eq("type", type) & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
                result = await db.GetCollection<BsonDocument>(user_collection).Aggregate().Match(filter)
                .Group(new BsonDocument {
                    { "_id", "$category" },
                    //{ "title", new BsonDocument("$last", "$category") },
                    { "sum", new BsonDocument("$sum", "$sum") }
                }).ToListAsync();
            }
            else
            {
                filter = builder.Eq("type", type) & builder.Eq("category", category) & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
                result = await db.GetCollection<BsonDocument>(user_collection).Aggregate().Match(filter)
                .Group(new BsonDocument {
                    { "_id", "$subcategory" },
                    { "sum", new BsonDocument("$sum", "$sum") }
                }).ToListAsync();
            }
            return result.ToJson();
        }

        private void DaysValidator(out DateTime firstDay, out DateTime lastDay, string first_day = "", string last_day = "", string month = "", string year = "")
        {
            //Ставим текущий месяц
            firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 00, 00, 00);
            lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 59);
            //Пытаемся пропарсить диапазон дат, если они получены
            if (!String.IsNullOrEmpty(first_day) && !String.IsNullOrEmpty(last_day))
            {
                if (!String.IsNullOrEmpty(first_day) && !String.IsNullOrEmpty(last_day))
                {
                    DateTime _firstDay, _lastDay;
                    if (DateTime.TryParseExact(first_day, "dd.MM.yyyy", null, DateTimeStyles.None, out _firstDay))
                    {   //Удалось получить первый день
                        firstDay = new DateTime(_firstDay.Year, _firstDay.Month, _firstDay.Day, 00, 00, 00);
                        if (DateTime.TryParseExact(last_day, "dd.MM.yyyy", null, DateTimeStyles.None, out _lastDay))
                        {   //Удалось получить последний день
                            if (_lastDay < firstDay) _lastDay = firstDay;
                            lastDay = new DateTime(_lastDay.Year, _lastDay.Month, _lastDay.Day, 23, 59, 59);
                        }
                    }
                }
            }
            //Пытаемся пропарсить месяц, если он получен
            if (!String.IsNullOrEmpty(month))
            {
                DateTime month_days;
                if (DateTime.TryParseExact(month, "MM.yyyy", null, DateTimeStyles.None, out month_days))
                {   //Удалось получить месяц и год
                    firstDay = new DateTime(month_days.Year, month_days.Month, 1, 00, 00, 00);
                    lastDay = new DateTime(month_days.Year, month_days.Month, DateTime.DaysInMonth(month_days.Year, month_days.Month), 23, 59, 59);
                }
            }
            //Пытаемся пропарсить год, если он получен
            if (!String.IsNullOrEmpty(year))
            {
                DateTime year_days;
                if (DateTime.TryParseExact(year, "yyyy", null, DateTimeStyles.None, out year_days))
                {   //Удалось получить год
                    firstDay = new DateTime(year_days.Year, 1, 1, 00, 00, 00);
                    lastDay = new DateTime(year_days.Year, 12, 31, 23, 59, 59);
                }
            }
        }



    }
}
