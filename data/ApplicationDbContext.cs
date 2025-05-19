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
    }

}
