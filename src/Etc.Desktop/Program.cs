using Avalonia;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop;
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if(args.Contains("--init-db")||args.Contains("--seed-demo"))
        {
            try
            {
                var connectionString=Infrastructure.AppSettings.Load().ConnectionString;
                if(string.IsNullOrWhiteSpace(connectionString))
                    throw new Shared.DomainException("Configuration","ETC_CONNECTION_STRING belum diatur pada terminal ini.");
                SqlConnectionStringBuilder settings;
                try{settings=new(connectionString);}
                catch(ArgumentException){throw new Shared.DomainException("Configuration","Format ETC_CONNECTION_STRING tidak valid. Periksa pemisah dan penulisan parameter tanpa membagikan password.");}
                if(string.IsNullOrWhiteSpace(settings.DataSource))
                    throw new Shared.DomainException("Configuration","Server harus diisi pada ETC_CONNECTION_STRING.");
                if(string.IsNullOrWhiteSpace(settings.InitialCatalog))
                    throw new Shared.DomainException("Configuration","Database harus diisi pada ETC_CONNECTION_STRING.");
                Console.WriteLine($"Target SQL: {settings.DataSource}; Database: {settings.InitialCatalog}.");
                var factory=new Infrastructure.SqlConnectionFactory(settings.ConnectionString);
                if(args.Contains("--seed-demo")){Demo.DemoSeeder.EnsureDemoTarget(factory.DatabaseName);new Demo.DemoSeeder(factory,TimeProvider.System).SeedAsync(Environment.GetEnvironmentVariable("ETC_DEMO_PASSWORD")).GetAwaiter().GetResult();Console.WriteLine("Data demonstrasi siap; password tidak ditampilkan.");}
                else{new Infrastructure.DatabaseInitializer(factory).InitializeAsync().GetAwaiter().GetResult();Console.WriteLine("Skema ETC siap.");}
                return 0;
            }
            catch(Shared.DomainException e){Console.Error.WriteLine(e.Message);return 2;}
            catch(SqlException e)
            {
                Console.Error.WriteLine($"Database belum dapat disiapkan. SQL {e.Number}, state {e.State}, baris {e.LineNumber}.");
                Console.Error.WriteLine(e.Number switch
                {
                    18456=>"Login SQL ditolak. Periksa User ID dan password SQL pada ETC_CONNECTION_STRING.",
                    4060=>"Database tujuan tidak dapat dibuka. Periksa nama database dan izin akun SQL.",
                    _=>"Periksa alamat/port SQL, izin akun, atau migrasi schema berdasarkan nomor error tersebut."
                });
                return 2;
            }
            catch(Exception e){Console.Error.WriteLine($"Database belum dapat disiapkan. Jenis error: {e.GetType().Name}. Password dan connection string tidak ditampilkan.");return 2;}
        }
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static Avalonia.AppBuilder BuildAvaloniaApp()=>Avalonia.AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
