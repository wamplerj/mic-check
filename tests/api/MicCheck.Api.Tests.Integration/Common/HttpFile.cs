using System.Text;

namespace MicCheck.Api.Tests.Integration.Common;

public sealed record HttpFileHeader(string Name, string Value);

public sealed record HttpFileRequest(string Name, string Method, string Url, IReadOnlyList<HttpFileHeader> Headers, string? Body)
{
    public HttpFileRequest WithVariables(IReadOnlyDictionary<string, string> variables)
    {
        string Substitute(string input)
        {
            var result = input;
            foreach (var (key, value) in variables)
                result = result.Replace("{{" + key + "}}", value, StringComparison.Ordinal);
            return result;
        }

        return new HttpFileRequest(
            Name,
            Method,
            Substitute(Url),
            Headers.Select(h => new HttpFileHeader(h.Name, Substitute(h.Value))).ToList(),
            Body is null ? null : Substitute(Body));
    }
}

public static class HttpFileParser
{
    public static IReadOnlyDictionary<string, HttpFileRequest> Parse(string content)
    {
        var requests = new Dictionary<string, HttpFileRequest>(StringComparer.OrdinalIgnoreCase);
        var blocks = SplitBlocks(content);

        foreach (var block in blocks)
        {
            var parsed = ParseBlock(block);
            if (parsed is not null)
                requests[parsed.Name] = parsed;
        }

        return requests;
    }

    private static List<string> SplitBlocks(string content)
    {
        var blocks = new List<string>();
        var current = new StringBuilder();

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("###", StringComparison.Ordinal))
            {
                if (current.Length > 0)
                {
                    blocks.Add(current.ToString());
                    current.Clear();
                }
                current.AppendLine(line);
            }
            else
            {
                current.AppendLine(line);
            }
        }

        if (current.Length > 0)
            blocks.Add(current.ToString());

        return blocks;
    }

    private static HttpFileRequest? ParseBlock(string block)
    {
        var lines = block.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count == 0)
            return null;

        var nameLine = lines[0];
        if (!nameLine.StartsWith("###", StringComparison.Ordinal))
            return null;

        var name = nameLine.TrimStart('#').Trim();
        if (string.IsNullOrEmpty(name))
            return null;

        var i = 1;
        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
            i++;

        if (i >= lines.Count)
            return null;

        var requestLineParts = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length < 2)
            throw new InvalidOperationException(
                $"Request '{name}' is missing a method and URL line.");

        var method = requestLineParts[0];
        var url = requestLineParts[1];
        i++;

        var headers = new List<HttpFileHeader>();
        while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]))
        {
            var colon = lines[i].IndexOf(':');
            if (colon <= 0)
                throw new InvalidOperationException(
                    $"Request '{name}' has a malformed header line: '{lines[i]}'.");

            headers.Add(new HttpFileHeader(
                lines[i][..colon].Trim(),
                lines[i][(colon + 1)..].Trim()));
            i++;
        }

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i]))
            i++;

        string? body = null;
        if (i < lines.Count)
        {
            var bodyText = string.Join("\n", lines.Skip(i)).Trim();
            if (bodyText.Length > 0)
                body = bodyText;
        }

        return new HttpFileRequest(name, method, url, headers, body);
    }
}
