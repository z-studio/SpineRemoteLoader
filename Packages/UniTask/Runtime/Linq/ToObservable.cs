using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IObservable<TSource> ToObservable<TSource>(this IUniTaskAsyncEnumerable<TSource> source) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new ToObservable<TSource>(source);
        }
    }

    internal sealed class ToObservable<T> : IObservable<T> {
        private readonly IUniTaskAsyncEnumerable<T> m_Source;

        public ToObservable(IUniTaskAsyncEnumerable<T> source) {
            m_Source = source;
        }

        public IDisposable Subscribe(IObserver<T> observer) {
            var ctd = new CancellationTokenDisposable();

            RunAsync(m_Source, observer, ctd.Token).Forget();

            return ctd;
        }

        static async UniTaskVoid RunAsync(
            IUniTaskAsyncEnumerable<T> src,
            IObserver<T> observer,
            CancellationToken cancellationToken
        ) {
            // cancellationToken.IsCancellationRequested is called when Rx's Disposed.
            // when disposed, finish silently.

            var e = src.GetAsyncEnumerator(cancellationToken);

            try {
                bool hasNext;

                do {
                    try {
                        hasNext = await e.MoveNextAsync();
                    } catch (Exception ex) {
                        if (cancellationToken.IsCancellationRequested) {
                            return;
                        }

                        observer.OnError(ex);
                        return;
                    }

                    if (hasNext) {
                        observer.OnNext(e.Current);
                    } else {
                        observer.OnCompleted();
                        return;
                    }
                } while (!cancellationToken.IsCancellationRequested);
            } finally {
                if (e != null) {
                    await e.DisposeAsync();
                }
            }
        }

        internal sealed class CancellationTokenDisposable : IDisposable {
            private readonly CancellationTokenSource m_Cts = new();

            public CancellationToken Token => m_Cts.Token;

            public void Dispose() {
                if (!m_Cts.IsCancellationRequested) {
                    m_Cts.Cancel();
                }
            }
        }
    }
}