namespace ProjetModelDrivenFront.Models
{
    public class App
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFavorite { get; set; }
        public string Status { get; set; } //Sucess - Archived - Deleted

        public string AppUrl { get; set; }

        public string JsonSchema { get; set; }

        public Guid EnvironnementDynamicsId { get; set; }
        public EnvironnementDynamics Environment { get; set; }
    }

}
