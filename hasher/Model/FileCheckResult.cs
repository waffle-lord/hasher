namespace hasher.Model;

public class FileCheckResult(
    string expectedHash,
    string actualHash,
    string relativePath,
    ValidationResult validationResult)
{
    public readonly string ExpectedHash = expectedHash;
    public readonly string ActualHash = actualHash;

    public bool Ok =>  ActualHash == ExpectedHash;
    public string RelativePath = relativePath;
    public ValidationResult ValidationResult = validationResult;
}