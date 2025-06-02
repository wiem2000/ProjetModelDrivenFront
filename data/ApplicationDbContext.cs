using Microsoft.EntityFrameworkCore;
using ProjetModelDrivenFront.Models;
using System.Collections.Generic;

namespace ProjetModelDrivenFront.data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<EnvironnementDynamics> Environments { get; set; }
        public DbSet<App> Applications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // One Account => Many Environments
            modelBuilder.Entity<EnvironnementDynamics>()
                .HasOne(e => e.Account)
                .WithMany(a => a.Environments)
                .HasForeignKey(e => e.AccountId);

            // One Environment => Many Applications
            modelBuilder.Entity<App>()
                .HasOne(app => app.Environment)
                .WithMany(env => env.Applications)
                .HasForeignKey(app => app.EnvironnementDynamicsId);

            base.OnModelCreating(modelBuilder);
        }


    }

}
