using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, IUniTaskAsyncEnumerable<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectMany<TSource, TResult, TResult>(source, selector, (x, y) => y);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, IUniTaskAsyncEnumerable<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectMany<TSource, TResult, TResult>(source, selector, (x, y) => y);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, IUniTaskAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectMany<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, IUniTaskAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectMany<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<IUniTaskAsyncEnumerable<TResult>>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectManyAwait<TSource, TResult, TResult>(source, selector, (x, y) => UniTask.FromResult(y));
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<IUniTaskAsyncEnumerable<TResult>>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectManyAwait<TSource, TResult, TResult>(source, selector, (x, y) => UniTask.FromResult(y));
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwait<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<IUniTaskAsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectManyAwait<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwait<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<IUniTaskAsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectManyAwait<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwaitWithCancellation<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TResult>>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectManyAwaitWithCancellation<TSource, TResult, TResult>(
                source,
                selector,
                (x, y, c) => UniTask.FromResult(y)
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwaitWithCancellation<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TResult>>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new SelectManyAwaitWithCancellation<TSource, TResult, TResult>(
                source,
                selector,
                (x, y, c) => UniTask.FromResult(y)
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwaitWithCancellation<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectManyAwaitWithCancellation<TSource, TCollection, TResult>(
                source,
                collectionSelector,
                resultSelector
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectManyAwaitWithCancellation<TSource, TCollection, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(collectionSelector, nameof(collectionSelector));

            return new SelectManyAwaitWithCancellation<TSource, TCollection, TResult>(
                source,
                collectionSelector,
                resultSelector
            );
        }
    }

    internal sealed class SelectMany<TSource, TCollection, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, IUniTaskAsyncEnumerable<TCollection>> m_Selector1;
        private readonly Func<TSource, int, IUniTaskAsyncEnumerable<TCollection>> m_Selector2;
        private readonly Func<TSource, TCollection, TResult> m_ResultSelector;

        public SelectMany(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, IUniTaskAsyncEnumerable<TCollection>> selector,
            Func<TSource, TCollection, TResult> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = selector;
            m_Selector2 = null;
            m_ResultSelector = resultSelector;
        }

        public SelectMany(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, IUniTaskAsyncEnumerable<TCollection>> selector,
            Func<TSource, TCollection, TResult> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = null;
            m_Selector2 = selector;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelectMany(m_Source, m_Selector1, m_Selector2, m_ResultSelector, cancellationToken);
        }

        private sealed class InnerSelectMany : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_SourceMoveNextCoreDelegate = SourceMoveNextCore;
            private static readonly Action<object> s_SelectedSourceMoveNextCoreDelegate = SelectedSourceMoveNextCore;

            private static readonly Action<object> s_SelectedEnumeratorDisposeAsyncCoreDelegate =
                SelectedEnumeratorDisposeAsyncCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

            private readonly Func<TSource, IUniTaskAsyncEnumerable<TCollection>> m_Selector1;
            private readonly Func<TSource, int, IUniTaskAsyncEnumerable<TCollection>> m_Selector2;
            private readonly Func<TSource, TCollection, TResult> m_ResultSelector;
            private CancellationToken m_CancellationToken;

            private TSource m_SourceCurrent;
            private int m_SourceIndex;
            private IUniTaskAsyncEnumerator<TSource> m_SourceEnumerator;
            private IUniTaskAsyncEnumerator<TCollection> m_SelectedEnumerator;
            private UniTask<bool>.Awaiter m_SourceAwaiter;
            private UniTask<bool>.Awaiter m_SelectedAwaiter;
            private UniTask.Awaiter m_SelectedDisposeAsyncAwaiter;

            public InnerSelectMany(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, IUniTaskAsyncEnumerable<TCollection>> selector1,
                Func<TSource, int, IUniTaskAsyncEnumerable<TCollection>> selector2,
                Func<TSource, TCollection, TResult> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector1 = selector1;
                m_Selector2 = selector2;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                // iterate selected field
                if (m_SelectedEnumerator != null) {
                    MoveNextSelected();
                } else {
                    // iterate source field
                    if (m_SourceEnumerator == null) {
                        m_SourceEnumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                    }

                    MoveNextSource();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNextSource() {
                try {
                    m_SourceAwaiter = m_SourceEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SourceAwaiter.IsCompleted) {
                    SourceMoveNextCore(this);
                } else {
                    m_SourceAwaiter.SourceOnCompleted(s_SourceMoveNextCoreDelegate, this);
                }
            }

            private void MoveNextSelected() {
                try {
                    m_SelectedAwaiter = m_SelectedEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SelectedAwaiter.IsCompleted) {
                    SelectedSourceMoveNextCore(this);
                } else {
                    m_SelectedAwaiter.SourceOnCompleted(s_SelectedSourceMoveNextCoreDelegate, this);
                }
            }

            private static void SourceMoveNextCore(object state) {
                var self = (InnerSelectMany)state;

                if (self.TryGetResult(self.m_SourceAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SourceCurrent = self.m_SourceEnumerator.Current;

                            if (self.m_Selector1 != null) {
                                self.m_SelectedEnumerator = self.m_Selector1(self.m_SourceCurrent)
                                                              .GetAsyncEnumerator(self.m_CancellationToken);
                            } else {
                                self.m_SelectedEnumerator =
                                    self.m_Selector2(self.m_SourceCurrent, checked(self.m_SourceIndex++))
                                        .GetAsyncEnumerator(self.m_CancellationToken);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        self.MoveNextSelected(); // iterated selected source.
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SelectedSourceMoveNextCore(object state) {
                var self = (InnerSelectMany)state;

                if (self.TryGetResult(self.m_SelectedAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.Current = self.m_ResultSelector(self.m_SourceCurrent, self.m_SelectedEnumerator.Current);
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        // dispose selected source and try iterate source.
                        try {
                            self.m_SelectedDisposeAsyncAwaiter = self.m_SelectedEnumerator.DisposeAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SelectedDisposeAsyncAwaiter.IsCompleted) {
                            SelectedEnumeratorDisposeAsyncCore(self);
                        } else {
                            self.m_SelectedDisposeAsyncAwaiter.SourceOnCompleted(
                                s_SelectedEnumeratorDisposeAsyncCoreDelegate,
                                self
                            );
                        }
                    }
                }
            }

            private static void SelectedEnumeratorDisposeAsyncCore(object state) {
                var self = (InnerSelectMany)state;

                if (self.TryGetResult(self.m_SelectedDisposeAsyncAwaiter)) {
                    self.m_SelectedEnumerator = null;
                    self.m_SelectedAwaiter = default;

                    self.MoveNextSource(); // iterate next source
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_SelectedEnumerator != null) {
                    await m_SelectedEnumerator.DisposeAsync();
                }

                if (m_SourceEnumerator != null) {
                    await m_SourceEnumerator.DisposeAsync();
                }
            }
        }
    }

    internal sealed class SelectManyAwait<TSource, TCollection, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector1;
        private readonly Func<TSource, int, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector2;
        private readonly Func<TSource, TCollection, UniTask<TResult>> m_ResultSelector;

        public SelectManyAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector,
            Func<TSource, TCollection, UniTask<TResult>> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = selector;
            m_Selector2 = null;
            m_ResultSelector = resultSelector;
        }

        public SelectManyAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector,
            Func<TSource, TCollection, UniTask<TResult>> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = null;
            m_Selector2 = selector;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelectManyAwait(m_Source, m_Selector1, m_Selector2, m_ResultSelector, cancellationToken);
        }

        private sealed class InnerSelectManyAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_SourceMoveNextCoreDelegate = SourceMoveNextCore;
            private static readonly Action<object> s_SelectedSourceMoveNextCoreDelegate = SelectedSourceMoveNextCore;

            private static readonly Action<object> s_SelectedEnumeratorDisposeAsyncCoreDelegate =
                SelectedEnumeratorDisposeAsyncCore;

            private static readonly Action<object> s_SelectorAwaitCoreDelegate = SelectorAwaitCore;
            private static readonly Action<object> s_ResultSelectorAwaitCoreDelegate = ResultSelectorAwaitCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

            private readonly Func<TSource, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector1;
            private readonly Func<TSource, int, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector2;
            private readonly Func<TSource, TCollection, UniTask<TResult>> m_ResultSelector;
            private CancellationToken m_CancellationToken;

            private TSource m_SourceCurrent;
            private int m_SourceIndex;
            private IUniTaskAsyncEnumerator<TSource> m_SourceEnumerator;
            private IUniTaskAsyncEnumerator<TCollection> m_SelectedEnumerator;
            private UniTask<bool>.Awaiter m_SourceAwaiter;
            private UniTask<bool>.Awaiter m_SelectedAwaiter;
            private UniTask.Awaiter m_SelectedDisposeAsyncAwaiter;

            // await additional
            private UniTask<IUniTaskAsyncEnumerable<TCollection>>.Awaiter m_CollectionSelectorAwaiter;
            private UniTask<TResult>.Awaiter m_ResultSelectorAwaiter;

            public InnerSelectManyAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector1,
                Func<TSource, int, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector2,
                Func<TSource, TCollection, UniTask<TResult>> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector1 = selector1;
                m_Selector2 = selector2;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                // iterate selected field
                if (m_SelectedEnumerator != null) {
                    MoveNextSelected();
                } else {
                    // iterate source field
                    if (m_SourceEnumerator == null) {
                        m_SourceEnumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                    }

                    MoveNextSource();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNextSource() {
                try {
                    m_SourceAwaiter = m_SourceEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SourceAwaiter.IsCompleted) {
                    SourceMoveNextCore(this);
                } else {
                    m_SourceAwaiter.SourceOnCompleted(s_SourceMoveNextCoreDelegate, this);
                }
            }

            private void MoveNextSelected() {
                try {
                    m_SelectedAwaiter = m_SelectedEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SelectedAwaiter.IsCompleted) {
                    SelectedSourceMoveNextCore(this);
                } else {
                    m_SelectedAwaiter.SourceOnCompleted(s_SelectedSourceMoveNextCoreDelegate, this);
                }
            }

            private static void SourceMoveNextCore(object state) {
                var self = (InnerSelectManyAwait)state;

                if (self.TryGetResult(self.m_SourceAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SourceCurrent = self.m_SourceEnumerator.Current;

                            if (self.m_Selector1 != null) {
                                self.m_CollectionSelectorAwaiter = self.m_Selector1(self.m_SourceCurrent).GetAwaiter();
                            } else {
                                self.m_CollectionSelectorAwaiter = self.m_Selector2(
                                                                         self.m_SourceCurrent,
                                                                         checked(self.m_SourceIndex++)
                                                                     )
                                                                     .GetAwaiter();
                            }

                            if (self.m_CollectionSelectorAwaiter.IsCompleted) {
                                SelectorAwaitCore(self);
                            } else {
                                self.m_CollectionSelectorAwaiter.SourceOnCompleted(s_SelectorAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SelectedSourceMoveNextCore(object state) {
                var self = (InnerSelectManyAwait)state;

                if (self.TryGetResult(self.m_SelectedAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_ResultSelectorAwaiter = self.m_ResultSelector(
                                                                 self.m_SourceCurrent,
                                                                 self.m_SelectedEnumerator.Current
                                                             )
                                                             .GetAwaiter();

                            if (self.m_ResultSelectorAwaiter.IsCompleted) {
                                ResultSelectorAwaitCore(self);
                            } else {
                                self.m_ResultSelectorAwaiter.SourceOnCompleted(s_ResultSelectorAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }
                    } else {
                        // dispose selected source and try iterate source.
                        try {
                            self.m_SelectedDisposeAsyncAwaiter = self.m_SelectedEnumerator.DisposeAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SelectedDisposeAsyncAwaiter.IsCompleted) {
                            SelectedEnumeratorDisposeAsyncCore(self);
                        } else {
                            self.m_SelectedDisposeAsyncAwaiter.SourceOnCompleted(
                                s_SelectedEnumeratorDisposeAsyncCoreDelegate,
                                self
                            );
                        }
                    }
                }
            }

            private static void SelectedEnumeratorDisposeAsyncCore(object state) {
                var self = (InnerSelectManyAwait)state;

                if (self.TryGetResult(self.m_SelectedDisposeAsyncAwaiter)) {
                    self.m_SelectedEnumerator = null;
                    self.m_SelectedAwaiter = default;

                    self.MoveNextSource(); // iterate next source
                }
            }

            private static void SelectorAwaitCore(object state) {
                var self = (InnerSelectManyAwait)state;

                if (self.TryGetResult(self.m_CollectionSelectorAwaiter, out var result)) {
                    self.m_SelectedEnumerator = result.GetAsyncEnumerator(self.m_CancellationToken);
                    self.MoveNextSelected(); // iterated selected source.
                }
            }

            private static void ResultSelectorAwaitCore(object state) {
                var self = (InnerSelectManyAwait)state;

                if (self.TryGetResult(self.m_ResultSelectorAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_SelectedEnumerator != null) {
                    await m_SelectedEnumerator.DisposeAsync();
                }

                if (m_SourceEnumerator != null) {
                    await m_SourceEnumerator.DisposeAsync();
                }
            }
        }
    }

    internal sealed class
        SelectManyAwaitWithCancellation<TSource, TCollection, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector1;
        private readonly Func<TSource, int, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector2;
        private readonly Func<TSource, TCollection, CancellationToken, UniTask<TResult>> m_ResultSelector;

        public SelectManyAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector,
            Func<TSource, TCollection, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = selector;
            m_Selector2 = null;
            m_ResultSelector = resultSelector;
        }

        public SelectManyAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector,
            Func<TSource, TCollection, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            m_Source = source;
            m_Selector1 = null;
            m_Selector2 = selector;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelectManyAwaitWithCancellation(
                m_Source,
                m_Selector1,
                m_Selector2,
                m_ResultSelector,
                cancellationToken
            );
        }

        private sealed class InnerSelectManyAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_SourceMoveNextCoreDelegate = SourceMoveNextCore;
            private static readonly Action<object> s_SelectedSourceMoveNextCoreDelegate = SelectedSourceMoveNextCore;

            private static readonly Action<object> s_SelectedEnumeratorDisposeAsyncCoreDelegate =
                SelectedEnumeratorDisposeAsyncCore;

            private static readonly Action<object> s_SelectorAwaitCoreDelegate = SelectorAwaitCore;
            private static readonly Action<object> s_ResultSelectorAwaitCoreDelegate = ResultSelectorAwaitCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

            private readonly Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector1;
            private readonly Func<TSource, int, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> m_Selector2;
            private readonly Func<TSource, TCollection, CancellationToken, UniTask<TResult>> m_ResultSelector;
            private CancellationToken m_CancellationToken;

            private TSource m_SourceCurrent;
            private int m_SourceIndex;
            private IUniTaskAsyncEnumerator<TSource> m_SourceEnumerator;
            private IUniTaskAsyncEnumerator<TCollection> m_SelectedEnumerator;
            private UniTask<bool>.Awaiter m_SourceAwaiter;
            private UniTask<bool>.Awaiter m_SelectedAwaiter;
            private UniTask.Awaiter m_SelectedDisposeAsyncAwaiter;

            // await additional
            private UniTask<IUniTaskAsyncEnumerable<TCollection>>.Awaiter m_CollectionSelectorAwaiter;
            private UniTask<TResult>.Awaiter m_ResultSelectorAwaiter;

            public InnerSelectManyAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector1,
                Func<TSource, int, CancellationToken, UniTask<IUniTaskAsyncEnumerable<TCollection>>> selector2,
                Func<TSource, TCollection, CancellationToken, UniTask<TResult>> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector1 = selector1;
                m_Selector2 = selector2;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                // iterate selected field
                if (m_SelectedEnumerator != null) {
                    MoveNextSelected();
                } else {
                    // iterate source field
                    if (m_SourceEnumerator == null) {
                        m_SourceEnumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                    }

                    MoveNextSource();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNextSource() {
                try {
                    m_SourceAwaiter = m_SourceEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SourceAwaiter.IsCompleted) {
                    SourceMoveNextCore(this);
                } else {
                    m_SourceAwaiter.SourceOnCompleted(s_SourceMoveNextCoreDelegate, this);
                }
            }

            private void MoveNextSelected() {
                try {
                    m_SelectedAwaiter = m_SelectedEnumerator.MoveNextAsync().GetAwaiter();
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (m_SelectedAwaiter.IsCompleted) {
                    SelectedSourceMoveNextCore(this);
                } else {
                    m_SelectedAwaiter.SourceOnCompleted(s_SelectedSourceMoveNextCoreDelegate, this);
                }
            }

            private static void SourceMoveNextCore(object state) {
                var self = (InnerSelectManyAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_SourceAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SourceCurrent = self.m_SourceEnumerator.Current;

                            if (self.m_Selector1 != null) {
                                self.m_CollectionSelectorAwaiter = self.m_Selector1(
                                                                         self.m_SourceCurrent,
                                                                         self.m_CancellationToken
                                                                     )
                                                                     .GetAwaiter();
                            } else {
                                self.m_CollectionSelectorAwaiter = self.m_Selector2(
                                                                         self.m_SourceCurrent,
                                                                         checked(self.m_SourceIndex++),
                                                                         self.m_CancellationToken
                                                                     )
                                                                     .GetAwaiter();
                            }

                            if (self.m_CollectionSelectorAwaiter.IsCompleted) {
                                SelectorAwaitCore(self);
                            } else {
                                self.m_CollectionSelectorAwaiter.SourceOnCompleted(s_SelectorAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SelectedSourceMoveNextCore(object state) {
                var self = (InnerSelectManyAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_SelectedAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_ResultSelectorAwaiter = self.m_ResultSelector(
                                                                 self.m_SourceCurrent,
                                                                 self.m_SelectedEnumerator.Current,
                                                                 self.m_CancellationToken
                                                             )
                                                             .GetAwaiter();

                            if (self.m_ResultSelectorAwaiter.IsCompleted) {
                                ResultSelectorAwaitCore(self);
                            } else {
                                self.m_ResultSelectorAwaiter.SourceOnCompleted(s_ResultSelectorAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }
                    } else {
                        // dispose selected source and try iterate source.
                        try {
                            self.m_SelectedDisposeAsyncAwaiter = self.m_SelectedEnumerator.DisposeAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SelectedDisposeAsyncAwaiter.IsCompleted) {
                            SelectedEnumeratorDisposeAsyncCore(self);
                        } else {
                            self.m_SelectedDisposeAsyncAwaiter.SourceOnCompleted(
                                s_SelectedEnumeratorDisposeAsyncCoreDelegate,
                                self
                            );
                        }
                    }
                }
            }

            private static void SelectedEnumeratorDisposeAsyncCore(object state) {
                var self = (InnerSelectManyAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_SelectedDisposeAsyncAwaiter)) {
                    self.m_SelectedEnumerator = null;
                    self.m_SelectedAwaiter = default;

                    self.MoveNextSource(); // iterate next source
                }
            }

            private static void SelectorAwaitCore(object state) {
                var self = (InnerSelectManyAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_CollectionSelectorAwaiter, out var result)) {
                    self.m_SelectedEnumerator = result.GetAsyncEnumerator(self.m_CancellationToken);
                    self.MoveNextSelected(); // iterated selected source.
                }
            }

            private static void ResultSelectorAwaitCore(object state) {
                var self = (InnerSelectManyAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_ResultSelectorAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_SelectedEnumerator != null) {
                    await m_SelectedEnumerator.DisposeAsync();
                }

                if (m_SourceEnumerator != null) {
                    await m_SourceEnumerator.DisposeAsync();
                }
            }
        }
    }
}