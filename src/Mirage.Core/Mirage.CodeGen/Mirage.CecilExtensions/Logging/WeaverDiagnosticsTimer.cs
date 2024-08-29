using System;
using System.IO;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mirage.CodeGen.Mirage.CecilExtensions.Logging
{
    /// <summary>
    /// Timer used to see how long different parts of weaver take
    /// Call Start when starting all code. Use <see cref="Sample"/> and using scope to record part of the code.
    /// <para>
    /// WEAVER_DEBUG_TIMER must be added to Compile defines for this class to do anything
    /// </para>
    /// </summary>
    public class WeaverDiagnosticsTimer(string baseName)
    {
        private readonly string _dir = $"./Logs/{baseName}_Logs";

        public bool WriteToFile;
        private StreamWriter _writer;
        private Stopwatch _stopwatch;
        private string _name;

        public long ElapsedMilliseconds => _stopwatch?.ElapsedMilliseconds ?? 0;

        private bool _checkDirectory = false;

        private void CheckDirectory()
        {
            if (_checkDirectory)
                return;
            _checkDirectory = true;

            if (!Directory.Exists(_dir))
                Directory.CreateDirectory(_dir);
        }

        [Conditional("WEAVER_DEBUG_TIMER")]
        public void Start(string name)
        {
            this._name = name;

            if (WriteToFile)
            {
                CheckDirectory();
                var path = $"{_dir}/Timer_{name}.log";
                try
                {
                    _writer = new StreamWriter(path)
                    {
                        AutoFlush = true,
                    };
                }
                catch (Exception e)
                {
                    _writer?.Dispose();
                    WriteToFile = false;
                    WriteLine($"Failed to open {path}: {e}");
                }
            }

            _stopwatch = Stopwatch.StartNew();

            WriteLine($"Weave Started - {name}");
            WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
#if WEAVER_DEBUG_LOGS
            WriteLine($"Debug logs enabled");
#else
            WriteLine($"Debug logs disabled");
#endif 
        }

        [Conditional("WEAVER_DEBUG_TIMER")]
        private void WriteLine(string msg)
        {
            Console.WriteLine($"[WeaverDiagnostics] {msg}");
            if (WriteToFile)
                _writer.WriteLine(msg);
        }

        public long End()
        {
            WriteLine($"Weave Finished: {ElapsedMilliseconds}ms - {_name}");
            WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
            _stopwatch?.Stop();
            _writer?.Close();
            _writer = null;
            return ElapsedMilliseconds;
        }

        public SampleScope Sample(string label)
        {
            return new SampleScope(this, label);
        }

        public readonly struct SampleScope(WeaverDiagnosticsTimer timer, string label) : IDisposable
        {
            private readonly WeaverDiagnosticsTimer _timer = timer;
            private readonly long _start = timer.ElapsedMilliseconds;
            private readonly string _label = label;

            public void Dispose()
            {
                _timer.WriteLine($"{_label}: {_timer.ElapsedMilliseconds - _start}ms - {_timer._name}");
            }
        }
    }
}
