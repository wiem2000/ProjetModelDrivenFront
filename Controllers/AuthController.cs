using Microsoft.AspNetCore.Mvc;
using ProjetModelDrivenFront.data;
using System.Text;
using System.Security.Cryptography;
using System.Drawing.Printing;
using ProjetModelDrivenFront.Models;

namespace ProjetModelDrivenFront.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string Username, string Password)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Username == Username);
            if (account == null || !VerifyPassword(Password, account.Password))
            {
                TempData["Error"] = "Identifiants incorrects.";
                return RedirectToAction("Login");
            }

            TempData["Success"] = "Bienvenue!";
            // Stocker dans la session
            HttpContext.Session.SetString("UserId", account.Id.ToString());
            HttpContext.Session.SetString("UserFirstName", account.Username);

            return RedirectToAction("Index", "AppGenerator");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Fonction de vérification du mot de passe
        private bool VerifyPassword(string password, string storedHash)
        {
            Console.WriteLine(HashPassword("admin123"));
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = Convert.FromBase64String(storedHash);
            using var sha256 = SHA256.Create();
            var computedHash = sha256.ComputeHash(passwordBytes);
            return computedHash.SequenceEqual(hashBytes);
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string Username, string Email, string Password, string ConfirmPassword)
        {
            // Vérifier si le nom d'utilisateur existe déjà
            if (_context.Accounts.Any(a => a.Username == Username))
            {
                TempData["Error"] = "Ce nom d'utilisateur est déjà utilisé.";
                return RedirectToAction("Register");
            }

            /* Vérifier si l'email existe déjà
            if (_context.Accounts.Any(a => a.Email == Email))
            {
                TempData["Error"] = "Cette adresse email est déjà utilisée.";
                return RedirectToAction("Register");
            }*/

            // Vérifier que les mots de passe correspondent
            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Les mots de passe ne correspondent pas.";
                return RedirectToAction("Register");
            }

            // Créer un nouvel utilisateur
            var newAccount = new Account
            {
                Username = Username,
                //Email = Email, // Assurez-vous d'ajouter l'email
                Password = HashPassword(Password),
                // Ajouter d'autres champs selon votre modèle Account
            };

            _context.Accounts.Add(newAccount);
            _context.SaveChanges();

            // Connecter automatiquement l'utilisateur
            HttpContext.Session.SetString("UserId", newAccount.Id.ToString());
            HttpContext.Session.SetString("UserFirstName", newAccount.Username);

            TempData["Success"] = "Votre compte a été créé avec succès!";
            return RedirectToAction("Index", "AppGenerator");
        }

        // API endpoint pour vérifier la disponibilité du nom d'utilisateur
        [HttpPost]
        public IActionResult CheckUsername([FromBody] UsernameCheckRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Json(new { available = false, message = "Nom d'utilisateur requis" });
            }

            // Validation des caractères
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
            {
                return Json(new { available = false, message = "Caractères non autorisés" });
            }

            // Validation de la longueur
            if (request.Username.Length < 3 || request.Username.Length > 50)
            {
                return Json(new { available = false, message = "Longueur invalide" });
            }

            // Vérifier la disponibilité dans la base de données
            bool isAvailable = !_context.Accounts.Any(a => a.Username.ToLower() == request.Username.ToLower());

            return Json(new { available = isAvailable });
        }
    }

    // Classe pour la requête de vérification du nom d'utilisateur
    public class UsernameCheckRequest
    {
        public string Username { get; set; } = string.Empty;
    }
}
