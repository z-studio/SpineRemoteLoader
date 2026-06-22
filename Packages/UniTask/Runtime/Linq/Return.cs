using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TValue> Return<TValue>(TValue value) {
            return new Return<TValue>(value);
        }
    }

    internal class Return<TValue> : IUniTaskAsyncEnumerable<TValue> {
        private readonly TValue m_Value;

        public Return(TValue value) {
            m_Value = value;
        }

        public IUniTaskAsyncEnumerator<TValue> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerReturn(m_Value, cancellationToken);
        }

        private class InnerReturn : IUniTaskAsyncEnumerator<TValue> {
            private readonly TValue m_Value;
            private CancellationToken m_CancellationToken;

            private bool m_Called;

            public InnerReturn(TValue value, CancellationToken cancellationToken) {
                m_Value = value;
                m_CancellationToken = cancellationToken;
                m_Called = false;
            }

            public TValue Current => m_Value;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (!m_Called) {
                    m_Called = true;
                    return CompletedTasks.True;
                }

                return CompletedTasks.False;
            }

            public UniTask DisposeAsync() {
                return default;
            }
        }
    }
}