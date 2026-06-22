#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    public partial struct UniTask {
        /// <summary>
        /// If running on mainthread, do nothing. Otherwise, same as UniTask.Yield(PlayerLoopTiming.Update).
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default) {
            return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Update, cancellationToken);
        }

        /// <summary>
        /// If running on mainthread, do nothing. Otherwise, same as UniTask.Yield(timing).
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(
            PlayerLoopTiming timing,
            CancellationToken cancellationToken = default
        ) {
            return new SwitchToMainThreadAwaitable(timing, cancellationToken);
        }

        /// <summary>
        /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default) {
            return new ReturnToMainThread(PlayerLoopTiming.Update, cancellationToken);
        }

        /// <summary>
        /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
        /// </summary>
        public static ReturnToMainThread ReturnToMainThread(
            PlayerLoopTiming timing,
            CancellationToken cancellationToken = default
        ) {
            return new ReturnToMainThread(timing, cancellationToken);
        }

        /// <summary>
        /// Queue the action to PlayerLoop.
        /// </summary>
        public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update) {
            PlayerLoopHelper.AddContinuation(timing, action);
        }

        public static SwitchToThreadPoolAwaitable SwitchToThreadPool() {
            return new SwitchToThreadPoolAwaitable();
        }

        /// <summary>
        /// Note: use SwitchToThreadPool is recommended.
        /// </summary>
        public static SwitchToTaskPoolAwaitable SwitchToTaskPool() {
            return new SwitchToTaskPoolAwaitable();
        }

        public static SwitchToSynchronizationContextAwaitable SwitchToSynchronizationContext(
            SynchronizationContext synchronizationContext,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
            return new SwitchToSynchronizationContextAwaitable(synchronizationContext, cancellationToken);
        }

        public static ReturnToSynchronizationContext ReturnToSynchronizationContext(
            SynchronizationContext synchronizationContext,
            CancellationToken cancellationToken = default
        ) {
            return new ReturnToSynchronizationContext(synchronizationContext, false, cancellationToken);
        }

        public static ReturnToSynchronizationContext ReturnToCurrentSynchronizationContext(
            bool dontPostWhenSameContext = true,
            CancellationToken cancellationToken = default
        ) {
            return new ReturnToSynchronizationContext(
                SynchronizationContext.Current,
                dontPostWhenSameContext,
                cancellationToken
            );
        }
    }

    public struct SwitchToMainThreadAwaitable {
        private readonly PlayerLoopTiming m_PlayerLoopTiming;
        private readonly CancellationToken m_CancellationToken;

        public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken) {
            m_PlayerLoopTiming = playerLoopTiming;
            m_CancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(m_PlayerLoopTiming, m_CancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion {
            private readonly PlayerLoopTiming m_InnerPlayerLoopTiming;
            private readonly CancellationToken m_InnerCancellationToken;

            public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken) {
                m_InnerPlayerLoopTiming = playerLoopTiming;
                m_InnerCancellationToken = cancellationToken;
            }

            public bool IsCompleted {
                get {
                    var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

                    if (PlayerLoopHelper.MainThreadId == currentThreadId) {
                        return true; // run immediate.
                    } else {
                        return false; // register continuation.
                    }
                }
            }

            public void GetResult() {
                m_InnerCancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_InnerPlayerLoopTiming, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_InnerPlayerLoopTiming, continuation);
            }
        }
    }

    public struct ReturnToMainThread {
        private readonly PlayerLoopTiming m_PlayerLoopTiming;
        private readonly CancellationToken m_CancellationToken;

        public ReturnToMainThread(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken) {
            m_PlayerLoopTiming = playerLoopTiming;
            m_CancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync() {
            return new Awaiter(m_PlayerLoopTiming, m_CancellationToken); // run immediate.
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion {
            private readonly PlayerLoopTiming m_Timing;
            private readonly CancellationToken m_InnerCancellationToken;

            public Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken) {
                m_Timing = timing;
                m_InnerCancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

            public bool IsCompleted =>
                PlayerLoopHelper.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;

            public void GetResult() {
                m_InnerCancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_Timing, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_Timing, continuation);
            }
        }
    }

    public struct SwitchToThreadPoolAwaitable {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : ICriticalNotifyCompletion {
            private static readonly WaitCallback s_SwitchToCallback = Callback;

            public bool IsCompleted => false;
            public void GetResult() { }

            public void OnCompleted(Action continuation) {
                ThreadPool.QueueUserWorkItem(s_SwitchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
#if NETCOREAPP3_1
                ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorkItem.Create(continuation), false);
#else
                ThreadPool.UnsafeQueueUserWorkItem(s_SwitchToCallback, continuation);
#endif
            }

            private static void Callback(object state) {
                var continuation = (Action)state;
                continuation();
            }
        }

#if NETCOREAPP3_1
        private sealed class ThreadPoolWorkItem : IThreadPoolWorkItem, ITaskPoolNode<ThreadPoolWorkItem> {
            private static TaskPool<ThreadPoolWorkItem> s_Pool;
            private ThreadPoolWorkItem m_NextNode;
            public ref ThreadPoolWorkItem NextNode => ref m_NextNode;

            static ThreadPoolWorkItem() {
                TaskPool.RegisterSizeGetter(typeof(ThreadPoolWorkItem), () => s_Pool.Size);
            }

            private Action m_Continuation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ThreadPoolWorkItem Create(Action continuation) {
                if (!s_Pool.TryPop(out var item)) {
                    item = new ThreadPoolWorkItem();
                }

                item.m_Continuation = continuation;
                return item;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute() {
                var call = m_Continuation;
                m_Continuation = null;

                if (call != null) {
                    s_Pool.TryPush(this);
                    call.Invoke();
                }
            }
        }

#endif
    }

    public struct SwitchToTaskPoolAwaitable {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : ICriticalNotifyCompletion {
            private static readonly Action<object> s_SwitchToCallback = Callback;

            public bool IsCompleted => false;
            public void GetResult() { }

            public void OnCompleted(Action continuation) {
                Task.Factory.StartNew(
                    s_SwitchToCallback,
                    continuation,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default
                );
            }

            public void UnsafeOnCompleted(Action continuation) {
                Task.Factory.StartNew(
                    s_SwitchToCallback,
                    continuation,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default
                );
            }

            private static void Callback(object state) {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct SwitchToSynchronizationContextAwaitable {
        private readonly SynchronizationContext m_SynchronizationContext;
        private readonly CancellationToken m_CancellationToken;

        public SwitchToSynchronizationContextAwaitable(
            SynchronizationContext synchronizationContext,
            CancellationToken cancellationToken
        ) {
            m_SynchronizationContext = synchronizationContext;
            m_CancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => new Awaiter(m_SynchronizationContext, m_CancellationToken);

        public struct Awaiter : ICriticalNotifyCompletion {
            private static readonly SendOrPostCallback s_SwitchToCallback = Callback;
            private readonly SynchronizationContext m_InnerSynchronizationContext;
            private readonly CancellationToken m_InnerCancellationToken;

            public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken) {
                m_InnerSynchronizationContext = synchronizationContext;
                m_InnerCancellationToken = cancellationToken;
            }

            public bool IsCompleted => false;

            public void GetResult() {
                m_InnerCancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation) {
                m_InnerSynchronizationContext.Post(s_SwitchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                m_InnerSynchronizationContext.Post(s_SwitchToCallback, continuation);
            }

            private static void Callback(object state) {
                var continuation = (Action)state;
                continuation();
            }
        }
    }

    public struct ReturnToSynchronizationContext {
        private readonly SynchronizationContext m_SyncContext;
        private readonly bool m_DontPostWhenSameContext;
        private readonly CancellationToken m_CancellationToken;

        public ReturnToSynchronizationContext(
            SynchronizationContext syncContext,
            bool dontPostWhenSameContext,
            CancellationToken cancellationToken
        ) {
            m_SyncContext = syncContext;
            m_DontPostWhenSameContext = dontPostWhenSameContext;
            m_CancellationToken = cancellationToken;
        }

        public Awaiter DisposeAsync() {
            return new Awaiter(m_SyncContext, m_DontPostWhenSameContext, m_CancellationToken);
        }

        public struct Awaiter : ICriticalNotifyCompletion {
            private static readonly SendOrPostCallback s_SwitchToCallback = Callback;

            private readonly SynchronizationContext m_SynchronizationContext;
            private readonly bool m_InnerDontPostWhenSameContext;
            private readonly CancellationToken m_InnerCancellationToken;

            public Awaiter(
                SynchronizationContext synchronizationContext,
                bool dontPostWhenSameContext,
                CancellationToken cancellationToken
            ) {
                m_SynchronizationContext = synchronizationContext;
                m_InnerDontPostWhenSameContext = dontPostWhenSameContext;
                m_InnerCancellationToken = cancellationToken;
            }

            public Awaiter GetAwaiter() => this;

            public bool IsCompleted {
                get {
                    if (!m_InnerDontPostWhenSameContext) {
                        return false;
                    }

                    var current = SynchronizationContext.Current;

                    if (current == m_SynchronizationContext) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }

            public void GetResult() {
                m_InnerCancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation) {
                m_SynchronizationContext.Post(s_SwitchToCallback, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                m_SynchronizationContext.Post(s_SwitchToCallback, continuation);
            }

            private static void Callback(object state) {
                var continuation = (Action)state;
                continuation();
            }
        }
    }
}