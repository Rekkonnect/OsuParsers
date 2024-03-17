namespace OsuParsers.Database.Objects;

public struct DbTimingPoint
{
    public double BPM { get; set; }
    public double Offset { get; set; }
    public bool Inherited { get; set; }
}
