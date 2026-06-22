#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks {
    public enum DelayType {
        /// <summary>use Time.deltaTime.</summary>
        DeltaTime,

        /// <summary>Ignore timescale, use Time.unscaledDeltaTime.</summary>
        UnscaledDeltaTime,

        /// <summary>use Stopwatch.GetTimestamp().</summary>
        Realtime
    }

    public partial struct UniTask {
        public static YieldAwaitable Yield() {
            // optimized for single continuation
            return new YieldAwaitable(PlayerLoopTiming.Update);
        }

        public static YieldAwaitable Yield(PlayerLoopTiming timing) {
            // optimized for single continuation
            return new YieldAwaitable(timing);
        }

        public static UniTask Yield(CancellationToken cancellationToken, bool cancelImmediately = false) {
            return new UniTask(
                YieldPromise.Create(PlayerLoopTiming.Update, cancellationToken, cancelImmediately, out var token),
                token
            );
        }

        public static UniTask Yield(
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            bool cancelImmediately = false
        ) {
            return new UniTask(YieldPromise.Create(timing, cancellationToken, cancelImmediately, out var token), token);
        }

        /// <summary>
        /// Similar as UniTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static UniTask NextFrame() {
            return new UniTask(
                NextFramePromise.Create(PlayerLoopTiming.Update, CancellationToken.None, false, out var token),
                token
            );
        }

        /// <summary>
        /// Similar as UniTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static UniTask NextFrame(PlayerLoopTiming timing) {
            return new UniTask(NextFramePromise.Create(timing, CancellationToken.None, false, out var token), token);
        }

        /// <summary>
        /// Similar as UniTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static UniTask NextFrame(CancellationToken cancellationToken, bool cancelImmediately = false) {
            return new UniTask(
                NextFramePromise.Create(PlayerLoopTiming.Update, cancellationToken, cancelImmediately, out var token),
                token
            );
        }

        /// <summary>
        /// Similar as UniTask.Yield but guaranteed run on next frame.
        /// </summary>
        public static UniTask NextFrame(
            PlayerLoopTiming timing,
            CancellationToken cancellationToken,
            bool cancelImmediately = false
        ) {
            return new UniTask(
                NextFramePromise.Create(timing, cancellationToken, cancelImmediately, out var token),
                token
            );
        }

        public static async UniTask WaitForEndOfFrame(CancellationToken cancellationToken = default) {
            await Awaitable.EndOfFrameAsync(cancellationToken);
        }

        public static UniTask WaitForEndOfFrame(MonoBehaviour coroutineRunner) {
            var source = WaitForEndOfFramePromise.Create(coroutineRunner, CancellationToken.None, false, out var token);
            return new UniTask(source, token);
        }

        public static UniTask WaitForEndOfFrame(
            MonoBehaviour coroutineRunner,
            CancellationToken cancellationToken,
            bool cancelImmediately = false
        ) {
            var source = WaitForEndOfFramePromise.Create(
                coroutineRunner,
                cancellationToken,
                cancelImmediately,
                out var token
            );

            return new UniTask(source, token);
        }

        /// <summary>
        /// Same as UniTask.Yield(PlayerLoopTiming.LastFixedUpdate).
        /// </summary>
        public static YieldAwaitable WaitForFixedUpdate() {
            // use LastFixedUpdate instead of FixedUpdate
            // https://github.com/Cysharp/UniTask/issues/377
            return UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
        }

        /// <summary>
        /// Same as UniTask.Yield(PlayerLoopTiming.LastFixedUpdate, cancellationToken).
        /// </summary>
        public static UniTask WaitForFixedUpdate(CancellationToken cancellationToken, bool cancelImmediately = false) {
            return UniTask.Yield(PlayerLoopTiming.LastFixedUpdate, cancellationToken, cancelImmediately);
        }

        public static UniTask WaitForSeconds(
            float duration,
            bool ignoreTimeScale = false,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return Delay(
                Mathf.RoundToInt(1000 * duration),
                ignoreTimeScale,
                delayTiming,
                cancellationToken,
                cancelImmediately
            );
        }

        public static UniTask WaitForSeconds(
            int duration,
            bool ignoreTimeScale = false,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            return Delay(1000 * duration, ignoreTimeScale, delayTiming, cancellationToken, cancelImmediately);
        }

        public static UniTask DelayFrame(
            int delayFrameCount,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            if (delayFrameCount < 0) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus delayFrameCount. delayFrameCount:" + delayFrameCount
                );
            }

            return new UniTask(
                DelayFramePromise.Create(
                    delayFrameCount,
                    delayTiming,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        public static UniTask Delay(
            int millisecondsDelay,
            bool ignoreTimeScale = false,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, ignoreTimeScale, delayTiming, cancellationToken, cancelImmediately);
        }

        public static UniTask Delay(
            TimeSpan delayTimeSpan,
            bool ignoreTimeScale = false,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            var delayType = ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime;
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken, cancelImmediately);
        }

        public static UniTask Delay(
            int millisecondsDelay,
            DelayType delayType,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            var delayTimeSpan = TimeSpan.FromMilliseconds(millisecondsDelay);
            return Delay(delayTimeSpan, delayType, delayTiming, cancellationToken, cancelImmediately);
        }

        public static UniTask Delay(
            TimeSpan delayTimeSpan,
            DelayType delayType,
            PlayerLoopTiming delayTiming = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            if (delayTimeSpan < TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException(
                    "Delay does not allow minus delayTimeSpan. delayTimeSpan:" + delayTimeSpan
                );
            }

#if UNITY_EDITOR

            // force use Realtime.
            if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying) {
                delayType = DelayType.Realtime;
            }
