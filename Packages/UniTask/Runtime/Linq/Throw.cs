using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TValue> Throw<TValue>(Exception exception) {
            return new Throw<TValue>(exception);
        }
    }

    internal class Throw<TValue> : IUniTaskAsyncEnumerable<TValue> {
        private readonly Exception m_Exception;

        public Throw(Exception exception) {
            m_Exception = exception;
        }

        public IUniTaskAsyncEnumerator<TValue> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerThrow(m_Exception, cancellationToken);
        }

        private class InnerThrow : IUniTaskAsyncEnumerator<TValue> {
            private readonly Exception m_Exception;
            private CancellationToken m_CancellationToken;

            public InnerThrow(Exception exception, CancellationToken cancellationToken) {
                m_Exception = exception;
                m_CancellationToken = cancellationToken;
            }

            public TValue Current => default;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromException<bool>(m_Exception);
            }

            public UniTask DisposeAsync() {
                return default;
            }
        }
    }
}