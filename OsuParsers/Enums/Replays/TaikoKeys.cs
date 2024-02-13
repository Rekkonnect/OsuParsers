namespace OsuParsers.Enums.Replays;

public enum TaikoKeys
{
    None = 0,
    LeftRed = 1 << 0,
    LeftBlue = 1 << 1,
    RightRed = 1 << 2,
    RightBlue = 1 << 3,

    BothLefts = LeftRed | LeftBlue,
    BothRights = RightRed | RightBlue,

    BothRed = LeftRed | RightRed,
    BothBlue = LeftBlue | RightBlue,
}
