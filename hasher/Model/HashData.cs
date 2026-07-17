namespace hasher.Model;

public class HashData
{
    public string Hash { get; private set; }
    public string RelativePath { get; private set; }

    public HashData(string hashInfo)
    {
        var info = hashInfo.Trim().Split(" :: ");

        if (info.Length != 2)
        {
            throw new Exception("Invalid hash info");
        }
        
        Hash = info[0];
        RelativePath = info[1];
    }
}