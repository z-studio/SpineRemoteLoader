using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Except<TSource>(
            this IUniTaskAsyncEnumerable<TSource> first,
            IUniTaskAsyncEnumerable<TSource> second
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));

            return new Except<TSource>(first, second, EqualityComparer<TSource>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> Except<TSource>(
            this IUniTaskAsyncEnumerable<TSource> first,
            IUniTaskAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource> comparer
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new Except<TSource>(first, second, comparer);
        }
    }

    internal sealed class Except<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_First;
        private readonly IUniTaskAsyncEnumerable<TSource> m_Second;
        private readonly IEqualityComparer<TSource> m_Comparer;

        public Except(
            IUniTaskAsyncEnumerable<TSource> first,
            IUniTaskAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource> comparer
        ) {
            m_First = first;
            m_Second = second;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerExcept(m_First, m_Second, m_Comparer, cancellationToken);
        }

        private class InnerExcept : AsyncEnumeratorBase<TSource, TSource> {
            private static Action<object> s_HashSetAsyncCoreDelegate = HashSetAsyncCore;

            private readonly IEqualityComparer<TSource> m_InnerComparer;
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSecond;

            private HashSet<TSource> m_Set;
            private UniTask<HashSet<TSource>>.Awaiter m_Awaiter;

            public InnerExcept(
                IUniTaskAsyncEnumerable<TSource> first,
                IUniTaskAsyncEnumerable<TSource> second,
                IEqualityComparer<TSource> comparer,
                CancellationToken cancellationToken
            ) : base(first, cancellationToken) {
                m_InnerSecond = second;
                m_InnerComparer = comparer;
            }

            protected override bool OnFirstIteration() {
                if (m_Set != null) {
                    return false;
                }

                m_Awaiter = m_InnerSecond.ToHashSetAsync(mCancellationToken).GetAwaiter();

                if (m_Awaiter.IsCompleted) {
                    m_Set = m_Awaiter.GetResult();
                    SourceMoveNext();
                } else {
                    m_Awaiter.SourceOnCompleted(s_HashSetAsyncCoreDelegate, this);
                }

                return true;
            }

            static void HashSetAsyncCore(object state) {
                var self = (InnerExcept)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    self.m_Set = result;
                    self.SourceMoveNext();
                }
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    var v = SourceCurrent;

                    if (m_Set.Add(v)) {
                        Current = v;
                        result = true;
                        return true;
                    } else {
                        result = default;
                        return false;
                    }
                }

                result = false;
                return true;
            }
        }
    }
}