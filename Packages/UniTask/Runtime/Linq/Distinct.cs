using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Distinct<TSource>(this IUniTaskAsyncEnumerable<TSource> source) {
            return Distinct(source, EqualityComparer<TSource>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> Distinct<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            IEqualityComparer<TSource> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new Distinct<TSource>(source, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> Distinct<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            return Distinct(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> Distinct<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new Distinct<TSource, TKey>(source, keySelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            return DistinctAwait(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctAwait<TSource, TKey>(source, keySelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            return DistinctAwaitWithCancellation(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctAwaitWithCancellation<TSource, TKey>(source, keySelector, comparer);
        }
    }

    internal sealed class Distinct<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly IEqualityComparer<TSource> m_Comparer;

        public Distinct(IUniTaskAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer) {
            m_Source = source;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinct(m_Source, m_Comparer, cancellationToken);
        }

        private class InnerDistinct : AsyncEnumeratorBase<TSource, TSource> {
            private readonly HashSet<TSource> m_Set;

            public InnerDistinct(
                IUniTaskAsyncEnumerable<TSource> source,
                IEqualityComparer<TSource> comparer,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Set = new HashSet<TSource>(comparer);
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

    internal sealed class Distinct<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, TKey> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public Distinct(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinct(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private class InnerDistinct : AsyncEnumeratorBase<TSource, TSource> {
            private readonly HashSet<TKey> m_Set;
            private readonly Func<TSource, TKey> m_InnerKeySelector;

            public InnerDistinct(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Set = new HashSet<TKey>(comparer);
                m_InnerKeySelector = keySelector;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    var v = SourceCurrent;

                    if (m_Set.Add(m_InnerKeySelector(v))) {
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

    internal sealed class DistinctAwait<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<TKey>> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public DistinctAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctAwait(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private class InnerDistinctAwait : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, TKey> {
            private readonly HashSet<TKey> m_Set;
            private readonly Func<TSource, UniTask<TKey>> m_InnerKeySelector;

            public InnerDistinctAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<TKey>> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Set = new HashSet<TKey>(comparer);
                m_InnerKeySelector = keySelector;
            }

            protected override UniTask<TKey> TransformAsync(TSource sourceCurrent) {
                return m_InnerKeySelector(sourceCurrent);
            }

            protected override bool TrySetCurrentCore(TKey awaitResult, out bool terminateIteration) {
                if (m_Set.Add(awaitResult)) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = false;
                    return false;
                }
            }
        }
    }

    internal sealed class DistinctAwaitWithCancellation<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public DistinctAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctAwaitWithCancellation(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private class InnerDistinctAwaitWithCancellation : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, TKey> {
            private readonly HashSet<TKey> m_Set;
            private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_InnerKeySelector;

            public InnerDistinctAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Set = new HashSet<TKey>(comparer);
                m_InnerKeySelector = keySelector;
            }

            protected override UniTask<TKey> TransformAsync(TSource sourceCurrent) {
                return m_InnerKeySelector(sourceCurrent, cancellationToken);
            }

            protected override bool TrySetCurrentCore(TKey awaitResult, out bool terminateIteration) {
                if (m_Set.Add(awaitResult)) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = false;
                    return false;
                }
            }
        }
    }
}