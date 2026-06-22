using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> TakeLast<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Int32 count
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            // non take.
            if (count <= 0) {
                return Empty<TSource>();
            }

            return new TakeLast<TSource>(source, count);
        }
    }

    internal sealed class TakeLast<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly int m_Count;

        public TakeLast(IUniTaskAsyncEnumerable<TSource> source, int count) {
            m_Source = source;
            m_Count = count;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeLast(m_Source, m_Count, cancellationToken);
        }

        private sealed class InnerTakeLast : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly int m_Count;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Queue<TSource> m_Queue;

            private bool m_IterateCompleted;
            private bool m_ContinueNext;

            public InnerTakeLast(IUniTaskAsyncEnumerable<TSource> source, int count, CancellationToken cancellationToken) {
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
                    m_Queue = new Queue<TSource>();
                }

                mCompletionSource.Reset();
                SourceMoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void SourceMoveNext() {
                if (m_IterateCompleted) {
                    if (m_Queue.Count > 0) {
                        Current = m_Queue.Dequeue();
                        mCompletionSource.TrySetResult(true);
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }

                    return;
                }

                try {
                    LOOP:
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerTakeLast)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        if (self.m_Queue.Count < self.m_Count) {
                            self.m_Queue.Enqueue(self.m_Enumerator.Current);

                            if (!self.m_ContinueNext) {
                                self.SourceMoveNext();
                            }
                        } else {
                            self.m_Queue.Dequeue();
                            self.m_Queue.Enqueue(self.m_Enumerator.Current);

                            if (!self.m_ContinueNext) {
                                self.SourceMoveNext();
                            }
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.m_IterateCompleted = true;
                        self.SourceMoveNext();
                    }
                } else {
                    self.m_ContinueNext = false;
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