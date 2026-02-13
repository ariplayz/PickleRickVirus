using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace PickleRick;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly PlaybackOptions _options;
    private readonly Random _random = new();

    public Worker(ILogger<Worker> logger, IOptions<PlaybackOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var playerPath = Path.Combine(AppContext.BaseDirectory, _options.PlayerExeName);
        var videoPath = ResolvePath(_options.VideoPath);

        if (!File.Exists(playerPath))
        {
            _logger.LogError("Player executable not found at {path}", playerPath);
        }

        if (!File.Exists(videoPath))
        {
            _logger.LogError("Video file not found at {path}", videoPath);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            var hourStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
            var hourEnd = hourStart.AddHours(1);

            var schedule = BuildSchedule(hourStart, hourEnd);
            foreach (var scheduledTime in schedule)
            {
                if (scheduledTime <= DateTimeOffset.Now)
                {
                    continue;
                }

                var delay = scheduledTime - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next playback scheduled at {time}", scheduledTime);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await PlayOnceAsync(playerPath, videoPath, stoppingToken);
            }

            var remaining = hourEnd - DateTimeOffset.Now;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, stoppingToken);
            }
        }
    }

    private List<DateTimeOffset> BuildSchedule(DateTimeOffset hourStart, DateTimeOffset hourEnd)
    {
        var minPlays = Math.Max(1, _options.MinPlaysPerHour);
        var maxPlays = Math.Max(minPlays, _options.MaxPlaysPerHour);
        var playCount = _random.Next(minPlays, maxPlays + 1);
        var secondsInHour = (int)Math.Max(1, (hourEnd - hourStart).TotalSeconds);

        var offsets = new HashSet<int>();
        while (offsets.Count < playCount)
        {
            offsets.Add(_random.Next(0, secondsInHour));
        }

        return offsets
            .Select(seconds => hourStart.AddSeconds(seconds))
            .OrderBy(time => time)
            .ToList();
    }

    private async Task PlayOnceAsync(string playerPath, string videoPath, CancellationToken stoppingToken)
    {
        if (!File.Exists(playerPath) || !File.Exists(videoPath))
        {
            return;
        }

        var arguments = $"--video \"{videoPath}\"";
        if (!ActiveSessionProcessLauncher.TryLaunch(playerPath, arguments, _logger, out var processId))
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            await process.WaitForExitAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            _logger.LogWarning(ex, "Failed to track player process {processId}", processId);
        }
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);
    }
}