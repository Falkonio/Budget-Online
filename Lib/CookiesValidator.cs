using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Budget.Lib
{
    public static class CookiesValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {       //Если авторизован                              и прошло больше 20 минут с момента установки куки
            if (context.Principal.Identity.IsAuthenticated && Convert.ToDateTime(context.Principal.FindFirst(x => x.Type == "time").Value).AddMinutes(20)<DateTime.UtcNow)
            {
                MongoDbSettings settings = context.HttpContext.RequestServices.GetRequiredService<MongoDbSettings>();
                var client = new MongoClient(settings.ConnectionString);
                var db = client.GetDatabase(settings.DatabaseName);
                var users = db.GetCollection<BsonDocument>("users");
                var user = await users
                                .Find(new BsonDocument { { "_id", context.Principal.FindFirst(x => x.Type == "id").Value },
                                                         {"password", context.Principal.FindFirst(x => x.Type == "password").Value} })
                                .FirstOrDefaultAsync();
                if (user == null)
                {   //Если пользователя нет в базе, то разлогиниваем
                    await context.HttpContext.Authentication.SignOutAsync("Cookies");
                }
                else
                {   //Если есть в базе, то переустанавливаем куки с актуальными данными
                    bool remember = bool.Parse(context.Principal.FindFirst(x => x.Type == "remember").Value);
                    await Authenticate.Set_Cookies(user["name"].ToString(), user["_id"].ToString(), user["password"].ToString(), user["lang"].ToString(), remember, context.HttpContext);
                }


            }
        }
    }
}
