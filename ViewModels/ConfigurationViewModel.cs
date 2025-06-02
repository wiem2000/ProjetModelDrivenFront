using ProjetModelDrivenFront.Models;

namespace ProjetModelDrivenFront.ViewModels
{
    public class ConfigurationViewModel
    {
        public List<EnvironnementDynamics> Environments { get; set; } = new List<EnvironnementDynamics>();
        public EnvironnementDynamics NewEnvironment { get; set; } = new EnvironnementDynamics();
        public Guid? DefaultEnvironmentId { get; set; }
        public Guid AccountId { get; set; }
    }
}
