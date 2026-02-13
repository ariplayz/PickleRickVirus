namespace PickleRick;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options => options.ServiceName = "PickleRick");
        builder.Services.Configure<PlaybackOptions>(builder.Configuration.GetSection("Playback"));
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}