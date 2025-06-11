
    using System.Globalization;
    using global::ProjetModelDrivenFront.Models;
    

    namespace ProjetModelDrivenFront.Services
    {
        public static class SchemaTransformer
        {
            public static TargetSchema Transform(SchemaRoot source)
            {
                var schema = source.schema;

                var result = new TargetSchema
                {
                    appName = schema.application_name,
                    appUniqueName = schema.application_name.ToLower(),
                    solutionName = schema.application_name + "Solution"
                };

                foreach (var kvp in schema.tables)
                {
                    var tableName = kvp.Key;
                    var table = kvp.Value;

                    var targetTable = new TargetTable
                    {
                        tableName = tableName,
                        displayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableName),
                        primaryField = table.primaryfieldname,
                        primaryFieldDisplayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(table.primaryfieldname)
                    };

                    foreach (var field in table.fields)
                    {
                        targetTable.fields.Add(new TargetField
                        {
                            logicalName = field.Key,
                            displayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(field.Key.Replace("_", " ")),
                            dataType = field.Value
                        });
                    }

                    result.tables.Add(targetTable);
                }

                foreach (var rel in schema.relations)
                {
                    if (rel.type == "one_to_many")
                    {
                        result.oneToManyRelations.Add(new OneToManyRelation
                        {
                            referencingEntity = rel.to,
                            referencingAttribute = rel.from + "_id", // supposé
                            referencedEntity = rel.from,
                            schemaName = $"{rel.from}To{rel.to}"
                        });
                    }
                    else if (rel.type == "many_to_many")
                    {
                        result.manyToManyRelations.Add(new ManyToManyRelation
                        {
                            entity1 = rel.from,
                            entity2 = rel.to,
                            schemaName = $"{rel.from}To{rel.to}",
                            intersectName = $"{rel.from}{rel.to}Intersect"
                        });
                    }
                }

                return result;
            }
        }
    }


