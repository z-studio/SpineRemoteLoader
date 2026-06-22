#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    public partial struct UniTask {
        public static UniTask WaitUntil(
            Func<bool> predicate,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return new UniTask(
                WaitUntilPromise.Create(predicate, timing, cancellationToken, cancelImmediately, out var token),
                token
            );
        }

        public static UniTask WaitUntil<T>(
            T state,
            Func<T, bool> predicate,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return new UniTask(
                WaitUntilPromise<T>.Create(
                    state,
                    predicate,
                    timing,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        public static UniTask WaitWhile(
            Func<bool> predicate,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return new UniTask(
                WaitWhilePromise.Create(predicate, timing, cancellationToken, cancelImmediately, out var token),
                token
            );
        }

        public static UniTask WaitWhile<T>(
            T state,
            Func<T, bool> predicate,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return new UniTask(
                WaitWhilePromise<T>.Create(
                    state,
                    predicate,
                    timing,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        public static UniTask WaitUntilCanceled(
            CancellationToken cancellationToken,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            bool completeImmediately = false
        ) {
            return new UniTask(
                WaitUntilCanceledPromise.Create(cancellationToken, timing, completeImmediately, out var token),
                token
            );
        }

        public static UniTask<U> WaitUntilValueChanged<T, U>(
            T target,
            Func<T, U> monitorFunction,
            PlayerLoopTiming monitorTiming = PlayerLoopTiming.Update,
            IEqualityComparer<U> equalityComparer = null,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        )
            where T : class {
            var unityObject = target as UnityEngine.Object;
            var isUnityObject = target is UnityEngine.Object; // don't use (unityObject == null)

            return new UniTask<U>(
                isUnityObject
                    ? WaitUntilValueChangedUnityObjectPromise<T, U>.Create(
                        target,
                        monitorFunction,
                        equalityComparer,
                        monitorTiming,
                        cancellationToken,
                        cancelImmediately,
                        out var token
                    )
                    : WaitUntilValueChangedStandardObjectPromise<T, U>.Create(
                        target,
                        monitorFunction,
                        equalityComparer,
                        monitorTiming,
                        cancellationToken,
                        cancelImmediately,
                        out token
                    ),
                token
            );
        }

        private sealed class WaitUntilPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise> {
            private static TaskPool<WaitUntilPromise> s_Pool;
            private WaitUntilPromise m_NextNode;
            public ref WaitUntilPromise NextNode => ref m_NextNode;

            static WaitUntilPromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise), () => s_Pool.Size);
            }

            private Func<bool> m_Predicate;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private WaitUntilPromise() { }

            public static IUniTaskSource Create(
                Func<bool> predicate,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitUntilPromise();
                }

                result.m_Predicate = predicate;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitUntilPromise)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                try {
                    if (!m_Predicate()) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(null);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Predicate = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class WaitUntilPromise<T> : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilPromise<T>> {
            private static TaskPool<WaitUntilPromise<T>> s_Pool;
            private WaitUntilPromise<T> m_NextNode;
            public ref WaitUntilPromise<T> NextNode => ref m_NextNode;

            static WaitUntilPromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise<T>), () => s_Pool.Size);
            }

            private Func<T, bool> m_Predicate;
            private T m_Argument;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private WaitUntilPromise() { }

            public static IUniTaskSource Create(
                T argument,
                Func<T, bool> predicate,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitUntilPromise<T>();
                }

                result.m_Predicate = predicate;
                result.m_Argument = argument;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitUntilPromise<T>)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                try {
                    if (!m_Predicate(m_Argument)) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(null);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Predicate = default;
                m_Argument = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class WaitWhilePromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise> {
            private static TaskPool<WaitWhilePromise> s_Pool;
            private WaitWhilePromise m_NextNode;
            public ref WaitWhilePromise NextNode => ref m_NextNode;

            static WaitWhilePromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise), () => s_Pool.Size);
            }

            private Func<bool> m_Predicate;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private WaitWhilePromise() { }

            public static IUniTaskSource Create(
                Func<bool> predicate,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitWhilePromise();
                }

                result.m_Predicate = predicate;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitWhilePromise)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                try {
                    if (m_Predicate()) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(null);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Predicate = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class WaitWhilePromise<T> : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitWhilePromise<T>> {
            private static TaskPool<WaitWhilePromise<T>> s_Pool;
            private WaitWhilePromise<T> m_NextNode;
            public ref WaitWhilePromise<T> NextNode => ref m_NextNode;

            static WaitWhilePromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise<T>), () => s_Pool.Size);
            }

            private Func<T, bool> m_Predicate;
            private T m_Argument;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private WaitWhilePromise() { }

            public static IUniTaskSource Create(
                T argument,
                Func<T, bool> predicate,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitWhilePromise<T>();
                }

                result.m_Predicate = predicate;
                result.m_Argument = argument;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitWhilePromise<T>)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                try {
                    if (m_Predicate(m_Argument)) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(null);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Predicate = default;
                m_Argument = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class
            WaitUntilCanceledPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<WaitUntilCanceledPromise> {
            private static TaskPool<WaitUntilCanceledPromise> s_Pool;
            private WaitUntilCanceledPromise m_NextNode;
            public ref WaitUntilCanceledPromise NextNode => ref m_NextNode;

            static WaitUntilCanceledPromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => s_Pool.Size);
            }

            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private WaitUntilCanceledPromise() { }

            public static IUniTaskSource Create(
                CancellationToken cancellationToken,
                PlayerLoopTiming timing,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitUntilCanceledPromise();
                }

                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitUntilCanceledPromise)state;
                            promise.m_Core.TrySetResult(null);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetResult(null);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        // where T : UnityEngine.Object, can not add constraint
        private sealed class WaitUntilValueChangedUnityObjectPromise<T, U> : IUniTaskSource<U>,
                                                                             IPlayerLoopItem,
                                                                             ITaskPoolNode<
                                                                                 WaitUntilValueChangedUnityObjectPromise<T,
                                                                                     U>> {
            private static TaskPool<WaitUntilValueChangedUnityObjectPromise<T, U>> s_Pool;
            private WaitUntilValueChangedUnityObjectPromise<T, U> m_NextNode;
            public ref WaitUntilValueChangedUnityObjectPromise<T, U> NextNode => ref m_NextNode;

            static WaitUntilValueChangedUnityObjectPromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedUnityObjectPromise<T, U>), () => s_Pool.Size);
            }

            private T m_Target;
            private UnityEngine.Object m_TargetAsUnityObject;
            private U m_CurrentValue;
            private Func<T, U> m_MonitorFunction;
            private IEqualityComparer<U> m_EqualityComparer;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<U> m_Core;

            private WaitUntilValueChangedUnityObjectPromise() { }

            public static IUniTaskSource<U> Create(
                T target,
                Func<T, U> monitorFunction,
                IEqualityComparer<U> equalityComparer,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitUntilValueChangedUnityObjectPromise<T, U>();
                }

                result.m_Target = target;
                result.m_TargetAsUnityObject = target as UnityEngine.Object;
                result.m_MonitorFunction = monitorFunction;
                result.m_CurrentValue = monitorFunction(target);
                result.m_EqualityComparer = equalityComparer ?? UnityEqualityComparer.GetDefault<U>();
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitUntilValueChangedUnityObjectPromise<T, U>)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public U GetResult(short token) {
                try {
                    return m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested || m_TargetAsUnityObject == null) // destroyed = cancel.
                {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                U nextValue = default(U);

                try {
                    nextValue = m_MonitorFunction(m_Target);

                    if (m_EqualityComparer.Equals(m_CurrentValue, nextValue)) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(nextValue);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Target = default;
                m_CurrentValue = default;
                m_MonitorFunction = default;
                m_EqualityComparer = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class WaitUntilValueChangedStandardObjectPromise<T, U> : IUniTaskSource<U>,
                                                                                IPlayerLoopItem,
                                                                                ITaskPoolNode<
                                                                                    WaitUntilValueChangedStandardObjectPromise<T
                                                                                      , U>>
            where T : class {
            private static TaskPool<WaitUntilValueChangedStandardObjectPromise<T, U>> s_Pool;
            private WaitUntilValueChangedStandardObjectPromise<T, U> m_NextNode;
            public ref WaitUntilValueChangedStandardObjectPromise<T, U> NextNode => ref m_NextNode;

            static WaitUntilValueChangedStandardObjectPromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedStandardObjectPromise<T, U>), () => s_Pool.Size);
            }

            private WeakReference<T> m_Target;
            private U m_CurrentValue;
            private Func<T, U> m_MonitorFunction;
            private IEqualityComparer<U> m_EqualityComparer;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<U> m_Core;

            private WaitUntilValueChangedStandardObjectPromise() { }

            public static IUniTaskSource<U> Create(
                T target,
                Func<T, U> monitorFunction,
                IEqualityComparer<U> equalityComparer,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitUntilValueChangedStandardObjectPromise<T, U>();
                }

                result.m_Target = new WeakReference<T>(target, false); // wrap in WeakReference.
                result.m_MonitorFunction = monitorFunction;
                result.m_CurrentValue = monitorFunction(target);
                result.m_EqualityComparer = equalityComparer ?? UnityEqualityComparer.GetDefault<U>();
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitUntilValueChangedStandardObjectPromise<T, U>)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public U GetResult(short token) {
                try {
                    return m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested
                    || !m_Target.TryGetTarget(out var t)) // doesn't find = cancel.
                {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                U nextValue = default(U);

                try {
                    nextValue = m_MonitorFunction(t);

                    if (m_EqualityComparer.Equals(m_CurrentValue, nextValue)) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_Core.TrySetResult(nextValue);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Target = default;
                m_CurrentValue = default;
                m_MonitorFunction = default;
                m_EqualityComparer = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }
    }
}