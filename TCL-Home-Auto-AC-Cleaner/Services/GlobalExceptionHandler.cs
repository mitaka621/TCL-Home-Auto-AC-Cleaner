using Microsoft.Extensions.DependencyInjection;
using TCL_Home_Auto_AC_Cleaner.Data;
using TCL_Home_Auto_AC_Cleaner.Data.Entities;

namespace TCL_Home_Auto_AC_Cleaner.Services;

public class GlobalExceptionHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _useDatabase;

    public GlobalExceptionHandler(IServiceProvider serviceProvider, bool useDatabase)
    {
        _serviceProvider = serviceProvider;
        _useDatabase = useDatabase;
    }

    public async Task HandleExceptionAsync(Exception exception, string? context = null)
    {
        var exceptionMessage = exception.ToString();
        var exceptionName = exception.GetType().Name;
        var contextInfo = string.IsNullOrEmpty(context) ? string.Empty : $"[{context}] ";

        Console.WriteLine($"ERROR {contextInfo}{exceptionName}: {exception.Message}");
        if (exception.StackTrace != null)
        {
            Console.WriteLine($"Stack Trace: {exception.StackTrace}");
        }

        if (_useDatabase)
        {
            await LogExceptionToDatabaseAsync(exceptionName, exceptionMessage);
        }
    }

    private async Task LogExceptionToDatabaseAsync(string name, string? value)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TclHomeDbContext>();

            var exceptionLog = new ExceptionLog
            {
                Name = name,
                Value = value,
                TimeOccurred = DateTime.Now
            };
            dbContext.Exceptions.Add(exceptionLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log exception to database: {ex.Message}");
        }
    }
}

