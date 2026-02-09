namespace CardGameScorer.Models;

public enum Position
{
    West = 0,
    North = 1,
    East = 2,
    South = 3
}

public static class PositionExtensions
{
    public static Position NextClockwise(this Position position)
    {
        return (Position)(((int)position + 1) % 4);
    }

    public static string ToDisplayString(this Position position)
    {
        return position switch
        {
            Position.West => "West",
            Position.North => "North",
            Position.East => "East",
            Position.South => "South",
            _ => position.ToString()
        };
    }

    public static string ToInitial(this Position position)
    {
        return position switch
        {
            Position.West => "W",
            Position.North => "N",
            Position.East => "E",
            Position.South => "S",
            _ => "?"
        };
    }
}
