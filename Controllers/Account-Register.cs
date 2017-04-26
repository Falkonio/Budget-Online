using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Budget.Controllers
{
    public class RegisterViewModel
    {
        [Display(Name = "Имя и фамилия")]
        [Required(ErrorMessage = "Обязательно для заполнения")]
        [StringLength(70, MinimumLength = 3, ErrorMessage = "Длина строки должна быть от 3 до 70 символов")]
        public string Name { get; set; }

        [Display(Name = "Адрес электронной почты")]
        [Required(ErrorMessage = "Не указан электронный адрес")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage = "Некорректный адрес")]
        public string Email { get; set; }

        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Введите пароль")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 50 символов")]
        public string Password { get; set; }

        [Display(Name = "Повторите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string PasswordConfirm { get; set; }
    }

    public partial class AccountController : Controller
    {

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                int errors_counter = 0;
                if (CheckEmail(model.Email) == false) //Проверяем уникальность email
                {
                    ModelState.AddModelError("Email", "E-mail уже используется");
                    errors_counter++;
                }
                if (errors_counter != 0) goto has_errors;
                //Солим и хешируем пароль
                string salt = Hasher.getSalt();
                string Pass_Hash = Hasher.getHash(model.Password + salt);

                string new_id = ObjectId.GenerateNewId().ToString();
                string lang = "ru";
                //Записываем пользователя в базу
                var user = new BsonDocument
                    {
                        { "_id" , new_id },
                        { "name" , model.Name },
                        { "email", model.Email.ToLower() },
                        { "password", Pass_Hash },
                        { "salt", salt },
                        { "lang", lang },
                        { "registred", DateTime.Now.ToString() }
                    };
                var users = db.GetCollection<BsonDocument>("users");
                await users.InsertOneAsync(user);

                //Получаем из базы демки категорий для нужного языка
                var demos = db.GetCollection<BsonDocument>("demo");
                var in_demo = await demos.Find(new BsonDocument { { "_id", lang + "_in" } }).FirstOrDefaultAsync();
                var out_demo = await demos.Find(new BsonDocument { { "_id", lang + "_out" } }).FirstOrDefaultAsync();
                in_demo["_id"] = new_id + "_in";
                out_demo["_id"] = new_id + "_out";
                //Множественная запись в базу
                var new_collection = db.GetCollection<BsonDocument>(new_id);
                await new_collection.InsertManyAsync(new[] { in_demo, out_demo });
                //Создаем индексы для новой коллекции
                await new_collection.Indexes.CreateOneAsync("{ date : 1}");
                await new_collection.Indexes.CreateOneAsync("{ type : 1}");
                //Авторизовываем нового пользователя
                await Authenticate.Set_Cookies(model.Name, new_id, Pass_Hash, "ru", false, HttpContext);

                return Redirect("~/Home/");
            }
            has_errors:
            // If we got this far, something failed, redisplay form
            return View(model);
        }




    }
}
