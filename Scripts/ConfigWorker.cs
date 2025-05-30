using System.Text.Json;
using Godot;

partial class ConfigWorker : Node
{
    public static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

#nullable enable
    public static ConfigData? GetConfig()
    {
        string configfile = FileAccess.GetFileAsString("res://config.json");

        if (configfile == string.Empty)
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
