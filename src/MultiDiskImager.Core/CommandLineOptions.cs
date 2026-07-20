namespace MultiDiskImager.Core;

public sealed record CommandLineOptions(
    string? ImagePath,
    IReadOnlyList<string> Devices,
    bool Read,
    bool Write,
    bool Verify,
    bool OnlyAllocated,
    bool AutoStart,
    bool ShowHelp,
    bool ShowVersion,
    bool ListDevices,
    bool PrivilegedHelper,
    string? PipeName)
{
    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        string? image = null;
        string? pipe = null;
        var devices = new List<string>();
        var read = false;
        var write = false;
        var verify = false;
        var allocated = false;
        var start = false;
        var help = false;
        var version = false;
        var listDevices = false;
        var helper = false;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument.ToLowerInvariant())
            {
                case "-i":
                case "-image":
                case "--image":
                    image = RequireValue(args, ref index, argument);
                    break;
                case "-d":
                case "-device":
                case "--device":
                    devices.Add(RequireValue(args, ref index, argument));
                    while (index + 1 < args.Count && !args[index + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        devices.Add(args[++index]);
                    }

                    break;
                case "-r":
                case "-read":
                case "--read":
                    read = true;
                    break;
                case "-w":
                case "-write":
                case "--write":
                    write = true;
                    break;
                case "-v":
                case "-verify":
                case "--verify":
                    verify = true;
                    break;
                case "-oa":
                case "-onlyallocated":
                case "--only-allocated":
                    allocated = true;
                    break;
                case "-s":
                case "-start":
                case "--start":
                    start = true;
                    break;
                case "-h":
                case "--help":
                case "/?":
                    help = true;
                    break;
                case "--version":
                    version = true;
                    break;
                case "--list-devices":
                    listDevices = true;
                    break;
                case "--privileged-helper":
                    helper = true;
                    break;
                case "--pipe":
                    pipe = RequireValue(args, ref index, argument);
                    break;
                case "-z":
                case "-zip":
                case "--zip":
                case "-e":
                case "-encryption":
                case "--encryption":
                    throw new ArgumentException($"{argument} is not supported. bNovate Multi Disk Imager uses raw, unencrypted .img files only.");
                default:
                    if (!argument.StartsWith("-", StringComparison.Ordinal) && args.Count == 1)
                    {
                        image = argument;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown argument: {argument}");
                    }

                    break;
            }
        }

        if ((read ? 1 : 0) + (write ? 1 : 0) > 1)
        {
            throw new ArgumentException("Read and write cannot be selected together.");
        }

        return new CommandLineOptions(image, devices, read, write, verify, allocated, start, help, version, listDevices, helper, pipe);
    }

    public static string HelpText => """
        bNovate Multi Disk Imager

        Usage: MultiDiskImager [image.img] [options]
          -i, --image PATH          Select a raw image file
          -d, --device ID [...]    Select one or more platform device IDs
          -r, --read               Read one selected device to the image
          -w, --write              Write the image to selected devices
          -v, --verify             Verify, or verify after read/write
          -oa, --only-allocated    Read/verify through the last allocated partition
          -s, --start              Start the selected operation after validation
              --version            Print the version
              --list-devices       List detected physical devices and exit
          -h, --help               Show this help

        Images are byte-for-byte raw disk data. Compression and encryption are not supported.
        """;

    private static string RequireValue(IReadOnlyList<string> args, ref int index, string argument)
    {
        if (++index >= args.Count || args[index].StartsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{argument} requires a value.");
        }

        return args[index];
    }
}
