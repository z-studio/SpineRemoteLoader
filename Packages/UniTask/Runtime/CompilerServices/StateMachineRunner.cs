#pragma warning disable CS1591

using Cysharp.Threading.Tasks.Internal;
using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cysharp.Threading.Tasks.CompilerServices {
    // #ENABLE_IL2CPP in this file is to avoid bug of IL2CPP VM.
    // Issue is tracked on https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
    // but currently it is labeled `Won't Fix`.

    internal interface IStateMachineRunner {
        Action MoveNext { get; }
        void Return();

#if ENABLE_IL2CPP
        Action ReturnAction { get; }
#endif
    }

    internal interface IStateMachineRunnerPromise : IUniTaskSource {
        Action MoveNext { get; }
        UniTask Task { get; }
        void SetResult();
        void SetException(Exception exception);
    }

    internal interface IStateMachineRunnerPromise<T> : IUniTaskSource<T> {
        Action MoveNext { get; }
        UniTask<T> Task { get; }
        void SetResult(T result);
        void SetException(Exception exception);
    }

    internal static class StateMachineUtility {
        // Get AsyncStateMachine internal state to check IL2CPP bug
        public static int GetState(IAsyncStateMachine stateMachine) {
            var info = stateMachine.GetType()
                                   .GetFields(
                                       System.Reflection.BindingFlags.Public
                                       | System.Reflection.BindingFlags.NonPublic
                                       | System.Reflection.BindingFlags.Instance
                                   )
                                   .First(x => x.Name.EndsWith("__state"));

            return (int)info.GetValue(stateMachine);
        }
    }

    internal sealed class AsyncUniTaskVoid<TStateMachine> : IStateMachineRunner,
                                                            ITaskPoolNode<AsyncUniTaskVoid<TStateMachine>>,
                                                            IUniTaskSource
        where TStateMachine : IAsyncStateMachine {
        private static TaskPool<AsyncUniTaskVoid<TStateMachine>> s_Pool;

#if ENABLE_IL2CPP
        public Action ReturnAction { get; }
#endif

        private TStateMachine m_StateMachine;

        public Action MoveNext { get; }

        public AsyncUniTaskVoid() {
            MoveNext = Run;
#if ENABLE_IL2CPP
            ReturnAction = Return;
#endif
        }

        public static void SetStateMachine(ref TStateMachine stateMachine, ref IStateMachineRunner runnerFieldRef) {
            if (!s_Pool.TryPop(out var result)) {
                result = new AsyncUniTaskVoid<TStateMachine>();
            }

            TaskTracker.TrackActiveTask(result, 3);

            runnerFieldRef = result; // set runner before copied.
            result.m_StateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        static AsyncUniTaskVoid() {
            TaskPool.RegisterSizeGetter(typeof(AsyncUniTaskVoid<TStateMachine>), () => s_Pool.Size);
        }

        private AsyncUniTaskVoid<TStateMachine> m_NextNode;
        public ref AsyncUniTaskVoid<TStateMachine> NextNode => ref m_NextNode;

        public void Return() {
            TaskTracker.RemoveTracking(this);
            m_StateMachine = default;
            s_Pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run() {
            m_StateMachine.MoveNext();
        }

        // dummy interface implementation for TaskTracker.

        UniTaskStatus IUniTaskSource.GetStatus(short token) {
            return UniTaskStatus.Pending;
        }

        UniTaskStatus IUniTaskSource.UnsafeGetStatus() {
            return UniTaskStatus.Pending;
        }

        void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token) { }

        void IUniTaskSource.GetResult(short token) { }
    }

    internal sealed class AsyncUniTask<TStateMachine> : IStateMachineRunnerPromise,
                                                        IUniTaskSource,
                                                        ITaskPoolNode<AsyncUniTask<TStateMachine>>
        where TStateMachine : IAsyncStateMachine {
        private static TaskPool<AsyncUniTask<TStateMachine>> s_Pool;

#if ENABLE_IL2CPP
        readonly Action returnDelegate;
#endif
        public Action MoveNext { get; }

        private TStateMachine m_StateMachine;
        private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

        AsyncUniTask() {
            MoveNext = Run;
#if ENABLE_IL2CPP
            returnDelegate = Return;
#endif
        }

        public static void SetStateMachine(
            ref TStateMachine stateMachine,
            ref IStateMachineRunnerPromise runnerPromiseFieldRef
        ) {
            if (!s_Pool.TryPop(out var result)) {
                result = new AsyncUniTask<TStateMachine>();
            }

            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.m_StateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        private AsyncUniTask<TStateMachine> m_NextNode;
        public ref AsyncUniTask<TStateMachine> NextNode => ref m_NextNode;

        static AsyncUniTask() {
            TaskPool.RegisterSizeGetter(typeof(AsyncUniTask<TStateMachine>), () => s_Pool.Size);
        }

        private void Return() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            m_StateMachine = default;
            s_Pool.TryPush(this);
        }

        private bool TryReturn() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            m_StateMachine = default;
            return s_Pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run() {
            m_StateMachine.MoveNext();
        }

        public UniTask Task {
            [DebuggerHidden]
            get => new(this, m_Core.Version);
        }

        [DebuggerHidden]
        public void SetResult() {
            m_Core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public void SetException(Exception exception) {
            m_Core.TrySetException(exception);
        }

        [DebuggerHidden]
        public void GetResult(short token) {
            try {
                m_Core.GetResult(token);
            } finally {
#if ENABLE_IL2CPP
                // workaround for IL2CPP bug.
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.LastPostLateUpdate, returnDelegate);
#else
                TryReturn();
#endif
            }
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }
    }

    internal sealed class AsyncUniTask<TStateMachine, T> : IStateMachineRunnerPromise<T>,
                                                           IUniTaskSource<T>,
                                                           ITaskPoolNode<AsyncUniTask<TStateMachine, T>>
        where TStateMachine : IAsyncStateMachine {
        private static TaskPool<AsyncUniTask<TStateMachine, T>> s_Pool;

#if ENABLE_IL2CPP
        readonly Action returnDelegate;
#endif

        public Action MoveNext { get; }

        private TStateMachine m_StateMachine;
        private UniTaskCompletionSourceCore<T> m_Core;

        private AsyncUniTask() {
            MoveNext = Run;
#if ENABLE_IL2CPP
            returnDelegate = Return;
#endif
        }

        public static void SetStateMachine(
            ref TStateMachine stateMachine,
            ref IStateMachineRunnerPromise<T> runnerPromiseFieldRef
        ) {
            if (!s_Pool.TryPop(out var result)) {
                result = new AsyncUniTask<TStateMachine, T>();
            }

            TaskTracker.TrackActiveTask(result, 3);

            runnerPromiseFieldRef = result; // set runner before copied.
            result.m_StateMachine = stateMachine; // copy struct StateMachine(in release build).
        }

        private AsyncUniTask<TStateMachine, T> m_NextNode;
        public ref AsyncUniTask<TStateMachine, T> NextNode => ref m_NextNode;

        static AsyncUniTask() {
            TaskPool.RegisterSizeGetter(typeof(AsyncUniTask<TStateMachine, T>), () => s_Pool.Size);
        }

        private void Return() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            m_StateMachine = default;
            s_Pool.TryPush(this);
        }

        private bool TryReturn() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            m_StateMachine = default;
            return s_Pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run() {
            // UnityEngine.Debug.Log($"MoveNext State:" + StateMachineUtility.GetState(stateMachine));
            m_StateMachine.MoveNext();
        }

        public UniTask<T> Task {
            [DebuggerHidden]
            get => new(this, m_Core.Version);
        }

        [DebuggerHidden]
        public void SetResult(T result) {
            m_Core.TrySetResult(result);
        }

        [DebuggerHidden]
        public void SetException(Exception exception) {
            m_Core.TrySetException(exception);
        }

        [DebuggerHidden]
        public T GetResult(short token) {
            try {
                return m_Core.GetResult(token);
            } finally {
#if ENABLE_IL2CPP
                // workaround for IL2CPP bug.
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.LastPostLateUpdate, returnDelegate);
#else
                TryReturn();
#endif
            }
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token) {
            GetResult(token);
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }
    }
}