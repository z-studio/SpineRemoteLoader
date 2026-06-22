using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> ToUniTaskAsyncEnumerable<TSource>(
            this IEnumerable<TSource> source
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new ToUniTaskAsyncEnumerable<TSource>(source);
        }

        public static IUniTaskAsyncEnumerable<TSource> ToUniTaskAsyncEnumerable<TSource>(this Task<TSource> source) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new ToUniTaskAsyncEnumerableTask<TSource>(source);
        }

        public static IUniTaskAsyncEnumerable<TSource> ToUniTaskAsyncEnumerable<TSource>(this UniTask<TSource> source) {
            return new ToUniTaskAsyncEnumerableUniTask<TSource>(source);
        }

        public static IUniTaskAsyncEnumerable<TSource> ToUniTaskAsyncEnumerable<TSource>(
            this IObservable<TSource> source
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new ToUniTaskAsyncEnumerableObservable<TSource>(source);
        }
    }

    internal class ToUniTaskAsyncEnumerable<T> : IUniTaskAsyncEnumerable<T> {
        private readonly IEnumerable<T> m_Source;

        public ToUniTaskAsyncEnumerable(IEnumerable<T> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerToUniTaskAsyncEnumerable(m_Source, cancellationToken);
        }

        private class InnerToUniTaskAsyncEnumerable : IUniTaskAsyncEnumerator<T> {
            private readonly IEnumerable<T> m_Source;
            private CancellationToken m_CancellationToken;

            private IEnumerator<T> m_Enumerator;

            public InnerToUniTaskAsyncEnumerable(IEnumerable<T> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
            }

            public T Current => m_Enumerator.Current;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Enumerator == null) {
                    m_Enumerator = m_Source.GetEnumerator();
                }

                if (m_Enumerator.MoveNext()) {
                    return CompletedTasks.True;
                }

                return CompletedTasks.False;
            }

            public UniTask DisposeAsync() {
                m_Enumerator.Dispose();
                return default;
            }
        }
    }

    internal class ToUniTaskAsyncEnumerableTask<T> : IUniTaskAsyncEnumerable<T> {
        private readonly Task<T> m_Source;

        public ToUniTaskAsyncEnumerableTask(Task<T> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerToUniTaskAsyncEnumerableTask(m_Source, cancellationToken);
        }

        private class InnerToUniTaskAsyncEnumerableTask : IUniTaskAsyncEnumerator<T> {
            private readonly Task<T> m_Source;
            private CancellationToken m_CancellationToken;

            private T m_Current;
            private bool m_Called;

            public InnerToUniTaskAsyncEnumerableTask(Task<T> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
                m_Called = false;
            }

            public T Current => m_Current;

            public async UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Called) {
                    return false;
                }

                m_Called = true;

                m_Current = await m_Source;
                return true;
            }

            public UniTask DisposeAsync() {
                return default;
            }
        }
    }

    internal class ToUniTaskAsyncEnumerableUniTask<T> : IUniTaskAsyncEnumerable<T> {
        private readonly UniTask<T> m_Source;

        public ToUniTaskAsyncEnumerableUniTask(UniTask<T> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerToUniTaskAsyncEnumerableUniTask(m_Source, cancellationToken);
        }

        private class InnerToUniTaskAsyncEnumerableUniTask : IUniTaskAsyncEnumerator<T> {
            private readonly UniTask<T> m_Source;
            private CancellationToken m_CancellationToken;

            private T m_Current;
            private bool m_Called;

            public InnerToUniTaskAsyncEnumerableUniTask(UniTask<T> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
                m_Called = false;
            }

            public T Current => m_Current;

            public async UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Called) {
                    return false;
                }

                m_Called = true;

                m_Current = await m_Source;
                return true;
            }

            public UniTask DisposeAsync() {
                return default;
            }
        }
    }

    internal class ToUniTaskAsyncEnumerableObservable<T> : IUniTaskAsyncEnumerable<T> {
        private readonly IObservable<T> m_Source;

        public ToUniTaskAsyncEnumerableObservable(IObservable<T> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerToUniTaskAsyncEnumerableObservable(m_Source, cancellationToken);
        }

        private class InnerToUniTaskAsyncEnumerableObservable : MoveNextSource, IUniTaskAsyncEnumerator<T>, IObserver<T> {
            private static readonly Action<object> s_OnCanceledDelegate = OnCanceled;

            private readonly IObservable<T> m_Source;
            private CancellationToken m_CancellationToken;

            private bool m_UseCachedCurrent;
            private T m_Current;
            private bool m_SubscribeCompleted;
            private readonly Queue<T> m_QueuedResult;
            private Exception m_Error;
            private IDisposable m_Subscription;
            private CancellationTokenRegistration m_CancellationTokenRegistration;

            public InnerToUniTaskAsyncEnumerableObservable(IObservable<T> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
                m_QueuedResult = new Queue<T>();

                if (cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_OnCanceledDelegate, this);
                }
            }

            public T Current {
                get {
                    if (m_UseCachedCurrent) {
                        return m_Current;
                    }

                    lock (m_QueuedResult) {
                        if (m_QueuedResult.Count != 0) {
                            m_Current = m_QueuedResult.Dequeue();
                            m_UseCachedCurrent = true;
                            return m_Current;
                        } else {
                            return default; // undefined.
                        }
                    }
                }
            }

            public UniTask<bool> MoveNextAsync() {
                lock (m_QueuedResult) {
                    m_UseCachedCurrent = false;

                    if (m_CancellationToken.IsCancellationRequested) {
                        return UniTask.FromCanceled<bool>(m_CancellationToken);
                    }

                    if (m_Subscription == null) {
                        m_Subscription = m_Source.Subscribe(this);
                    }

                    if (m_Error != null) {
                        return UniTask.FromException<bool>(m_Error);
                    }

                    if (m_QueuedResult.Count != 0) {
                        return CompletedTasks.True;
                    }

                    if (m_SubscribeCompleted) {
                        return CompletedTasks.False;
                    }

                    mCompletionSource.Reset();
                    return new UniTask<bool>(this, mCompletionSource.Version);
                }
            }

            public UniTask DisposeAsync() {
                m_Subscription.Dispose();
                m_CancellationTokenRegistration.Dispose();
                mCompletionSource.Reset();
                return default;
            }

            public void OnCompleted() {
                lock (m_QueuedResult) {
                    m_SubscribeCompleted = true;
                    mCompletionSource.TrySetResult(false);
                }
            }

            public void OnError(Exception error) {
                lock (m_QueuedResult) {
                    m_Error = error;
                    mCompletionSource.TrySetException(error);
                }
            }

            public void OnNext(T value) {
                lock (m_QueuedResult) {
                    m_QueuedResult.Enqueue(value);
                    mCompletionSource.TrySetResult(true); // include callback execution, too long lock?
                }
            }

            static void OnCanceled(object state) {
                var self = (InnerToUniTaskAsyncEnumerableObservable)state;

                lock (self.m_QueuedResult) {
                    self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
                }
            }
        }
    }
}