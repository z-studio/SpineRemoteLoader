using Cysharp.Threading.Tasks.Internal;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<int> Range(int start, int count) {
            if (count < 0) {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            var end = (long)start + count - 1L;

            if (end > int.MaxValue) {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            if (count == 0) {
                UniTaskAsyncEnumerable.Empty<int>();
            }

            return new Cysharp.Threading.Tasks.Linq.Range(start, count);
        }
    }

    internal class Range : IUniTaskAsyncEnumerable<int> {
        private readonly int m_Start;
        private readonly int m_End;

        public Range(int start, int count) {
            m_Start = start;
            m_End = start + count;
        }

        public IUniTaskAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerRange(m_Start, m_End, cancellationToken);
        }

        private class InnerRange : IUniTaskAsyncEnumerator<int> {
            private readonly int m_Start;
            private readonly int m_End;
            private int m_Current;
            private CancellationToken m_CancellationToken;

            public InnerRange(int start, int end, CancellationToken cancellationToken) {
                m_Start = start;
                m_End = end;
                m_CancellationToken = cancellationToken;
                m_Current = start - 1;
            }

            public int Current => m_Current;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                m_Current++;

                if (m_Current != m_End) {
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