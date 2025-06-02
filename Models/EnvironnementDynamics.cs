using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ProjetModelDrivenFront.Models
{
    public class EnvironnementDynamics
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public bool IsDefault { get; set; }

        [BindNever]
        public string? Status { get; set; }

        public Guid AccountId { get; set; }

        [BindNever]
        public Account? Account { get; set; }

        [BindNever]
        public ICollection<App>? Applications { get; set; }
    }
}
