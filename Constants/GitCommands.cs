namespace GitInformationManager.Constants;

public static class GitCommands
{
    public const string GetRoot = "rev-parse --show-toplevel";
    public const string LogFormat = "log --pretty=format:\"%H|%an|%ai|%s\"";
    public const string ShowCommit = "show --name-status --pretty=format:\"\" {0}";
    public const string FileHistory = "log --pretty=format:\"%H|%an|%ai|%s\" -- \"{0}\"";
    public const string Blame = "blame --line-porcelain -- \"{0}\"";
    public const string ParentBlame = "blame {0}^ --line-porcelain -- \"{1}\"";
    public const string DeletedBy = "show -s --format=%an {0}";
    public const string Diff = "show {0} --no-color --unified=0 --pretty=format:\"\" -p {1}";
}
