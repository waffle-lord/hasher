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
            SupportedAlgorithms.MD5 => await GetMD5HashAsync(fileInfo),
            SupportedAlgorithms.SHA1 => await GetSHA1HashAsync(fileInfo),
            SupportedAlgorithms.SHA256 => await GetSHA256HashAsync(fileInfo),
            SupportedAlgorithms.SHA384 => await GetSHA384HashAsync(fileInfo),
            SupportedAlgorithms.SHA512 => await GetSHA512HashAsync(fileInfo),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
    
    private static async Task<string> GetMD5HashAsync(FileInfo fileInfo)
    {
        using var md5 = MD5.Create();
        using (var stream = fileInfo.OpenRead())
        {
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }

    private static async Task<string> GetSHA1HashAsync(FileInfo fileInfo)
    {
        throw new NotImplementedException();
    }
    
    private static async Task<string> GetSHA256HashAsync(FileInfo fileInfo)
    {
        throw new NotImplementedException();
    }

    private static async Task<string> GetSHA384HashAsync(FileInfo fileInfo)
    {
        throw new NotImplementedException();
    }

    private static async Task<string> GetSHA512HashAsync(FileInfo fileInfo)
    {
        throw new NotImplementedException();
    }
    
}