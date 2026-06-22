#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Threading;
using System;
using Cysharp.Threading.Tasks.Internal;
using UnityEngine;

namespace Cysharp.Threading.Tasks {
    public abstract class PlayerLoopTimer : IDisposable, IPlayerLoopItem {
        private readonly CancellationToken m_CancellationToken;
        private readonly Action<object> m_TimerCallback;
        private readonly object m_State;
        private readonly PlayerLoopTiming m_PlayerLoopTiming;
        private readonly bool m_Periodic;

        private bool m_IsRunning;
        private bool m_TryStop;
        private bool m_IsDisposed;

        protected PlayerLoopTimer(
            bool periodic,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) {
            m_Periodic = periodic;
            m_PlayerLoopTiming = playerLoopTiming;
            m_CancellationToken = cancellationToken;
            m_TimerCallback = timerCallback;
            m_State = state;
        }

        public static PlayerLoopTimer Create(
            TimeSpan interval,
            bool periodic,
            DelayType delayType,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) {
#if UNITY_EDITOR

            // force use Realtime.
            if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying) {
                delayType = DelayType.Realtime;
            }
#endif

            switch (delayType) {
                case DelayType.UnscaledDeltaTime:
                    return new IgnoreTimeScalePlayerLoopTimer(
                        interval,
                        periodic,
                        playerLoopTiming,
                        cancellationToken,
                        timerCallback,
                        state
                    );
                case DelayType.Realtime:
                    return new RealtimePlayerLoopTimer(
                        interval,
                        periodic,
                        playerLoopTiming,
                        cancellationToken,
                        timerCallback,
                        state
                    );
                case DelayType.DeltaTime:
                default:
                    return new DeltaTimePlayerLoopTimer(
                        interval,
                        periodic,
                        playerLoopTiming,
                        cancellationToken,
                        timerCallback,
                        state
                    );
            }
        }

        public static PlayerLoopTimer StartNew(
            TimeSpan interval,
            bool periodic,
            DelayType delayType,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) {
            var timer = Create(
                interval,
                periodic,
                delayType,
                playerLoopTiming,
                cancellationToken,
                timerCallback,
                state
            );

            timer.Restart();
            return timer;
        }

        /// <summary>
        /// Restart(Reset and Start) timer.
        /// </summary>
        public void Restart() {
            if (m_IsDisposed) {
                throw new ObjectDisposedException(null);
            }

            ResetCore(null); // init state

            if (!m_IsRunning) {
                m_IsRunning = true;
                PlayerLoopHelper.AddAction(m_PlayerLoopTiming, this);
            }

            m_TryStop = false;
        }

        /// <summary>
        /// Restart(Reset and Start) and change interval.
        /// </summary>
        public void Restart(TimeSpan interval) {
            if (m_IsDisposed) {
                throw new ObjectDisposedException(null);
            }

            ResetCore(interval); // init state

            if (!m_IsRunning) {
                m_IsRunning = true;
                PlayerLoopHelper.AddAction(m_PlayerLoopTiming, this);
            }

            m_TryStop = false;
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        public void Stop() {
            m_TryStop = true;
        }

        protected abstract void ResetCore(TimeSpan? newInterval);

        public void Dispose() {
            m_IsDisposed = true;
        }

        bool IPlayerLoopItem.MoveNext() {
            if (m_IsDisposed) {
                m_IsRunning = false;
                return false;
            }

            if (m_TryStop) {
                m_IsRunning = false;
                return false;
            }

            if (m_CancellationToken.IsCancellationRequested) {
                m_IsRunning = false;
                return false;
            }

            if (!MoveNextCore()) {
                m_TimerCallback(m_State);

                if (m_Periodic) {
                    ResetCore(null);
                    return true;
                } else {
                    m_IsRunning = false;
                    return false;
                }
            }

            return true;
        }

        protected abstract bool MoveNextCore();
    }

    internal sealed class DeltaTimePlayerLoopTimer : PlayerLoopTimer {
        private int m_InitialFrame;
        private float m_Elapsed;
        private float m_Interval;

        public DeltaTimePlayerLoopTimer(
            TimeSpan interval,
            bool periodic,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) : base(periodic, playerLoopTiming, cancellationToken, timerCallback, state) {
            ResetCore(interval);
        }

        protected override bool MoveNextCore() {
            if (m_Elapsed == 0.0f) {
                if (m_InitialFrame == Time.frameCount) {
                    return true;
                }
            }

            m_Elapsed += Time.deltaTime;

            if (m_Elapsed >= m_Interval) {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval) {
            m_Elapsed = 0.0f;
            m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;

            if (interval != null) {
                m_Interval = (float)interval.Value.TotalSeconds;
            }
        }
    }

    internal sealed class IgnoreTimeScalePlayerLoopTimer : PlayerLoopTimer {
        private int m_InitialFrame;
        private float m_Elapsed;
        private float m_Interval;

        public IgnoreTimeScalePlayerLoopTimer(
            TimeSpan interval,
            bool periodic,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) : base(periodic, playerLoopTiming, cancellationToken, timerCallback, state) {
            ResetCore(interval);
        }

        protected override bool MoveNextCore() {
            if (m_Elapsed == 0.0f) {
                if (m_InitialFrame == Time.frameCount) {
                    return true;
                }
            }

            m_Elapsed += Time.unscaledDeltaTime;

            if (m_Elapsed >= m_Interval) {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval) {
            m_Elapsed = 0.0f;
            m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;

            if (interval != null) {
                m_Interval = (float)interval.Value.TotalSeconds;
            }
        }
    }

    internal sealed class RealtimePlayerLoopTimer : PlayerLoopTimer {
        private ValueStopwatch m_Stopwatch;
        private long m_IntervalTicks;

        public RealtimePlayerLoopTimer(
            TimeSpan interval,
            bool periodic,
            PlayerLoopTiming playerLoopTiming,
            CancellationToken cancellationToken,
            Action<object> timerCallback,
            object state
        ) : base(periodic, playerLoopTiming, cancellationToken, timerCallback, state) {
            ResetCore(interval);
        }

        protected override bool MoveNextCore() {
            if (m_Stopwatch.ElapsedTicks >= m_IntervalTicks) {
                return false;
            }

            return true;
        }

        protected override void ResetCore(TimeSpan? interval) {
            m_Stopwatch = ValueStopwatch.StartNew();

            if (interval != null) {
                m_IntervalTicks = interval.Value.Ticks;
            }
        }
    }
}