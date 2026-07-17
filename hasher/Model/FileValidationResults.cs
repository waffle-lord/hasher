using System.Collections.Concurrent;

namespace hasher.Model;

public class FileValidationResults
{
    // TODO: setup dictionary with start values
    public ConcurrentDictionary<ValidationResult, int> Totals { get; private set; } = [];
    private readonly ConcurrentBag<FileCheckResult> _bag = [];

    public void CountOnly(ValidationResult result)
    {
        Totals[result]++;
    }
    
    public void AddAndCount(FileCheckResult result)
    {
        // only use results bag if we are outputing the data
        _bag.Add(result);
        Totals[result.ValidationResult]++;
    }

    public async Task SaveBagToJsonAsync(string filePath)
    {
        await FileHelper.SaveToJsonAsync(_bag.ToArray(), filePath);
    }
}