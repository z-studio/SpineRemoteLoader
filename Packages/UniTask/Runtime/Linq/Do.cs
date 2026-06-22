using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Do<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Action<TSource> onNext
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            return source.Do(onNext, null, null);
        }

        public static IUniTaskAsyncEnumerable<TSource> Do<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Action<TSource> onNext,
            Action<Exception> onError
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            return source.Do(onNext, onError, null);
        }

        public static IUniTaskAsyncEnumerable<TSource> Do<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Action<TSource> onNext,
            Action onCompleted
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            return source.Do(onNext, null, onCompleted);
        }

        public static IUniTaskAsyncEnumerable<TSource> Do<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Action<TSource> onNext,
            Action<Exception> onError,
            Action onCompleted
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            return new Do<TSource>(source, onNext, onError, onCompleted);
        }

        public static IUniTaskAsyncEnumerable<TSource> Do<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            IObserver<TSource> observer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(observer, nameof(observer));

            return source.Do(observer.OnNext, observer.OnError, observer.OnCompleted); // alloc delegate.
        }

        // not yet impl.

        //public static IUniTaskAsyncEnumerable<TSource> DoAwait<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask> onNext)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwait<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask> onNext, Func<Exception, UniTask> onError)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwait<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask> onNext, Func<UniTask> onCompleted)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwait<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask> onNext, Func<Exception, UniTask> onError, Func<UniTask> onCompleted)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwaitWithCancellation<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, UniTask> onNext)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwaitWithCancellation<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, UniTask> onNext, Func<Exception, CancellationToken, UniTask> onError)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwaitWithCancellation<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, UniTask> onNext, Func<CancellationToken, UniTask> onCompleted)
        //{
        //    throw new NotImplementedException();
        //}

        //public static IUniTaskAsyncEnumerable<TSource> DoAwaitWithCancellation<TSource>(this IUniTaskAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, UniTask> onNext, Func<Exception, CancellationToken, UniTask> onError, Func<CancellationToken, UniTask> onCompleted)
        //{
        //    throw new NotImplementedException();
        //}
    }

    internal sealed class Do<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Action<TSource> m_OnNext;
        private readonly Action<Exception> m_OnError;
        private readonly Action m_OnCompleted;

        public Do(
            IUniTaskAsyncEnumerable<TSource> source,
            Action<TSource> onNext,
            Action<Exception> onError,
            Action onCompleted
        ) {
            m_Source = source;
            m_OnNext = onNext;
            m_OnError = onError;
            m_OnCompleted = onCompleted;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDo(m_Source, m_OnNext, m_OnError, m_OnCompleted, cancellationToken);
        }

        private sealed class InnerDo : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Action<TSource> m_InnerOnNext;
            private readonly Action<Exception> m_InnerOnError;
            private readonly Action m_InnerOnCompleted;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerDo(
                IUniTaskAsyncEnumerable<TSource> source,
                Action<TSource> onNext,
                Action<Exception> onError,
                Action onCompleted,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerOnNext = onNext;
                m_InnerOnError = onError;
                m_InnerOnCompleted = onCompleted;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                bool isCompleted = false;

                try {
                    if (m_Enumerator == null) {
                        m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                    }

                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();
                    isCompleted = m_Awaiter.IsCompleted;
                } catch (Exception ex) {
                    CallTrySetExceptionAfterNotification(ex);
                    return new UniTask<bool>(this, mCompletionSource.Version);
                }

                if (isCompleted) {
                    MoveNextCore(this);
                } else {
                    m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void CallTrySetExceptionAfterNotification(Exception ex) {
                if (m_InnerOnError != null) {
                    try {
                        m_InnerOnError(ex);
                    } catch (Exception ex2) {
                        mCompletionSource.TrySetException(ex2);
                        return;
                    }
                }

                mCompletionSource.TrySetException(ex);
            }

            private bool TryGetResultWithNotification<T>(UniTask<T>.Awaiter awaiter, out T result) {
                try {
                    result = awaiter.GetResult();
                    return true;
                } catch (Exception ex) {
                    CallTrySetExceptionAfterNotification(ex);
                    result = default;
                    return false;
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerDo)state;

                if (self.TryGetResultWithNotification(self.m_Awaiter, out var result)) {
                    if (result) {
                        var v = self.m_Enumerator.Current;

                        if (self.m_InnerOnNext != null) {
                            try {
                                self.m_InnerOnNext(v);
                            } catch (Exception ex) {
                                self.CallTrySetExceptionAfterNotification(ex);
                            }
                        }

                        self.Current = v;
                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        if (self.m_InnerOnCompleted != null) {
                            try {
                                self.m_InnerOnCompleted();
                            } catch (Exception ex) {
                                self.CallTrySetExceptionAfterNotification(ex);
                                return;
                            }
                        }

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