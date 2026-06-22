using System;

namespace Cysharp.Threading.Tasks {
    public abstract class MoveNextSource : IUniTaskSource<bool> {
        protected UniTaskCompletionSourceCore<bool> mCompletionSource;

        public bool GetResult(short token) {
            return mCompletionSource.GetResult(token);
        }

        public UniTaskStatus GetStatus(short token) {
            return mCompletionSource.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token) {
            mCompletionSource.OnCompleted(continuation, state, token);
        }

        public UniTaskStatus UnsafeGetStatus() {
            return mCompletionSource.UnsafeGetStatus();
        }

        void IUniTaskSource.GetResult(short token) {
            mCompletionSource.GetResult(token);
        }

        protected bool TryGetResult<T>(UniTask<T>.Awaiter awaiter, out T result) {
            try {
                result = awaiter.GetResult();
                return true;
            } catch (Exception ex) {
                mCompletionSource.TrySetException(ex);
                result = default;
                return false;
            }
        }

        protected bool TryGetResult(UniTask.Awaiter awaiter) {
            try {
                awaiter.GetResult();
                return true;
            } catch (Exception ex) {
                mCompletionSource.TrySetException(ex);
                return false;
            }
        }
    }
}