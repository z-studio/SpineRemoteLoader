using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<T> Merge<T>(
            this IUniTaskAsyncEnumerable<T> first,
            IUniTaskAsyncEnumerable<T> second
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));

            return new Merge<T>(new[] { first, second });
        }

        public static IUniTaskAsyncEnumerable<T> Merge<T>(
            this IUniTaskAsyncEnumerable<T> first,
            IUniTaskAsyncEnumerable<T> second,
            IUniTaskAsyncEnumerable<T> third
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));
            Error.ThrowArgumentNullException(third, nameof(third));

            return new Merge<T>(new[] { first, second, third });
        }

        public static IUniTaskAsyncEnumerable<T> Merge<T>(this IEnumerable<IUniTaskAsyncEnumerable<T>> sources) {
            return sources is IUniTaskAsyncEnumerable<T>[] array
                ? new Merge<T>(array)
                : new Merge<T>(sources.ToArray());
        }

        public static IUniTaskAsyncEnumerable<T> Merge<T>(params IUniTaskAsyncEnumerable<T>[] sources) {
            return new Merge<T>(sources);
        }
    }

    internal sealed class Merge<T> : IUniTaskAsyncEnumerable<T> {
        private readonly IUniTaskAsyncEnumerable<T>[] m_Sources;

        public Merge(IUniTaskAsyncEnumerable<T>[] sources) {
            if (sources.Length <= 0) {
                Error.ThrowArgumentException("No source async enumerable to merge");
            }

            m_Sources = sources;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new InnerMerge(m_Sources, cancellationToken);

        private enum EMergeSourceState {
            Pending,
            Running,
            Completed,
        }

        private sealed class InnerMerge : MoveNextSource, IUniTaskAsyncEnumerator<T> {
            private static readonly Action<object> s_GetResultAtAction = GetResultAt;

            private readonly int m_Length;
            private readonly IUniTaskAsyncEnumerator<T>[] m_Enumerators;
            private readonly EMergeSourceState[] m_States;
            private readonly Queue<(T, Exception, bool)> m_QueuedResult = new();
            private readonly CancellationToken m_CancellationToken;

            private int m_MoveNextCompleted;

            public T Current { get; private set; }

            public InnerMerge(IUniTaskAsyncEnumerable<T>[] sources, CancellationToken cancellationToken) {
                m_CancellationToken = cancellationToken;
                m_Length = sources.Length;
                m_States = ArrayPool<EMergeSourceState>.Shared.Rent(m_Length);
                m_Enumerators = ArrayPool<IUniTaskAsyncEnumerator<T>>.Shared.Rent(m_Length);

                for (var i = 0; i < m_Length; i++) {
                    m_Enumerators[i] = sources[i].GetAsyncEnumerator(cancellationToken);
                    m_States[i] = (int)EMergeSourceState.Pending;
                }
            }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();
                Interlocked.Exchange(ref m_MoveNextCompleted, 0);

                if (HasQueuedResult() && Interlocked.CompareExchange(ref m_MoveNextCompleted, 1, 0) == 0) {
                    (T, Exception, bool) value;

                    lock (m_States) {
                        value = m_QueuedResult.Dequeue();
                    }

                    var resultValue = value.Item1;
                    var exception = value.Item2;
                    var hasNext = value.Item3;

                    if (exception != null) {
                        mCompletionSource.TrySetException(exception);
                    } else {
                        Current = resultValue;
                        mCompletionSource.TrySetResult(hasNext);
                    }

                    return new UniTask<bool>(this, mCompletionSource.Version);
                }

                for (var i = 0; i < m_Length; i++) {
                    lock (m_States) {
                        if (m_States[i] == EMergeSourceState.Pending) {
                            m_States[i] = EMergeSourceState.Running;
                        } else {
                            continue;
                        }
                    }

                    var awaiter = m_Enumerators[i].MoveNextAsync().GetAwaiter();

                    if (awaiter.IsCompleted) {
                        GetResultAt(i, awaiter);
                    } else {
                        awaiter.SourceOnCompleted(s_GetResultAtAction, StateTuple.Create(this, i, awaiter));
                    }
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public async UniTask DisposeAsync() {
                for (var i = 0; i < m_Length; i++) {
                    await m_Enumerators[i].DisposeAsync();
                }

                ArrayPool<EMergeSourceState>.Shared.Return(m_States, true);
                ArrayPool<IUniTaskAsyncEnumerator<T>>.Shared.Return(m_Enumerators, true);
            }

            private static void GetResultAt(object state) {
                using (var tuple = (StateTuple<InnerMerge, int, UniTask<bool>.Awaiter>)state) {
                    tuple.Item1.GetResultAt(tuple.Item2, tuple.Item3);
                }
            }

            private void GetResultAt(int index, UniTask<bool>.Awaiter awaiter) {
                bool hasNext;
                bool completedAll;

                try {
                    hasNext = awaiter.GetResult();
                } catch (Exception ex) {
                    if (Interlocked.CompareExchange(ref m_MoveNextCompleted, 1, 0) == 0) {
                        mCompletionSource.TrySetException(ex);
                    } else {
                        lock (m_States) {
                            m_QueuedResult.Enqueue((default, ex, default));
                        }
                    }

                    return;
                }

                lock (m_States) {
                    m_States[index] = hasNext ? EMergeSourceState.Pending : EMergeSourceState.Completed;
                    completedAll = !hasNext && IsCompletedAll();
                }

                if (hasNext || completedAll) {
                    if (Interlocked.CompareExchange(ref m_MoveNextCompleted, 1, 0) == 0) {
                        Current = m_Enumerators[index].Current;
                        mCompletionSource.TrySetResult(!completedAll);
                    } else {
                        lock (m_States) {
                            m_QueuedResult.Enqueue((m_Enumerators[index].Current, null, !completedAll));
                        }
                    }
                }
            }

            private bool HasQueuedResult() {
                lock (m_States) {
                    return m_QueuedResult.Count > 0;
                }
            }

            private bool IsCompletedAll() {
                lock (m_States) {
                    for (var i = 0; i < m_Length; i++) {
                        if (m_States[i] != EMergeSourceState.Completed) {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}