namespace PickleRick;

public sealed class PlaybackOptions
{
    public int MinPlaysPerHour { get; set; } = 1;
    public int MaxPlaysPerHour { get; set; } = 4;
    public string VideoPath { get; set; } = "I'mPickeRick.mp4";
    public string PlayerExeName { get; set; } = "PickleRick.Player.exe";
}

