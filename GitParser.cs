using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using GitInformationManager.Models;

namespace GitInformationManager;

public static partial class GitParser
{
    [GeneratedRegex(@"@@ -(\d+)")]
    private static partial Regex HunkHeaderRegex();
    
    public static IEnumerable<T> ParseDelimited<T>(string output, char delimiter = '|')
    {
        var ctor = typeof(T).GetConstructors().FirstOrDefault();

        if (ctor == null)
            throw new InvalidOperationException($"Тип {typeof(T).Name} не имеет публичного конструктора");

        var parameters = ctor.GetParameters();

        var lines = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        var parsedLines = lines
            .Select(line => line.Split(delimiter))
            .Where(parts => parts.Length >= parameters.Length);

        foreach (var parts in parsedLines)
        {
            var args = parameters
                .Select((p, i) => ConvertValue(parts[i].Trim(), p.ParameterType))
                .ToArray();

            yield return (T)ctor.Invoke(args);
        }
    }

    private static IEnumerable<FileBlameLine> ParseBlameInternal(string output)
    {
        var lines = output.Split('\n');
        string? sha = null;
        string? author = null;
        DateTime date = default;

        var lineNumber = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith('\t'))
            {
                var content = line[1..];
                lineNumber++;
                
                if (sha != null && author != null)
                {
                    yield return new FileBlameLine(lineNumber, sha, author, date, content);
                }

                sha = null;
                author = null;
                date = default;
                continue;
            }

            if (line.Length >= 40 && sha is null)
            {
                sha = line[..40];
            }
            else if (line.StartsWith("author "))
            {
                author = line[7..];
            }
            else if (line.StartsWith("author-time "))
            {
                if (long.TryParse(line[12..], out var unix))
                    date = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
            }
        }
    }

    public static IEnumerable<FileBlameLine> ParseBlame(string output) =>
        ParseBlameInternal(output);

    public static IReadOnlyDictionary<int, (string sha, string author)> ParseBlameMap(string output) =>
        ParseBlameInternal(output)
            .ToDictionary(x => x.LineNumber, x => (x.CommitSha, x.Author));

    public static IReadOnlyDictionary<string, List<(int lineNumber, string text)>> ParseDiff(string diff)
    {
        var result = new Dictionary<string, List<(int, string)>>();
        string? currentFile = null;
        var oldLine = 0;

        foreach (var line in diff.Split('\n'))
        {
            if (line.StartsWith("diff --git"))
            {
                var parts = line.Split(' ');
                currentFile = parts.Last().Trim();
                if (!result.ContainsKey(currentFile))
                    result[currentFile] = [];
            }
            else if (line.StartsWith("@@"))
            {
                var match = HunkHeaderRegex().Match(line);
                if (match.Success)
                    oldLine = int.Parse(match.Groups[1].Value);
            }
            else if (line.StartsWith('-') && !line.StartsWith("---"))
            {
                result[currentFile!].Add((oldLine, line[1..]));
                oldLine++;
            }
            else if (!line.StartsWith('+') && !line.StartsWith("\\"))
            {
                oldLine++;
            }
        }

        return result;
    }

    private static object ConvertValue(string raw, Type type)
    {
        var method = typeof(GitParser)
            .GetMethod(nameof(ConvertValueGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type);

        return method.Invoke(null, [raw])!;
    }

    private static T ConvertValueGeneric<T>(string raw)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)raw;

        if (typeof(T) == typeof(DateTime))
            return (T)(object)DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

        if (typeof(T) == typeof(DateTimeOffset))
            return (T)(object)DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture);

        return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
    }
}