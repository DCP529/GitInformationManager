namespace GitInformationManager.Models;

public record DeletedLineInfo(
    int LineNumber,
    string Text,
    string DeletedBy,
    string OriginalAuthor,
    string FilePath,
    string CommitSha,
    DateTime CommitDate
);