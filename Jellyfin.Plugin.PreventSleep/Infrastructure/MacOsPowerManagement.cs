using System;
using System.Diagnostics;
using Jellyfin.Plugin.PreventSleep.Interface;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PreventSleep.Infrastructure;

public sealed class MacOsPowerManagement : IPowerManagement, IDisposable
{
    private readonly ILogger<MacOsPowerManagement> _logger;
    private Process? _caffeinateProcess;

    public MacOsPowerManagement(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MacOsPowerManagement>();
    }

    /// <inheritdoc/>
    public void BlockSleep()
    {
        if (_caffeinateProcess is not null)
        {
            return;
        }

        try
        {
            _caffeinateProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/caffeinate",
                // -d: prevent the display from sleeping
                // -i: prevent the system from idle sleeping
                Arguments = "-d -i",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            });

            _logger.LogInformation("Started caffeinate process (PID {Pid}) to prevent sleep", _caffeinateProcess?.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start caffeinate process: {Error}", e.Message);
            _caffeinateProcess = null;
        }
    }

    /// <inheritdoc/>
    public void UnblockSleep()
    {
        if (_caffeinateProcess is null)
        {
            return;
        }

        try
        {
            if (!_caffeinateProcess.HasExited)
            {
                _caffeinateProcess.Kill();
                _caffeinateProcess.WaitForExit(5000);
                _logger.LogInformation("Stopped caffeinate process to allow sleep");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop caffeinate process: {Error}", e.Message);
        }
        finally
        {
            _caffeinateProcess.Dispose();
            _caffeinateProcess = null;
        }
    }

    public void Dispose()
    {
        UnblockSleep();
    }
}
