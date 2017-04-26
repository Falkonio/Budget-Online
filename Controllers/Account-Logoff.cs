using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Budget.Controllers
{
    public partial class AccountController : Controller
    {
        //
        // GET: /Account/LogOff
        public async Task<IActionResult> LogOff()
        {
            await HttpContext.Authentication.SignOutAsync("Cookies");
            return Redirect("/Home/");
        }
    }
}
