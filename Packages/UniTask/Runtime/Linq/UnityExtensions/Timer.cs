using System;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<AsyncUnit> Timer(
            TimeSpan dueTime,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false,
            bool cancelImmediately = false
        ) {
            return new Timer(dueTime, null, updateTiming, ignoreTimeScale, cancelImmediately);
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> Timer(
            TimeSpan dueTime,
            TimeSpan period,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false,
            bool cancelImmediately = false
        ) {
            return new Timer(dueTime, period, updateTiming, ignoreTimeScale, cancelImmediately);
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> Interval(
            TimeSpan period,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false,
            bool cancelImmediately = false
        ) {
            return new Timer(period, period, updateTiming, ignoreTimeScale, cancelImmediately);
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> TimerFrame(
            int dueTimeFrameCount,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool cancelImmediately = false
        ) {
            if (dueTimeFrameCount < 0) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus delayFrameCount. dueTimeFrameCount:" + dueTimeFrameCount
                );
            }

            return new TimerFrame(dueTimeFrameCount, null, updateTiming, cancelImmediately);
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> TimerFrame(
            int dueTimeFrameCount,
            int periodFrameCount,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool cancelImmediately = false
        ) {
            if (dueTimeFrameCount < 0) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus delayFrameCount. dueTimeFrameCount:" + dueTimeFrameCount
                );
            }

            if (periodFrameCount < 0) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus periodFrameCount. periodFrameCount:" + dueTimeFrameCount
                );
            }

            return new TimerFrame(dueTimeFrameCount, periodFrameCount, updateTiming, cancelImmediately);
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> IntervalFrame(
            int intervalFrameCount,
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool cancelImmediately = false
        ) {
            if (intervalFrameCount < 0) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus intervalFrameCount. intervalFrameCount:" + intervalFrameCount
                );
            }

            return new TimerFrame(intervalFrameCount, intervalFrameCount, updateTiming, cancelImmediately);
        }
    }

    internal class Timer : IUniTaskAsyncEnumerable<AsyncUnit> {
        private readonly PlayerLoopTiming m_UpdateTiming;
        private readonly TimeSpan m_DueTime;
        private readonly TimeSpan? m_Period;
        private readonly bool m_IgnoreTimeScale;
        private readonly bool m_CancelImmediately;

        public Timer(
            TimeSpan dueTime,
            TimeSpan? period,
            PlayerLoopTiming updateTiming,
            bool ignoreTimeScale,
            bool cancelImmediately
        ) {
            m_UpdateTiming = updateTiming;
            m_DueTime = dueTime;
            m_Period = period;
            m_IgnoreTimeScale = ignoreTimeScale;
            m_CancelImmediately = cancelImmediately;
        }

        public IUniTaskAsyncEnumerator<AsyncUnit> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTimer(m_DueTime, m_Period, m_UpdateTiming, m_IgnoreTimeScale, cancellationToken, m_CancelImmediately);
        }

        private class InnerTimer : MoveNextSource, IUniTaskAsyncEnumerator<AsyncUnit>, IPlayerLoopItem {
            private readonly float m_InnerDueTime;
            private readonly float? m_InnerPeriod;
            private readonly PlayerLoopTiming m_InnerUpdateTiming;
            private readonly bool m_InnerIgnoreTimeScale;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;

            private int m_InitialFrame;
            private float m_Elapsed;
            private bool m_DueTimePhase;
            private bool m_Completed;
            private bool m_Disposed;

            public InnerTimer(
                TimeSpan dueTime,
                TimeSpan? period,
                PlayerLoopTiming updateTiming,
                bool ignoreTimeScale,
                CancellationToken cancellationToken,
                bool cancelImmediately
            ) {
                m_InnerDueTime = (float)dueTime.TotalSeconds;
                m_InnerPeriod = (period == null) ? null : (float?)period.Value.TotalSeconds;

                if (m_InnerDueTime <= 0) {
                    m_InnerDueTime = 0;
                }

                if (m_InnerPeriod != null) {
                    if (m_InnerPeriod <= 0) {
                        m_InnerPeriod = 1;
                    }
                }

                m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                m_DueTimePhase = true;
                m_InnerUpdateTiming = updateTiming;
                m_InnerIgnoreTimeScale = ignoreTimeScale;
                m_CancellationToken = cancellationToken;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (InnerTimer)state;
                            source.mCompletionSource.TrySetCanceled(source.m_CancellationToken);
                        },
                        this
                    );
                }

                TaskTracker.TrackActiveTask(this, 2);
                PlayerLoopHelper.AddAction(updateTiming, this);
            }

            public AsyncUnit Current => default;

            public UniTask<bool> MoveNextAsync() {
                // return false instead of throw
                if (m_Disposed || m_Completed) {
                    return CompletedTasks.False;
                }

                // reset value here.
                m_Elapsed = 0;

                mCompletionSource.Reset();

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_Disposed) {
                    m_CancellationTokenRegistration.Dispose();
                    m_Disposed = true;
                    TaskTracker.RemoveTracking(this);
                }

                return default;
            }

            public bool MoveNext() {
                if (m_Disposed) {
                    mCompletionSource.TrySetResult(false);
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_DueTimePhase) {
                    if (m_Elapsed == 0) {
                        // skip in initial frame.
                        if (m_InitialFrame == Time.frameCount) {
                            return true;
                        }
                    }

                    m_Elapsed += (m_InnerIgnoreTimeScale) ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;

                    if (m_Elapsed >= m_InnerDueTime) {
                        m_DueTimePhase = false;
                        mCompletionSource.TrySetResult(true);
                    }
                } else {
                    if (m_InnerPeriod == null) {
                        m_Completed = true;
                        mCompletionSource.TrySetResult(false);
                        return false;
                    }

                    m_Elapsed += (m_InnerIgnoreTimeScale) ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;

                    if (m_Elapsed >= m_InnerPeriod) {
                        mCompletionSource.TrySetResult(true);
                    }
                }

                return true;
            }
        }
    }

    internal class TimerFrame : IUniTaskAsyncEnumerable<AsyncUnit> {
        private readonly PlayerLoopTiming m_UpdateTiming;
        private readonly int m_DueTimeFrameCount;
        private readonly int? m_PeriodFrameCount;
        private readonly bool m_CancelImmediately;

        public TimerFrame(
            int dueTimeFrameCount,
            int? periodFrameCount,
            PlayerLoopTiming updateTiming,
            bool cancelImmediately
        ) {
            m_UpdateTiming = updateTiming;
            m_DueTimeFrameCount = dueTimeFrameCount;
            m_PeriodFrameCount = periodFrameCount;
            m_CancelImmediately = cancelImmediately;
        }

        public IUniTaskAsyncEnumerator<AsyncUnit> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTimerFrame(
                m_DueTimeFrameCount,
                m_PeriodFrameCount,
                m_UpdateTiming,
                cancellationToken,
                m_CancelImmediately
            );
        }

        private class InnerTimerFrame : MoveNextSource, IUniTaskAsyncEnumerator<AsyncUnit>, IPlayerLoopItem {
            private readonly int m_InnerDueTimeFrameCount;
            private readonly int? m_InnerPeriodFrameCount;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;

            private int m_InitialFrame;
            private int m_CurrentFrame;
            private bool m_DueTimePhase;
            private bool m_Completed;
            private bool m_Disposed;

            public InnerTimerFrame(
                int dueTimeFrameCount,
                int? periodFrameCount,
                PlayerLoopTiming updateTiming,
                CancellationToken cancellationToken,
                bool cancelImmediately
            ) {
                if (dueTimeFrameCount <= 0) {
                    dueTimeFrameCount = 0;
                }

                if (periodFrameCount != null) {
                    if (periodFrameCount <= 0) {
                        periodFrameCount = 1;
                    }
                }

                m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                m_DueTimePhase = true;
                m_InnerDueTimeFrameCount = dueTimeFrameCount;
                m_InnerPeriodFrameCount = periodFrameCount;
                m_CancellationToken = cancellationToken;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (InnerTimerFrame)state;
                            source.mCompletionSource.TrySetCanceled(source.m_CancellationToken);
                        },
                        this
                    );
                }

                TaskTracker.TrackActiveTask(this, 2);
                PlayerLoopHelper.AddAction(updateTiming, this);
            }

            public AsyncUnit Current => default;

            public UniTask<bool> MoveNextAsync() {
                if (m_Disposed || m_Completed) {
                    return CompletedTasks.False;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                }

                // reset value here.
                m_CurrentFrame = 0;
                mCompletionSource.Reset();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_Disposed) {
                    m_CancellationTokenRegistration.Dispose();
                    m_Disposed = true;
                    TaskTracker.RemoveTracking(this);
                }

                return default;
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Disposed) {
                    mCompletionSource.TrySetResult(false);
                    return false;
                }

                if (m_DueTimePhase) {
                    if (m_CurrentFrame == 0) {
                        if (m_InnerDueTimeFrameCount == 0) {
                            m_DueTimePhase = false;
                            mCompletionSource.TrySetResult(true);
                            return true;
                        }

                        // skip in initial frame.
                        if (m_InitialFrame == Time.frameCount) {
                            return true;
                        }
                    }

                    if (++m_CurrentFrame >= m_InnerDueTimeFrameCount) {
                        m_DueTimePhase = false;
                        mCompletionSource.TrySetResult(true);
                    } else { }
                } else {
                    if (m_InnerPeriodFrameCount == null) {
                        m_Completed = true;
                        mCompletionSource.TrySetResult(false);
                        return false;
                    }

                    if (++m_CurrentFrame >= m_InnerPeriodFrameCount) {
                        mCompletionSource.TrySetResult(true);
                    }
                }

                return true;
            }
        }
    }
}