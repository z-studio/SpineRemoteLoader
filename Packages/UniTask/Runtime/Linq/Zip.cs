using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(
            this IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));

            return Zip(first, second, (x, y) => (x, y));
        }

        public static IUniTaskAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
            this IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));
            Error.ThrowArgumentNullException(resultSelector, nameof(resultSelector));

            return new Zip<TFirst, TSecond, TResult>(first, second, resultSelector);
        }

        public static IUniTaskAsyncEnumerable<TResult> ZipAwait<TFirst, TSecond, TResult>(
            this IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new ZipAwait<TFirst, TSecond, TResult>(first, second, selector);
        }

        public static IUniTaskAsyncEnumerable<TResult> ZipAwaitWithCancellation<TFirst, TSecond, TResult>(
            this IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, CancellationToken, UniTask<TResult>> selector
        ) {
            Error.ThrowArgumentNullException(first, nameof(first));
            Error.ThrowArgumentNullException(second, nameof(second));
            Error.ThrowArgumentNullException(selector, nameof(selector));

            return new ZipAwaitWithCancellation<TFirst, TSecond, TResult>(first, second, selector);
        }
    }

    internal sealed class Zip<TFirst, TSecond, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
        private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, TResult> m_ResultSelector;

        public Zip(
            IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector
        ) {
            m_First = first;
            m_Second = second;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerZip(m_First, m_Second, m_ResultSelector, cancellationToken);
        }

        private sealed class InnerZip : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_FirstMoveNextCoreDelegate = FirstMoveNextCore;
            private static readonly Action<object> s_SecondMoveNextCoreDelegate = SecondMoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
            private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
            private readonly Func<TFirst, TSecond, TResult> m_ResultSelector;

            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TFirst> m_FirstEnumerator;
            private IUniTaskAsyncEnumerator<TSecond> m_SecondEnumerator;

            private UniTask<bool>.Awaiter m_FirstAwaiter;
            private UniTask<bool>.Awaiter m_SecondAwaiter;

            public InnerZip(
                IUniTaskAsyncEnumerable<TFirst> first,
                IUniTaskAsyncEnumerable<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_First = first;
                m_Second = second;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                if (m_FirstEnumerator == null) {
                    m_FirstEnumerator = m_First.GetAsyncEnumerator(m_CancellationToken);
                    m_SecondEnumerator = m_Second.GetAsyncEnumerator(m_CancellationToken);
                }

                m_FirstAwaiter = m_FirstEnumerator.MoveNextAsync().GetAwaiter();

                if (m_FirstAwaiter.IsCompleted) {
                    FirstMoveNextCore(this);
                } else {
                    m_FirstAwaiter.SourceOnCompleted(s_FirstMoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private static void FirstMoveNextCore(object state) {
                var self = (InnerZip)state;

                if (self.TryGetResult(self.m_FirstAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SecondAwaiter = self.m_SecondEnumerator.MoveNextAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SecondAwaiter.IsCompleted) {
                            SecondMoveNextCore(self);
                        } else {
                            self.m_SecondAwaiter.SourceOnCompleted(s_SecondMoveNextCoreDelegate, self);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SecondMoveNextCore(object state) {
                var self = (InnerZip)state;

                if (self.TryGetResult(self.m_SecondAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.Current = self.m_ResultSelector(
                                self.m_FirstEnumerator.Current,
                                self.m_SecondEnumerator.Current
                            );
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                        }

                        if (self.m_CancellationToken.IsCancellationRequested) {
                            self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
                        } else {
                            self.mCompletionSource.TrySetResult(true);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_FirstEnumerator != null) {
                    await m_FirstEnumerator.DisposeAsync();
                }

                if (m_SecondEnumerator != null) {
                    await m_SecondEnumerator.DisposeAsync();
                }
            }
        }
    }

    internal sealed class ZipAwait<TFirst, TSecond, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
        private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, UniTask<TResult>> m_ResultSelector;

        public ZipAwait(
            IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, UniTask<TResult>> resultSelector
        ) {
            m_First = first;
            m_Second = second;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerZipAwait(m_First, m_Second, m_ResultSelector, cancellationToken);
        }

        private sealed class InnerZipAwait : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_FirstMoveNextCoreDelegate = FirstMoveNextCore;
            private static readonly Action<object> s_SecondMoveNextCoreDelegate = SecondMoveNextCore;
            private static readonly Action<object> s_ResultAwaitCoreDelegate = ResultAwaitCore;

            private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
            private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
            private readonly Func<TFirst, TSecond, UniTask<TResult>> m_ResultSelector;

            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TFirst> m_FirstEnumerator;
            private IUniTaskAsyncEnumerator<TSecond> m_SecondEnumerator;

            private UniTask<bool>.Awaiter m_FirstAwaiter;
            private UniTask<bool>.Awaiter m_SecondAwaiter;
            private UniTask<TResult>.Awaiter m_ResultAwaiter;

            public InnerZipAwait(
                IUniTaskAsyncEnumerable<TFirst> first,
                IUniTaskAsyncEnumerable<TSecond> second,
                Func<TFirst, TSecond, UniTask<TResult>> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_First = first;
                m_Second = second;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                if (m_FirstEnumerator == null) {
                    m_FirstEnumerator = m_First.GetAsyncEnumerator(m_CancellationToken);
                    m_SecondEnumerator = m_Second.GetAsyncEnumerator(m_CancellationToken);
                }

                m_FirstAwaiter = m_FirstEnumerator.MoveNextAsync().GetAwaiter();

                if (m_FirstAwaiter.IsCompleted) {
                    FirstMoveNextCore(this);
                } else {
                    m_FirstAwaiter.SourceOnCompleted(s_FirstMoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private static void FirstMoveNextCore(object state) {
                var self = (InnerZipAwait)state;

                if (self.TryGetResult(self.m_FirstAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SecondAwaiter = self.m_SecondEnumerator.MoveNextAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SecondAwaiter.IsCompleted) {
                            SecondMoveNextCore(self);
                        } else {
                            self.m_SecondAwaiter.SourceOnCompleted(s_SecondMoveNextCoreDelegate, self);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SecondMoveNextCore(object state) {
                var self = (InnerZipAwait)state;

                if (self.TryGetResult(self.m_SecondAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_ResultAwaiter = self.m_ResultSelector(
                                                         self.m_FirstEnumerator.Current,
                                                         self.m_SecondEnumerator.Current
                                                     )
                                                     .GetAwaiter();

                            if (self.m_ResultAwaiter.IsCompleted) {
                                ResultAwaitCore(self);
                            } else {
                                self.m_ResultAwaiter.SourceOnCompleted(s_ResultAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void ResultAwaitCore(object state) {
                var self = (InnerZipAwait)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;

                    if (self.m_CancellationToken.IsCancellationRequested) {
                        self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
                    } else {
                        self.mCompletionSource.TrySetResult(true);
                    }
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_FirstEnumerator != null) {
                    await m_FirstEnumerator.DisposeAsync();
                }

                if (m_SecondEnumerator != null) {
                    await m_SecondEnumerator.DisposeAsync();
                }
            }
        }
    }

    internal sealed class ZipAwaitWithCancellation<TFirst, TSecond, TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
        private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, CancellationToken, UniTask<TResult>> m_ResultSelector;

        public ZipAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TFirst> first,
            IUniTaskAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, CancellationToken, UniTask<TResult>> resultSelector
        ) {
            m_First = first;
            m_Second = second;
            m_ResultSelector = resultSelector;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerZipAwaitWithCancellation(m_First, m_Second, m_ResultSelector, cancellationToken);
        }

        private sealed class InnerZipAwaitWithCancellation : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
            private static readonly Action<object> s_FirstMoveNextCoreDelegate = FirstMoveNextCore;
            private static readonly Action<object> s_SecondMoveNextCoreDelegate = SecondMoveNextCore;
            private static readonly Action<object> s_ResultAwaitCoreDelegate = ResultAwaitCore;

            private readonly IUniTaskAsyncEnumerable<TFirst> m_First;
            private readonly IUniTaskAsyncEnumerable<TSecond> m_Second;
            private readonly Func<TFirst, TSecond, CancellationToken, UniTask<TResult>> m_ResultSelector;

            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TFirst> m_FirstEnumerator;
            private IUniTaskAsyncEnumerator<TSecond> m_SecondEnumerator;

            private UniTask<bool>.Awaiter m_FirstAwaiter;
            private UniTask<bool>.Awaiter m_SecondAwaiter;
            private UniTask<TResult>.Awaiter m_ResultAwaiter;

            public InnerZipAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TFirst> first,
                IUniTaskAsyncEnumerable<TSecond> second,
                Func<TFirst, TSecond, CancellationToken, UniTask<TResult>> resultSelector,
                CancellationToken cancellationToken
            ) {
                m_First = first;
                m_Second = second;
                m_ResultSelector = resultSelector;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TResult Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                mCompletionSource.Reset();

                if (m_FirstEnumerator == null) {
                    m_FirstEnumerator = m_First.GetAsyncEnumerator(m_CancellationToken);
                    m_SecondEnumerator = m_Second.GetAsyncEnumerator(m_CancellationToken);
                }

                m_FirstAwaiter = m_FirstEnumerator.MoveNextAsync().GetAwaiter();

                if (m_FirstAwaiter.IsCompleted) {
                    FirstMoveNextCore(this);
                } else {
                    m_FirstAwaiter.SourceOnCompleted(s_FirstMoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private static void FirstMoveNextCore(object state) {
                var self = (InnerZipAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_FirstAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_SecondAwaiter = self.m_SecondEnumerator.MoveNextAsync().GetAwaiter();
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                            return;
                        }

                        if (self.m_SecondAwaiter.IsCompleted) {
                            SecondMoveNextCore(self);
                        } else {
                            self.m_SecondAwaiter.SourceOnCompleted(s_SecondMoveNextCoreDelegate, self);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void SecondMoveNextCore(object state) {
                var self = (InnerZipAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_SecondAwaiter, out var result)) {
                    if (result) {
                        try {
                            self.m_ResultAwaiter = self.m_ResultSelector(
                                                         self.m_FirstEnumerator.Current,
                                                         self.m_SecondEnumerator.Current,
                                                         self.m_CancellationToken
                                                     )
                                                     .GetAwaiter();

                            if (self.m_ResultAwaiter.IsCompleted) {
                                ResultAwaitCore(self);
                            } else {
                                self.m_ResultAwaiter.SourceOnCompleted(s_ResultAwaitCoreDelegate, self);
                            }
                        } catch (Exception ex) {
                            self.mCompletionSource.TrySetException(ex);
                        }
                    } else {
                        self.mCompletionSource.TrySetResult(false);
                    }
                }
            }

            private static void ResultAwaitCore(object state) {
                var self = (InnerZipAwaitWithCancellation)state;

                if (self.TryGetResult(self.m_ResultAwaiter, out var result)) {
                    self.Current = result;

                    if (self.m_CancellationToken.IsCancellationRequested) {
                        self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
                    } else {
                        self.mCompletionSource.TrySetResult(true);
                    }
                }
            }

            public async UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_FirstEnumerator != null) {
                    await m_FirstEnumerator.DisposeAsync();
                }

                if (m_SecondEnumerator != null) {
                    await m_SecondEnumerator.DisposeAsync();
                }
            }
        }
    }
}