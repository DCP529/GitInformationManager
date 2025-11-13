using System.Diagnostics;
using System.Text;
using GitInformationManager.Constants;
using GitInformationManager.Models;

namespace GitInformationManager;

public class GitCliService
{
    private static readonly string RepoPath = FindGitRoot(Directory.GetCurrentDirectory())
                                              ?? throw new InvalidOperationException("Git-репозиторий не найден.");

    private static string? FindGitRoot(string startDir)
    {
        var output = RunGit(GitCommands.GetRoot, startDir, staticMode: true);
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    public static IEnumerable<CommitInfo> GetCommits()
    {
        var output = RunGit(GitCommands.LogFormat);
        return GitParser.ParseDelimited<CommitInfo>(output);
    }

    public static IEnumerable<FileChange> GetCommitChanges(string commitSha)
    {
        var args = string.Format(GitCommands.ShowCommit, commitSha);
        var output = RunGit(args);

        return GitParser.ParseDelimited<FileChange>(output, '\t');
    }

    public static IEnumerable<FileHistoryEntry> GetFileHistory(string relativePath)
    {
        var args = string.Format(GitCommands.FileHistory, relativePath);
        var output = RunGit(args);

        return GitParser.ParseDelimited<FileHistoryEntry>(output);
    }

    public static IEnumerable<FileBlameLine> GetFileBlame(string relativePath)
    {
        var args = string.Format(GitCommands.Blame, relativePath);
        var output = RunGit(args);

        return GitParser.ParseBlame(output);
    }

    public static IEnumerable<DeletedLineInfo> GetDeletedLinesWithAuthors(string commitSha, string? fileName = null)
    {
        var deletedBy = GetDeletedBy(commitSha);
        var fileDiffs = GetFileDiffs(commitSha, fileName);

        foreach (var (filePath, deletedLines) in fileDiffs)
        {
            var cleanPath = NormalizeGitPath(filePath);
            var fileStatus = GetFileStatus(commitSha, cleanPath);

            if (fileStatus == "A")
            {
                Debug.WriteLine($"{cleanPath} был добавлен — пропускаем blame");
                continue;
            }

            var blameMap = GetParentCommitBlame(commitSha, cleanPath);
            if (blameMap.Count == 0)
                continue;

            foreach (var (lineNumber, text) in deletedLines)
            {
                if (blameMap.TryGetValue(lineNumber, out var info))
                {
                    yield return new DeletedLineInfo(lineNumber, deletedBy, info.author, text);
                }
            }
        }
    }

    private static string GetDeletedBy(string commitSha)
    {
        var args = string.Format(GitCommands.DeletedBy, commitSha);
        return RunGit(args).Trim();
    }

    private static IReadOnlyDictionary<string, List<(int lineNumber, string text)>> GetFileDiffs(string commitSha,
        string? fileName)
    {
        var fileArg = string.IsNullOrWhiteSpace(fileName) ? "" : fileName;
        var args = string.Format(GitCommands.Diff, commitSha, fileArg);
        var output = RunGit(args);
        return GitParser.ParseDiff(output);
    }

    private static string GetFileStatus(string commitSha, string cleanPath)
    {
        var args = string.Format(GitCommands.ShowCommit, commitSha);
        var output = RunGit(args);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Split('\t'))
            .Where(parts => parts.Length == 2 && parts[1] == cleanPath)
            .Select(parts => parts[0])
            .FirstOrDefault() ?? "M";
    }

    private static IReadOnlyDictionary<int, (string sha, string author)> GetParentCommitBlame(string commitSha,
        string cleanPath)
    {
        try
        {
            var args = string.Format(GitCommands.ParentBlame, commitSha, cleanPath);
            var output = RunGit(args);
            return GitParser.ParseBlameMap(output);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no such path"))
        {
            Debug.WriteLine($"{cleanPath} отсутствует в предыдущем коммите (возможно, был удалён целиком)");
            return new Dictionary<int, (string sha, string author)>();
        }
    }

    private static string NormalizeGitPath(string file)
    {
        return file.StartsWith("a/") || file.StartsWith("b/") ? file[2..] : file;
    }


    private static string RunGit(string arguments, string? workingDirectory = null, bool staticMode = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? RepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi)!;

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return output;
        }

        if (staticMode)
        {
            return "";
        }

        throw new InvalidOperationException($"Git error: {error}");
    }
}