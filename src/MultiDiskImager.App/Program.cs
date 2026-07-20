using Avalonia;
using MultiDiskImager.Core;
using MultiDiskImager.Platform;
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
            Console.WriteLine(Services.UpdateService.CurrentVersionText);
            return 0;
        }

        if (options.ListDevices)
        {
            try
            {
                var devices = PlatformServices.CreateCatalog().GetDevicesAsync().GetAwaiter().GetResult();
                foreach (var device in devices)
                {
                    Console.WriteLine($"{device.Id}\t{ByteSize.Format(device.Size)}\t{device.Model}\t{device.BusType}\t" +
                                      $"{(device.IsSystem ? "system" : device.IsRemovable ? "removable" : device.IsExternal ? "external" : "internal")}");
                }

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Unable to enumerate devices: {exception.Message}");
                return 1;
            }
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
