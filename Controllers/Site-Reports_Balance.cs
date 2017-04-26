using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Budget.Controllers
{
    [DataContract]
    public class Balance_Data
    {
        [DataMember]
        public string period;
        [DataMember]
        public double income=0;
        [DataMember]
        public double expense=0;
        [DataMember]
        public double balance=0;
        [DataMember]
        public int dashLengthLine=0;
        [DataMember]
        public int dashLengthColumn=0;
        [DataMember]
        public double alpha=1;
        [DataMember]
        public string additional="";

        public Balance_Data(string period, double income, double expense)
        {
            this.period = period;
            this.income = income;
            this.expense = expense;
            balance = income - expense;
        }
        public Balance_Data(string period)
        {
            this.period = period;
        }

    }

    public partial class SiteController : Controller
    {
        //
        // GET: /Site/Reports_Balance
        public async Task<IActionResult> Reports_Balance(string year = "")
        {
            if (String.IsNullOrEmpty(year)) year = DateTime.Today.Year.ToString();
            string data = await Get_Balance_Data(year);
            JArray json = JArray.Parse(data);
            ViewBag.Year = json[12]["period"].ToString();
            ViewBag.Balance_Data = data;
            return View();
        }

        public async Task<string> Get_Balance_Data(string year = "")
        {
            //Ставим текущий год
            string year_true = DateTime.Today.Year.ToString();
            DateTime firstDay = new DateTime(DateTime.Today.Year, 1, 1, 00, 00, 00);
            DateTime lastDay = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59);
            //Пытаемся пропарсить год, если он получен
            if (!String.IsNullOrEmpty(year))
            {
                DateTime year_days;
                if (DateTime.TryParseExact(year, "yyyy", null, DateTimeStyles.None, out year_days))
                {   //Удалось получить год
                    firstDay = new DateTime(year_days.Year, 1, 1, 00, 00, 00);
                    lastDay = new DateTime(year_days.Year, 12, 31, 23, 59, 59);
                    year_true = year_days.Year.ToString();
                }
            }
            //Получаем данные
            string user_collection = User.FindFirst(x => x.Type == "id").Value;
            var collection = db.GetCollection<BsonDocument>(user_collection);
            var builder = Builders<BsonDocument>.Filter;
            var filter = (builder.Eq("type", "in")| builder.Eq("type", "out")) & builder.Gte("date", firstDay) & builder.Lte("date", lastDay);
            var result = await db.GetCollection<BsonDocument>(user_collection).Aggregate().Match(filter)
                .Group(new BsonDocument{
                    { "_id", new BsonDocument {
                        { "month", new BsonDocument("$month", "$date") },
                        { "type","$type" } }
                    },
                    { "sum", new BsonDocument("$sum", "$sum") }
                })
                .ToListAsync();

            Balance_Data[] list = new Balance_Data[13];
            string[] monthes_ru = new string[] {"ЯНВ","ФЕВ","МАР","АПР","МАЙ","ИЮН","ИЮЛ","АВГ","СЕН","ОКТ","НОЯ","ДЕК"};
            //Инициализация массива данных значениями по-умолчанию
            for (int i=0;i<12;i++)
            {
                list[i] = new Balance_Data(monthes_ru[i]);
                if (i == 11) list[i].dashLengthLine = 5;
            }

            //Счетчики для вычисления средних значений
            int in_count = 0;
            int out_count = 0;
            double in_sum = 0;
            double out_sum = 0;
            //Перебор данных из базы
            foreach (var item in result)
            {
                int i = item["_id"]["month"].AsInt32;
                if (item["_id"]["type"] == "in" && item["sum"].AsDouble!=0)
                {
                    list[i - 1].income += item["sum"].AsDouble;
                    in_count++;
                    in_sum += item["sum"].AsDouble;
                }
                if (item["_id"]["type"] == "out" && item["sum"].AsDouble != 0)
                {
                    list[i - 1].expense += item["sum"].AsDouble;
                    out_count++;
                    out_sum += item["sum"].AsDouble;
                }
                list[i - 1].balance = list[i - 1].income - list[i - 1].expense;

            }
            //Выводим средние значения за год
            list[12] = new Balance_Data(year_true, Math.Round(in_sum / in_count,2), Math.Round(out_sum / out_count,2));
            list[12].dashLengthColumn = 5;
            list[12].alpha = 0.2;
            list[12].additional = "(среднее)";

            return JsonConvert.SerializeObject(list);
        }



    }
}
