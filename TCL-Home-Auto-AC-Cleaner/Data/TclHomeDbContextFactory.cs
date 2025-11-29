using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TCL_Home_Auto_AC_Cleaner.Data;

public class TclHomeDbContextFactory : IDesignTimeDbContextFactory<TclHomeDbContext>
{
    public TclHomeDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.GetDirectoryName(typeof(TclHomeDbContextFactory).Assembly.Location)
            ?? Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets("tcl-home-ac-cleaner")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback connection string for design-time if not in user secrets
            connectionString = "Server=.;Database=TCLHome;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        }

        var optionsBuilder = new DbContextOptionsBuilder<TclHomeDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TclHomeDbContext(optionsBuilder.Options);
    }
}

