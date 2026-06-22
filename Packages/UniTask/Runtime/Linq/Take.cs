using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Take<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Int32 count
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new Take<TSource>(source, count);
        }
    }

    internal sealed class Take<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly int m_Count;

        public Take(IUniTaskAsyncEnumerable<TSource> source, int count) {
            m_Source = source;
            m_Count = count;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTake(m_Source, m_Count, cancellationToken);
        }

        private sealed class InnerTake : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly int m_Count;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private int m_Index;

            public InnerTake(IUniTaskAsyncEnumerable<TSource> source, int count, CancellationToken cancellationToken) {
                m_Source = source;
                m_Count = count;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Enumerator == null) {
                    m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                }

                if (checked(m_Index) >= m_Count) {
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
                var self = (InnerTake)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_Index++;
                        self.Current = self.m_Enumerator.Current;
                        self.mCompletionSource.TrySetResult(true);
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