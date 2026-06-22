#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;
using UnityEngine;

namespace Cysharp.Threading.Tasks {
    public static class EnumeratorAsyncExtensions {
        public static UniTask.Awaiter GetAwaiter<T>(this T enumerator)
            where T : IEnumerator {
            var e = (IEnumerator)enumerator;
            Error.ThrowArgumentNullException(e, nameof(enumerator));

            return new UniTask(
                EnumeratorPromise.Create(e, PlayerLoopTiming.Update, CancellationToken.None, out var token),
                token
            ).GetAwaiter();
        }

        public static UniTask WithCancellation(this IEnumerator enumerator, CancellationToken cancellationToken) {
            Error.ThrowArgumentNullException(enumerator, nameof(enumerator));

            return new UniTask(
                EnumeratorPromise.Create(enumerator, PlayerLoopTiming.Update, cancellationToken, out var token),
                token
            );
        }

        public static UniTask ToUniTask(
            this IEnumerator enumerator,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken)
        ) {
            Error.ThrowArgumentNullException(enumerator, nameof(enumerator));
            return new UniTask(EnumeratorPromise.Create(enumerator, timing, cancellationToken, out var token), token);
        }

        public static UniTask ToUniTask(this IEnumerator enumerator, MonoBehaviour coroutineRunner) {
            var source = AutoResetUniTaskCompletionSource.Create();
            coroutineRunner.StartCoroutine(Core(enumerator, coroutineRunner, source));
            return source.Task;
        }

        private static IEnumerator Core(
            IEnumerator inner,
            MonoBehaviour coroutineRunner,
            AutoResetUniTaskCompletionSource source
        ) {
            yield return coroutineRunner.StartCoroutine(inner);
            source.TrySetResult();
        }

        private sealed class EnumeratorPromise : IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<EnumeratorPromise> {
            private static TaskPool<EnumeratorPromise> s_Pool;
            private EnumeratorPromise m_NextNode;
            public ref EnumeratorPromise NextNode => ref m_NextNode;

            static EnumeratorPromise() {
                TaskPool.RegisterSizeGetter(typeof(EnumeratorPromise), () => s_Pool.Size);
            }

            private IEnumerator m_InnerEnumerator;
            private CancellationToken m_CancellationToken;
            private int m_InitialFrame;
            private bool m_LoopRunning;
            private bool m_CalledGetResult;

            private UniTaskCompletionSourceCore<object> m_Core;

            private EnumeratorPromise() { }

            public static IUniTaskSource Create(
                IEnumerator innerEnumerator,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new EnumeratorPromise();
                }

                TaskTracker.TrackActiveTask(result, 3);

                result.m_InnerEnumerator = ConsumeEnumerator(innerEnumerator);
                result.m_CancellationToken = cancellationToken;
                result.m_LoopRunning = true;
                result.m_CalledGetResult = false;
                result.m_InitialFrame = -1;

                token = result.m_Core.Version;

                // run immediately.
                if (result.MoveNext()) {
                    PlayerLoopHelper.AddAction(timing, result);
                }

                return result;
            }

            public void GetResult(short token) {
                try {
                    m_CalledGetResult = true;
                    m_Core.GetResult(token);
                } finally {
                    if (!m_LoopRunning) {
                        TryReturn();
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
                if (m_CalledGetResult) {
                    m_LoopRunning = false;
                    TryReturn();
                    return false;
                }

                if (m_InnerEnumerator == null) { // invalid status, returned but loop running?
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_LoopRunning = false;
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_InitialFrame == -1) {
                    // Time can not touch in threadpool.
                    if (PlayerLoopHelper.IsMainThread) {
                        m_InitialFrame = Time.frameCount;
                    }
                } else if (m_InitialFrame == Time.frameCount) {
                    return true; // already executed in first frame, skip.
                }

                try {
                    if (m_InnerEnumerator.MoveNext()) {
                        return true;
                    }
                } catch (Exception ex) {
                    m_LoopRunning = false;
                    m_Core.TrySetException(ex);
                    return false;
                }

                m_LoopRunning = false;
                m_Core.TrySetResult(null);
                return false;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_InnerEnumerator = default;
                m_CancellationToken = default;

                return s_Pool.TryPush(this);
            }

            // Unwrap YieldInstructions

            private static IEnumerator ConsumeEnumerator(IEnumerator enumerator) {
                while (enumerator.MoveNext()) {
                    var current = enumerator.Current;

                    if (current == null) {
                        yield return null;
                    } else if (current is CustomYieldInstruction cyi) {
                        // WWW, WaitForSecondsRealtime
                        while (cyi.keepWaiting) {
                            yield return null;
                        }
                    } else if (current is YieldInstruction) {
                        IEnumerator innerCoroutine = null;

                        switch (current) {
                            case AsyncOperation ao:
                                innerCoroutine = UnwrapWaitAsyncOperation(ao);
                                break;
                            case WaitForSeconds wfs:
                                innerCoroutine = UnwrapWaitForSeconds(wfs);
                                break;
                        }

                        if (innerCoroutine != null) {
                            while (innerCoroutine.MoveNext()) {
                                yield return null;
                            }
                        } else {
                            goto WARN;
                        }
                    } else if (current is IEnumerator e3) {
                        var e4 = ConsumeEnumerator(e3);

                        while (e4.MoveNext()) {
                            yield return null;
                        }
                    } else {
                        goto WARN;
                    }

                    continue;

                    WARN:

                    // WaitForEndOfFrame, WaitForFixedUpdate, others.
                    UnityEngine.Debug.LogWarning(
                        $"yield {current.GetType().Name} is not supported on await IEnumerator or IEnumerator.ToUniTask(), please use ToUniTask(MonoBehaviour coroutineRunner) instead."
                    );

                    yield return null;
                }
            }

            private static readonly FieldInfo s_WaitForSecondsSeconds = typeof(WaitForSeconds).GetField(
                "m_Seconds",
                BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic
            );

            private static IEnumerator UnwrapWaitForSeconds(WaitForSeconds waitForSeconds) {
                var second = (float)s_WaitForSecondsSeconds.GetValue(waitForSeconds);
                var elapsed = 0.0f;

                while (true) {
                    yield return null;

                    elapsed += Time.deltaTime;

                    if (elapsed >= second) {
                        break;
                    }
                }
            }

            private static IEnumerator UnwrapWaitAsyncOperation(AsyncOperation asyncOperation) {
                while (!asyncOperation.isDone) {
                    yield return null;
                }
            }
        }
    }
}