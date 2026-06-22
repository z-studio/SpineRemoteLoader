using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Concat<TSource>(
            this IUniTaskAsyncEnumerable<TSource> first,
            IUniTaskAsyncEnumerable<TSource> second
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));

            return new Concat<TSource>(first, second);
        }
    }

    internal sealed class Concat<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_First;
        private readonly IUniTaskAsyncEnumerable<TSource> m_Second;

        public Concat(IUniTaskAsyncEnumerable<TSource> first, IUniTaskAsyncEnumerable<TSource> second) {
            m_First = first;
            m_Second = second;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerConcat(m_First, m_Second, cancellationToken);
        }

        private sealed class InnerConcat : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private enum EIteratingState {
                IteratingFirst,
                IteratingSecond,
                Complete
            }

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerFirst;
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSecond;
            private CancellationToken m_CancellationToken;

            private EIteratingState m_IteratingState;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerConcat(
                IUniTaskAsyncEnumerable<TSource> first,
                IUniTaskAsyncEnumerable<TSource> second,
                CancellationToken cancellationToken
            ) {
                m_InnerFirst = first;
                m_InnerSecond = second;
                m_CancellationToken = cancellationToken;
                m_IteratingState = EIteratingState.IteratingFirst;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_IteratingState == EIteratingState.Complete) {
                    return CompletedTasks.False;
                }

                mCompletionSource.Reset();
                StartIterate();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void StartIterate() {
                if (m_Enumerator == null) {
                    if (m_IteratingState == EIteratingState.IteratingFirst) {
                        m_Enumerator = m_InnerFirst.GetAsyncEnumerator(m_CancellationToken);
                    } else if (m_IteratingState == EIteratingState.IteratingSecond) {
                        m_Enumerator = m_InnerSecond.GetAsyncEnumerator(m_CancellationToken);
                    }
                }

                try {
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_Awaiter.IsCompleted) {
                    s_MoveNextCoreDelegate(this);
                } else {
                    m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerConcat)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.Current = self.m_Enumerator.Current;
                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        if (self.m_IteratingState == EIteratingState.IteratingFirst) {
                            self.RunSecondAfterDisposeAsync().Forget();
                            return;
                        }

                        self.m_IteratingState = EIteratingState.Complete;
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private async UniTaskVoid RunSecondAfterDisposeAsync() {
                try {
                    await m_Enumerator.DisposeAsync();
                    m_Enumerator = null;
                    m_Awaiter = default;
                    m_IteratingState = EIteratingState.IteratingSecond;
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }

                StartIterate();
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