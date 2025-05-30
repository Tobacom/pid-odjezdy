using Godot;
using System;

public partial class SRStop : ItemList
{
    [Export] public LineEdit targetLE;

    public override void _Ready()
    {
        base._Ready();

        ItemClicked += OnItemClick;
    }

    public void OnItemClick(long index, Vector2 mousePos, long mouseBtnIndex)
    {
        StopSearchInput ssinput = targetLE as StopSearchInput; 
        targetLE.Text = ssinput.currentStopName = GetItemText((int)index);
        ssinput.currentStopID = ssinput.StopIDs[GetItemText((int)index)];
        Hide();
    }
}