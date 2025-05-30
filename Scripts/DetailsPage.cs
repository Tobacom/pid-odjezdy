using System;
using System.Text;
using Godot;
using Godot.Collections;

public partial class DetailsPage : Control
{
	[Export] public Button backBtn, refreshBtn;
	[Export] public BoxContainer stopsContainer;
	[Export] public Control searchText;
	[Export] public PackedScene StopPrefab;
	private string currentTripId;
	private string currentStopId;
	private Dictionary currentData;
	private HttpRequest requestNode = new();

	public override void _Ready()
	{
		base._Ready();

		backBtn.ButtonDown += Hide;

		requestNode.RequestCompleted += OnRequestComplete;
		AddChild(requestNode);

		refreshBtn.ButtonDown += OnRefreshBtnPressed;
	}

	public void build(Dictionary data, string thisStopId)
	{
		refreshBtn.Disabled = true;

		currentData = data;
		currentTripId = (string)data["tripid"];
		currentStopId = thisStopId;

		(GetNode("Title") as Label).Text = "Linka " + data["route_display_name"];

		foreach (Node child in stopsContainer.GetChildren())
		{
			child.QueueFree();
		}

		searchText.Show();

		requestNode.CancelRequest();
		string[] headers = [
			"Authorization: Bearer " + ConfigWorker.GetConfig().ApiKey
		];
		requestNode.Request("https://votavto22.sps-prosek.cz/3-projekt/api/v1/item/" + data["tripid"] + "/all/", headers);
	}

	public void OnRequestComplete(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200)
		{
			GD.PrintErr("API Error! Code " + response_code);
			return;
		}
		else
		{
			searchText.Hide();
		}

		Dictionary data = (Dictionary)((Dictionary)Json.ParseString(Encoding.UTF8.GetString(body)))[currentTripId];

		int thisStopSequence = 0;
		for (int i = 1; i < data.Count; i++)
		{
			if ((string)((Dictionary)data[i.ToString()])["stop_id"] == currentStopId)
			{
				thisStopSequence = i;
				break;
			}
		}

		(GetNode("Linka/FromStop") as Label).Text = (string)((Dictionary)data["1"])["stop_name"];

		(GetNode("Linka/ToStop") as Label).Text = (string)((Dictionary)data[data.Count.ToString()])["stop_name"];


		var stop = GetNode("Stops/FirstStop") as Control;
		(stop.GetNode("Time") as Label).Text = DateTime.Parse((string)((Dictionary)data["1"])["departure_time"]).ToString("HH:mm");
		var stopName = stop.GetNode("StopName") as Label;
		stopName.Text = (string)((Dictionary)data["1"])["stop_name"];
		if (1 < thisStopSequence) stopName.AddThemeColorOverride("font_color", new Color("9b9b9b"));

		if (thisStopSequence == 1) SetOdjezdText((string)((Dictionary)data["1"])["stop_name"], (string)((Dictionary)data["1"])["departure_time"]);

		stop = GetNode("Stops/LastStop") as Control;
		(stop.GetNode("Time") as Label).Text = DateTime.Parse((string)((Dictionary)data[data.Count.ToString()])["departure_time"]).ToString("HH:mm");
		(stop.GetNode("StopName") as Label).Text = (string)((Dictionary)data[data.Count.ToString()])["stop_name"];


		for (int i = 2; i < data.Count; i++)
		{
			if (!((string)((Dictionary)data[i.ToString()])["stop_id"]).Trim().StartsWith('U')) continue;

			if (thisStopSequence == i) SetOdjezdText((string)((Dictionary)data[i.ToString()])["stop_name"], (string)((Dictionary)data[i.ToString()])["departure_time"]);

			Control prefab = GD.Load<PackedScene>(StopPrefab.ResourcePath).Instantiate() as Control;

			(prefab.GetNode("Time") as Label).Text = DateTime.Parse((string)((Dictionary)data[i.ToString()])["departure_time"]).ToString("HH:mm");

			stopName = prefab.GetNode("StopName") as Label;
			stopName.Text = (string)((Dictionary)data[i.ToString()])["stop_name"];
			if (i < thisStopSequence) stopName.AddThemeColorOverride("font_color", new Color("9b9b9b"));


			stopsContainer.AddChild(prefab);
		}

		refreshBtn.Disabled = false;
	}

	private void SetOdjezdText(string name, string cas)
	{
		(GetNode("Odjezd/StopName") as Label).Text = "Odjezd z " + name;

		if (DateTime.Now > (DateTime.Parse(cas) + new TimeSpan(0, 1, 0)))
		{
			(GetNode("Odjezd/Time") as Label).Text = "Již odjel :(";
			(GetNode("Odjezd/Time") as Label).AddThemeColorOverride("font_color", new Color("ff0000"));
		}
		else
		{
			(GetNode("Odjezd/Time") as Label).Text = SearchBtn.GetOdjezdZaDobu(DateTime.Parse(cas));
			(GetNode("Odjezd/Time") as Label).AddThemeColorOverride("font_color", new Color("009600"));
		}
	}

	private void OnRefreshBtnPressed()
	{
		build(currentData, currentStopId);
		refreshBtn.Disabled = true;
	}
}
