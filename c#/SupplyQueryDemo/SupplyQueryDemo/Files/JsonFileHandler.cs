namespace SupplyQueryDemo.Files;

public static class JsonFileHandler
{
    internal static void SaveToJsonFile(string json, string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        
        File.AppendAllText(path, json);
    }
}