#endif

            switch (delayType) {
                case DelayType.UnscaledDeltaTime: {
                    return new UniTask(
                        DelayIgnoreTimeScalePromise.Create(
                            delayTimeSpan,
                            delayTiming,
                            cancellationToken,
                            cancelImmediately,
                            out var token
                        ),
                        token
                    );
                }

                case DelayType.Realtime: {
                    return new UniTask(
                        DelayRealtimePromise.Create(
                            delayTimeSpan,
                            delayTiming,
                            cancellationToken,
                            cancelImmediately,
                            out var token
                        ),
                        token
                    );
                }

                case DelayType.DeltaTime:
                default: {
                    return new UniTask(
                        DelayPromise.Create(
                            delayTimeSpan,
                            delayTiming,
                            cancellationToken,
                            cancelImmediately,
                            out var token
                        ),
                        token
                    );
                }
            }
        }

        private sealed class YieldPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<YieldPromise> {
            private static TaskPool<YieldPromise> s_Pool;
            private YieldPromise m_NextNode;
            public ref YieldPromise NextNode => ref m_NextNode;

            static YieldPromise() {
                TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => s_Pool.Size);
            }

            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private UniTaskCompletionSourceCore<object> m_Core;

            private YieldPromise() { }

            public static IUniTaskSource Create(
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new YieldPromise();
                }

                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (YieldPromise)state;
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

                m_Core.TrySetResult(null);
                return false;
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

        private sealed class NextFramePromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<NextFramePromise> {
            private static TaskPool<NextFramePromise> s_Pool;
            private NextFramePromise m_NextNode;
            public ref NextFramePromise NextNode => ref m_NextNode;

            static NextFramePromise() {
                TaskPool.RegisterSizeGetter(typeof(NextFramePromise), () => s_Pool.Size);
            }

            private int m_FrameCount;
            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private NextFramePromise() { }

            public static IUniTaskSource Create(
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new NextFramePromise();
                }

                result.m_FrameCount = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (NextFramePromise)state;
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

                if (m_FrameCount == Time.frameCount) {
                    return true;
                }

                m_Core.TrySetResult(AsyncUnit.Default);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                return s_Pool.TryPush(this);
            }
        }

        private sealed class WaitForEndOfFramePromise : IUniTaskSource,
                                                        ITaskPoolNode<WaitForEndOfFramePromise>,
                                                        System.Collections.IEnumerator {
            private static TaskPool<WaitForEndOfFramePromise> s_Pool;
            private WaitForEndOfFramePromise m_NextNode;
            public ref WaitForEndOfFramePromise NextNode => ref m_NextNode;

            static WaitForEndOfFramePromise() {
                TaskPool.RegisterSizeGetter(typeof(WaitForEndOfFramePromise), () => s_Pool.Size);
            }

            private UniTaskCompletionSourceCore<object> m_Core;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private WaitForEndOfFramePromise() { }

            public static IUniTaskSource Create(
                MonoBehaviour coroutineRunner,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitForEndOfFramePromise();
                }

                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (WaitForEndOfFramePromise)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                coroutineRunner.StartCoroutine(result);

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

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                Reset(); // Reset Enumerator
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                return s_Pool.TryPush(this);
            }

            // Coroutine Runner implementation

            private static readonly WaitForEndOfFrame s_WaitForEndOfFrameYieldInstruction = new();
            private bool m_IsFirst = true;

            object IEnumerator.Current => s_WaitForEndOfFrameYieldInstruction;

            bool IEnumerator.MoveNext() {
                if (m_IsFirst) {
                    m_IsFirst = false;
                    return true; // start WaitForEndOfFrame
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                m_Core.TrySetResult(null);
                return false;
            }

            public void Reset() {
                m_IsFirst = true;
            }
        }

        private sealed class DelayFramePromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayFramePromise> {
            private static TaskPool<DelayFramePromise> s_Pool;
            private DelayFramePromise m_NextNode;
            public ref DelayFramePromise NextNode => ref m_NextNode;

            static DelayFramePromise() {
                TaskPool.RegisterSizeGetter(typeof(DelayFramePromise), () => s_Pool.Size);
            }

            private int m_InitialFrame;
            private int m_DelayFrameCount;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private int m_CurrentFrameCount;
            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private DelayFramePromise() { }

            public static IUniTaskSource Create(
                int delayFrameCount,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new DelayFramePromise();
                }

                result.m_DelayFrameCount = delayFrameCount;
                result.m_CancellationToken = cancellationToken;
                result.m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (DelayFramePromise)state;
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

                if (m_CurrentFrameCount == 0) {
                    if (m_DelayFrameCount == 0) { // same as Yield
                        m_Core.TrySetResult(AsyncUnit.Default);
                        return false;
                    }

                    // skip in initial frame.
                    if (m_InitialFrame == Time.frameCount) {
#if UNITY_EDITOR

                        // force use Realtime.
                        if (PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying) {
                            //goto ++currentFrameCount
                        } else {
                            return true;
                        }
#else
                        return true;
#endif
                    }
                }

                if (++m_CurrentFrameCount >= m_DelayFrameCount) {
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_CurrentFrameCount = default;
                m_DelayFrameCount = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class DelayPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayPromise> {
            private static TaskPool<DelayPromise> s_Pool;
            private DelayPromise m_NextNode;
            public ref DelayPromise NextNode => ref m_NextNode;

            static DelayPromise() {
                TaskPool.RegisterSizeGetter(typeof(DelayPromise), () => s_Pool.Size);
            }

            private int m_InitialFrame;
            private float m_DelayTimeSpan;
            private float m_Elapsed;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private DelayPromise() { }

            public static IUniTaskSource Create(
                TimeSpan delayTimeSpan,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new DelayPromise();
                }

                result.m_Elapsed = 0.0f;
                result.m_DelayTimeSpan = (float)delayTimeSpan.TotalSeconds;
                result.m_CancellationToken = cancellationToken;
                result.m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (DelayPromise)state;
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

                if (m_Elapsed == 0.0f) {
                    if (m_InitialFrame == Time.frameCount) {
                        return true;
                    }
                }

                m_Elapsed += Time.deltaTime;

                if (m_Elapsed >= m_DelayTimeSpan) {
                    m_Core.TrySetResult(null);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_DelayTimeSpan = default;
                m_Elapsed = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class DelayIgnoreTimeScalePromise : IUniTaskSource,
                                                           IPlayerLoopItem,
                                                           ITaskPoolNode<DelayIgnoreTimeScalePromise> {
            private static TaskPool<DelayIgnoreTimeScalePromise> s_Pool;
            private DelayIgnoreTimeScalePromise m_NextNode;
            public ref DelayIgnoreTimeScalePromise NextNode => ref m_NextNode;

            static DelayIgnoreTimeScalePromise() {
                TaskPool.RegisterSizeGetter(typeof(DelayIgnoreTimeScalePromise), () => s_Pool.Size);
            }

            private float m_DelayFrameTimeSpan;
            private float m_Elapsed;
            private int m_InitialFrame;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<object> m_Core;

            private DelayIgnoreTimeScalePromise() { }

            public static IUniTaskSource Create(
                TimeSpan delayFrameTimeSpan,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new DelayIgnoreTimeScalePromise();
                }

                result.m_Elapsed = 0.0f;
                result.m_DelayFrameTimeSpan = (float)delayFrameTimeSpan.TotalSeconds;
                result.m_InitialFrame = PlayerLoopHelper.IsMainThread ? Time.frameCount : -1;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (DelayIgnoreTimeScalePromise)state;
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

                if (m_Elapsed == 0.0f) {
                    if (m_InitialFrame == Time.frameCount) {
                        return true;
                    }
                }

                m_Elapsed += Time.unscaledDeltaTime;

                if (m_Elapsed >= m_DelayFrameTimeSpan) {
                    m_Core.TrySetResult(null);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_DelayFrameTimeSpan = default;
                m_Elapsed = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        private sealed class DelayRealtimePromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<DelayRealtimePromise> {
            private static TaskPool<DelayRealtimePromise> s_Pool;
            private DelayRealtimePromise m_NextNode;
            public ref DelayRealtimePromise NextNode => ref m_NextNode;

            static DelayRealtimePromise() {
                TaskPool.RegisterSizeGetter(typeof(DelayRealtimePromise), () => s_Pool.Size);
            }

            private long m_DelayTimeSpanTicks;
            private ValueStopwatch m_Stopwatch;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private DelayRealtimePromise() { }

            public static IUniTaskSource Create(
                TimeSpan delayTimeSpan,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new DelayRealtimePromise();
                }

                result.m_Stopwatch = ValueStopwatch.StartNew();
                result.m_DelayTimeSpanTicks = delayTimeSpan.Ticks;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (DelayRealtimePromise)state;
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

                if (m_Stopwatch.IsInvalid) {
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                if (m_Stopwatch.ElapsedTicks >= m_DelayTimeSpanTicks) {
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Stopwatch = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }
    }

    public readonly struct YieldAwaitable {
        private readonly PlayerLoopTiming m_Timing;

        public YieldAwaitable(PlayerLoopTiming timing) {
            m_Timing = timing;
        }

        public Awaiter GetAwaiter() {
            return new Awaiter(m_Timing);
        }

        public UniTask ToUniTask() {
            return UniTask.Yield(m_Timing, CancellationToken.None);
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion {
            private readonly PlayerLoopTiming m_InnerTiming;

            public Awaiter(PlayerLoopTiming timing) {
                m_InnerTiming = timing;
            }

            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_InnerTiming, continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                PlayerLoopHelper.AddContinuation(m_InnerTiming, continuation);
            }
        }
    }
}