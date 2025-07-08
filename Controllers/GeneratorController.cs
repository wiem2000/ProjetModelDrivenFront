using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetModelDrivenFront.data;
using ProjetModelDrivenFront.Filters;
using ProjetModelDrivenFront.Models;

namespace ProjetModelDrivenFront.Controllers
{

    [SessionAuthorize]
    public class GeneratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GeneratorController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult Index2()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Index()
        {
            // Récupérer le JSON depuis la session si disponible
            var jsonFromSession = HttpContext.Session.GetString("powerapps_json");

            // Passer le JSON à la vue si nécessaire
            ViewBag.InitialJson = jsonFromSession;

            return View(); // Retourne votre page HTML du générateur
        }



        [HttpPost]
        public IActionResult SaveGeneratedApp([FromBody] App newApp)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized("Utilisateur non connecté.");
            }

            // Récupérer l'environnement par défaut du compte connecté
            var environment = _context.Environments
                .FirstOrDefault(e => e.AccountId == userId && e.IsDefault);

            if (environment == null)
            {
                return BadRequest("Aucun environnement par défaut trouvé pour cet utilisateur.");
            }

            Console.WriteLine($"Reçu App : {System.Text.Json.JsonSerializer.Serialize(newApp)}");
            newApp.CreatedAt = DateTime.UtcNow;
            newApp.EnvironnementDynamicsId = environment.Id;
            newApp.JsonSchema =HttpContext.Session.GetString("powerapps_json_before_transform");
           


            _context.Applications.Add(newApp);
            _context.SaveChanges();

            return Ok(new { success = true, appId = newApp.Id });
        }
    }
}
