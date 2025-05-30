using Godot;

public partial class FavouriteItem : Control
{
    public long favId;
    public string stopId;
    public string stopName;
    public string departureTime;

    public override void _Ready()
    {
        base._Ready();

        (GetNode("StopName") as Label).Text = stopName;
        if (departureTime != null)
        {
            (GetNode("Time") as Label).Text = "Odjezd po " + departureTime;
        }
        else
        {
            (GetNode("Time") as Label).Text = "Současný čas";
        }

        (GetNode("BtnNormal") as Button).ButtonDown += OnBtnDown;
        (GetNode("BtnRemove") as Button).ButtonDown += OnBtnRemoveDown;
    }

    private void OnBtnDown()
    {
        (GetNode("../../../../SearchParent/InputFrom") as StopSearchInput).currentStopName = stopName;
        (GetNode("../../../../SearchParent/InputFrom") as LineEdit).Text = stopName;
        (GetNode("../../../../SearchParent/InputFrom") as StopSearchInput).currentStopID = stopId;
        (GetNode("../../../../SearchParent/InputTime") as LineEdit).Text = departureTime;

        (GetNode("../../../../SearchParent/SearchBtn") as SearchBtn).SearchForDepartures();
    }

    private void OnBtnRemoveDown()
    {
        SQLiteManager.SqlWrite(@$"DELETE FROM favourites WHERE ID = {favId};");
        (GetNode("../../../../SearchParent/SearchBtn") as SearchBtn).BuildFavourites();
    }
}
