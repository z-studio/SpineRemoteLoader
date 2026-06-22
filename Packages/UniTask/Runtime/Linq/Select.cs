using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TResult> Select<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.Select<TSource, TResult>(source, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> Select<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, TResult> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.SelectInt<TSource, TResult>(source, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.SelectAwait<TSource, TResult>(source, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.SelectIntAwait<TSource, TResult>(source, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectAwaitWithCancellation<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.SelectAwaitWithCancellation<TSource, TResult>(source, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> SelectAwaitWithCancellation<TSource, TResult>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new Cysharp.Threading.Tasks.Linq.SelectIntAwaitWithCancellation<TSource, TResult>(source, selector);
        }
    }

    internal sealed class Select<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, TResult> m_Selector;

        public Select(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, TResult> selector) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelect(m_Source, m_Selector, cancellationToken);
        }

        private sealed class InnerSelect : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, TResult> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;

            public InnerSelect(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, TResult> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                Current = m_Selector(m_Enumerator.Current);
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class SelectInt<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, TResult> m_Selector;

        public SelectInt(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new Select(m_Source, m_Selector, cancellationToken);
        }

        private sealed class Select : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, TResult> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;
            private int m_Index;

            public Select(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, TResult> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                Current = m_Selector(m_Enumerator.Current, checked(m_Index++));
                                goto CONTINUE;
                            } else {
                                goto DONE;
                            }

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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class SelectAwait<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<TResult>> m_Selector;

        public SelectAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask<TResult>> selector) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelectAwait(m_Source, m_Selector, cancellationToken);
        }

        private sealed class InnerSelectAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, UniTask<TResult>> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TResult>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;

            public InnerSelectAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<TResult>> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                m_Awaiter2 = m_Selector(m_Enumerator.Current).GetAwaiter();

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
                            Current = m_Awaiter2.GetResult();
                            goto CONTINUE;
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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class SelectIntAwait<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, UniTask<TResult>> m_Selector;

        public SelectIntAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, UniTask<TResult>> selector) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new SelectAwait(m_Source, m_Selector, cancellationToken);
        }

        private sealed class SelectAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, UniTask<TResult>> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TResult>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private int m_Index;

            public SelectAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, UniTask<TResult>> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                m_Awaiter2 = m_Selector(m_Enumerator.Current, checked(m_Index++)).GetAwaiter();

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
                            Current = m_Awaiter2.GetResult();
                            goto CONTINUE;
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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class SelectAwaitWithCancellation<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<TResult>> m_Selector;

        public SelectAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<TResult>> selector
        ) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSelectAwaitWithCancellation(m_Source, m_Selector, cancellationToken);
        }

        private sealed class InnerSelectAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, CancellationToken, UniTask<TResult>> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TResult>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;

            public InnerSelectAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<TResult>> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                m_Awaiter2 = m_Selector(m_Enumerator.Current, m_CancellationToken).GetAwaiter();

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
                            Current = m_Awaiter2.GetResult();
                            goto CONTINUE;
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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }

    internal sealed class SelectIntAwaitWithCancellation<TSource, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, CancellationToken, UniTask<TResult>> m_Selector;

        public SelectIntAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, UniTask<TResult>> selector
        ) {
            m_Source = source;
            m_Selector = selector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new SelectAwaitWithCancellation(m_Source, m_Selector, cancellationToken);
        }

        private sealed class SelectAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, CancellationToken, UniTask<TResult>> m_Selector;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<TResult>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private int m_Index;

            public SelectAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, UniTask<TResult>> selector,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Selector = selector;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                            m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                            goto case 0;
                        case 0:
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
                                m_Awaiter2 = m_Selector(m_Enumerator.Current, checked(m_Index++), m_CancellationToken)
                                    .GetAwaiter();

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
                            Current = m_Awaiter2.GetResult();
                            goto CONTINUE;
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
                TaskTracker.RemoveTracking(this);
                return m_Enumerator.DisposeAsync();
            }
        }
    }
}