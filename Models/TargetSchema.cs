namespace ProjetModelDrivenFront.Models
{

    public class TargetSchema
    {
        public string appName { get; set; }
        public string appUniqueName { get; set; }
        public string solutionName { get; set; }
        public List<TargetTable> tables { get; set; } = new();
        public List<OneToManyRelation> oneToManyRelations { get; set; } = new();
        public List<ManyToManyRelation> manyToManyRelations { get; set; } = new();
    }

    public class TargetTable
    {
        public string tableName { get; set; }
        public string displayName { get; set; }
        public string primaryField { get; set; }
        public string primaryFieldDisplayName { get; set; }
        public List<TargetField> fields { get; set; } = new();
    }

    public class TargetField
    {
        public string logicalName { get; set; }
        public string displayName { get; set; }
        public string dataType { get; set; }
    }

    public class OneToManyRelation
    {
        public string referencingEntity { get; set; }
        public string referencingAttribute { get; set; }
        public string referencedEntity { get; set; }
        public string schemaName { get; set; }
    }

    public class ManyToManyRelation
    {
        public string entity1 { get; set; }
        public string entity2 { get; set; }
        public string schemaName { get; set; }
        public string intersectName { get; set; }
    }
}

