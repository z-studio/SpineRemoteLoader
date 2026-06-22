using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        #region OrderBy_OrderByDescending

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerable<TSource, TKey>(source, keySelector, Comparer<TKey>.Default, false, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerable<TSource, TKey>(source, keySelector, comparer, false, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerableAwait<TSource, TKey>(
                source,
                keySelector,
                Comparer<TKey>.Default,
                false,
                null
            );
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerableAwait<TSource, TKey>(source, keySelector, comparer, false, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerableAwaitWithCancellation<TSource, TKey>(
                source,
                keySelector,
                Comparer<TKey>.Default,
                false,
                null
            );
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerableAwaitWithCancellation<TSource, TKey>(
                source,
                keySelector,
                comparer,
                false,
                null
            );
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerable<TSource, TKey>(source, keySelector, Comparer<TKey>.Default, true, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerable<TSource, TKey>(source, keySelector, comparer, true, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescendingAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerableAwait<TSource, TKey>(
                source,
                keySelector,
                Comparer<TKey>.Default,
                true,
                null
            );
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescendingAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerableAwait<TSource, TKey>(source, keySelector, comparer, true, null);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescendingAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new OrderedAsyncEnumerableAwaitWithCancellation<TSource, TKey>(
                source,
                keySelector,
                Comparer<TKey>.Default,
                true,
                null
            );
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> OrderByDescendingAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new OrderedAsyncEnumerableAwaitWithCancellation<TSource, TKey>(
                source,
                keySelector,
                comparer,
                true,
                null
            );
        }

        #endregion


        #region ThenBy_ThenByDescending

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByAwait<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByAwait<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, true);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescendingAwait<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, true);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescendingAwait<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescendingAwaitWithCancellation<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return source.CreateOrderedEnumerable(keySelector, Comparer<TKey>.Default, true);
        }

        public static IUniTaskOrderedAsyncEnumerable<TSource> ThenByDescendingAwaitWithCancellation<TSource, TKey>(
            this IUniTaskOrderedAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }

        #endregion
    }

    internal abstract class AsyncEnumerableSorter<TElement> {
        internal abstract UniTask ComputeKeysAsync(TElement[] elements, int count);

        internal abstract int CompareKeys(int index1, int index2);

        internal async UniTask<int[]> SortAsync(TElement[] elements, int count) {
            await ComputeKeysAsync(elements, count);

            int[] map = new int[count];

            for (var i = 0; i < count; i++) {
                map[i] = i;
            }

            QuickSort(map, 0, count - 1);
            return map;
        }

        private void QuickSort(int[] map, int left, int right) {
            do {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];

                do {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0) {
                        i++;
                    }

                    while (j >= 0 && CompareKeys(x, map[j]) < 0) {
                        j--;
                    }

                    if (i > j) {
                        break;
                    }

                    if (i < j) {
                        (map[i], map[j]) = (map[j], map[i]);
                    }

                    i++;
                    j--;
                } while (i <= j);

                if (j - left <= right - i) {
                    if (left < j) {
                        QuickSort(map, left, j);
                    }

                    left = i;
                } else {
                    if (i < right) {
                        QuickSort(map, i, right);
                    }

                    right = j;
                }
            } while (left < right);
        }
    }

    internal class SyncSelectorAsyncEnumerableSorter<TElement, TKey> : AsyncEnumerableSorter<TElement> {
        private readonly Func<TElement, TKey> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly AsyncEnumerableSorter<TElement> m_Next;
        private TKey[] m_Keys;

        internal SyncSelectorAsyncEnumerableSorter(
            Func<TElement, TKey> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            AsyncEnumerableSorter<TElement> next
        ) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Next = next;
        }

        internal override async UniTask ComputeKeysAsync(TElement[] elements, int count) {
            m_Keys = new TKey[count];

            for (var i = 0; i < count; i++) {
                m_Keys[i] = m_KeySelector(elements[i]);
            }

            if (m_Next != null) {
                await m_Next.ComputeKeysAsync(elements, count);
            }
        }

        internal override int CompareKeys(int index1, int index2) {
            int c = m_Comparer.Compare(m_Keys[index1], m_Keys[index2]);

            if (c == 0) {
                if (m_Next == null) {
                    return index1 - index2;
                }

                return m_Next.CompareKeys(index1, index2);
            }

            return m_Descending ? -c : c;
        }
    }

    internal class AsyncSelectorEnumerableSorter<TElement, TKey> : AsyncEnumerableSorter<TElement> {
        private readonly Func<TElement, UniTask<TKey>> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly AsyncEnumerableSorter<TElement> m_Next;
        private TKey[] m_Keys;

        internal AsyncSelectorEnumerableSorter(
            Func<TElement, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            AsyncEnumerableSorter<TElement> next
        ) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Next = next;
        }

        internal override async UniTask ComputeKeysAsync(TElement[] elements, int count) {
            m_Keys = new TKey[count];

            for (var i = 0; i < count; i++) {
                m_Keys[i] = await m_KeySelector(elements[i]);
            }

            if (m_Next != null) {
                await m_Next.ComputeKeysAsync(elements, count);
            }
        }

        internal override int CompareKeys(int index1, int index2) {
            int c = m_Comparer.Compare(m_Keys[index1], m_Keys[index2]);

            if (c == 0) {
                if (m_Next == null) {
                    return index1 - index2;
                }

                return m_Next.CompareKeys(index1, index2);
            }

            return m_Descending ? -c : c;
        }
    }

    internal class AsyncSelectorWithCancellationEnumerableSorter<TElement, TKey> : AsyncEnumerableSorter<TElement> {
        private readonly Func<TElement, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly AsyncEnumerableSorter<TElement> m_Next;
        private CancellationToken m_CancellationToken;
        private TKey[] m_Keys;

        internal AsyncSelectorWithCancellationEnumerableSorter(
            Func<TElement, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            AsyncEnumerableSorter<TElement> next,
            CancellationToken cancellationToken
        ) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Next = next;
            m_CancellationToken = cancellationToken;
        }

        internal override async UniTask ComputeKeysAsync(TElement[] elements, int count) {
            m_Keys = new TKey[count];

            for (var i = 0; i < count; i++) {
                m_Keys[i] = await m_KeySelector(elements[i], m_CancellationToken);
            }

            if (m_Next != null) {
                await m_Next.ComputeKeysAsync(elements, count);
            }
        }

        internal override int CompareKeys(int index1, int index2) {
            int c = m_Comparer.Compare(m_Keys[index1], m_Keys[index2]);

            if (c == 0) {
                if (m_Next == null) {
                    return index1 - index2;
                }

                return m_Next.CompareKeys(index1, index2);
            }

            return m_Descending ? -c : c;
        }
    }

    internal abstract class OrderedAsyncEnumerable<TElement> : IUniTaskOrderedAsyncEnumerable<TElement> {
        protected readonly IUniTaskAsyncEnumerable<TElement> mSource;

        public OrderedAsyncEnumerable(IUniTaskAsyncEnumerable<TElement> source) {
            mSource = source;
        }

        public IUniTaskOrderedAsyncEnumerable<TElement> CreateOrderedEnumerable<TKey>(
            Func<TElement, TKey> keySelector,
            IComparer<TKey> comparer,
            bool descending
        ) {
            return new OrderedAsyncEnumerable<TElement, TKey>(mSource, keySelector, comparer, descending, this);
        }

        public IUniTaskOrderedAsyncEnumerable<TElement> CreateOrderedEnumerable<TKey>(
            Func<TElement, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending
        ) {
            return new OrderedAsyncEnumerableAwait<TElement, TKey>(mSource, keySelector, comparer, descending, this);
        }

        public IUniTaskOrderedAsyncEnumerable<TElement> CreateOrderedEnumerable<TKey>(
            Func<TElement, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending
        ) {
            return new OrderedAsyncEnumerableAwaitWithCancellation<TElement, TKey>(
                mSource,
                keySelector,
                comparer,
                descending,
                this
            );
        }

        internal abstract AsyncEnumerableSorter<TElement> GetAsyncEnumerableSorter(
            AsyncEnumerableSorter<TElement> next,
            CancellationToken cancellationToken
        );

        public IUniTaskAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new OrderedAsyncEnumerator(this, cancellationToken);
        }

        private class OrderedAsyncEnumerator : MoveNextSource, IUniTaskAsyncEnumerator<TElement> {
            protected readonly OrderedAsyncEnumerable<TElement> mParent;
            private CancellationToken m_CancellationToken;
            private TElement[] m_Buffer;
            private int[] m_Map;
            private int m_Index;

            public OrderedAsyncEnumerator(
                OrderedAsyncEnumerable<TElement> parent,
                CancellationToken cancellationToken
            ) {
                mParent = parent;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TElement Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Map == null) {
                    mCompletionSource.Reset();
                    CreateSortSource().Forget();
                    return new UniTask<bool>(this, mCompletionSource.Version);
                }

                if (m_Index < m_Buffer.Length) {
                    Current = m_Buffer[m_Map[m_Index++]];
                    return CompletedTasks.True;
                } else {
                    return CompletedTasks.False;
                }
            }

            private async UniTaskVoid CreateSortSource() {
                try {
                    m_Buffer = await mParent.mSource.ToArrayAsync();

                    if (m_Buffer.Length == 0) {
                        mCompletionSource.TrySetResult(false);
                        return;
                    }

                    var sorter = mParent.GetAsyncEnumerableSorter(null, m_CancellationToken);
                    m_Map = await sorter.SortAsync(m_Buffer, m_Buffer.Length);
                    sorter = null;

                    // set first value
                    Current = m_Buffer[m_Map[m_Index++]];
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                mCompletionSource.TrySetResult(true);
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);
                return default;
            }
        }
    }

    internal class OrderedAsyncEnumerable<TElement, TKey> : OrderedAsyncEnumerable<TElement> {
        private readonly Func<TElement, TKey> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly OrderedAsyncEnumerable<TElement> m_Parent;

        public OrderedAsyncEnumerable(
            IUniTaskAsyncEnumerable<TElement> source,
            Func<TElement, TKey> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            OrderedAsyncEnumerable<TElement> parent
        ) : base(source) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Parent = parent;
        }

        internal override AsyncEnumerableSorter<TElement> GetAsyncEnumerableSorter(
            AsyncEnumerableSorter<TElement> next,
            CancellationToken cancellationToken
        ) {
            AsyncEnumerableSorter<TElement> sorter =
                new SyncSelectorAsyncEnumerableSorter<TElement, TKey>(m_KeySelector, m_Comparer, m_Descending, next);

            if (m_Parent != null) {
                sorter = m_Parent.GetAsyncEnumerableSorter(sorter, cancellationToken);
            }

            return sorter;
        }
    }

    internal class OrderedAsyncEnumerableAwait<TElement, TKey> : OrderedAsyncEnumerable<TElement> {
        private readonly Func<TElement, UniTask<TKey>> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly OrderedAsyncEnumerable<TElement> m_Parent;

        public OrderedAsyncEnumerableAwait(
            IUniTaskAsyncEnumerable<TElement> source,
            Func<TElement, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            OrderedAsyncEnumerable<TElement> parent
        ) : base(source) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Parent = parent;
        }

        internal override AsyncEnumerableSorter<TElement> GetAsyncEnumerableSorter(
            AsyncEnumerableSorter<TElement> next,
            CancellationToken cancellationToken
        ) {
            AsyncEnumerableSorter<TElement> sorter =
                new AsyncSelectorEnumerableSorter<TElement, TKey>(m_KeySelector, m_Comparer, m_Descending, next);

            if (m_Parent != null) {
                sorter = m_Parent.GetAsyncEnumerableSorter(sorter, cancellationToken);
            }

            return sorter;
        }
    }

    internal class OrderedAsyncEnumerableAwaitWithCancellation<TElement, TKey> : OrderedAsyncEnumerable<TElement> {
        private readonly Func<TElement, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly IComparer<TKey> m_Comparer;
        private readonly bool m_Descending;
        private readonly OrderedAsyncEnumerable<TElement> m_Parent;

        public OrderedAsyncEnumerableAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TElement> source,
            Func<TElement, CancellationToken, UniTask<TKey>> keySelector,
            IComparer<TKey> comparer,
            bool descending,
            OrderedAsyncEnumerable<TElement> parent
        ) : base(source) {
            m_KeySelector = keySelector;
            m_Comparer = comparer;
            m_Descending = descending;
            m_Parent = parent;
        }

        internal override AsyncEnumerableSorter<TElement> GetAsyncEnumerableSorter(
            AsyncEnumerableSorter<TElement> next,
            CancellationToken cancellationToken
        ) {
            AsyncEnumerableSorter<TElement> sorter = new AsyncSelectorWithCancellationEnumerableSorter<TElement, TKey>(
                m_KeySelector,
                m_Comparer,
                m_Descending,
                next,
                cancellationToken
            );

            if (m_Parent != null) {
                sorter = m_Parent.GetAsyncEnumerableSorter(sorter, cancellationToken);
            }

            return sorter;
        }
    }
}