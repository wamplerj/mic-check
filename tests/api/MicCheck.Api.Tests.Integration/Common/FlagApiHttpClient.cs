using System.Net.Http.Headers;
using System.Text;

namespace MicCheck.Api.Tests.Integration.Common;

public sealed class FlagApiHttpClient : IDisposable
{
    private static readonly string HttpFilesDirectory = Path.Combine(AppContext.BaseDirectory, "HttpFiles");

    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyDictionary<string, HttpFileRequest>> requestsByFile = new(StringComparer.OrdinalIgnoreCase);

    public FlagApiHttpClient(IntegrationTestSettings settings)
    {
        var handler = new HttpClientHandler();
        if (settings.AcceptAnyServerCertificate)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        httpClient = new HttpClient(handler);
    }

    public async Task<HttpResponseMessage> SendAsync(
        string httpFile,
        string requestName,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var requests = LoadRequests(httpFile);
        if (!requests.TryGetValue(requestName, out var request))
            throw new InvalidOperationException(
                $"Request '{requestName}' was not found in '{httpFile}'. Known requests: {string.Join(", ", requests.Keys)}.");

        var resolved = request.WithVariables(variables);
        var httpRequest = new HttpRequestMessage(new HttpMethod(resolved.Method), resolved.Url);

        string? contentType = null;
        foreach (var header in resolved.Headers)
        {
            if (string.Equals(header.Name, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentType = header.Value;
                continue;
            }

            if (!httpRequest.Headers.TryAddWithoutValidation(header.Name, header.Value))
                throw new InvalidOperationException(
                    $"Could not add header '{header.Name}' on request '{requestName}'.");
        }

        if (resolved.Body is not null)
        {
            var content = new StringContent(resolved.Body, Encoding.UTF8);
            if (contentType is not null)
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            httpRequest.Content = content;
        }

        return await httpClient.SendAsync(httpRequest, ct);
    }

    public void Dispose() => httpClient.Dispose();

    private IReadOnlyDictionary<string, HttpFileRequest> LoadRequests(string httpFile)
    {
        if (requestsByFile.TryGetValue(httpFile, out var cached))
            return cached;

        var path = Path.Combine(HttpFilesDirectory, httpFile);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"HTTP file '{httpFile}' was not found at '{path}'. Check the file is in HttpFiles/ and set to CopyToOutputDirectory.",
                path);

        var parsed = HttpFileParser.Parse(File.ReadAllText(path));
        requestsByFile[httpFile] = parsed;
        return parsed;
    }
}
