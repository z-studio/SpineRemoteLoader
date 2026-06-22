#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    // CancellationTokenSource itself can not reuse but CancelAfter(Timeout.InfiniteTimeSpan) allows reuse if did not reach timeout.
    // Similar discussion:
    // https://github.com/dotnet/runtime/issues/4694
    // https://github.com/dotnet/runtime/issues/48492
    // This TimeoutController emulate similar implementation, using CancelAfterSlim; to achieve zero allocation timeout.

    public sealed class TimeoutController : IDisposable {
        private static readonly Action<object> s_CancelCancellationTokenSourceStateDelegate =
            new Action<object>(CancelCancellationTokenSourceState);

        private static void CancelCancellationTokenSourceState(object state) {
            var cts = (CancellationTokenSource)state;
            cts.Cancel();
        }

        private CancellationTokenSource m_TimeoutSource;
        private CancellationTokenSource m_LinkedSource;
        private PlayerLoopTimer m_Timer;
        private bool m_IsDisposed;

        private readonly DelayType m_DelayType;
        private readonly PlayerLoopTiming m_DelayTiming;
        private readonly CancellationTokenSource m_OriginalLinkCancellationTokenSource;

        public TimeoutController(
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update
        ) {
            m_TimeoutSource = new CancellationTokenSource();
            m_OriginalLinkCancellationTokenSource = null;
            m_LinkedSource = null;
            m_DelayType = delayType;
            m_DelayTiming = delayTiming;
        }

        public TimeoutController(
            CancellationTokenSource linkCancellationTokenSource,
            DelayType delayType = DelayType.DeltaTime,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update
        ) {
            m_TimeoutSource = new CancellationTokenSource();
            m_OriginalLinkCancellationTokenSource = linkCancellationTokenSource;

            m_LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                m_TimeoutSource.Token,
                linkCancellationTokenSource.Token
            );

            m_DelayType = delayType;
            m_DelayTiming = delayTiming;
        }

        public CancellationToken Timeout(int millisecondsTimeout) {
            return Timeout(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        public CancellationToken Timeout(TimeSpan timeout) {
            if (m_OriginalLinkCancellationTokenSource != null
                && m_OriginalLinkCancellationTokenSource.IsCancellationRequested) {
                return m_OriginalLinkCancellationTokenSource.Token;
            }

            // Timeouted, create new source and timer.
            if (m_TimeoutSource.IsCancellationRequested) {
                m_TimeoutSource.Dispose();
                m_TimeoutSource = new CancellationTokenSource();

                if (m_LinkedSource != null) {
                    m_LinkedSource.Cancel();
                    m_LinkedSource.Dispose();

                    m_LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        m_TimeoutSource.Token,
                        m_OriginalLinkCancellationTokenSource.Token
                    );
                }

                m_Timer?.Dispose();
                m_Timer = null;
            }

            var useSource = (m_LinkedSource != null) ? m_LinkedSource : m_TimeoutSource;
            var token = useSource.Token;

            if (m_Timer == null) {
                // Timer complete => timeoutSource.Cancel() -> linkedSource will be canceled.
                // (linked)token is canceled => stop timer
                m_Timer = PlayerLoopTimer.StartNew(
                    timeout,
                    false,
                    m_DelayType,
                    m_DelayTiming,
                    token,
                    s_CancelCancellationTokenSourceStateDelegate,
                    m_TimeoutSource
                );
            } else {
                m_Timer.Restart(timeout);
            }

            return token;
        }

        public bool IsTimeout() {
            return m_TimeoutSource.IsCancellationRequested;
        }

        public void Reset() {
            m_Timer?.Stop();
        }

        public void Dispose() {
            if (m_IsDisposed)
                return;

            try {
                // stop timer.
                m_Timer?.Dispose();

                // cancel and dispose.
                m_TimeoutSource.Cancel();
                m_TimeoutSource.Dispose();

                if (m_LinkedSource != null) {
                    m_LinkedSource.Cancel();
                    m_LinkedSource.Dispose();
                }
            } finally {
                m_IsDisposed = true;
            }
        }
    }
}