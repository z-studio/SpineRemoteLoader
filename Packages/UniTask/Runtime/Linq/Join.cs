using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new Join<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new Join<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> JoinAwait<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new JoinAwait<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> JoinAwait<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new JoinAwait<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> JoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new JoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> JoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new JoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }
    }

    internal sealed class Join<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, TKey> m_OuterKeySelector;
        private readonly Func<TInner, TKey> m_InnerKeySelector;
        private readonly Func<TOuter, TInner, TResult> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public Join(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Outer = outer;
            m_Inner = inner;
            m_OuterKeySelector = outerKeySelector;
            m_InnerKeySelector = innerKeySelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerJoin(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerJoin : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, TKey> m_OuterKeySelector;
            private readonly Func<TInner, TKey> m_InnerKeySelector;
            private readonly Func<TOuter, TInner, TResult> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private TOuter m_CurrentOuterValue;
            private IEnumerator<TInner> m_ValueEnumerator;

            private bool m_ContinueNext;

            public InnerJoin(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, TKey> outerKeySelector,
                Func<TInner, TKey> innerKeySelector,
                Func<TOuter, TInner, TResult> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_Outer = outer;
                m_Inner = inner;
                m_OuterKeySelector = outerKeySelector;
                m_InnerKeySelector = innerKeySelector;
                m_ResultSelector = resultSelector;
                m_Comparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_Lookup == null) {
                    CreateInnerHashSet().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateInnerHashSet() {
                try {
                    m_Lookup = await m_Inner.ToLookupAsync(m_InnerKeySelector, m_Comparer, m_CancellationToken);
                    m_Enumerator = m_Outer.GetAsyncEnumerator(m_CancellationToken);
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    LOOP:

                    if (m_ValueEnumerator != null) {
                        if (m_ValueEnumerator.MoveNext()) {
                            Current = m_ResultSelector(m_CurrentOuterValue, m_ValueEnumerator.Current);
                            goto TRY_SET_RESULT_TRUE;
                        } else {
                            m_ValueEnumerator.Dispose();
                            m_ValueEnumerator = null;
                        }
                    }

                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }

                return;

                TRY_SET_RESULT_TRUE:
                mCompletionSource.TrySetResult(true);
            }

            private static void MoveNextCore(object state) {
                var self = (InnerJoin)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_CurrentOuterValue = self.m_Enumerator.Current;
                        var key = self.m_OuterKeySelector(self.m_CurrentOuterValue);
                        self.m_ValueEnumerator = self.m_Lookup[key].GetEnumerator();

                        if (self.m_ContinueNext) {
                            return;
                        } else {
                            self.SourceMoveNext();
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.mCompletionSource.TrySetResult(false);
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_ValueEnumerator != null) {
                    m_ValueEnumerator.Dispose();
                }

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }

    internal sealed class JoinAwait<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, UniTask<TKey>> m_OuterKeySelector;
        private readonly Func<TInner, UniTask<TKey>> m_InnerKeySelector;
        private readonly Func<TOuter, TInner, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public JoinAwait(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Outer = outer;
            m_Inner = inner;
            m_OuterKeySelector = outerKeySelector;
            m_InnerKeySelector = innerKeySelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerJoinAwait(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerJoinAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;
            private static readonly Action<object> s_OuterSelectCoreDelegate = OuterSelectCore;
            private static readonly Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, UniTask<TKey>> m_OuterKeySelector;
            private readonly Func<TInner, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TOuter, TInner, UniTask<TResult>> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private TOuter m_CurrentOuterValue;
            private IEnumerator<TInner> m_ValueEnumerator;

            private UniTask<TResult>.Awaiter m_ResultAwaiter;
            private UniTask<TKey>.Awaiter m_OuterKeyAwaiter;

            private bool m_ContinueNext;

            public InnerJoinAwait(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, UniTask<TKey>> outerKeySelector,
                Func<TInner, UniTask<TKey>> innerKeySelector,
                Func<TOuter, TInner, UniTask<TResult>> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_Outer = outer;
                m_Inner = inner;
                m_OuterKeySelector = outerKeySelector;
                m_InnerKeySelector = innerKeySelector;
                m_ResultSelector = resultSelector;
                m_Comparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_Lookup == null) {
                    CreateInnerHashSet().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateInnerHashSet() {
                try {
                    m_Lookup = await m_Inner.ToLookupAwaitAsync(m_InnerKeySelector, m_Comparer, m_CancellationToken);
                    m_Enumerator = m_Outer.GetAsyncEnumerator(m_CancellationToken);
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    LOOP:

                    if (m_ValueEnumerator != null) {
                        if (m_ValueEnumerator.MoveNext()) {
                            m_ResultAwaiter = m_ResultSelector(m_CurrentOuterValue, m_ValueEnumerator.Current).GetAwaiter();

                            if (m_ResultAwaiter.IsCompleted) {
                                ResultSelectCore(this);
                            } else {
                                m_ResultAwaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, this);
                            }

                            return;
                        } else {
                            m_ValueEnumerator.Dispose();
                            m_ValueEnumerator = null;
                        }
                    }

                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerJoinAwait)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_CurrentOuterValue = self.m_Enumerator.Current;

                        self.m_OuterKeyAwaiter = self.m_OuterKeySelector(self.m_CurrentOuterValue).GetAwaiter();

                        if (self.m_OuterKeyAwaiter.IsCompleted) {
                            OuterSelectCore(self);
                        } else {
                            self.m_ContinueNext = false;
                            self.m_OuterKeyAwaiter.SourceOnCompleted(s_OuterSelectCoreDelegate, self);
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.mCompletionSource.TrySetResult(false);
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            private static void OuterSelectCore(object state) {
                var self = (InnerJoinAwait)state;

                if (self.TryGetResult(self.m_OuterKeyAwaiter, out var key)) {
                    self.m_ValueEnumerator = self.m_Lookup[key].GetEnumerator();

                    if (self.m_ContinueNext) {
                        return;
                    } else {
                        self.SourceMoveNext();
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerJoinAwait)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_ValueEnumerator != null) {
                    m_ValueEnumerator.Dispose();
                }

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }

    internal sealed class JoinAwaitWithCancellation<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, CancellationToken, UniTask<TKey>> m_OuterKeySelector;
        private readonly Func<TInner, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
        private readonly Func<TOuter, TInner, CancellationToken, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public JoinAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, TInner, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Outer = outer;
            m_Inner = inner;
            m_OuterKeySelector = outerKeySelector;
            m_InnerKeySelector = innerKeySelector;
            m_ResultSelector = resultSelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerJoinAwaitWithCancellation(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerJoinAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;
            private static readonly Action<object> s_OuterSelectCoreDelegate = OuterSelectCore;
            private static readonly Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, CancellationToken, UniTask<TKey>> m_OuterKeySelector;
            private readonly Func<TInner, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TOuter, TInner, CancellationToken, UniTask<TResult>> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private TOuter m_CurrentOuterValue;
            private IEnumerator<TInner> m_ValueEnumerator;

            private UniTask<TResult>.Awaiter m_ResultAwaiter;
            private UniTask<TKey>.Awaiter m_OuterKeyAwaiter;

            private bool m_ContinueNext;

            public InnerJoinAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
                Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
                Func<TOuter, TInner, CancellationToken, UniTask<TResult>> resultSelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_Outer = outer;
                m_Inner = inner;
                m_OuterKeySelector = outerKeySelector;
                m_InnerKeySelector = innerKeySelector;
                m_ResultSelector = resultSelector;
                m_Comparer = comparer;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_Lookup == null) {
                    CreateInnerHashSet().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateInnerHashSet() {
                try {
                    m_Lookup = await m_Inner.ToLookupAwaitWithCancellationAsync(
                        m_InnerKeySelector,
                        m_Comparer,
                        cancellationToken: m_CancellationToken
                    );

                    m_Enumerator = m_Outer.GetAsyncEnumerator(m_CancellationToken);
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                SourceMoveNext();
            }

            private void SourceMoveNext() {
                try {
                    LOOP:

                    if (m_ValueEnumerator != null) {
                        if (m_ValueEnumerator.MoveNext()) {
                            m_ResultAwaiter = m_ResultSelector(
                                    m_CurrentOuterValue,
                                    m_ValueEnumerator.Current,
                                    m_CancellationToken
                                )
                                .GetAwaiter();

                            if (m_ResultAwaiter.IsCompleted) {
                                ResultSelectCore(this);
                            } else {
                                m_ResultAwaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, this);
                            }

                            return;
                        } else {
                            m_ValueEnumerator.Dispose();
                            m_ValueEnumerator = null;
                        }
                    }

                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_CurrentOuterValue = self.m_Enumerator.Current;

                        self.m_OuterKeyAwaiter = self.m_OuterKeySelector(self.m_CurrentOuterValue, self.m_CancellationToken)
                                                   .GetAwaiter();

                        if (self.m_OuterKeyAwaiter.IsCompleted) {
                            OuterSelectCore(self);
                        } else {
                            self.m_ContinueNext = false;
                            self.m_OuterKeyAwaiter.SourceOnCompleted(s_OuterSelectCoreDelegate, self);
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.mCompletionSource.TrySetResult(false);
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            private static void OuterSelectCore(object state) {
                var self = (InnerJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_OuterKeyAwaiter, out var key)) {
                    self.m_ValueEnumerator = self.m_Lookup[key].GetEnumerator();

                    if (self.m_ContinueNext) {
                        return;
                    } else {
                        self.SourceMoveNext();
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_ValueEnumerator != null) {
                    m_ValueEnumerator.Dispose();
                }

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}