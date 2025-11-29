using Microsoft.EntityFrameworkCore;
using TCL_Home_Auto_AC_Cleaner.Data.Entities;

namespace TCL_Home_Auto_AC_Cleaner.Data;

public class TclHomeDbContext : DbContext
{
    public TclHomeDbContext(DbContextOptions<TclHomeDbContext> options) : base(options)
    {
    }

    public DbSet<ExceptionLog> Exceptions { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceStatus> DeviceStatuses { get; set; }
    public DbSet<Cleaning> Cleanings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId);
            entity.HasIndex(e => e.DeviceId).IsUnique();
        });

        modelBuilder.Entity<DeviceStatus>(entity =>
        {
            entity.HasOne(d => d.Device)
                .WithMany(p => p.DeviceStatuses)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cleaning>(entity =>
        {
            entity.HasOne(d => d.Device)
                .WithMany(p => p.Cleanings)
                .HasForeignKey(d => d.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

