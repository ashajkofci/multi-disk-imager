using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace MultiDiskImager.Services;

internal sealed record UpdateInfo(Version Version, Uri ReleasePage);

internal sealed class UpdateService(HttpClient httpClient)
{
    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        var repository = Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "Repository")?.Value;
        if (string.IsNullOrWhiteSpace(repository) || !repository.Contains('/', StringComparison.Ordinal))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repository}/releases/latest");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("bNovateMultiDiskImager", CurrentVersion.ToString()));
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var tag = document.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v');
        var url = document.RootElement.GetProperty("html_url").GetString();
        return Version.TryParse(tag?.Split('-', 2)[0], out var version) && IsNewer(version) && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? new UpdateInfo(version, uri)
            : null;
    }

    public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
    public static string CurrentVersionText =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+', 2)[0]
        ?? CurrentVersion.ToString(3);

    private static bool IsNewer(Version candidate)
    {
        var candidateParts = (candidate.Major, candidate.Minor, Math.Max(0, candidate.Build));
        var currentParts = (CurrentVersion.Major, CurrentVersion.Minor, Math.Max(0, CurrentVersion.Build));
        var comparison = candidateParts.CompareTo(currentParts);
        return comparison > 0 || comparison == 0 && CurrentVersionText.Contains('-', StringComparison.Ordinal);
    }
}
