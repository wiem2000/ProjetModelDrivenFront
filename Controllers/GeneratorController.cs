using Microsoft.AspNetCore.Mvc;

namespace ProjetModelDrivenFront.Controllers
{
    public class GeneratorController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
