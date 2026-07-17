namespace hasher.Model;

public class FileCheckResult(
    string expectedHash,
    string actualHash,
    string relativePath,
    ValidationResult validationResult)
{
    public string ExpectedHash { get; }= expectedHash;
    public string ActualHash { get; }= actualHash;

    public bool Ok =>  ActualHash == ExpectedHash;
    public string RelativePath { get; }= relativePath;
    public ValidationResult ValidationResult { get; } = validationResult;
}