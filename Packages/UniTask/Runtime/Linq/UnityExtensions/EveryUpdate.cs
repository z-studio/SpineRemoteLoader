using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<AsyncUnit> EveryUpdate(
            PlayerLoopTiming updateTiming = PlayerLoopTiming.Update,
            bool cancelImmediately = false
        ) {
            return new EveryUpdate(updateTiming, cancelImmediately);
        }
    }

    internal class EveryUpdate : IUniTaskAsyncEnumerable<AsyncUnit> {
        private readonly PlayerLoopTiming m_UpdateTiming;
        private readonly bool m_CancelImmediately;

        public EveryUpdate(PlayerLoopTiming updateTiming, bool cancelImmediately) {
            m_UpdateTiming = updateTiming;
            m_CancelImmediately = cancelImmediately;
        }

        public IUniTaskAsyncEnumerator<AsyncUnit> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerEveryUpdate(m_UpdateTiming, cancellationToken, m_CancelImmediately);
        }

        private class InnerEveryUpdate : MoveNextSource, IUniTaskAsyncEnumerator<AsyncUnit>, IPlayerLoopItem {
            private readonly PlayerLoopTiming m_InnerUpdateTiming;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;

            private bool m_Disposed;

            public InnerEveryUpdate(
                PlayerLoopTiming updateTiming,
                CancellationToken cancellationToken,
                bool cancelImmediately
            ) {
                m_InnerUpdateTiming = updateTiming;
                m_CancellationToken = cancellationToken;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (InnerEveryUpdate)state;
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
                if (m_Disposed) {
                    return CompletedTasks.False;
                }

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
                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Disposed) {
                    mCompletionSource.TrySetResult(false);
                    return false;
                }

                mCompletionSource.TrySetResult(true);
                return true;
            }
        }
    }
}