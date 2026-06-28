using System.Security.Cryptography;

namespace hasher.Model;

public static class FileHelper
{
    
    public static List<FileInfo> GetFilesToCheck(DirectoryInfo directory, List<FileInfo> exclusions)
    {
        var files = directory.GetFiles("*.*", SearchOption.AllDirectories).ToList();

        foreach (var exclusion in exclusions)
        {
            for (var i = 0; i < files.Count; i++)
            {
                if (files[i].FullName == exclusion.FullName)
                {
                    files.RemoveAt(i);
                }
            }
        }
        
        return files;
    }
    
    public static async Task<string> GetFileHashAsync(SupportedAlgorithms algorithm, FileInfo fileInfo)
    {
        return algorithm switch
        {
            SupportedAlgorithms.MD5 => await GetMd5HashAsync(fileInfo),
            SupportedAlgorithms.SHA1 => await GetSha1HashAsync(fileInfo),
            SupportedAlgorithms.SHA256 => await GetSha256HashAsync(fileInfo),
            SupportedAlgorithms.SHA384 => await GetSha384HashAsync(fileInfo),
            SupportedAlgorithms.SHA512 => await GetSha512HashAsync(fileInfo),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
    
    private static async Task<string> GetMd5HashAsync(FileInfo fileInfo)
    {
        using var md5 = MD5.Create();
        await using var stream = fileInfo.OpenRead();
        var hash = await md5.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private static async Task<string> GetSha1HashAsync(FileInfo fileInfo)
    {
        using var sha1 = SHA1.Create();
        await using var stream = fileInfo.OpenRead();
        var hash = await sha1.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }
    
    private static async Task<string> GetSha256HashAsync(FileInfo fileInfo)
    {
        using var sha256 = SHA256.Create();
        await using var stream = fileInfo.OpenRead();
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private static async Task<string> GetSha384HashAsync(FileInfo fileInfo)
    {
        using var sha384 = SHA384.Create();
        await using var stream = fileInfo.OpenRead();
        var hash = await sha384.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private static async Task<string> GetSha512HashAsync(FileInfo fileInfo)
    {
        using var sha512 = SHA512.Create();
        await using var stream = fileInfo.OpenRead();
        var hash = await sha512.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }
    
}