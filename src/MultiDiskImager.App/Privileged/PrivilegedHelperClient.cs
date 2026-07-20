using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;
using MultiDiskImager.Core;

namespace MultiDiskImager.Privileged;

internal static class PrivilegedHelperClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ImagingJobResult> RunAsync(
        HelperRequest request,
        IProgress<ImagingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var pipeName = $"multi-disk-imager-{Environment.ProcessId}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        await using var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
        using var helperProcess = LaunchHelper(pipeName);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var connectionTask = pipe.WaitForConnectionAsync(timeout.Token);
        var helperExitTask = helperProcess.WaitForExitAsync(CancellationToken.None);
        if (await Task.WhenAny(connectionTask, helperExitTask).ConfigureAwait(false) == helperExitTask)
        {
            await helperExitTask.ConfigureAwait(false);
            throw new UnauthorizedAccessException("Administrator approval was canceled or the privileged helper could not start.");
        }

        await connectionTask.ConfigureAwait(false);
        using var reader = new StreamReader(pipe, leaveOpen: true);
        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions).AsMemory(), cancellationToken).ConfigureAwait(false);

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                writer.WriteLine(JsonSerializer.Serialize(new HelperControl("cancel"), JsonOptions));
                writer.Flush();
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        });

        while (true)
        {
            var line = await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false);
            if (line is null)
            {
                throw new IOException("The privileged helper disconnected unexpectedly.");
            }

            var value = JsonSerializer.Deserialize<HelperEvent>(line, JsonOptions)
                ?? throw new InvalidDataException("The privileged helper sent an invalid response.");
            switch (value.Type)
            {
                case "progress" when value.Progress is not null:
                    progress?.Report(value.Progress);
                    break;
                case "result" when value.Result is not null:
                    return value.Result;
                case "error":
                    throw new IOException(value.Message ?? "The privileged operation failed.");
            }
        }
    }

    private static Process LaunchHelper(string pipeName)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Unable to determine the application executable path.");
        ProcessStartInfo startInfo;

        if (OperatingSystem.IsWindows())
        {
            startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add("--privileged-helper");
            startInfo.ArgumentList.Add("--pipe");
            startInfo.ArgumentList.Add(pipeName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var command = $"{ShellQuote(executable)} --privileged-helper --pipe {ShellQuote(pipeName)}";
            var appleScript = $"do shell script \"{command.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\" with administrator privileges";
            startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(appleScript);
        }
        else
        {
            startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--privileged-helper");
            startInfo.ArgumentList.Add("--pipe");
            startInfo.ArgumentList.Add(pipeName);
        }

        return Process.Start(startInfo) ?? throw new IOException("Unable to start the privileged helper.");
    }

    private static string ShellQuote(string value) => $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";
}
