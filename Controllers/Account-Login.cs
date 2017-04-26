using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Budget.Controllers
{

    public class LoginViewModel
    {
        [Display(Name = "Адрес электронной почты")]
        [Required(ErrorMessage = "Не указан электронный адрес")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,4}", ErrorMessage = "Некорректный адрес")]
        public string Email { get; set; }

        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Введите пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить?")]
        public bool RememberMe { get; set; }

        public string Salt { get; set; }
    }

    public partial class AccountController : Controller
    {
        //
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var collection = db.GetCollection<BsonDocument>("users");

                var login_in_base = await collection
                    .Find(new BsonDocument("email", model.Email.ToLower()))
                    .FirstOrDefaultAsync();
                if (login_in_base != null)
                {
                    string id_base = login_in_base["_id"].ToString();
                    string salt_base = login_in_base["salt"].ToString();
                    string password_base = login_in_base["password"].ToString();
                    string name_base = login_in_base["name"].ToString();
                    string lang_base = login_in_base["lang"].ToString();

                    string Sended_Pass_Hash = Hasher.getHash(model.Password + salt_base);
                    if (Sended_Pass_Hash == password_base)
                    {
                        await Authenticate.Set_Cookies(name_base, id_base, password_base, lang_base, model.RememberMe, HttpContext);
                        return Redirect("/Site/");
                    }
                }
                ModelState.AddModelError(string.Empty, "Неправильные логин и пароль");
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

    }
}
