using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        // Ix-Async returns IGrouping but it is competely waste, use standard IGrouping.

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            return new GroupBy<TSource, TKey, TSource>(source, keySelector, x => x, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));
            return new GroupBy<TSource, TKey, TSource>(source, keySelector, x => x, comparer);
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));

            return new GroupBy<TSource, TKey, TElement>(
                source,
                keySelector,
                elementSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));
            return new GroupBy<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupBy<TSource, TKey, TSource, TResult>(
                source,
                keySelector,
                x => x,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));
            return new GroupBy<TSource, TKey, TSource, TResult>(source, keySelector, x => x, resultSelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupBy<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupBy<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                comparer
            );
        }

        // await

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new GroupByAwait<TSource, TKey, TSource>(
                source,
                keySelector,
                x => UniTask.FromResult(x),
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));
            return new GroupByAwait<TSource, TKey, TSource>(source, keySelector, x => UniTask.FromResult(x), comparer);
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> GroupByAwait<TSource, TKey, TElement>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));

            return new GroupByAwait<TSource, TKey, TElement>(
                source,
                keySelector,
                elementSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> GroupByAwait<TSource, TKey, TElement>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));
            return new GroupByAwait<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwait<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TKey, IEnumerable<TSource>, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupByAwait<TSource, TKey, TSource, TResult>(
                source,
                keySelector,
                x => UniTask.FromResult(x),
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwait<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupByAwait<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwait<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TKey, IEnumerable<TSource>, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwait<TSource, TKey, TSource, TResult>(
                source,
                keySelector,
                x => UniTask.FromResult(x),
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwait<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwait<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                comparer
            );
        }

        // with ct

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));

            return new GroupByAwaitWithCancellation<TSource, TKey, TSource>(
                source,
                keySelector,
                (x, _) => UniTask.FromResult(x),
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwaitWithCancellation<TSource, TKey, TSource>(
                source,
                keySelector,
                (x, _) => UniTask.FromResult(x),
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>>
            GroupByAwaitWithCancellation<TSource, TKey, TElement>(
                this IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                Func<TSource, CancellationToken, UniTask<TElement>> elementSelector
            ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));

            return new GroupByAwaitWithCancellation<TSource, TKey, TElement>(
                source,
                keySelector,
                elementSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>>
            GroupByAwaitWithCancellation<TSource, TKey, TElement>(
                this IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
                IEqualityComparer<TKey> comparer
            ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwaitWithCancellation<TSource, TKey, TElement>(
                source,
                keySelector,
                elementSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwaitWithCancellation<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TKey, IEnumerable<TSource>, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupByAwaitWithCancellation<TSource, TKey, TSource, TResult>(
                source,
                keySelector,
                (x, _) => UniTask.FromResult(x),
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwaitWithCancellation<TSource, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TKey, IEnumerable<TSource>, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwaitWithCancellation<TSource, TKey, TSource, TResult>(
                source,
                keySelector,
                (x, _) => UniTask.FromResult(x),
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(elementSelector, nameof(elementSelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(
                source,
                keySelector,
                elementSelector,
                resultSelector,
                comparer
            );
        }
    }

    internal sealed class GroupBy<TSource, TKey, TElement> : IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, TKey> m_KeySelector;
        private readonly Func<TSource, TElement> m_ElementSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupBy(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerGroupBy(m_Source, m_KeySelector, m_ElementSelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerGroupBy : MoveNextSource, IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, TKey> m_InnerKeySelector;
            private readonly Func<TSource, TElement> m_InnerElementSelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private CancellationToken m_CancellationToken;

            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;

            public InnerGroupBy(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerElementSelector = elementSelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public IGrouping<TKey, TElement> Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_InnerSource.ToLookupAsync(m_InnerKeySelector, m_InnerElementSelector, m_InnerComparer, m_CancellationToken);
                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        Current = m_GroupEnumerator.Current as IGrouping<TKey, TElement>;
                        mCompletionSource.TrySetResult(true);
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }

    internal sealed class GroupBy<TSource, TKey, TElement, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, TKey> m_KeySelector;
        private readonly Func<TSource, TElement> m_ElementSelector;
        private readonly Func<TKey, IEnumerable<TElement>, TResult> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupBy(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerGroupBy(m_Source, m_KeySelector, m_ElementSelector, m_ResultSelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerGroupBy : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, TKey> m_InnerKeySelector;
            private readonly Func<TSource, TElement> m_InnerElementSelector;
            private readonly Func<TKey, IEnumerable<TElement>, TResult> m_InnerResultSelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private CancellationToken m_CancellationToken;

            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;

            public InnerGroupBy(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerElementSelector = elementSelector;
                m_InnerResultSelector = resultSelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_InnerSource.ToLookupAsync(m_InnerKeySelector, m_InnerElementSelector, m_InnerComparer, m_CancellationToken);
                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        var current = m_GroupEnumerator.Current;
                        Current = m_InnerResultSelector(current.Key, current);
                        mCompletionSource.TrySetResult(true);
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }

    internal sealed class GroupByAwait<TSource, TKey, TElement> : IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<TKey>> m_KeySelector;
        private readonly Func<TSource, UniTask<TElement>> m_ElementSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupByAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerGroupByAwait(m_Source, m_KeySelector, m_ElementSelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerGroupByAwait : MoveNextSource, IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TSource, UniTask<TElement>> m_InnerElementSelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private CancellationToken m_CancellationToken;
            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;

            public InnerGroupByAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<TKey>> keySelector,
                Func<TSource, UniTask<TElement>> elementSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerElementSelector = elementSelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public IGrouping<TKey, TElement> Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_InnerSource.ToLookupAwaitAsync(
                        m_InnerKeySelector,
                        m_InnerElementSelector,
                        m_InnerComparer,
                        m_CancellationToken
                    );

                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        Current = m_GroupEnumerator.Current as IGrouping<TKey, TElement>;
                        mCompletionSource.TrySetResult(true);
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }

    internal sealed class GroupByAwait<TSource, TKey, TElement, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<TKey>> m_KeySelector;
        private readonly Func<TSource, UniTask<TElement>> m_ElementSelector;
        private readonly Func<TKey, IEnumerable<TElement>, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupByAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            Func<TSource, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerGroupByAwait(m_Source, m_KeySelector, m_ElementSelector, m_ResultSelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerGroupByAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly static Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TSource, UniTask<TElement>> m_InnerElementSelector;
            private readonly Func<TKey, IEnumerable<TElement>, UniTask<TResult>> m_InnerResultSelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private CancellationToken m_CancellationToken;

            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;
            private UniTask<TResult>.Awaiter m_Awaiter;

            public InnerGroupByAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<TKey>> keySelector,
                Func<TSource, UniTask<TElement>> elementSelector,
                Func<TKey, IEnumerable<TElement>, UniTask<TResult>> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerElementSelector = elementSelector;
                m_InnerResultSelector = resultSelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_InnerSource.ToLookupAwaitAsync(
                        m_InnerKeySelector,
                        m_InnerElementSelector,
                        m_InnerComparer,
                        m_CancellationToken
                    );

                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        var current = m_GroupEnumerator.Current;

                        m_Awaiter = m_InnerResultSelector(current.Key, current).GetAwaiter();

                        if (m_Awaiter.IsCompleted) {
                            ResultSelectCore(this);
                        } else {
                            m_Awaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, this);
                        }

                        return;
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerGroupByAwait)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }

    internal sealed class
        GroupByAwaitWithCancellation<TSource, TKey, TElement> : IUniTaskAsyncEnumerable<IGrouping<TKey, TElement>> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly Func<TSource, CancellationToken, UniTask<TElement>> m_ElementSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupByAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerGroupByAwaitWithCancellation(m_Source, m_KeySelector, m_ElementSelector, m_Comparer, cancellationToken);
        }

        private sealed class
            InnerGroupByAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<IGrouping<TKey, TElement>> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TSource, CancellationToken, UniTask<TElement>> m_InnerElementSelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private CancellationToken m_CancellationToken;

            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;

            public InnerGroupByAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerElementSelector = elementSelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public IGrouping<TKey, TElement> Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_InnerSource.ToLookupAwaitWithCancellationAsync(
                        m_InnerKeySelector,
                        m_InnerElementSelector,
                        m_InnerComparer,
                        m_CancellationToken
                    );

                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        Current = m_GroupEnumerator.Current as IGrouping<TKey, TElement>;
                        mCompletionSource.TrySetResult(true);
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }

    internal sealed class
        GroupByAwaitWithCancellation<TSource, TKey, TElement, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly Func<TSource, CancellationToken, UniTask<TElement>> m_ElementSelector;
        private readonly Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupByAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
            Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_ElementSelector = elementSelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerGroupByAwaitWithCancellation(
                m_Source,
                m_KeySelector,
                m_ElementSelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerGroupByAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly static Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_KeySelector;
            private readonly Func<TSource, CancellationToken, UniTask<TElement>> m_ElementSelector;
            private readonly Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private IEnumerator<IGrouping<TKey, TElement>> m_GroupEnumerator;
            private UniTask<TResult>.Awaiter m_Awaiter;

            public InnerGroupByAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                Func<TSource, CancellationToken, UniTask<TElement>> elementSelector,
                Func<TKey, IEnumerable<TElement>, CancellationToken, UniTask<TResult>> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_KeySelector = keySelector;
                m_ElementSelector = elementSelector;
                m_ResultSelector = resultSelector;
                m_Comparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_GroupEnumerator == null) {
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    var lookup = await m_Source.ToLookupAwaitWithCancellationAsync(
                        m_KeySelector,
                        m_ElementSelector,
                        m_Comparer,
                        m_CancellationToken
                    );

                    m_GroupEnumerator = lookup.GetEnumerator();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    if (m_GroupEnumerator.MoveNext()) {
                        var current = m_GroupEnumerator.Current;

                        m_Awaiter = m_ResultSelector(current.Key, current, m_CancellationToken).GetAwaiter();

                        if (m_Awaiter.IsCompleted) {
                            ResultSelectCore(this);
                        } else {
                            m_Awaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, this);
                        }

                        return;
                    } else {
                        mCompletionSource.TrySetResult(false);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerGroupByAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_GroupEnumerator != null) {
                    m_GroupEnumerator.Dispose();
                }

                return default;
            }
        }
    }
}