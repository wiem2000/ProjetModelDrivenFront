using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crm.Sdk.Messages;

using Microsoft.PowerPlatform.Dataverse.Client;
using ProjetModelDrivenFront.data;
using ProjetModelDrivenFront.Models;

namespace ProjetModelDrivenFront.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _dbContext;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

  
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    public IActionResult LoginSuccess()
    {
        /*
        // Vérifier que l'utilisateur est authentifié
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
        {
            return RedirectToAction("Index"); // Ou une autre action qui demande de se connecter
        }
        */

        return View();
    }

    [HttpPost]
    public IActionResult LoginWithMicrosoft2()
    {
      
      

        string connectionString = @"
         AuthType=OAuth;
         Username=;
         Url=https://orgcef9757a.api.crm4.dynamics.com;
         Password=;
       
         RedirectUri=http://localhost;
         
       LoginPrompt=Always;

     "
        ;



        try
        {
            var serviceClient = new ServiceClient(connectionString);

            if (serviceClient.IsReady)
            {
                var whoAmI = (WhoAmIResponse)serviceClient.Execute(new WhoAmIRequest());
                var userId = whoAmI.UserId.ToString();

                var columns = new Microsoft.Xrm.Sdk.Query.ColumnSet("domainname", "firstname", "lastname");

                var systemUser = serviceClient.Retrieve("systemuser", new Guid(userId), columns);

                string email = systemUser.GetAttributeValue<string>("domainname");
                string firstName = systemUser.GetAttributeValue<string>("firstname");
                string lastName = systemUser.GetAttributeValue<string>("lastname");
                ViewBag.UserId = whoAmI.UserId.ToString();
                ViewBag.email = email;

                return View("LoginSuccess");
            }
            else
            {
                ViewBag.Error = "Échec de l'authentification.";
                return View("LoginFailed");
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Erreur : " + ex.Message;
            return View("LoginFailed");
        }
    }



    [HttpPost]
    public IActionResult LoginWithMicrosoft()
    {

        
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
        {
            return RedirectToAction("Index", "AppGenerator");
        }
        
        string connectionString = @"
            AuthType=OAuth;
            Username=;
            Url=https://orgcef9757a.api.crm4.dynamics.com;
            Password=;
            RedirectUri=http://localhost;
            LoginPrompt=Always;
        ";

        try
        {
            var serviceClient = new ServiceClient(connectionString);

            if (serviceClient.IsReady)
            {
                var whoAmI = (WhoAmIResponse)serviceClient.Execute(new WhoAmIRequest());
                var userId = whoAmI.UserId.ToString();

               
                var user = _dbContext.Users.FirstOrDefault(u => u.UserId == userId);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserId", user.UserId);
                    HttpContext.Session.SetString("UserEmail", user.EmailMicrosoft);
                    HttpContext.Session.SetString("UserFirstName", user.FirstName);
                    HttpContext.Session.SetString("UserLastName", user.LastName);
                    HttpContext.Session.SetString("UserRole", user.Role);

                    return RedirectToAction("Index","AppGenerator");
                }
                else
                {
                    ViewBag.Error = "Utilisateur non autorisé.";
                    return View("LoginFailed");
                }
            }
            else
            {
                ViewBag.Error = "Échec de l'authentification Microsoft.";
                return View("LoginFailed");
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Erreur : " + ex.Message;
            return View("LoginFailed");
        }
    }

   

    public IActionResult AccessDenied()
    {
        return View();
    }






}
