using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlantTracker.Api.Models;

namespace PlantTracker.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserPlant> UserPlants => Set<UserPlant>();
    public DbSet<PlantPhoto> PlantPhotos => Set<PlantPhoto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserPlant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PlantPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<UserPlant>()
                  .WithMany()
                  .HasForeignKey(e => e.UserPlantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
