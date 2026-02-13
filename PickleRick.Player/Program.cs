using System.Diagnostics;

var videoPath = GetArgumentValue(args, "--video");
if (string.IsNullOrWhiteSpace(videoPath))
{
    return;
}

videoPath = Path.GetFullPath(videoPath);
if (!File.Exists(videoPath))
{
    return;
}

var playerPath = Path.Combine(Environment.SystemDirectory, "wmplayer.exe");
if (!File.Exists(playerPath))
{
    playerPath = "wmplayer.exe";
}

var arguments = $"/fullscreen /play \"{videoPath}\"";
var startInfo = new ProcessStartInfo
{
    FileName = playerPath,
    Arguments = arguments,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = Process.Start(startInfo);
if (process != null)
{
    process.WaitForExit();
}

static string? GetArgumentValue(string[] args, string name)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }

    return null;
}
