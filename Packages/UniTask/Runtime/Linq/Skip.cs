using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Skip<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Int32 count
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new Skip<TSource>(source, count);
        }
    }

    internal sealed class Skip<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly int m_Count;

        public Skip(IUniTaskAsyncEnumerable<TSource> source, int count) {
            m_Source = source;
            m_Count = count;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkip(m_Source, m_Count, cancellationToken);
        }

        private sealed class InnerSkip : AsyncEnumeratorBase<TSource, TSource> {
            private readonly int m_Count;

            private int m_Index;

            public InnerSkip(IUniTaskAsyncEnumerable<TSource> source, int count, CancellationToken cancellationToken)
                : base(source, cancellationToken) {
                m_Count = count;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    if (m_Count <= checked(m_Index++)) {
                        Current = SourceCurrent;
                        result = true;
                        return true;
                    } else {
                        result = default;
                        return false;
                    }
                } else {
                    result = false;
                    return true;
                }
            }
        }
    }
}