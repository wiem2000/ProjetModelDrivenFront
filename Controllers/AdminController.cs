using Microsoft.AspNetCore.Mvc;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using ProjetModelDrivenFront.data;
using ProjetModelDrivenFront.Filters;
using ProjetModelDrivenFront.Models;
using ProjetModelDrivenFront.ViewModels;

namespace ProjetModelDrivenFront.Controllers
{

    /*
    public class AdminController : Controller
    {
        public IActionResult Panel()
        {
            return View();
        }
    }
    */
    [SessionAuthorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Panel()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var account = await _context.Accounts
                .Include(a => a.Environments)
                .FirstOrDefaultAsync(a => a.Id == userId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Compte introuvable. Veuillez vous reconnecter.";
                return RedirectToAction("Login", "Auth");
            }


            var configViewModel = new ConfigurationViewModel
            {
                Environments = account.Environments?.ToList() ?? new List<EnvironnementDynamics>(),
                AccountId = account.Id,
                DefaultEnvironmentId = account.Environments?.FirstOrDefault(e => e.IsDefault)?.Id
            };

            ViewBag.ConfigurationData = configViewModel;

           



            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddEnvironment([FromForm] ConfigurationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newEnv = new EnvironnementDynamics
                {
                    Id = Guid.NewGuid(),
                    Username = model.NewEnvironment.Username,
                    Password = model.NewEnvironment.Password,
                    Url = model.NewEnvironment.Url,
                    Status = "Connected",
                    IsDefault = false, // Valeur par défaut (écrasée si nécessaire)
                    AccountId = model.AccountId
                };

                // 🧠 Si c'est le premier environnement pour ce compte, on le met par défaut
                var existingEnvs = _context.Environments
                    .Where(e => e.AccountId == model.AccountId)
                    .ToList();

                if (!existingEnvs.Any())
                {
                    newEnv.IsDefault = true;
                }

                _context.Environments.Add(newEnv);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Environnement ajouté avec succès!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur DB : " + ex.Message);
                    TempData["ErrorMessage"] = "Erreur technique : " + ex.Message;
                    return RedirectToAction("Panel", new { tab = "config" });
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Erreur lors de l'ajout de l'environnement.";
            }

            // Log ModelState
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Count > 0)
                {
                    Console.WriteLine($"Erreur dans {key} : {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return RedirectToAction("Panel", new { tab = "config" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEnvironment(Guid id)
        {
            var environment = await _context.Environments.FindAsync(id); // Utilisation du bon DbSet
            if (environment != null)
            {
              


                var accountId = environment.AccountId;

                _context.Environments.Remove(environment);
                await _context.SaveChangesAsync();

                // 🔁 Vérifier s’il ne reste qu’un seul env
                await EnsureSingleDefaultEnvironment(accountId);

                TempData["SuccessMessage"] = "Environnement supprimé avec succès!";
            }
            else
            {
                TempData["ErrorMessage"] = "Environnement introuvable.";
            }

            return RedirectToAction("Panel", new { tab = "config" });
        }

        private async Task EnsureSingleDefaultEnvironment(Guid accountId)
        {
            var environments = await _context.Environments
                .Where(e => e.AccountId == accountId)
                .ToListAsync();

            if (environments.Count == 1)
            {
                environments[0].IsDefault = true;
            }
            else if (environments.Count > 1 && environments.All(e => !e.IsDefault))
            {
                // Aucun par défaut → on en force un
                environments[0].IsDefault = true;
            }

            await _context.SaveChangesAsync();
        }


        [HttpPost]
        public async Task<IActionResult> SetDefaultEnvironment(Guid environmentId, Guid accountId)
        {
            // Retirer le statut par défaut de tous les environnements du compte
            var environments = await _context.Environments
                .Where(e => e.AccountId == accountId)
                .ToListAsync();

            foreach (var env in environments)
            {
                env.IsDefault = env.Id == environmentId;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Environnement par défaut mis à jour!";

            return RedirectToAction("Panel", new { tab = "config" });
        }


        /*
        [HttpPost]
        public IActionResult TestMicrosoftConnection([FromBody] EnvironnementDynamics env)
        {
            string connectionString = $@"
        AuthType=OAuth;
        Username={env.Username};
        Password={env.Password};
        Url={env.Url};
        RedirectUri=http://localhost;
         LoginPrompt=Always;
    ";

            try
            {
                var serviceClient = new ServiceClient(connectionString);

                if (!serviceClient.IsReady)
                    return Json(new { success = false, message = "Connexion échouée (Service non prêt)" });

                var whoAmI = (WhoAmIResponse)serviceClient.Execute(new WhoAmIRequest());
                var userId = whoAmI.UserId.ToString();
              

                // TEST CRITIQUE : lecture d'une entité sécurisée (ex: account)
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name"),
                    TopCount = 1
                };

                var result = serviceClient.RetrieveMultiple(query);

                // Si aucune exception → il a bien un rôle Dynamics
                return Json(new { success = true, message = "Connexion réussie et accès Dynamics valide." });
            }
            catch (Exception ex)
            {
                // Cas typique : 2147746581 → l’utilisateur n’a aucun rôle
                return Json(new { success = false, message = $"Accès refusé : {ex.Message}" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> TestConnection(Guid id)
        {
            var environment = await _context.Environments.FindAsync(id);
            if (environment == null)
            {
                return Json(new { success = false, message = "Environnement introuvable" });
            }

            try
            {
                var connectionSuccess = await TestDynamicsConnection(environment);

                environment.Status = connectionSuccess ? "Connected" : "Expired";
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = connectionSuccess,
                    message = connectionSuccess
                        ? "Connexion réussie et accès Dynamics confirmé."
                        : "Connexion échouée ou accès refusé (vérifiez les rôles Dynamics)."
                });
            }
            catch (Exception ex)
            {
                environment.Status = "Error";
                await _context.SaveChangesAsync();

                return Json(new { success = false, message = $"Erreur technique : {ex.Message}" });
            }
        }

        private async Task<bool> TestDynamicsConnection(EnvironnementDynamics env)
        {
            try
            {
                var connectionString = $@"
            AuthType=OAuth;
            Username={env.Username};
            Password={env.Password};
            Url={env.Url};
            RedirectUri=http://localhost;
            LoginPrompt=Never;
        ";

                var serviceClient = new ServiceClient(connectionString);

                if (!serviceClient.IsReady)
                    return false;

                // Test d'une entité protégée
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name"),
                    TopCount = 1
                };

                var result = serviceClient.RetrieveMultiple(query);

                return true; // Connexion et autorisation OK
            }
            catch
            {
                return false;
            }
        }
        */


        private (bool success, string? message) TryDynamicsConnection(string username, string password, string url, bool interactiveLogin)
        {
            try
            {
               

                var connectionString = $@"
            AuthType=OAuth;
            Username={username};
            Password={password};
            Url={url};
            RedirectUri=http://localhost;
            LoginPrompt=Never
        ";


                var serviceClient = new ServiceClient(connectionString);

                if (!serviceClient.IsReady)
                    return (false, "Connexion échouée (Service non prêt)");

                // Requête sécurisée (vérifie les privilèges)
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name"),
                    TopCount = 1
                };

                var result = serviceClient.RetrieveMultiple(query);

                return (true, "Connexion réussie et accès Dynamics confirmé.");
            }
            catch (Exception ex)
            {
                return (false, $"Accès refusé : {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult TestMicrosoftConnection([FromBody] EnvironnementDynamics env)
        {
            var (success, message) = TryDynamicsConnection(env.Username, env.Password, env.Url, interactiveLogin: true);
            return Json(new { success, message });
        }
        [HttpGet]
        public async Task<IActionResult> TestConnection(Guid id)
        {
            var environment = await _context.Environments.FindAsync(id);
            if (environment == null)
            {
                return Json(new { success = false, message = "Environnement introuvable" });
            }

            var (success, message) = TryDynamicsConnection(environment.Username, environment.Password, environment.Url, interactiveLogin: false);

            environment.Status = success ? "Connected" : "Expired";
            await _context.SaveChangesAsync();

            return Json(new { success, message });
        }


        // Méthode pour obtenir l'environnement par défaut (utile pour d'autres parties de l'application)
        public async Task<EnvironnementDynamics?> GetDefaultEnvironment(Guid accountId)
        {
            return await _context.Environments
                .FirstOrDefaultAsync(e => e.AccountId == accountId && e.IsDefault);
        }

        // Méthode pour obtenir tous les environnements d'un compte
        public async Task<List<EnvironnementDynamics>> GetAccountEnvironments(Guid accountId)
        {
            return await _context.Environments
                .Where(e => e.AccountId == accountId)
                .OrderByDescending(e => e.IsDefault)
                .ToListAsync();
        }
    }

}
