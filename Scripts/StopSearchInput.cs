using Godot;
using Godot.Collections;
using System.Linq;
using System.Text;

public partial class StopSearchInput : LineEdit
{
    [Export]
    public ItemList resultList;
    private HttpRequest requestNode = new();

    public Dictionary<string, string> StopIDs = new();
    public string currentStopID = null;
    public string currentStopName = null;

    public override void _Ready()
    {
        base._Ready();

        TextChanged += SearchForStops;

        AddChild(requestNode);
        requestNode.RequestCompleted += FormatStopsData;
    }

    public void SearchForStops(string toSearchFor)
    {
        if (toSearchFor.Length < 4)
        {
            resultList.Hide();
            resultList.Clear();
            return;
        }

        requestNode.CancelRequest();
        string[] headers = [
            "Authorization: Bearer " + ConfigWorker.GetConfig().ApiKey
        ];
        requestNode.Request("https://votavto22.sps-prosek.cz/3-projekt/api/v1/search/"+toSearchFor.Replace(" ", "%20")+"/", headers);
    }

    public void FormatStopsData(long result, long response_code, string[] headers, byte[] body)
    {
        if (response_code != 200) 
        {
            GD.PrintErr("API Error! Code "+response_code);
            return;
        }

        StopIDs.Clear();
        resultList.Clear();
        resultList.Show();

        foreach(Dictionary stop in ((Array)Json.ParseString(Encoding.UTF8.GetString(body))).Select(v => (Dictionary)v))
        {
            resultList.AddItem((string)stop["stop_name"]);
            StopIDs.Add((string)stop["stop_name"], (string)stop["stop_id"]);
        }
    }
}