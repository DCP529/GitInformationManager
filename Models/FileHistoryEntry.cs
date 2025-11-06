namespace GitInformationManager.Models;

public record FileHistoryEntry(string Sha, string Author, DateTime Date, string Message);