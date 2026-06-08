using System;
using System.IO;
using System.IO.Compression;
using Abstract;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

public class GameLogger : Singleton<GameLogger>
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }

    // ── Config ────────────────────────────────────────────────────────────────

    private const int RetentionDays = 7; // delete logs older than this

    private bool _initialized;
    private LogLevel _minimumLevel = LogLevel.Trace;

    private StreamWriter _writer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Instance.Setup();
    }

    private void Setup()
    {
        var logDir = Application.persistentDataPath;

        HandleOldLogs(logDir);

        var path = Path.Combine(logDir,
            $"game_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        _writer = new StreamWriter(path, false) { AutoFlush = true };
        _initialized = true;

        Application.logMessageReceived += OnUnityLog;
        Application.quitting += Shutdown;

        _writer.WriteLine($"=== Session started {DateTime.Now} ===");
        _writer.WriteLine($"=== Log path: {path} ===");
        _writer.WriteLine();
    }

    // ── Log rotation / cleanup ───────────────────────────────────────────────

    private void HandleOldLogs(string logDir)
    {
        try
        {
            var files = Directory.GetFiles(logDir, "game_*.log");

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var age = DateTime.Now - info.CreationTime;

                if (age.TotalDays > RetentionDays)
                {
                    File.Delete(file);
                    continue;
                }

                var gzPath = file + ".gz";

                // Skip if already compressed
                if (File.Exists(gzPath))
                    continue;

                using (var originalFileStream = File.OpenRead(file))
                using (var compressedFileStream = File.Create(gzPath))
                using (var compressionStream = new GZipStream(
                           compressedFileStream,
                           CompressionLevel.Optimal))
                {
                    originalFileStream.CopyTo(compressionStream);
                }

                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Logger] Failed to process old logs: {ex.Message}");
        }
    }

    // ── Unity log hook ───────────────────────────────────────────────────────

    private void OnUnityLog(string message, string stackTrace, LogType type)
    {
        if (!_initialized) return;

        var level = type switch
        {
            LogType.Error => "ERROR",
            LogType.Assert => "ASSERT",
            LogType.Warning => "WARN",
            LogType.Exception => "EXCEPTION",
            _ => "INFO"
        };

        _writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");

        if (type is LogType.Error or LogType.Exception or LogType.Assert)
            foreach (var line in stackTrace.Split('\n'))
            {
                var t = line.Trim();
                if (!string.IsNullOrEmpty(t))
                    _writer.WriteLine($"    {t}");
            }
    }

    private void Shutdown()
    {
        Application.logMessageReceived -= OnUnityLog;
        _writer?.WriteLine($"\n=== Session ended {DateTime.Now} ===");
        _writer?.Close();
        _initialized = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LogTrace(string message, string tag = null)
    {
        if (_minimumLevel > LogLevel.Trace) return;
        Debug.Log(Format(message, tag, "TRACE"));
    }

    public void Log(string message, string tag = null)
    {
        if (_minimumLevel > LogLevel.Debug) return;
        Debug.Log(Format(message, tag, "DEBUG"));
    }

    public void LogInfo(string message, string tag = null)
    {
        if (_minimumLevel > LogLevel.Info) return;
        Debug.Log(Format(message, tag, "INFO"));
    }

    public void LogDebug(string message, string tag = null)
    {
        if (_minimumLevel > LogLevel.Debug) return;
        Debug.Log(Format(message, tag, "DEBUG"));
    }

    public void LogWarning(string message, string tag = null)
    {
        if (_minimumLevel > LogLevel.Warning) return;
        Debug.LogWarning(Format(message, tag, "WARN"));
    }

    public void LogError(string message, string tag = null)
    {
        Debug.LogError(Format(message, tag, "ERROR"));
    }

    public void SetMinimumLevel(LogLevel level)
    {
        _minimumLevel = level;
    }

    private static string Format(string message, string tag, string level)
    {
        return tag != null ? $"[{level}] [{tag}] {message}" : $"[{level}] {message}";
    }
}