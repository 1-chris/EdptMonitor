using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace EdptMonitor.Client;

public class EndpointMonitorClientInstaller
{
        public static void Install(string baseUrl, string authorizationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // install to program files and run at startup
            // doesnt work var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
            var envCommandLineArgs = Environment.GetCommandLineArgs();
            var currentExecutablePath = Path.GetFullPath(envCommandLineArgs[0]);
            var currentExecutableName = Path.GetFileName(currentExecutablePath);
            var currentExecutableDirectory = Path.GetDirectoryName(currentExecutablePath);

            // copy executable to program files
            var newExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), currentExecutableName);
            File.Copy(currentExecutablePath, newExecutablePath, true);

            // create config file
            var config = new EndpointMonitorClientConfiguration
            {
                EndpointUrl = baseUrl,
                AuthorizationToken = authorizationToken
            };
            var configJson = JsonSerializer.Serialize<EndpointMonitorClientConfiguration>(config);
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EdptMonitorClient", "config.json");
            var configDirectory = Path.GetDirectoryName(configPath);

            if (configDirectory is null)
                throw new NotSupportedException("Your flavour of Windows is not supported");
                
            Directory.CreateDirectory(configDirectory);
            File.WriteAllText(configPath, configJson);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"create EdptMonitorClient start= auto binPath= \"{newExecutablePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            // create uninstall registry key
            var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
            var key = uninstallKey?.CreateSubKey("EdptMonitorClient");

            if (key is null)
                throw new NotSupportedException("Your flavour of Windows is not supported");

            key.SetValue("DisplayName", "Device Data Ingestion Client");
            key.SetValue("DisplayVersion", "1.0.0");
            key.SetValue("Publisher", "1-chris");
            key.SetValue("UninstallString", $"\"{newExecutablePath}\" uninstall");
            key.SetValue("InstallLocation", newExecutablePath);
            key.SetValue("DisplayIcon", newExecutablePath);
            key.SetValue("NoModify", 1);
            key.SetValue("NoRepair", 1);
            key.SetValue("NoRemove", 0);
            
            Console.WriteLine("Installation complete.");

            // now run the executable in the background after this process exits
            var startInfo = new ProcessStartInfo(newExecutablePath)
            {
                FileName = "sc",
                Arguments = $"start EdptMonitorClient",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
            return;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var currentExecutablePath = Process.GetCurrentProcess()?.MainModule?.FileName;
            var currentExecutableName = Path.GetFileName(currentExecutablePath);
            var currentExecutableDirectory = Path.GetDirectoryName(currentExecutablePath);

            if (currentExecutableName is null || currentExecutablePath is null)
                throw new NotSupportedException("Your method of deployment is not supported.");

            // create config in /etc
            var config = new EndpointMonitorClientConfiguration()
            {
                EndpointUrl = baseUrl,
                AuthorizationToken = authorizationToken
            };
            var configJson = JsonSerializer.Serialize<EndpointMonitorClientConfiguration>(config);
            var configPath = Path.Combine("/etc", "EdptMonitorClient", "config.json");
            Directory.CreateDirectory(Path.Combine("/etc", "EdptMonitorClient"));
            File.WriteAllText(configPath, configJson);

            // copy executable to /usr/local/bin
            var newExecutablePath = Path.Combine("/usr/local/bin", currentExecutableName);
            File.Copy(currentExecutablePath, newExecutablePath, true);

            if (Directory.Exists("/etc/systemd/system"))
            {
                var serviceFile = $@"
                    [Unit]
                    Description=Device Data Ingestion Client

                    [Service]
                    ExecStart={newExecutablePath}
                    User=root
                    Restart=always
                    RestartSec=10
                    SyslogIdentifier=EdptMonitorClient

                    [Install]
                    WantedBy=multi-user.target";
                
                File.WriteAllText("/etc/systemd/system/EdptMonitorClient.service", serviceFile);
                Process.Start("systemctl", "enable EdptMonitorClient.service");
                Process.Start("systemctl", "start EdptMonitorClient.service");

                var uninstallScript = $@"
                    #!/bin/bash
                    systemctl stop EdptMonitorClient.service
                    systemctl disable EdptMonitorClient.service
                    rm /etc/systemd/system/EdptMonitorClient.service
                    rm {newExecutablePath}
                    systemctl daemon-reload";
                File.WriteAllText("/usr/local/bin/EdptMonitorClient-uninstall.sh", uninstallScript);
                Process.Start("chmod", "+x /usr/local/bin/EdptMonitorClient-uninstall.sh");

                Console.WriteLine("Installation complete.");
                return;
            }
            if (!Directory.Exists("/etc/systemd/system") && File.Exists("/etc/cron.d"))
            {
                // cron
                var cronFile = $@"
                    @reboot root {newExecutablePath}
                    * * * * * root {newExecutablePath}";
                File.WriteAllText("/etc/cron.d/EdptMonitorClient", cronFile);

                // create uninstall script
                var uninstallScript = $@"
                    #!/bin/bash
                    rm /etc/cron.d/EdptMonitorClient
                    rm {newExecutablePath}";
                File.WriteAllText("/usr/local/bin/EdptMonitorClient-uninstall.sh", uninstallScript);
                Process.Start("chmod", "+x /usr/local/bin/EdptMonitorClient-uninstall.sh");

                Console.WriteLine("Installation complete.");
                return;
            }
            throw new NotSupportedException("No supported init system found.");
        }

        throw new NotSupportedException("The current operating system is not supported.");
    }

    public static void Uninstall()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // stop service
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"stop EdptMonitorClient",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // delete executable from program files
            try {
                var executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EdptMonitorClient.exe");
                File.Delete(executablePath);
            } catch (Exception) { 
                System.Console.WriteLine("Could not delete executable");
            }

            // delete uninstall registry key
            try {
                var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
                uninstallKey?.DeleteSubKey("EdptMonitorClient");
            } catch (Exception exc) {
                Console.WriteLine($"Could not delete uninstall registry key: {exc.Message}");
            }

            // delete config file
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EdptMonitorClient", "config.json");
                File.Delete(configPath);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Could not delete config file: {exc.Message}");
            }

            // delete service
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"delete EdptMonitorClient",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();
            Console.WriteLine("Uninstallation complete.");
            return;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var currentExecutablePath = Process.GetCurrentProcess()?.MainModule?.FileName;
            var currentExecutableName = Path.GetFileName(currentExecutablePath);
            var currentExecutableDirectory = Path.GetDirectoryName(currentExecutablePath);

            currentExecutableName = currentExecutableName ?? "EdptMonitorClient";

            // delete executable from /usr/local/bin
            var newExecutablePath = Path.Combine("/usr/local/bin", currentExecutableName);
            File.Delete(newExecutablePath);

            // delete uninstall script
            File.Delete("/usr/local/bin/EdptMonitorClient-uninstall.sh");

            // delete config file
            var configPath = Path.Combine("/etc", "EdptMonitorClient", "config.json");

            // choose cron or systemd depending which is available
            if (Directory.Exists("/etc/systemd/system"))
            {
                // systemd
                Process.Start("systemctl", "stop EdptMonitorClient.service");
                Process.Start("systemctl", "disable EdptMonitorClient.service");
                File.Delete("/etc/systemd/system/EdptMonitorClient.service");
                Process.Start("systemctl", "daemon-reload");
                Console.WriteLine("Uninstallation complete.");
                return;
            }
            if (!Directory.Exists("/etc/systemd/system") && File.Exists("/etc/cron.d"))
            {
                // cron
                File.Delete("/etc/cron.d/EdptMonitorClient");
                Console.WriteLine("Uninstallation complete.");
                return;
            }

            throw new NotSupportedException("No supported init system found.");
        }
        throw new NotSupportedException("The current operating system is not supported.");
        }
}