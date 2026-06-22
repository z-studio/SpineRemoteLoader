using Cysharp.Threading.Tasks.Internal;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Reverse<TSource>(this IUniTaskAsyncEnumerable<TSource> source) {
            Error.ThrowArgumentNullException(source, nameof(source));
            return new Reverse<TSource>(source);
        }
    }

    internal sealed class Reverse<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

        public Reverse(IUniTaskAsyncEnumerable<TSource> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerReverse(m_Source, cancellationToken);
        }

        private sealed class InnerReverse : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken;

            private TSource[] m_Array;
            private int m_Index;

            public InnerReverse(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            // after consumed array, don't use await so allow async(not require UniTaskCompletionSourceCore).
            public async UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Array == null) {
                    m_Array = await m_Source.ToArrayAsync(m_CancellationToken);
                    m_Index = m_Array.Length - 1;
                }

                if (m_Index != -1) {
                    Current = m_Array[m_Index];
                    --m_Index;
                    return true;
                } else {
                    return false;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);
                return default;
            }
        }
    }
}