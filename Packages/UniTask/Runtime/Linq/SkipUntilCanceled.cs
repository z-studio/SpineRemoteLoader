using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> SkipUntilCanceled<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new SkipUntilCanceled<TSource>(source, cancellationToken);
        }
    }

    internal sealed class SkipUntilCanceled<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly CancellationToken m_CancellationToken;

        public SkipUntilCanceled(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
            m_Source = source;
            m_CancellationToken = cancellationToken;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipUntilCanceled(m_Source, m_CancellationToken, cancellationToken);
        }

        private sealed class InnerSkipUntilCanceled : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_CancelDelegate1 = OnCanceled1;
            private static readonly Action<object> s_CancelDelegate2 = OnCanceled2;
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken1;
            private CancellationToken m_CancellationToken2;
            private CancellationTokenRegistration m_CancellationTokenRegistration1;
            private CancellationTokenRegistration m_CancellationTokenRegistration2;

            private int m_IsCanceled;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private bool m_ContinueNext;

            public InnerSkipUntilCanceled(
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
                if (m_Enumerator == null) {
                    if (m_CancellationToken1.IsCancellationRequested) {
                        m_IsCanceled = 1;
                    }

                    if (m_CancellationToken2.IsCancellationRequested) {
                        m_IsCanceled = 1;
                    }

                    m_Enumerator =
                        m_Source.GetAsyncEnumerator(m_CancellationToken2); // use only AsyncEnumerator provided token.
                }

                mCompletionSource.Reset();

                if (m_IsCanceled != 0) {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void SourceMoveNext() {
                try {
                    LOOP:
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP;
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerSkipUntilCanceled)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.Current = self.m_Enumerator.Current;
                        self.mCompletionSource.TrySetResult(true);

                        if (self.m_ContinueNext) {
                            self.SourceMoveNext();
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void OnCanceled1(object state) {
                var self = (InnerSkipUntilCanceled)state;

                if (self.m_IsCanceled == 0) {
                    if (Interlocked.Increment(ref self.m_IsCanceled) == 1) {
                        self.m_CancellationTokenRegistration2.Dispose();
                        self.SourceMoveNext();
                    }
                }
            }

            private static void OnCanceled2(object state) {
                var self = (InnerSkipUntilCanceled)state;

                if (self.m_IsCanceled == 0) {
                    if (Interlocked.Increment(ref self.m_IsCanceled) == 1) {
                        self.m_CancellationTokenRegistration2.Dispose();
                        self.SourceMoveNext();
                    }
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