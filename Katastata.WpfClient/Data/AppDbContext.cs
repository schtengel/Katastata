using Microsoft.EntityFrameworkCore;
using Katastata.Models;

namespace Katastata.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Program> Programs { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Statistics> Statistics { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Statistics)
                .WithOne(st => st.User)
                .HasForeignKey(st => st.UserId);

            modelBuilder.Entity<Program>()
                .HasMany(p => p.Sessions)
                .WithOne(s => s.Program)
                .HasForeignKey(s => s.ProgramId);

            modelBuilder.Entity<Program>()
                .HasMany(p => p.Statistics)
                .WithOne(st => st.Program)
                .HasForeignKey(st => st.ProgramId);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Programs)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Не классифицировано"
                }
            );
        }
    }
}
