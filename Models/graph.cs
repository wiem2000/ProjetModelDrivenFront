namespace ProjetModelDrivenFront.Models
{
    public class graph
    {
    }
    public class Node
    {
        public string Id { get; set; }
    }

    public class Edge
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }
    public class GraphViewModel
    {
        public List<Node> Nodes { get; set; }
        public List<Edge> Edges { get; set; }
    }


    public class SchemaRoot
    {
        public Schema schema { get; set; }
    }

    public class Schema
    {
        public string application_name { get; set; }
        public Dictionary<string, Table> tables { get; set; }

        // Initialise par défaut avec une liste vide
        public List<Relation> relations { get; set; } = new List<Relation>();
    }

    public class Table
    {
        public string primaryfieldname { get; set; }
        public Dictionary<string, string> fields { get; set; }
    }

    public class Relation
    {
        public string from { get; set; }
        public string to { get; set; }
        public string type { get; set; }
    }




}
