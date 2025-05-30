using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public partial class SearchBtn : Button
{
	[Export] public PackedScene FavouritePrefab;
	[Export] public BoxContainer FavouritesBox;
	[Export] public Label AddToFavouritesLabel;
	[Export] public Button AddToFavouritesBtn, ExportBtn, RefreshBtn;
	[Export] public FileDialog ExportLocationDialog;
	private long? currentFavId = null;
	[Export] public Control LandingPage, SearchesPage;
	[Export] public StopSearchInput fromStop, toStop;
	[Export] public LineEdit timeInput;
	[Export] public Button SearchesBackBtn;
	[Export] public Label SearchingTxt;
	[Export] public BoxContainer SearchResultsBox;
	private HttpRequest requestNode = new();
	private string timeToSearch;
	private byte[] currentRawData;

	public override void _Ready()
	{
		base._Ready();

		BuildFavourites();

		AddChild(requestNode);
		requestNode.RequestCompleted += OnRequestComplete;

		Pressed += SearchForDepartures;
		SearchesBackBtn.Pressed += SearchBackBtnPressed;

		AddToFavouritesBtn.ButtonDown += ToggleSearchInFavourites;

		ExportBtn.ButtonDown += ExportLocationDialog.Show;
		ExportLocationDialog.FileSelected += OnExportPressed;

		RefreshBtn.ButtonDown += SearchForDepartures;
	}

	public void SearchForDepartures()
	{
		RefreshBtn.Disabled = true;
		ExportBtn.Disabled = true;

		foreach (Node child in SearchResultsBox.GetChildren())
		{
			child.QueueFree();
		}

		SearchesPage.Show();
		LandingPage.Hide();
		SearchingTxt.Text = "Vyhledávání odjezdů...";
		SearchingTxt.Show();

		timeToSearch = string.Empty;
		if (timeInput.Text != string.Empty)
		{
			var match = Regex.Match(timeInput.Text.Trim(), @"(\d{1,2}[:]\d{2}(?:[:]\d{2})?)");
			if (match.Groups.Count > 0)
			{
				timeToSearch = match.Groups.Values.First().Value;
			}
			else
			{
				// když čas bude ve špatném formátu
			}
		}
		UpdateSearchesFavouriteStarIcon();

		requestNode.CancelRequest();
		string[] headers = [
			"Authorization: Bearer " + ConfigWorker.GetConfig().ApiKey
		];
		//fromStop.currentStopID.Replace(" ", "%20")+"/"+toStop.currentStopID.Replace(" ", "%20")
		requestNode.Request("https://votavto22.sps-prosek.cz/3-projekt/api/v1/departures/" + fromStop.currentStopID + "/2/" + (timeToSearch != string.Empty ? timeToSearch + "/" : string.Empty), headers);
	}

	private void OnRequestComplete(long result, long response_code, string[] headers, byte[] body)
	{
		if (response_code != 200)
		{
			GD.PrintErr("API Error! Code " + response_code);
			SearchingTxt.Text = "API Error! Code " + response_code;
			return;
		}
		else
		{
			SearchingTxt.Hide();
		}

		currentRawData = body;

		foreach (Dictionary searchres in ((Godot.Collections.Array)Json.ParseString(Encoding.UTF8.GetString(body))).Select(v => (Dictionary)v))
		{
			if (fromStop.currentStopName == (string)searchres["end_station"]) continue;

			var newSR = GD.Load<PackedScene>("res://Prefabs/SearchTemplate.tscn").Instantiate() as Control;
			newSR.CustomMinimumSize = new Vector2(300, 125);

			var rdn = newSR.GetNode("RouteDisplayName") as Label;
			rdn.Text = (string)searchres["route_display_name"];

			var tts = newSR.GetNode("TripTerminalStation") as Label;
			tts.Text = "Směr " + searchres["end_station"];

			var td = newSR.GetNode("TripDetails") as Label;
			td.Text = "Přes " + searchres["route_route"];

			var dt = newSR.GetNode("DepartureTime") as Label;
			try
			{
				DateTime depTime = DateTime.Parse((string)searchres["departure_time"]);
				dt.Text = depTime.ToString("HH:mm") + " - " + GetOdjezdZaDobu(depTime);
			}
			catch (FormatException)
			{
				GD.Print(searchres["tripid"] + " jede po 24 hodině, nepřipadá v úvahu >:(");
				continue;
			}

			var srBtn = newSR.GetNode("Button") as SearchResult;
			srBtn.data = searchres;
			srBtn.currentStopId = fromStop.currentStopID;

			SearchResultsBox.AddChild(newSR);
		}

		RefreshBtn.Disabled = false;
		ExportBtn.Disabled = false;
	}

	private void SearchBackBtnPressed()
	{
		LandingPage.Show();
		SearchesPage.Hide();
	}

	public void BuildFavourites()
	{
		foreach (var item in FavouritesBox.GetChildren())
		{
			item.QueueFree();
		}

		SQLiteManager.SqlWrite(@"CREATE TABLE IF NOT EXISTS favourites(ID INTEGER not null PRIMARY KEY, stop_id TEXT not null, stop_name TEXT not null, departure_time TEXT null);");

		var favouritePrefab = GD.Load<PackedScene>(FavouritePrefab.ResourcePath);

		foreach (var item in SQLiteManager.SqlRead("SELECT * FROM favourites;"))
		{
			var prefab = favouritePrefab.Instantiate() as FavouriteItem;

			prefab.favId = (long)item.Value["ID"];
			prefab.stopId = (string)item.Value["stop_id"];
			prefab.stopName = (string)item.Value["stop_name"];
			prefab.departureTime = (string)item.Value["departure_time"];

			FavouritesBox.AddChild(prefab);
		}
	}

	private void ToggleSearchInFavourites()
	{
		if (currentFavId != null)
		{
			SQLiteManager.SqlWrite($"DELETE FROM favourites WHERE ID = {currentFavId};");
		}
		else
		{
			SQLiteManager.SqlWrite($"INSERT INTO favourites(stop_id, stop_name, departure_time) VALUES('{fromStop.currentStopID}', '{fromStop.currentStopName}', " + (timeToSearch == string.Empty ? "null" : $"'{timeToSearch}'") + ");");
		}
		UpdateSearchesFavouriteStarIcon();
		BuildFavourites();
	}

	private void UpdateSearchesFavouriteStarIcon()
	{
		var relatedFavs = SQLiteManager.SqlRead($"SELECT * FROM favourites WHERE stop_id = '{fromStop.currentStopID}' AND departure_time " + ((timeToSearch == string.Empty || timeToSearch == null) ? "IS NULL" : $"= '{timeToSearch}'") + ";");

		if (relatedFavs.Count > 0)
		{
			currentFavId = (long)relatedFavs.First().Value["ID"];
			ToggleSearchesFavouritesIcon(true);
		}
		else
		{
			currentFavId = null;
			ToggleSearchesFavouritesIcon(false);
		}
	}

	private void ToggleSearchesFavouritesIcon(bool active)
	{
		if (active)
		{
			AddToFavouritesLabel.AddThemeColorOverride("font_color", new Color("ffff00"));
		}
		else
		{
			AddToFavouritesLabel.AddThemeColorOverride("font_color", new Color("c8c8c8"));
		}
	}

	public static string GetOdjezdZaDobu(DateTime casOdjezdu)
	{
		string odjezd = "Za ";

		var rozdil = casOdjezdu - DateTime.Now + new TimeSpan(0, 1, 0);

		if (rozdil.Hours == 0 && rozdil.Minutes == 0)
		{
			return "Právě teď";
		}

		switch (rozdil.Hours)
		{
			case 0:
				break;
			case 1:
				odjezd += $"1 hodinu ";
				break;
			case 2:
			case 3:
			case 4:
				odjezd += $"{rozdil.Hours} hodiny ";
				break;
			default:
				odjezd += $"{rozdil.Hours} hodin ";
				break;
		}

		if (rozdil.Hours > 0 && rozdil.Minutes > 0)
		{
			odjezd += "a ";
		}

		switch (rozdil.Minutes)
		{
			case 0:
				break;
			case 1:
				odjezd += $"1 minutu";
				break;
			case 2:
			case 3:
			case 4:
				odjezd += $"{rozdil.Minutes} minuty";
				break;
			default:
				odjezd += $"{rozdil.Minutes} minut";
				break;
		}

		return odjezd;
	}

	private void OnExportPressed(string path)
	{
		GD.Print(path);
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file.StoreBuffer(currentRawData);
	}
}
