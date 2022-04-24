public enum PathType
{
    Empty,
    Horizontal,
    Vertical,
    TopLeftCorner,
    BottomLeftCorner,
    TopRightCorner,
    BottomRightCorner,
    Cross
}

public enum ButtonState
{
    Hidden,
    Next,
    Close,
}

public enum LevelMode
{
    Edit,       // Player entered edit mode but as is not actively drawing a path
    Drawing,    // Player is actively drawing path
    Playing,    // Player has switched to making the shepherds move
}

public enum EntranceState
{
    Opened,
    Closed,
}