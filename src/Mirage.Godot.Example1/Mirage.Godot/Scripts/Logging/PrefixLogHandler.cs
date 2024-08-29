using System;
using System.Runtime.CompilerServices;
using Mirage.Logging;

namespace Mirage.Godot.Scripts.Logging;

/// <summary>
/// Log handler that adds prefixes to logging
/// </summary>
public class PrefixLogHandler : ILogHandler
{
    private readonly Settings _settings;
    private readonly ILogHandler _inner;
    private readonly string _label;

    public PrefixLogHandler(Settings settings, string fullTypeName = null, ILogHandler inner = null)
    {
        _inner = inner ?? Debug.unityLogger;
        _settings = settings;

        if (_settings.Label && !string.IsNullOrEmpty(fullTypeName))
        {
            var (name, _) = LogSettingsSO.LoggerSettings.GetNameAndNameSpaceFromFullname(fullTypeName);
            _label = $"[{name}]";
        }
        else
        {
            _label = null;
        }
    }

    public void LogException(Exception exception)
    {
        _inner.LogException(exception);
    }

    public void LogFormat(LogType logType, string format, params object[] args)
    {
        // add label before frame nummber
        if (!string.IsNullOrEmpty(_label))
            format = $"{_label} {format}";

        format = AddTimePrefix(format);

        _inner.LogFormat(logType, format, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string AddTimePrefix(string format)
    {
        string timePrefix;
        switch (_settings.TimePrefix)
        {
            default:
                return format;
            case TimePrefix.DateTimeMilliSeconds:
                timePrefix = DateTime.Now.ToString("HH:mm:ss.fff");
                break;
            case TimePrefix.DateTimeSeconds:
                timePrefix = DateTime.Now.ToString("HH:mm:ss");
                break;
        }

        return $"{timePrefix}: {format}";
    }

    public enum TimePrefix
    {
        None,
        DateTimeMilliSeconds,
        DateTimeSeconds,
    }
    [Serializable]
    public class Settings(PrefixLogHandler.TimePrefix timePrefix, bool label)
    {
        public TimePrefix TimePrefix = timePrefix;
        public readonly bool Label = label;
    }
}
