using Godot;
using Godot.Collections;

public partial class SearchResult : Button
{
    public Control DetailsPg;
    public Dictionary data;
    public string currentStopId;

    public override void _Ready()
    {
        base._Ready();

        DetailsPg = GetNode("/root/App/Details/") as Control;

        ButtonDown += OnBtnPress;
    }

    private void OnBtnPress()
    {
        (DetailsPg as DetailsPage).build(data, currentStopId);
        DetailsPg.Show();
    }
}
