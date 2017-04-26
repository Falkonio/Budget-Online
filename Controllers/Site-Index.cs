using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System;
using System.Globalization;

namespace Budget.Controllers
{
    public partial class SiteController : Controller
    {
        //
        // GET: /Site/Index
        public async Task<IActionResult> Index(string error="", string first_day = "", string last_day = "", string month="", string year="")
        {
            DateTime firstDay = DateTime.Today;
            DateTime lastDay = DateTime.Today;

            //Ставим диапазон по-умолчанию, если ничего не получено
            if (String.IsNullOrEmpty(first_day) && String.IsNullOrEmpty(last_day) && String.IsNullOrEmpty(month) && String.IsNullOrEmpty(year))
            {
                string cookies_first_day = HttpContext.Request.Cookies.FirstOrDefault(x => x.Key == "firstDay").Value;
                string cookies_last_day = HttpContext.Request.Cookies.FirstOrDefault(x => x.Key == "lastDay").Value;
                if (!String.IsNullOrEmpty(cookies_first_day) && !String.IsNullOrEmpty(cookies_last_day))
                {   //Если значения есть, то парсим их
                    if (DateTime.TryParseExact(cookies_first_day, "dd.MM.yyyy", null, DateTimeStyles.None, out firstDay))
                    {   //Удалось получить первый день
                        firstDay = new DateTime(firstDay.Year, firstDay.Month, firstDay.Day, 00, 00, 00);
                        if (DateTime.TryParseExact(cookies_last_day, "dd.MM.yyyy", null, DateTimeStyles.None, out lastDay))
                        {   //Удалось получить последний день
                            if (lastDay < firstDay) lastDay = firstDay;
                            lastDay = new DateTime(lastDay.Year, lastDay.Month, lastDay.Day, 23, 59, 59);
                        }
                    }
                }
                else
                {   //Если хоть одно значение пустое, то ставим текущий месяц
                    firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 00, 00, 00);
                    lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 59);
                }
            }//**Ставим диапазон по-умолчанию, если ничего не получено

            //Пытаемся пропарсить диапазон дат, если они получены
            if (!String.IsNullOrEmpty(first_day) && !String.IsNullOrEmpty(last_day))
            {
                DateTime _firstDay, _lastDay;
                if (DateTime.TryParseExact(first_day, "dd.MM.yyyy", null, DateTimeStyles.None, out _firstDay))
                {   //Удалось получить последний день
                    firstDay = new DateTime(_firstDay.Year, _firstDay.Month, _firstDay.Day, 00, 00, 00);
                    if (DateTime.TryParseExact(last_day, "dd.MM.yyyy", null, DateTimeStyles.None, out _lastDay))
                    {   //Удалось получить последний день
                        if (_lastDay < firstDay) _lastDay = firstDay;
                        lastDay = new DateTime(_lastDay.Year, _lastDay.Month, _lastDay.Day, 23, 59, 59);
                        SetDaysInCookies(firstDay, lastDay);
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
                    SetDaysInCookies(firstDay, lastDay);
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
                    SetDaysInCookies(firstDay, lastDay);

                }
            }

            string user_collection = User.FindFirst(x => x.Type == "id").Value;
            var collection = db.GetCollection<BsonDocument>(user_collection);
            var builder = Builders<BsonDocument>.Filter;
            var filter = (builder.Eq("type", "in") | builder.Eq("type", "out")) & (builder.Gte("date", firstDay) & builder.Lte("date", lastDay));
            var transactions = await collection.Find(filter).ToListAsync();

            ViewBag.Inputs =  transactions.Where(t => t["type"] == "in").OrderByDescending(t => t["date"]).ThenBy(t => t["text"]);
            ViewBag.Outputs = transactions.Where(t => t["type"] == "out").OrderByDescending(t => t["date"]).ThenBy(t => t["text"]);
            ViewBag.Inputs_Sum = transactions.Where(t => t["type"] == "in").Sum(t => t["sum"].ToDouble());
            ViewBag.Outputs_Sum = transactions.Where(t => t["type"] == "out").Sum(t => t["sum"].ToDouble());

            ViewData["First_Day"] = firstDay.ToString("dd.MM.yyyy");
            ViewData["Last_Day"] = lastDay.ToString("dd.MM.yyyy");
            ViewData["Error"] = error;
            return View();
        }

        private void SetDaysInCookies(DateTime firstDay, DateTime lastDay)
        {
            HttpContext.Response.Cookies.Append("firstDay", firstDay.ToString("dd.MM.yyyy"), new Microsoft.AspNetCore.Http.CookieOptions(){Expires = DateTime.UtcNow.AddYears(1)});
            HttpContext.Response.Cookies.Append("lastDay", lastDay.ToString("dd.MM.yyyy"), new Microsoft.AspNetCore.Http.CookieOptions(){Expires = DateTime.UtcNow.AddYears(1)});
        }
    }
}
