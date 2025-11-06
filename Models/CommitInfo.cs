namespace GitInformationManager.Models;

public record CommitInfo(string Sha, string Author, DateTime Date, string Message);