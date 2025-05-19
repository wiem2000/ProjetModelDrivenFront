using Microsoft.AspNetCore.Mvc;
using ProjetModelDrivenFront.Models;
using System.Text.Json;

namespace ProjetModelDrivenFront.Controllers
{
    public class AppGeneratorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessPhrase(string userPhrase)
        {
            // Ici tu peux traiter la phrase reçue ou la stocker temporairement
            // Pour l'instant, redirige vers la page Create

            return RedirectToAction("Create");
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



    }

}

