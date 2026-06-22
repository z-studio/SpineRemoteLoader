#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public class AsyncLazy {
        private static Action<object> s_Continuation = SetCompletionSource;

        private Func<UniTask> m_TaskFactory;
        private UniTaskCompletionSource m_CompletionSource;
        private UniTask.Awaiter m_Awaiter;

        private object m_SyncLock;
        private bool m_Initialized;

        public AsyncLazy(Func<UniTask> taskFactory) {
            m_TaskFactory = taskFactory;
            m_CompletionSource = new UniTaskCompletionSource();
            m_SyncLock = new object();
            m_Initialized = false;
        }

        internal AsyncLazy(UniTask task) {
            m_TaskFactory = null;
            m_CompletionSource = new UniTaskCompletionSource();
            m_SyncLock = null;
            m_Initialized = true;

            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted) {
                SetCompletionSource(awaiter);
            } else {
                m_Awaiter = awaiter;
                awaiter.SourceOnCompleted(s_Continuation, this);
            }
        }

        public UniTask Task {
            get {
                EnsureInitialized();
                return m_CompletionSource.Task;
            }
        }

        public UniTask.Awaiter GetAwaiter() => Task.GetAwaiter();

        private void EnsureInitialized() {
            if (Volatile.Read(ref m_Initialized)) {
                return;
            }

            EnsureInitializedCore();
        }

        private void EnsureInitializedCore() {
            lock (m_SyncLock) {
                if (!Volatile.Read(ref m_Initialized)) {
                    var f = Interlocked.Exchange(ref m_TaskFactory, null);

                    if (f != null) {
                        var task = f();
                        var awaiter = task.GetAwaiter();

                        if (awaiter.IsCompleted) {
                            SetCompletionSource(awaiter);
                        } else {
                            m_Awaiter = awaiter;
                            awaiter.SourceOnCompleted(s_Continuation, this);
                        }

                        Volatile.Write(ref m_Initialized, true);
                    }
                }
            }
        }

        private void SetCompletionSource(in UniTask.Awaiter awaiter) {
            try {
                awaiter.GetResult();
                m_CompletionSource.TrySetResult();
            } catch (Exception ex) {
                m_CompletionSource.TrySetException(ex);
            }
        }

        private static void SetCompletionSource(object state) {
            var self = (AsyncLazy)state;

            try {
                self.m_Awaiter.GetResult();
                self.m_CompletionSource.TrySetResult();
            } catch (Exception ex) {
                self.m_CompletionSource.TrySetException(ex);
            } finally {
                self.m_Awaiter = default;
            }
        }
    }

    public class AsyncLazy<T> {
        private static Action<object> s_Continuation = SetCompletionSource;

        private Func<UniTask<T>> m_TaskFactory;
        private UniTaskCompletionSource<T> m_CompletionSource;
        private UniTask<T>.Awaiter m_Awaiter;

        private object m_SyncLock;
        private bool m_Initialized;

        public AsyncLazy(Func<UniTask<T>> taskFactory) {
            m_TaskFactory = taskFactory;
            m_CompletionSource = new UniTaskCompletionSource<T>();
            m_SyncLock = new object();
            m_Initialized = false;
        }

        internal AsyncLazy(UniTask<T> task) {
            m_TaskFactory = null;
            m_CompletionSource = new UniTaskCompletionSource<T>();
            m_SyncLock = null;
            m_Initialized = true;

            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted) {
                SetCompletionSource(awaiter);
            } else {
                m_Awaiter = awaiter;
                awaiter.SourceOnCompleted(s_Continuation, this);
            }
        }

        public UniTask<T> Task {
            get {
                EnsureInitialized();
                return m_CompletionSource.Task;
            }
        }

        public UniTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

        private void EnsureInitialized() {
            if (Volatile.Read(ref m_Initialized)) {
                return;
            }

            EnsureInitializedCore();
        }

        private void EnsureInitializedCore() {
            lock (m_SyncLock) {
                if (!Volatile.Read(ref m_Initialized)) {
                    var f = Interlocked.Exchange(ref m_TaskFactory, null);

                    if (f != null) {
                        var task = f();
                        var awaiter = task.GetAwaiter();

                        if (awaiter.IsCompleted) {
                            SetCompletionSource(awaiter);
                        } else {
                            m_Awaiter = awaiter;
                            awaiter.SourceOnCompleted(s_Continuation, this);
                        }

                        Volatile.Write(ref m_Initialized, true);
                    }
                }
            }
        }

        private void SetCompletionSource(in UniTask<T>.Awaiter awaiter) {
            try {
                var result = awaiter.GetResult();
                m_CompletionSource.TrySetResult(result);
            } catch (Exception ex) {
                m_CompletionSource.TrySetException(ex);
            }
        }

        private static void SetCompletionSource(object state) {
            var self = (AsyncLazy<T>)state;

            try {
                var result = self.m_Awaiter.GetResult();
                self.m_CompletionSource.TrySetResult(result);
            } catch (Exception ex) {
                self.m_CompletionSource.TrySetException(ex);
            } finally {
                self.m_Awaiter = default;
            }
        }
    }
}