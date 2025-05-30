using System.IO;
using System.Text.Json;

static class ConfigWorker
{
    public static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

#nullable enable
    public static ConfigData? GetConfig()
    {
        string configfile;

        try
        {
            configfile = File.ReadAllText("config.json");
        }
        catch (FileNotFoundException)
        {
            throw new System.Exception("config.json not found!");
        }

        return JsonSerializer.Deserialize<ConfigData>(configfile, JsonOptions);
    }

    public class ConfigData
    {
        public string? ApiKey { get; set;}
    }
}
