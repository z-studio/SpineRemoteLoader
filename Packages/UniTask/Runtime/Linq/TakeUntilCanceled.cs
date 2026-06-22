using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> TakeUntilCanceled<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new TakeUntilCanceled<TSource>(source, cancellationToken);
        }
    }

    internal sealed class TakeUntilCanceled<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly CancellationToken m_CancellationToken;

        public TakeUntilCanceled(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
            m_Source = source;
            m_CancellationToken = cancellationToken;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeUntilCanceled(m_Source, m_CancellationToken, cancellationToken);
        }

        private sealed class InnerTakeUntilCanceled : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_CancelDelegate1 = OnCanceled1;
            private static readonly Action<object> s_CancelDelegate2 = OnCanceled2;
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken1;
            private CancellationToken m_CancellationToken2;
            private CancellationTokenRegistration m_CancellationTokenRegistration1;
            private CancellationTokenRegistration m_CancellationTokenRegistration2;

            private bool m_IsCanceled;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerTakeUntilCanceled(
                IUniTaskAsyncEnumerable<TSource> source,
                CancellationToken cancellationToken1,
                CancellationToken cancellationToken2
            ) {
                m_Source = source;
                m_CancellationToken1 = cancellationToken1;
                m_CancellationToken2 = cancellationToken2;

                if (cancellationToken1.CanBeCanceled) {
                    m_CancellationTokenRegistration1 =
                        cancellationToken1.RegisterWithoutCaptureExecutionContext(s_CancelDelegate1, this);
                }

                if (cancellationToken1 != cancellationToken2 && cancellationToken2.CanBeCanceled) {
                    m_CancellationTokenRegistration2 =
                        cancellationToken2.RegisterWithoutCaptureExecutionContext(s_CancelDelegate2, this);
                }

                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_CancellationToken1.IsCancellationRequested) {
                    m_IsCanceled = true;
                }

                if (m_CancellationToken2.IsCancellationRequested) {
                    m_IsCanceled = true;
                }

                if (m_Enumerator == null) {
                    m_Enumerator =
                        m_Source.GetAsyncEnumerator(m_CancellationToken2); // use only AsyncEnumerator provided token.
                }

                if (m_IsCanceled) {
                    return CompletedTasks.False;
                }

                mCompletionSource.Reset();
                SourceMoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void SourceMoveNext() {
                try {
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        MoveNextCore(this);
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerTakeUntilCanceled)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        if (self.m_IsCanceled) {
                            self.mCompletionSource.TrySetResult(false);
                        } else {
                            self.Current = self.m_Enumerator.Current;
                            self.mCompletionSource.TrySetResult(true);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void OnCanceled1(object state) {
                var self = (InnerTakeUntilCanceled)state;

                if (!self.m_IsCanceled) {
                    self.m_CancellationTokenRegistration2.Dispose();
                    self.mCompletionSource.TrySetResult(false);
                }
            }

            private static void OnCanceled2(object state) {
                var self = (InnerTakeUntilCanceled)state;

                if (!self.m_IsCanceled) {
                    self.m_CancellationTokenRegistration1.Dispose();
                    self.mCompletionSource.TrySetResult(false);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);
                m_CancellationTokenRegistration1.Dispose();
                m_CancellationTokenRegistration2.Dispose();

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}