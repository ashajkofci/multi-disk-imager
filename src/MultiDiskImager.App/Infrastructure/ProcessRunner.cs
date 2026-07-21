using System.Diagnostics;

namespace MultiDiskImager.Infrastructure;

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public void EnsureSuccess(string operation)
    {
        if (ExitCode != 0)
        {
            throw new IOException($"{operation} failed ({ExitCode}): {StandardError.Trim()}");
        }
    }
}

internal delegate Task<ProcessResult> ProcessRunnerDelegate(
    string fileName,
    IEnumerable<string> arguments,
    CancellationToken cancellationToken = default);

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new IOException($"Unable to start {fileName}.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                // Process.Exited/WaitForExitAsync can fail to signal for a
                // child created by the administrator AppleScript shell even
                // though diskutil has completed. Polling HasExited also reaps
                // the child and lets the helper continue immediately.
                await WaitForExitByPollingAsync(process, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            var output = await outputTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            var error = await errorTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new ProcessResult(process.ExitCode, output, error);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }

            throw;
        }
    }

    internal static async Task WaitForExitByPollingAsync(Process process, CancellationToken cancellationToken)
    {
        while (!process.HasExited)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }
}
