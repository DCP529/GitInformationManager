using GitInformationManager;

while (true)
{
    Console.WriteLine("Выберите действие:");
    Console.WriteLine("1. Последние коммиты");
    Console.WriteLine("2. Изменения последнего коммита");
    Console.WriteLine("3. История Program.cs");
    Console.WriteLine("4. Blame Program.cs");
    Console.WriteLine("5. Удалённые строки Program.cs");
    Console.Write("> ");


    var choice = Console.ReadLine();
    switch (choice)
    {
        case "1": PrintCommits(); break;
        case "2": PrintCommitChanges(GitCliService.GetCommits().First().Sha); break;
        case "3": PrintFileHistory("Program.cs"); break;
        case "4": PrintFileBlame("Program.cs"); break;
        case "5": PrintDeletedLines(GitCliService.GetCommits().First().Sha, "Program.cs"); break;
        default: Console.WriteLine("Неизвестная команда."); break;
    }
    
    Console.WriteLine("\nНажмите любую клавишу, чтобы вернуться в меню...");
    Console.ReadKey();
}

static void PrintCommits()
{
    foreach (var commit in GitCliService.GetCommits())
        Console.WriteLine($"{commit.Date:g} {commit.Author}: {commit.Message}");
}

static void PrintCommitChanges(string sha)
{
    foreach (var file in GitCliService.GetCommitChanges(sha))
        Console.WriteLine($"{file.Status}\t{file.Path}");
}

static void PrintFileHistory(string relativePath)
{
    foreach (var entry in GitCliService.GetFileHistory(relativePath))
        Console.WriteLine($"{entry.Date:g} {entry.Author}: {entry.Message}");
}

static void PrintFileBlame(string relativePath)
{
    foreach (var line in GitCliService.GetFileBlame(relativePath))
        Console.WriteLine($"{line.LineNumber,3}: {line.Author,-15} {line.Date:g} {line.Content}");
}

static void PrintDeletedLines(string commitSha, string relativePath)
{
    var lines = GitCliService.GetDeletedLinesWithAuthors(commitSha, relativePath).ToList();

    if (lines.Count == 0)
    {
        Console.WriteLine("Нет удалённых строк.");
        return;
    }

    var commit = GitCliService.GetCommits().FirstOrDefault(c => c.Sha == commitSha);
    if (commit != null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nУдалённые строки в {relativePath}");
        Console.WriteLine($"Коммит: {commit.Sha}");
        Console.WriteLine($"{commit.Date:g} — {commit.Author}: {commit.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }

    foreach (var line in lines)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Файл: {line.FilePath}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"- {line.LineNumber,3}: {line.Text}");
        Console.ResetColor();

        Console.WriteLine($"Удалил: {line.DeletedBy}");
        Console.WriteLine($"Автор:  {line.OriginalAuthor}");
        Console.WriteLine($"Дата коммита: {line.CommitDate:g}");
        Console.WriteLine(new string('-', 60));
    }

    Console.WriteLine($"\nВсего удалённых строк: {lines.Count}");
}


