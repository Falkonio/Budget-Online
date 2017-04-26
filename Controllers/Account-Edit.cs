using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Budget.Controllers
{
    public partial class AccountController : Controller
    {
        //
        // GET: /Account/User
        [Authorize]
        [HttpGet]
        [ActionName("User")]
        public async Task<IActionResult> _User(string message="")
        {
            var users = db.GetCollection<BsonDocument>("users");

            var user = await users
                .Find(new BsonDocument { { "_id", User.FindFirst(x => x.Type == "id").Value } })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.User = user;
            ViewBag.Message = message;
            return View();
        }

        //
        // GET: /Account/UserNameEditForm
        [Authorize]
        public IActionResult UserNameEditForm()
        {
            ViewData["name"] = User.FindFirst(x => x.Type == ClaimTypes.Name).Value;
            return PartialView();
        }
        //
        // POST: /Account/UserNameEdit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserNameEdit(string name)
        {
            string message;
            if (!String.IsNullOrEmpty(name) && name.Length >= 3 && name.Length <= 70)
            {
                string id = User.FindFirst(x => x.Type == "id").Value;
                var collection = db.GetCollection<BsonDocument>("users");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var update = Builders<BsonDocument>.Update.Set("name", name);
                var result = await collection.UpdateOneAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    message = "success";
                    await Authenticate.Set_Cookies(name, id, User.FindFirst(x => x.Type == "password").Value, User.FindFirst(x => x.Type == "lang").Value, bool.Parse(User.FindFirst(x => x.Type == "remember").Value), HttpContext);
                }
                else message = "fail";
            }
            else message = "error";
            return Redirect($"~/Account/User/?message={message}");
        }

        //
        // GET: /Account/UserEmailEditForm
        [Authorize]
        public async Task<IActionResult> UserEmailEditForm()
        {
            var users = db.GetCollection<BsonDocument>("users");
            var user = await users
                            .Find(new BsonDocument{{"_id", User.FindFirst(x => x.Type == "id").Value}})
                            .FirstOrDefaultAsync();
            if(user!=null)
            {
                ViewData["email"] = user["email"];
            }
            else ViewData["email"] = "Ошибка!";
            return PartialView();
        }
        //
        // POST: /Account/UserEmailEdit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEmailEdit(string email)
        {
            string message;
            string pattern = @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}";
            if (!String.IsNullOrEmpty(email) && Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase))
            {
                if (CheckEmail(email) == true) //Проверяем уникальность email
                {
                    string id = User.FindFirst(x => x.Type == "id").Value;
                    var collection = db.GetCollection<BsonDocument>("users");
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                    var update = Builders<BsonDocument>.Update.Set("email", email);
                    var result = await collection.UpdateOneAsync(filter, update);
                    if (result.ModifiedCount > 0)
                    {
                        message = "success";
                    }
                    else message = "fail";
                }
                else message = "email_unique";
            }
            else message = "error";
            return Redirect($"~/Account/User/?message={message}");
        }

        //
        // GET: /Account/UserPasswordEditForm
        [Authorize]
        public IActionResult UserPasswordEditForm()
        {
            return PartialView();
        }

        //
        // POST: /Account/UserNameEdit
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserChangePassword(string old_password, string new_password, string confirm_password)
        {
            string message;
            if (!String.IsNullOrEmpty(old_password) && !String.IsNullOrEmpty(new_password) && !String.IsNullOrEmpty(confirm_password))
            {   //Есои все строки не пустые
                if (old_password.Length >= 6     && old_password.Length <= 50  &&
                    new_password.Length >= 6     && new_password.Length <= 50  &&
                    confirm_password.Length >= 6 && confirm_password.Length <= 50)
                {   //Если все строки соответствуют длине
                    if (new_password== confirm_password)
                    {   //Если повтор пароля совпадает
                        string id = User.FindFirst(x => x.Type == "id").Value;
                        var collection = db.GetCollection<BsonDocument>("users");
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                        var user = await collection.Find(filter).FirstOrDefaultAsync();
                        string Old_Pass_Hash = Hasher.getHash(old_password + user["salt"].ToString());
                        if (Old_Pass_Hash== user["password"].ToString())
                        {   //Если старый пароль совпадает с базой
                            string salt = Hasher.getSalt(); //Генерируем новую соль
                            string New_Pass_Hash = Hasher.getHash(new_password + salt); //Хешируем новый пароль с новой солью
                            var changeList = new List<UpdateDefinition<BsonDocument>>();
                            changeList.Add(Builders<BsonDocument>.Update.Set("salt", salt));
                            changeList.Add(Builders<BsonDocument>.Update.Set("password", New_Pass_Hash));
                            var update = Builders<BsonDocument>.Update.Combine(changeList);
                            var result = await collection.UpdateOneAsync(filter, update);
                            if (result.ModifiedCount > 0)
                            {
                                message = "success";
                                await Authenticate.Set_Cookies(User.FindFirst(x => x.Type == ClaimTypes.Name).Value, id, New_Pass_Hash, User.FindFirst(x => x.Type == "lang").Value, bool.Parse(User.FindFirst(x => x.Type == "remember").Value), HttpContext);
                            }
                            else message = "fail";
                        }
                        else message = "pass_wrong";
                    }
                    else message = "pass_unconfirmed";
                }
                else message = "pass_length";
            }
            else message = "error";
            return Redirect($"~/Account/User/?message={message}");
        }





    }
}
