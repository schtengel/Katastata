using Katastata.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Katastata.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<ProgramEntity> Programs => Set<ProgramEntity>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Statistics> Statistics => Set<Statistics>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Statistics)
                .WithOne(st => st.User)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgramEntity>()
                .HasMany(p => p.Sessions)
                .WithOne(s => s.Program)
                .HasForeignKey(s => s.ProgramId);

            modelBuilder.Entity<ProgramEntity>()
                .HasMany(p => p.Statistics)
                .WithOne(st => st.Program)
                .HasForeignKey(st => st.ProgramId);

            modelBuilder.Entity<Category>().HasData(new Category { Id = 1, Name = "Не классифицировано" });
            modelBuilder.Entity<ProgramEntity>().HasIndex(x => x.Path).IsUnique();
            modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        }
    }
}
