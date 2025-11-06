namespace GitInformationManager.Models;

public record DeletedLineInfo(
    int LineNumber,
    string DeletedBy,
    string OriginalAuthor,
    string Text
);