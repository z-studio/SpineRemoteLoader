using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source
        ) {
            return DistinctUntilChanged(source, EqualityComparer<TSource>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            IEqualityComparer<TSource> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctUntilChanged<TSource>(source, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector
        ) {
            return DistinctUntilChanged(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctUntilChanged<TSource, TKey>(source, keySelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChangedAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector
        ) {
            return DistinctUntilChangedAwait(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChangedAwait<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctUntilChangedAwait<TSource, TKey>(source, keySelector, comparer);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChangedAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector
        ) {
            return DistinctUntilChangedAwaitWithCancellation(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DistinctUntilChangedAwaitWithCancellation<TSource, TKey>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(keySelector, nameof(keySelector));
            Error.ThrowArgumentNullException(comparer, nameof(comparer));

            return new DistinctUntilChangedAwaitWithCancellation<TSource, TKey>(source, keySelector, comparer);
        }
    }

    internal sealed class DistinctUntilChanged<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly IEqualityComparer<TSource> m_Comparer;

        public DistinctUntilChanged(IUniTaskAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer) {
            m_Source = source;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctUntilChanged(m_Source, m_Comparer, cancellationToken);
        }

        private sealed class InnerDistinctUntilChanged : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly IEqualityComparer<TSource> m_InnerComparer;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;

            public InnerDistinctUntilChanged(
                IUniTaskAsyncEnumerable<TSource> source,
                IEqualityComparer<TSource> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                REPEAT:

                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case -3;
                            } else {
                                m_State = -3;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case -3: // first
                            if (m_Awaiter.GetResult()) {
                                Current = m_Enumerator.Current;
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

                        case 0: // normal
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case 1;
                            } else {
                                m_State = 1;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case 1:
                            if (m_Awaiter.GetResult()) {
                                var v = m_Enumerator.Current;

                                if (!m_InnerComparer.Equals(Current, v)) {
                                    Current = v;
                                    goto CONTINUE;
                                } else {
                                    m_State = 0;
                                    goto REPEAT;
                                }
                            } else {
                                goto DONE;
                            }

                        case -2:
                        default:
                            goto DONE;
                    }
                } catch (Exception ex) {
                    m_State = -2;
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                m_State = -2;
                mCompletionSource.TrySetResult(false);
                return;

                CONTINUE:
                m_State = 0;
                mCompletionSource.TrySetResult(true);
                return;
            }

            public UniTask DisposeAsync() {
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class DistinctUntilChanged<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, TKey> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public DistinctUntilChanged(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctUntilChanged(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerDistinctUntilChanged : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, TKey> m_InnerKeySelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;
            private TKey m_Prev;

            public InnerDistinctUntilChanged(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                REPEAT:

                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case -3;
                            } else {
                                m_State = -3;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case -3: // first
                            if (m_Awaiter.GetResult()) {
                                Current = m_Enumerator.Current;
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

                        case 0: // normal
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case 1;
                            } else {
                                m_State = 1;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case 1:
                            if (m_Awaiter.GetResult()) {
                                var v = m_Enumerator.Current;
                                var key = m_InnerKeySelector(v);

                                if (!m_InnerComparer.Equals(m_Prev, key)) {
                                    m_Prev = key;
                                    Current = v;
                                    goto CONTINUE;
                                } else {
                                    m_State = 0;
                                    goto REPEAT;
                                }
                            } else {
                                goto DONE;
                            }

                        case -2:
                        default:
                            goto DONE;
                    }
                } catch (Exception ex) {
                    m_State = -2;
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                m_State = -2;
                mCompletionSource.TrySetResult(false);
                return;

                CONTINUE:
                m_State = 0;
                mCompletionSource.TrySetResult(true);
                return;
            }

            public UniTask DisposeAsync() {
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class DistinctUntilChangedAwait<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<TKey>> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public DistinctUntilChangedAwait(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctUntilChangedAwait(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerDistinctUntilChangedAwait : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, UniTask<TKey>> m_InnerKeySelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TKey>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private TSource m_EnumeratorCurrent;
            private TKey m_Prev;

            public InnerDistinctUntilChangedAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<TKey>> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                REPEAT:

                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case -3;
                            } else {
                                m_State = -3;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case -3: // first
                            if (m_Awaiter.GetResult()) {
                                Current = m_Enumerator.Current;
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

                        case 0: // normal
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case 1;
                            } else {
                                m_State = 1;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case 1:
                            if (m_Awaiter.GetResult()) {
                                m_EnumeratorCurrent = m_Enumerator.Current;
                                m_Awaiter2 = m_InnerKeySelector(m_EnumeratorCurrent).GetAwaiter();

                                if (m_Awaiter2.IsCompleted) {
                                    goto case 2;
                                } else {
                                    m_State = 2;
                                    m_Awaiter2.UnsafeOnCompleted(m_MoveNextAction);
                                    return;
                                }
                            } else {
                                goto DONE;
                            }

                        case 2:
                            var key = m_Awaiter2.GetResult();

                            if (!m_InnerComparer.Equals(m_Prev, key)) {
                                m_Prev = key;
                                Current = m_EnumeratorCurrent;
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
                            }

                        case -2:
                        default:
                            goto DONE;
                    }
                } catch (Exception ex) {
                    m_State = -2;
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                m_State = -2;
                mCompletionSource.TrySetResult(false);
                return;

                CONTINUE:
                m_State = 0;
                mCompletionSource.TrySetResult(true);
                return;
            }

            public UniTask DisposeAsync() {
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class DistinctUntilChangedAwaitWithCancellation<TSource, TKey> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_KeySelector;
        private readonly IEqualityComparer<TKey> m_Comparer;

        public DistinctUntilChangedAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
            IEqualityComparer<TKey> comparer
        ) {
            m_Source = source;
            m_KeySelector = keySelector;
            m_Comparer = comparer;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDistinctUntilChangedAwaitWithCancellation(m_Source, m_KeySelector, m_Comparer, cancellationToken);
        }

        private sealed class InnerDistinctUntilChangedAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly Func<TSource, CancellationToken, UniTask<TKey>> m_InnerKeySelector;
            private readonly IEqualityComparer<TKey> m_InnerComparer;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TKey>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private TSource m_EnumeratorCurrent;
            private TKey m_Prev;

            public InnerDistinctUntilChangedAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TKey>> keySelector,
                IEqualityComparer<TKey> comparer,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerKeySelector = keySelector;
                m_InnerComparer = comparer;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                REPEAT:

                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case -3;
                            } else {
                                m_State = -3;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case -3: // first
                            if (m_Awaiter.GetResult()) {
                                Current = m_Enumerator.Current;
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

                        case 0: // normal
                            m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                            if (m_Awaiter.IsCompleted) {
                                goto case 1;
                            } else {
                                m_State = 1;
                                m_Awaiter.UnsafeOnCompleted(m_MoveNextAction);
                                return;
                            }

                        case 1:
                            if (m_Awaiter.GetResult()) {
                                m_EnumeratorCurrent = m_Enumerator.Current;
                                m_Awaiter2 = m_InnerKeySelector(m_EnumeratorCurrent, m_CancellationToken).GetAwaiter();

                                if (m_Awaiter2.IsCompleted) {
                                    goto case 2;
                                } else {
                                    m_State = 2;
                                    m_Awaiter2.UnsafeOnCompleted(m_MoveNextAction);
                                    return;
                                }
                            } else {
                                goto DONE;
                            }

                        case 2:
                            var key = m_Awaiter2.GetResult();

                            if (!m_InnerComparer.Equals(m_Prev, key)) {
                                m_Prev = key;
                                Current = m_EnumeratorCurrent;
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
                            }

                        case -2:
                        default:
                            goto DONE;
                    }
                } catch (Exception ex) {
                    m_State = -2;
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                m_State = -2;
                mCompletionSource.TrySetResult(false);
                return;

                CONTINUE:
                m_State = 0;
                mCompletionSource.TrySetResult(true);
                return;
            }

            public UniTask DisposeAsync() {
                return m_Enumerator.DisposeAsync();
            }
        }
    }
}