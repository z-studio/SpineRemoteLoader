using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Where<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new Where<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> Where<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new WhereInt<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> WhereAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new WhereAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> WhereAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new WhereIntAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> WhereAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new WhereAwaitWithCancellation<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> WhereAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new WhereIntAwaitWithCancellation<TSource>(source, predicate);
        }
    }

    internal sealed class Where<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, bool> m_Predicate;

        public Where(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerWhere(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class InnerWhere : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, bool> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;

            public InnerWhere(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                if (m_Predicate(Current)) {
                                    goto CONTINUE;
                                } else {
                                    m_State = 0;
                                    goto REPEAT;
                                }
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

    internal sealed class WhereInt<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, bool> m_Predicate;

        public WhereInt(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new Where(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class Where : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, bool> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private Action m_MoveNextAction;
            private int m_Index;

            public Where(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, bool> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                if (m_Predicate(Current, checked(m_Index++))) {
                                    goto CONTINUE;
                                } else {
                                    m_State = 0;
                                    goto REPEAT;
                                }
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

    internal sealed class WhereAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<bool>> m_Predicate;

        public WhereAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerWhereAwait(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class InnerWhereAwait : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, UniTask<bool>> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<bool>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;

            public InnerWhereAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                m_Awaiter2 = m_Predicate(Current).GetAwaiter();

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
                            if (m_Awaiter2.GetResult()) {
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
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

    internal sealed class WhereIntAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, UniTask<bool>> m_Predicate;

        public WhereIntAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new WhereAwait(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class WhereAwait : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, UniTask<bool>> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<bool>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private int m_Index;

            public WhereAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                m_Awaiter2 = m_Predicate(Current, checked(m_Index++)).GetAwaiter();

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
                            if (m_Awaiter2.GetResult()) {
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
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

    internal sealed class WhereAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;

        public WhereAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerWhereAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class InnerWhereAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<bool>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;

            public InnerWhereAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                m_Awaiter2 = m_Predicate(Current, m_CancellationToken).GetAwaiter();

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
                            if (m_Awaiter2.GetResult()) {
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
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

    internal sealed class WhereIntAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;

        public WhereIntAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new WhereAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private sealed class WhereAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private readonly Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private UniTask<bool>.Awaiter m_Awaiter2;
            private Action m_MoveNextAction;
            private int m_Index;

            public WhereAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) {
                m_Source = source;
                m_Predicate = predicate;
                m_CancellationToken = cancellationToken;
                m_MoveNextAction = MoveNext;
                TaskTracker.TrackActiveTask(this, 3);
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
                                Current = m_Enumerator.Current;

                                m_Awaiter2 = m_Predicate(Current, checked(m_Index++), m_CancellationToken).GetAwaiter();

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
                            if (m_Awaiter2.GetResult()) {
                                goto CONTINUE;
                            } else {
                                m_State = 0;
                                goto REPEAT;
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
}