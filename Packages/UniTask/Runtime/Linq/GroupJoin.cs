using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupJoin<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupJoin<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupJoinAwait<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupJoinAwait<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupJoinAwait<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupJoinAwait<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupJoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new GroupJoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                EqualityComparer<TKey>.Default
            );
        }

        public static IUniTaskAsyncEnumerable<TResult> GroupJoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
            this IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> resultSelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(outer, nameof(outer));
            Error.ThrowArgumentNullException(inner, nameof(inner));
            Error.ThrowArgumentNullException(outerKeySelector, nameof(outerKeySelector));
            Error.ThrowArgumentNullException(innerKeySelector, nameof(innerKeySelector));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new GroupJoinAwaitWithCancellation<TOuter, TInner, TKey, TResult>(
                outer,
                inner,
                outerKeySelector,
                innerKeySelector,
                resultSelector,
                comparer
            );
        }
    }

    internal sealed class GroupJoin<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, TKey> m_OuterKeySelector;
        private readonly Func<TInner, TKey> m_InnerKeySelector;
        private readonly Func<TOuter, IEnumerable<TInner>, TResult> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupJoin(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
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
            return new InnerGroupJoin(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerGroupJoin : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, TKey> m_OuterKeySelector;
            private readonly Func<TInner, TKey> m_InnerKeySelector;
            private readonly Func<TOuter, IEnumerable<TInner>, TResult> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerGroupJoin(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, TKey> outerKeySelector,
                Func<TInner, TKey> innerKeySelector,
                Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
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
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
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
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        MoveNextCore(this);
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerGroupJoin)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        var outer = self.m_Enumerator.Current;
                        var key = self.m_OuterKeySelector(outer);
                        var values = self.m_Lookup[key];

                        self.Current = self.m_ResultSelector(outer, values);
                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }

    internal sealed class GroupJoinAwait<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, UniTask<TKey>> m_OuterKeySelector;
        private readonly Func<TInner, UniTask<TKey>> m_InnerKeySelector;
        private readonly Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupJoinAwait(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, UniTask<TKey>> outerKeySelector,
            Func<TInner, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> resultSelector,
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
            return new InnerGroupJoinAwait(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerGroupJoinAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;
            private readonly static Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;
            private readonly static Action<object> s_OuterKeySelectCoreDelegate = OuterKeySelectCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, UniTask<TKey>> m_OuterKeySelector;
            private readonly Func<TInner, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private TOuter m_OuterValue;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TKey>.Awaiter m_OuterKeyAwaiter;
            private UniTask<TResult>.Awaiter m_ResultAwaiter;

            public InnerGroupJoinAwait(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, UniTask<TKey>> outerKeySelector,
                Func<TInner, UniTask<TKey>> innerKeySelector,
                Func<TOuter, IEnumerable<TInner>, UniTask<TResult>> resultSelector,
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
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
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
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        MoveNextCore(this);
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerGroupJoinAwait)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_OuterValue = self.m_Enumerator.Current;
                            self.m_OuterKeyAwaiter = self.m_OuterKeySelector(self.m_OuterValue).GetAwaiter();

                            if (self.m_OuterKeyAwaiter.IsCompleted) {
                                OuterKeySelectCore(self);
                            } else {
                                self.m_OuterKeyAwaiter.SourceOnCompleted(s_OuterKeySelectCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void OuterKeySelectCore(object state) {
                var self = (InnerGroupJoinAwait)state;

                if (self.TryGetResult(self.m_OuterKeyAwaiter, out var result)) {
                    try {
                        var values = self.m_Lookup[result];
                        self.m_ResultAwaiter = self.m_ResultSelector(self.m_OuterValue, values).GetAwaiter();

                        if (self.m_ResultAwaiter.IsCompleted) {
                            ResultSelectCore(self);
                        } else {
                            self.m_ResultAwaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, self);
                        }
                    } catch (Exception ex) {
                        self.mCompletionSource.TrySetException(ex);
                    }
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerGroupJoinAwait)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }

    internal sealed class
        GroupJoinAwaitWithCancellation<TOuter, TInner, TKey, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
        private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
        private readonly Func<TOuter, CancellationToken, UniTask<TKey>> m_OuterKeySelector;
        private readonly Func<TInner, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
        private readonly Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> m_ResultSelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public GroupJoinAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TOuter> outer,
            IUniTaskAsyncEnumerable<TInner> inner,
            Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
            Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
            Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> resultSelector,
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
            return new InnerGroupJoinAwaitWithCancellation(
                m_Outer,
                m_Inner,
                m_OuterKeySelector,
                m_InnerKeySelector,
                m_ResultSelector,
                m_Comparer,
                cancellationToken
            );
        }

        private sealed class InnerGroupJoinAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;
            private readonly static Action<object> s_ResultSelectCoreDelegate = ResultSelectCore;
            private readonly static Action<object> s_OuterKeySelectCoreDelegate = OuterKeySelectCore;

            private readonly IUniTaskAsyncEnumerable<TOuter> m_Outer;
            private readonly IUniTaskAsyncEnumerable<TInner> m_Inner;
            private readonly Func<TOuter, CancellationToken, UniTask<TKey>> m_OuterKeySelector;
            private readonly Func<TInner, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
            private readonly Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> m_ResultSelector;
            private readonly IEqualityComparer<TKey> m_Comparer;
            private CancellationToken m_CancellationToken;

            private ILookup<TKey, TInner> m_Lookup;
            private IUniTaskAsyncEnumerator<TOuter> m_Enumerator;
            private TOuter m_OuterValue;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TKey>.Awaiter m_OuterKeyAwaiter;
            private UniTask<TResult>.Awaiter m_ResultAwaiter;

            public InnerGroupJoinAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TOuter> outer,
                IUniTaskAsyncEnumerable<TInner> inner,
                Func<TOuter, CancellationToken, UniTask<TKey>> outerKeySelector,
                Func<TInner, CancellationToken, UniTask<TKey>> innerKeySelector,
                Func<TOuter, IEnumerable<TInner>, CancellationToken, UniTask<TResult>> resultSelector,
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
                    CreateLookup().Forget();
                } else {
                    SourceMoveNext();
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private async UniTaskVoid CreateLookup() {
                try {
                    m_Lookup = await m_Inner.ToLookupAwaitWithCancellationAsync(
                        m_InnerKeySelector,
                        m_Comparer,
                        m_CancellationToken
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
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        MoveNextCore(this);
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerGroupJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_OuterValue = self.m_Enumerator.Current;

                            self.m_OuterKeyAwaiter = self.m_OuterKeySelector(self.m_OuterValue, self.m_CancellationToken)
                                                       .GetAwaiter();

                            if (self.m_OuterKeyAwaiter.IsCompleted) {
                                OuterKeySelectCore(self);
                            } else {
                                self.m_OuterKeyAwaiter.SourceOnCompleted(s_OuterKeySelectCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void OuterKeySelectCore(object state) {
                var self = (InnerGroupJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_OuterKeyAwaiter, out var result)) {
                    try {
                        var values = self.m_Lookup[result];

                        self.m_ResultAwaiter = self.m_ResultSelector(self.m_OuterValue, values, self.m_CancellationToken)
                                                 .GetAwaiter();

                        if (self.m_ResultAwaiter.IsCompleted) {
                            ResultSelectCore(self);
                        } else {
                            self.m_ResultAwaiter.SourceOnCompleted(s_ResultSelectCoreDelegate, self);
                        }
                    } catch (Exception ex) {
                        self.mCompletionSource.TrySetException(ex);
                    }
                }
            }

            private static void ResultSelectCore(object state) {
                var self = (InnerGroupJoinAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;
                    self.mCompletionSource.TrySetResult(true);
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}