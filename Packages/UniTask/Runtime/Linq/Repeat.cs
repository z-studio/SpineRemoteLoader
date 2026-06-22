using Cysharp.Threading.Tasks.Internal;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TElement> Repeat<TElement>(TElement element, int count) {
            if (count < 0) {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            return new Repeat<TElement>(element, count);
        }
    }

    internal class Repeat<TElement> : IUniTaskAsyncEnumerable<TElement> {
        private readonly TElement m_Element;
        private readonly int m_Count;

        public Repeat(TElement element, int count) {
            m_Element = element;
            m_Count = count;
        }

        public IUniTaskAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerRepeat(m_Element, m_Count, cancellationToken);
        }

        private class InnerRepeat : IUniTaskAsyncEnumerator<TElement> {
            private readonly TElement m_Element;
            private readonly int m_Count;
            private int m_Remaining;
            private CancellationToken m_CancellationToken;

            public InnerRepeat(TElement element, int count, CancellationToken cancellationToken) {
                m_Element = element;
                m_Count = count;
                m_CancellationToken = cancellationToken;
                m_Remaining = count;
            }

            public TElement Current => m_Element;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Remaining-- != 0) {
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