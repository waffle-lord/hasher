using System.Collections.Concurrent;
using Spectre.Console;

namespace hasher.Model;

public class FileValidationResults
{
    public int TotalMatch = 0;
    public int TotalMisMatch = 0;
    public int TotalMissing = 0;
    public int TotalUnexpected = 0;
    
    private readonly ConcurrentBag<FileCheckResult> _bag = [];

    private void CountResult(ValidationResult result)
    {
        switch (result)
        {
            case ValidationResult.Match:
                TotalMatch++;
                break;
            case ValidationResult.Mismatch:
                TotalMisMatch++;
                break;
            case ValidationResult.Missing:
                TotalMissing++;
                break;
            case ValidationResult.Unexpected:
                TotalUnexpected++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }

    public void CountOnly(ValidationResult result)
    {
        CountResult(result);
    }
    
    public void AddAndCount(FileCheckResult result)
    {
        _bag.Add(result);
        CountResult(result.ValidationResult);
    }

    public async Task<bool> SaveBagToJsonAsync(FileInfo saveFile)
    {
        if (saveFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]{saveFile.Name.EscapeMarkup()} already exists somehow. This is very unlikely and is probably and error. Please report this[/]");
            return false;
        }
        
        await FileHelper.SaveToJsonAsync(_bag.ToArray(), saveFile);
        
        saveFile.Refresh();
        
        return saveFile.Exists;
    }
}