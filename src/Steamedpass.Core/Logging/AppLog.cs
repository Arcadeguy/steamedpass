using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Steamedpass.Core.Logging;

/// <summary>
/// Shared file+console logger with a runtime-adjustable level, matching
/// UWPHook's log level setting (0 = Error, 1 = Debug, 2 = Verbose/Trace).
/// </summary>
public static class AppLog
{
    private static readonly LoggingLevelSwitch LevelSwitch = new(LogEventLevel.Error);
    private static bool _initialized;

    public static void Initialize(int logLevelIndex)
    {
        if (!_initialized)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "steamedpass", "application.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LevelSwitch)
                .WriteTo.File(logPath, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, retainedFileCountLimit: 5)
                .WriteTo.Console()
                .CreateLogger();

            _initialized = true;
        }

        SetLevel(logLevelIndex);
    }

    public static void SetLevel(int logLevelIndex)
    {
        LevelSwitch.MinimumLevel = logLevelIndex switch
        {
            1 => LogEventLevel.Debug,
            2 => LogEventLevel.Verbose,
            _ => LogEventLevel.Error,
        };
    }
}
