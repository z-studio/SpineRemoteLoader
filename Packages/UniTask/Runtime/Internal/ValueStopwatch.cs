using System;
using System.Diagnostics;

namespace Cysharp.Threading.Tasks.Internal {
    internal readonly struct ValueStopwatch {
        private static readonly double s_TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private readonly long m_StartTimestamp;

        public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

        private ValueStopwatch(long startTimestamp) {
            m_StartTimestamp = startTimestamp;
        }

        public TimeSpan Elapsed => TimeSpan.FromTicks(this.ElapsedTicks);

        public bool IsInvalid => m_StartTimestamp == 0;

        public long ElapsedTicks {
            get {
                if (m_StartTimestamp == 0) {
                    throw new InvalidOperationException(
                        "Detected invalid initialization(use 'default'), only to create from StartNew()."
                    );
                }

                var delta = Stopwatch.GetTimestamp() - m_StartTimestamp;
                return (long)(delta * s_TimestampToTicks);
            }
        }
    }
}