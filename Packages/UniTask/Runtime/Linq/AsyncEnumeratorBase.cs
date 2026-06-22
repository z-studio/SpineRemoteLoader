using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    // note: refactor all inherit class and should remove this.
    // see Select and Where.
    internal abstract class AsyncEnumeratorBase<TSource, TResult> : MoveNextSource, IUniTaskAsyncEnumerator<TResult> {
        private static readonly Action<object> s_MoveNextCallbackDelegate = MoveNextCallBack;

        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        protected CancellationToken mCancellationToken;

        private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
        private UniTask<bool>.Awaiter m_SourceMoveNext;

        public AsyncEnumeratorBase(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
            m_Source = source;
            mCancellationToken = cancellationToken;
            TaskTracker.TrackActiveTask(this, 4);
        }

        // abstract

        /// <summary>
        /// If return value is false, continue source.MoveNext.
        /// </summary>
        protected abstract bool TryMoveNextCore(bool sourceHasCurrent, out bool result);

        // Util
        protected TSource SourceCurrent => m_Enumerator.Current;

        // IUniTaskAsyncEnumerator<T>

        public TResult Current { get; protected set; }

        public UniTask<bool> MoveNextAsync() {
            if (m_Enumerator == null) {
                m_Enumerator = m_Source.GetAsyncEnumerator(mCancellationToken);
            }

            mCompletionSource.Reset();

            if (!OnFirstIteration()) {
                SourceMoveNext();
            }

            return new UniTask<bool>(this, mCompletionSource.Version);
        }

        protected virtual bool OnFirstIteration() {
            return false;
        }

        protected void SourceMoveNext() {
            CONTINUE:
            m_SourceMoveNext = m_Enumerator.MoveNextAsync().GetAwaiter();

            if (m_SourceMoveNext.IsCompleted) {
                bool result = false;

                try {
                    if (!TryMoveNextCore(m_SourceMoveNext.GetResult(), out result)) {
                        goto CONTINUE;
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                if (mCancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(mCancellationToken);
                } else {
                    mCompletionSource.TrySetResult(result);
                }
            } else {
                m_SourceMoveNext.SourceOnCompleted(s_MoveNextCallbackDelegate, this);
            }
        }

        private static void MoveNextCallBack(object state) {
            var self = (AsyncEnumeratorBase<TSource, TResult>)state;
            bool result;

            try {
                if (!self.TryMoveNextCore(self.m_SourceMoveNext.GetResult(), out result)) {
                    self.SourceMoveNext();
                    return;
                }
            } catch (Exception ex) {
                self.mCompletionSource.TrySetException(ex);
                return;
            }

            if (self.mCancellationToken.IsCancellationRequested) {
                self.mCompletionSource.TrySetCanceled(self.mCancellationToken);
            } else {
                self.mCompletionSource.TrySetResult(result);
            }
        }

        // if require additional resource to dispose, override and call base.DisposeAsync.
        public virtual UniTask DisposeAsync() {
            TaskTracker.RemoveTracking(this);

            if (m_Enumerator != null) {
                return m_Enumerator.DisposeAsync();
            }

            return default;
        }
    }

    internal abstract class AsyncEnumeratorAwaitSelectorBase<TSource, TResult, TAwait> : MoveNextSource,
        IUniTaskAsyncEnumerator<TResult> {
        private static readonly Action<object> s_MoveNextCallbackDelegate = MoveNextCallBack;
        private static readonly Action<object> s_SetCurrentCallbackDelegate = SetCurrentCallBack;

        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        protected CancellationToken cancellationToken;

        private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
        private UniTask<bool>.Awaiter m_SourceMoveNext;

        private UniTask<TAwait>.Awaiter m_ResultAwaiter;

        public AsyncEnumeratorAwaitSelectorBase(
            IUniTaskAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken
        ) {
            m_Source = source;
            this.cancellationToken = cancellationToken;
            TaskTracker.TrackActiveTask(this, 4);
        }

        // abstract

        protected abstract UniTask<TAwait> TransformAsync(TSource sourceCurrent);
        protected abstract bool TrySetCurrentCore(TAwait awaitResult, out bool terminateIteration);

        // Util
        protected TSource SourceCurrent { get; private set; }

        protected (bool waitCallback, bool requireNextIteration) ActionCompleted(
            bool trySetCurrentResult,
            out bool moveNextResult
        ) {
            if (trySetCurrentResult) {
                moveNextResult = true;
                return (false, false);
            } else {
                moveNextResult = default;
                return (false, true);
            }
        }

        protected (bool waitCallback, bool requireNextIteration) WaitAwaitCallback(out bool moveNextResult) {
            moveNextResult = default;
            return (true, false);
        }

        protected (bool waitCallback, bool requireNextIteration) IterateFinished(out bool moveNextResult) {
            moveNextResult = false;
            return (false, false);
        }

        // IUniTaskAsyncEnumerator<T>

        public TResult Current { get; protected set; }

        public UniTask<bool> MoveNextAsync() {
            if (m_Enumerator == null) {
                m_Enumerator = m_Source.GetAsyncEnumerator(cancellationToken);
            }

            mCompletionSource.Reset();
            SourceMoveNext();
            return new UniTask<bool>(this, mCompletionSource.Version);
        }

        protected void SourceMoveNext() {
            CONTINUE:
            m_SourceMoveNext = m_Enumerator.MoveNextAsync().GetAwaiter();

            if (m_SourceMoveNext.IsCompleted) {
                bool result = false;

                try {
                    (bool waitCallback, bool requireNextIteration) = TryMoveNextCore(
                        m_SourceMoveNext.GetResult(),
                        out result
                    );

                    if (waitCallback) {
                        return;
                    }

                    if (requireNextIteration) {
                        goto CONTINUE;
                    } else {
                        mCompletionSource.TrySetResult(result);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    return;
                }
            } else {
                m_SourceMoveNext.SourceOnCompleted(s_MoveNextCallbackDelegate, this);
            }
        }

        (bool waitCallback, bool requireNextIteration) TryMoveNextCore(bool sourceHasCurrent, out bool result) {
            if (sourceHasCurrent) {
                SourceCurrent = m_Enumerator.Current;
                var task = TransformAsync(SourceCurrent);

                if (UnwarapTask(task, out var taskResult)) {
                    var currentResult = TrySetCurrentCore(taskResult, out var terminateIteration);

                    if (terminateIteration) {
                        return IterateFinished(out result);
                    }

                    return ActionCompleted(currentResult, out result);
                } else {
                    return WaitAwaitCallback(out result);
                }
            }

            return IterateFinished(out result);
        }

        protected bool UnwarapTask(UniTask<TAwait> taskResult, out TAwait result) {
            m_ResultAwaiter = taskResult.GetAwaiter();

            if (m_ResultAwaiter.IsCompleted) {
                result = m_ResultAwaiter.GetResult();
                return true;
            } else {
                m_ResultAwaiter.SourceOnCompleted(s_SetCurrentCallbackDelegate, this);
                result = default;
                return false;
            }
        }

        private static void MoveNextCallBack(object state) {
            var self = (AsyncEnumeratorAwaitSelectorBase<TSource, TResult, TAwait>)state;
            bool result = false;

            try {
                (bool waitCallback, bool requireNextIteration) = self.TryMoveNextCore(
                    self.m_SourceMoveNext.GetResult(),
                    out result
                );

                if (waitCallback) {
                    return;
                }

                if (requireNextIteration) {
                    self.SourceMoveNext();
                    return;
                } else {
                    self.mCompletionSource.TrySetResult(result);
                }
            } catch (Exception ex) {
                self.mCompletionSource.TrySetException(ex);
                return;
            }
        }

        private static void SetCurrentCallBack(object state) {
            var self = (AsyncEnumeratorAwaitSelectorBase<TSource, TResult, TAwait>)state;

            bool doneSetCurrent;
            bool terminateIteration;

            try {
                var result = self.m_ResultAwaiter.GetResult();
                doneSetCurrent = self.TrySetCurrentCore(result, out terminateIteration);
            } catch (Exception ex) {
                self.mCompletionSource.TrySetException(ex);
                return;
            }

            if (self.cancellationToken.IsCancellationRequested) {
                self.mCompletionSource.TrySetCanceled(self.cancellationToken);
            } else {
                if (doneSetCurrent) {
                    self.mCompletionSource.TrySetResult(true);
                } else {
                    if (terminateIteration) {
                        self.mCompletionSource.TrySetResult(false);
                    } else {
                        self.SourceMoveNext();
                    }
                }
            }
        }

        // if require additional resource to dispose, override and call base.DisposeAsync.
        public virtual UniTask DisposeAsync() {
            TaskTracker.RemoveTracking(this);

            if (m_Enumerator != null) {
                return m_Enumerator.DisposeAsync();
            }

            return default;
        }
    }
}