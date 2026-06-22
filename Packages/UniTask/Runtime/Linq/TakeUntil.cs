using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> TakeUntil<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            UniTask other
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new TakeUntil<TSource>(source, other, null);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeUntil<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<CancellationToken, UniTask> other
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(source, nameof(other));

            return new TakeUntil<TSource>(source, default, other);
        }
    }

    internal sealed class TakeUntil<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly UniTask m_Other;
        private readonly Func<CancellationToken, UniTask> m_Other2;

        public TakeUntil(
            IUniTaskAsyncEnumerable<TSource> source,
            UniTask other,
            Func<CancellationToken, UniTask> other2
        ) {
            m_Source = source;
            m_Other = other;
            m_Other2 = other2;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            if (m_Other2 != null) {
                return new InnerTakeUntil(m_Source, this.m_Other2(cancellationToken), cancellationToken);
            } else {
                return new InnerTakeUntil(m_Source, this.m_Other, cancellationToken);
            }
        }

        private sealed class InnerTakeUntil : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_CancelDelegate1 = OnCanceled1;
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken1;
            private CancellationTokenRegistration m_CancellationTokenRegistration1;

            private bool m_Completed;
            private Exception m_Exception;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerTakeUntil(
                IUniTaskAsyncEnumerable<TSource> source,
                UniTask other,
                CancellationToken cancellationToken1
            ) {
                m_Source = source;
                m_CancellationToken1 = cancellationToken1;

                if (cancellationToken1.CanBeCanceled) {
                    m_CancellationTokenRegistration1 =
                        cancellationToken1.RegisterWithoutCaptureExecutionContext(s_CancelDelegate1, this);
                }

                TaskTracker.TrackActiveTask(this, 3);

                RunOther(other).Forget();
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_Completed) {
                    return CompletedTasks.False;
                }

                if (m_Exception != null) {
                    return UniTask.FromException<bool>(m_Exception);
                }

                if (m_CancellationToken1.IsCancellationRequested) {
                    return UniTask.FromCanceled<bool>(m_CancellationToken1);
                }

                if (m_Enumerator == null) {
                    m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken1);
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
                var self = (InnerTakeUntil)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        if (self.m_Exception != null) {
                            self.mCompletionSource.TrySetException(self.m_Exception);
                        } else if (self.m_CancellationToken1.IsCancellationRequested) {
                            self.mCompletionSource.TrySetCanceled(self.m_CancellationToken1);
                        } else {
                            self.Current = self.m_Enumerator.Current;
                            self.mCompletionSource.TrySetResult(true);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private async UniTaskVoid RunOther(UniTask other) {
                try {
                    await other;
                    m_Completed = true;
                    mCompletionSource.TrySetResult(false);
                } catch (Exception ex) {
                    m_Exception = ex;
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void OnCanceled1(object state) {
                var self = (InnerTakeUntil)state;
                self.mCompletionSource.TrySetCanceled(self.m_CancellationToken1);
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);
                m_CancellationTokenRegistration1.Dispose();

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}