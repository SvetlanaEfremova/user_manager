using Microsoft.AspNetCore.Mvc;

namespace task4.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult SignUp()
        {
            return View();
        }

        public IActionResult LogIn()
        {
            return View();
        }

    }
}
