using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<T> Never<T>() {
            return Cysharp.Threading.Tasks.Linq.Never<T>.Instance;
        }
    }

    internal class Never<T> : IUniTaskAsyncEnumerable<T> {
        public static readonly IUniTaskAsyncEnumerable<T> Instance = new Never<T>();

        private Never() { }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerNever(cancellationToken);
        }

        private class InnerNever : IUniTaskAsyncEnumerator<T> {
            private CancellationToken m_CancellationToken;

            public InnerNever(CancellationToken cancellationToken) {
                m_CancellationToken = cancellationToken;
            }

            public T Current => default;

            public UniTask<bool> MoveNextAsync() {
                var tcs = new UniTaskCompletionSource<bool>();

                m_CancellationToken.Register(
                    state => {
                        var task = (UniTaskCompletionSource<bool>)state;
                        task.TrySetCanceled(m_CancellationToken);
                    },
                    tcs
                );

                return tcs.Task;
            }

            public UniTask DisposeAsync() {
                return default;
            }
        }
    }
}