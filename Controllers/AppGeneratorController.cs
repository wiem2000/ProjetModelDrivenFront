using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetModelDrivenFront.data;
using ProjetModelDrivenFront.Filters;
using ProjetModelDrivenFront.Models;
using ProjetModelDrivenFront.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;





namespace ProjetModelDrivenFront.Controllers
{
    [SessionAuthorize]
    public class AppGeneratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _configuration;

        private readonly string _feedbackApiUrl;

        public AppGeneratorController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            _feedbackApiUrl = _configuration["NLP_API:BaseUrl"];
        }



        public IActionResult Index()
        {
            return View();
        }



        [HttpGet]
        public IActionResult Apps()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Utilisateur non connecté.");

            // Récupérer l'environnement par défaut
            var environment = _context.Environments
                .Include(e => e.Applications)
                .FirstOrDefault(e => e.AccountId == userId && e.IsDefault);

            if (environment == null)
            {
                ViewBag.Apps = new List<App>(); // Aucun environnement par défaut
            }
            else
            {
                var apps = environment.Applications?.OrderByDescending(a => a.CreatedAt).ToList() ?? new List<App>();
                ViewBag.Apps = apps;
            }

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                    return Unauthorized(new { success = false, message = "Utilisateur non connecté" });

                var app = await _context.Applications.FindAsync(request.AppId);
                if (app == null)
                {
                    return NotFound(new { success = false, message = "Application non trouvée" });
                }

                // Vérifier que l'application appartient à l'utilisateur
                var environment = await _context.Environments.FindAsync(app.EnvironnementDynamicsId);
                if (environment == null || environment.AccountId != userId)
                {
                    return Forbid();
                }

                // Toggle favorite status
                app.IsFavorite = !app.IsFavorite;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isFavorite = app.IsFavorite,
                    message = app.IsFavorite ? "Application ajoutée aux favoris" : "Application retirée des favoris"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erreur lors de la mise à jour" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveApp([FromBody] ArchiveAppRequest request)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                    return Unauthorized(new { success = false, message = "Utilisateur non connecté" });

                var app = await _context.Applications.FindAsync(request.AppId);
                if (app == null)
                {
                    return NotFound(new { success = false, message = "Application non trouvée" });
                }

                // Vérifier que l'application appartient à l'utilisateur
                var environment = await _context.Environments.FindAsync(app.EnvironnementDynamicsId);
                if (environment == null || environment.AccountId != userId)
                {
                    return Forbid();
                }

                // Set status to archived
                app.Status = "Archived";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Application archivée avec succès"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erreur lors de l'archivage" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnarchiveApp([FromBody] UnarchiveAppRequest request)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                    return Unauthorized(new { success = false, message = "Utilisateur non connecté" });

                var app = await _context.Applications.FindAsync(request.AppId);
                if (app == null)
                {
                    return NotFound(new { success = false, message = "Application non trouvée" });
                }

                // Vérifier que l'application appartient à l'utilisateur
                var environment = await _context.Environments.FindAsync(app.EnvironnementDynamicsId);
                if (environment == null || environment.AccountId != userId)
                {
                    return Forbid();
                }

                // Restore to Success status (you can modify this logic as needed)
                app.Status = "Success";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    newStatus = app.Status,
                    message = "Application désarchivée avec succès"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erreur lors du désarchivage" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> RecommendAPP([FromBody] RecommendRequest request)
        {
            var userPhrase = request.Prompt;

            HttpContext.Session.SetString("userprompt", userPhrase);

            var client = new HttpClient();
            var payload = new { prompt = userPhrase };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_feedbackApiUrl + "/find_app", content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("🟢 JSON Reçu :");
            Console.WriteLine(result);

            if (string.IsNullOrEmpty(result))
            {
                return Content("Pas de résultats");
            }

            return Content(result, "application/json");
        }








        [HttpPost]
        public async Task<IActionResult> ProcessPhrase(IFormCollection form)
        {
            var userPhrase = form["userPhrase"];
            HttpContext.Session.SetString("userprompt", userPhrase);
            var client = new HttpClient();

            var payload = new { prompt = userPhrase };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_feedbackApiUrl+ "/generate", content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("🟢 JSON Reçu :");
            Console.WriteLine(result);

            try
            {
                // Désérialiser le JSON reçu en SchemaRoot
                var schemaRoot = JsonSerializer.Deserialize<SchemaRoot>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Générer les éléments Cytoscape
                var elements = GenerateElements(schemaRoot);
                ViewData["elements"] = JsonSerializer.Serialize(elements);

                Console.WriteLine(schemaRoot);
                // Rediriger vers la vue Graph en passant le modèle et les éléments
                return View("Graph", schemaRoot);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur de désérialisation : " + ex.Message);
                return Content("Erreur lors du traitement du schéma JSON : " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GraphFromDb(Guid appId)
        {
            var app = await _context.Applications.FindAsync(appId);

            if (app == null || string.IsNullOrEmpty(app.JsonSchema))
            {
                return NotFound("Aucune conception trouvée pour cette application.");
            }

            var schemaRoot = System.Text.Json.JsonSerializer.Deserialize<SchemaRoot>(
                app.JsonSchema,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            ViewData["elements"] = JsonSerializer.Serialize(GenerateElements(schemaRoot));

            return View("Graph", schemaRoot);
        }



        [HttpGet]
        public IActionResult Graph()
        {
            var json = @"
    {
      ""schema"": {
        ""application_name"": ""BookHub"",
        ""tables"": {
          ""Books"": {
            ""primaryfieldname"": ""title"",
            ""fields"": {
              ""title"": ""string"",
              ""isbn"": ""string"",
              ""publication_date"": ""date""
            }
          },
          ""Authors"": {
            ""primaryfieldname"": ""name"",
            ""fields"": {
              ""name"": ""string"",
              ""birthdate"": ""date""
            }
          }
        },
        ""relations"": [
          {
            ""from"": ""Books"",
            ""to"": ""Authors"",
            ""type"": ""many_to_many""
          }
        ]
      }
    }";

            var root = JsonSerializer.Deserialize<SchemaRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewData["elements"] = JsonSerializer.Serialize(GenerateElements(root));

            return View(root);
        }

    
        //va etre utiliser dans le mathode graph
        private List<object> GenerateElements(SchemaRoot root)
        {
            var elements = new List<object>();
            var appName = root.schema.application_name;

            elements.Add(new { data = new { id = appName, label = appName, type = "application" } });

            foreach (var tableEntry in root.schema.tables)
            {
                var tableName = tableEntry.Key;
                var table = tableEntry.Value;

                elements.Add(new { data = new { id = tableName, label = tableName, type = "table" } });
                elements.Add(new { data = new { source = appName, target = tableName, label = "has_table" } });

                foreach (var fieldEntry in table.fields)
                {
                    var fieldName = $"{tableName}_{fieldEntry.Key}";
                    elements.Add(new { data = new { id = fieldName, label = fieldEntry.Key, type = "field" } });
                    elements.Add(new { data = new { source = tableName, target = fieldName, label = "has_field" } });
                }
            }
            if (root?.schema?.relations != null)
            {
                string relationType;

                foreach (var relation in root.schema.relations)
                {
                    if (relation.type == "many_to_one")
                    {
                        relationType = "has_one_to_many";
                        elements.Add(new
                        {
                            data = new
                            {
                                source = relation.to,
                                target = relation.from,
                                label = relationType
                            }
                        });


                    }
                    else
                    {

                        relationType = relation.type == "many_to_many" ? "has_many_to_many" : "has_one_to_many";

                        elements.Add(new
                        {
                            data = new
                            {
                                source = relation.from,
                                target = relation.to,
                                label = relationType
                            }
                        });
                    }

                }
            }
            return elements;
        }

        [HttpPost]
        public IActionResult Graph(SchemaRoot model)
        {
            if (!ModelState.IsValid)
            {
                // Erreur de validation
                return View(model);
            }

            var schema = model.schema;

            // Exemple d'utilisation
            Console.WriteLine("Application : " + schema.application_name);

            foreach (var tableEntry in schema.tables)
            {
                Console.WriteLine($"Table: {tableEntry.Key}");
                Console.WriteLine("Primary Field: " + tableEntry.Value.primaryfieldname);
                foreach (var field in tableEntry.Value.fields)
                {
                    Console.WriteLine($"Field: {field.Key} - Type: {field.Value}");
                }
            }

            foreach (var relation in schema.relations)
            {
                Console.WriteLine($"Relation: {relation.from} -> {relation.to} [{relation.type}]");
            }

       
            return View("Result", model); 
        }

        [HttpPost]
        [Route("AppGenerator/GenerateElementsFromJson")]
        public JsonResult GenerateElementsFromJson([FromBody] SchemaRoot model)
        {
            if (model == null || model.schema == null)
                return Json(new { success = false, message = "Modèle invalide" });

            var elements = GenerateElements(model);
            return Json(new { success = true, elements });
        }





        
        public IActionResult ShowResult()
        {
            var json = HttpContext.Session.GetString("powerapps_json");
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index");

            var model = System.Text.Json.JsonSerializer.Deserialize<TargetSchema>(json);
            return View("Result", model);
        }

        
        public IActionResult StoreAndRedirect2([FromBody] SchemaRoot root)
        {
            if (root?.schema == null)
                return BadRequest("Schéma invalide");

            var json = SchemaTransformer.Transform(root);
            Console.WriteLine(json.ToString());
            var serialized = System.Text.Json.JsonSerializer.Serialize(json);
            Console.WriteLine(serialized);
            HttpContext.Session.SetString("powerapps_json", serialized);

            return Ok(new { redirectUrl = Url.Action("ShowResult") });
        }


        public async Task<IActionResult> StoreAndRedirect([FromBody] SchemaRoot root)
        {
            if (root?.schema == null)
                return BadRequest("Schéma invalide");

            var json = SchemaTransformer.Transform(root);
     
            var serialized = System.Text.Json.JsonSerializer.Serialize(json);
           
            HttpContext.Session.SetString("powerapps_json", serialized);


            // Stockage JSON original
            var originalSchemaJson = System.Text.Json.JsonSerializer.Serialize(root);
            HttpContext.Session.SetString("powerapps_json_before_transform", originalSchemaJson);


            // Appel vers l'API feedback
            try
            {
                var feedbackPayload = new
                {
                    prompt = HttpContext.Session.GetString("userprompt") , // tu remplaces par ton vrai prompt
                    corrected_json = serialized
                };

                var feedbackSerialized = System.Text.Json.JsonSerializer.Serialize(feedbackPayload);

                using var httpClient = new HttpClient();
                var content = new StringContent(feedbackSerialized, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_feedbackApiUrl +"/feedback", content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Feedback API error: {response.StatusCode}");
                    // Tu peux décider de retourner une erreur ou juste logger
                }
                else
                {
                    var feedbackResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Feedback API response: {feedbackResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors de l'appel à l'API feedback: {ex.Message}");
                // Tu peux aussi logger plus de détails si besoin
            }

            //return Ok(new { redirectUrl = Url.Action("ShowResult") });
            return Ok(new { redirectUrl = Url.Action("Index", "Generator") , serializedJson= serialized });
        }





    }


    // Request models
    public class ToggleFavoriteRequest
    {
        public Guid AppId { get; set; }
    }

    public class ArchiveAppRequest
    {
        public Guid AppId { get; set; }
    }

    public class UnarchiveAppRequest
    {
        public Guid AppId { get; set; }
    }

    public class RecommendRequest
    {
        public string Prompt { get; set; }
    }

}


