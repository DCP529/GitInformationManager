namespace GitInformationManager.Models;

public record FileBlameLine(int LineNumber, string CommitSha, string Author, DateTime Date, string Content);