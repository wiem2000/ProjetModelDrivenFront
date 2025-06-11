using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetModelDrivenFront.data;
using ProjetModelDrivenFront.Models;
using ProjetModelDrivenFront.Services;
using System.Text;
using System.Text.Json;





namespace ProjetModelDrivenFront.Controllers
{
    public class AppGeneratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppGeneratorController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> ProcessPhrase(IFormCollection form)
        {
            var userPhrase = form["userPhrase"];
            var client = new HttpClient();

            var payload = new { prompt = userPhrase };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://c144-34-148-115-230.ngrok-free.app/generate", content);
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
                foreach (var relation in root.schema.relations)
                {
                    string relationType = relation.type == "many_to_many" ? "has_many_to_many" : "has_one_to_many";
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

       
            return View("Result", model); // ou rediriger, etc.
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

        
        public IActionResult StoreAndRedirect([FromBody] SchemaRoot root)
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





    }

}

