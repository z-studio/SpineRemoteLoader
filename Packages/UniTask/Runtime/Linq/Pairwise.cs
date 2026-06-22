using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<(TSource, TSource)> Pairwise<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new Pairwise<TSource>(source);
        }
    }

    internal sealed class Pairwise<TSource> : IUniTaskAsyncEnumerable<(TSource, TSource)> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

        public Pairwise(IUniTaskAsyncEnumerable<TSource> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<(TSource, TSource)> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerPairwise(m_Source, cancellationToken);
        }

        private sealed class InnerPairwise : MoveNextSource, IUniTaskAsyncEnumerator<(TSource, TSource)> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            private TSource m_Prev;
            private bool m_IsFirst;

            public InnerPairwise(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public (TSource, TSource) Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Enumerator == null) {
                    m_IsFirst = true;
                    m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
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
                var self = (InnerPairwise)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        if (self.m_IsFirst) {
                            self.m_IsFirst = false;
                            self.m_Prev = self.m_Enumerator.Current;
                            self.SourceMoveNext(); // run again. okay to use recursive(only one more).
                        } else {
                            var p = self.m_Prev;
                            self.m_Prev = self.m_Enumerator.Current;
                            self.Current = (p, self.m_Prev);
                            self.mCompletionSource.TrySetResult(true);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}