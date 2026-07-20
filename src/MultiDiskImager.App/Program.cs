using Avalonia;
using MultiDiskImager.Core;
using MultiDiskImager.Privileged;

namespace MultiDiskImager;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        CommandLineOptions options;
        try
        {
            options = CommandLineOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(CommandLineOptions.HelpText);
            return 2;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(CommandLineOptions.HelpText);
            return 0;
        }

        if (options.ShowVersion)
        {
            Console.WriteLine(Services.UpdateService.CurrentVersion);
            return 0;
        }

        if (options.PrivilegedHelper)
        {
            if (string.IsNullOrWhiteSpace(options.PipeName))
            {
                Console.Error.WriteLine("--privileged-helper requires --pipe.");
                return 2;
            }

            return PrivilegedHelperServer.RunAsync(options.PipeName).GetAwaiter().GetResult();
        }

        App.StartupOptions = options;
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

