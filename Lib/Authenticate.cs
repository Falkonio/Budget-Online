using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Budget
{
    public class Authenticate
    {
        public static async Task Set_Cookies(string _name, string _id, string _password, string _lang, bool _remember, HttpContext _context)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, _name),
                new Claim("id", _id),
                new Claim("password", _password),
                new Claim("lang", _lang),
                new Claim("time", DateTime.UtcNow.ToString()),
                new Claim("remember", _remember.ToString()),
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            if (_remember == true)
            {
                await _context.Authentication.SignInAsync(
                    "Cookies",
                    new ClaimsPrincipal(id),
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddDays(14),
                        IsPersistent = true
                    });
            }
            else
            {
                await _context.Authentication.SignInAsync(
                    "Cookies",
                    new ClaimsPrincipal(id),
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddHours(1),
                        IsPersistent = false
                    });
            }
        }
    }
}
