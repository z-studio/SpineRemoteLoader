// asmdef Version Defines, enabled when com.demigiant.dotween is imported.

#if UNITASK_DOTWEEN_SUPPORT
using Cysharp.Threading.Tasks.Internal;
using DG.Tweening;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public enum TweenCancelBehaviour {
        Kill,
        KillWithCompleteCallback,
        Complete,
        CompleteWithSequenceCallback,
        CancelAwait,

        // AndCancelAwait
        KillAndCancelAwait,
        KillWithCompleteCallbackAndCancelAwait,
        CompleteAndCancelAwait,
        CompleteWithSequenceCallbackAndCancelAwait
    }

    public static class DOTweenAsyncExtensions {
        private enum CallbackType {
            Kill,
            Complete,
            Pause,
            Play,
            Rewind,
            StepComplete
        }

        public static TweenAwaiter GetAwaiter(this Tween tween) {
            return new TweenAwaiter(tween);
        }

        public static UniTask WithCancellation(this Tween tween, CancellationToken cancellationToken) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    TweenCancelBehaviour.Kill,
                    cancellationToken,
                    CallbackType.Kill,
                    out var token
                ),
                token
            );
        }

        public static UniTask ToUniTask(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.Kill,
                    out var token
                ),
                token
            );
        }

        public static UniTask AwaitForComplete(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.Complete,
                    out var token
                ),
                token
            );
        }

        public static UniTask AwaitForPause(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.Pause,
                    out var token
                ),
                token
            );
        }

        public static UniTask AwaitForPlay(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.Play,
                    out var token
                ),
                token
            );
        }

        public static UniTask AwaitForRewind(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.Rewind,
                    out var token
                ),
                token
            );
        }

        public static UniTask AwaitForStepComplete(
            this Tween tween,
            TweenCancelBehaviour tweenCancelBehaviour =
                TweenCancelBehaviour.Kill,
            CancellationToken cancellationToken = default
        ) {
            Error.ThrowArgumentNullException(tween, nameof(tween));

            if (!tween.IsActive()) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                TweenConfiguredSource.Create(
                    tween,
                    tweenCancelBehaviour,
                    cancellationToken,
                    CallbackType.StepComplete,
                    out var token
                ),
                token
            );
        }

        public struct TweenAwaiter : ICriticalNotifyCompletion {
            private readonly Tween m_Tween;

            // killed(non active) as completed.
            public bool IsCompleted => !m_Tween.IsActive();

            public TweenAwaiter(Tween tween) {
                m_Tween = tween;
            }

            public TweenAwaiter GetAwaiter() {
                return this;
            }

            public void GetResult() { }

            public void OnCompleted(System.Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(System.Action continuation) {
                // onKill is called after OnCompleted, both Complete(false/true) and Kill(false/true).
                m_Tween.onKill = PooledTweenCallback.Create(continuation);
            }
        }

        private sealed class TweenConfiguredSource : IUniTaskSource, ITaskPoolNode<TweenConfiguredSource> {
            private static TaskPool<TweenConfiguredSource> s_Pool;
            private TweenConfiguredSource m_NextNode;
            public ref TweenConfiguredSource NextNode => ref m_NextNode;

            static TweenConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(TweenConfiguredSource), () => s_Pool.Size);
            }

            private readonly TweenCallback m_OnCompleteCallbackDelegate;

            private Tween m_Tween;
            private TweenCancelBehaviour m_CancelBehaviour;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationRegistration;
            private CallbackType m_CallbackType;
            private bool m_Canceled;

            private TweenCallback m_OriginalCompleteAction;
            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private TweenConfiguredSource() {
                m_OnCompleteCallbackDelegate = OnCompleteCallbackDelegate;
            }

            public static IUniTaskSource Create(
                Tween tween,
                TweenCancelBehaviour cancelBehaviour,
                CancellationToken cancellationToken,
                CallbackType callbackType,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    DoCancelBeforeCreate(tween, cancelBehaviour);
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new TweenConfiguredSource();
                }

                result.m_Tween = tween;
                result.m_CancelBehaviour = cancelBehaviour;
                result.m_CancellationToken = cancellationToken;
                result.m_CallbackType = callbackType;
                result.m_Canceled = false;

                switch (callbackType) {
                    case CallbackType.Kill:
                        result.m_OriginalCompleteAction = tween.onKill;
                        tween.onKill = result.m_OnCompleteCallbackDelegate;
                        break;
                    case CallbackType.Complete:
                        result.m_OriginalCompleteAction = tween.onComplete;
                        tween.onComplete = result.m_OnCompleteCallbackDelegate;
                        break;
                    case CallbackType.Pause:
                        result.m_OriginalCompleteAction = tween.onPause;
                        tween.onPause = result.m_OnCompleteCallbackDelegate;
                        break;
                    case CallbackType.Play:
                        result.m_OriginalCompleteAction = tween.onPlay;
                        tween.onPlay = result.m_OnCompleteCallbackDelegate;
                        break;
                    case CallbackType.Rewind:
                        result.m_OriginalCompleteAction = tween.onRewind;
                        tween.onRewind = result.m_OnCompleteCallbackDelegate;
                        break;
                    case CallbackType.StepComplete:
                        result.m_OriginalCompleteAction = tween.onStepComplete;
                        tween.onStepComplete = result.m_OnCompleteCallbackDelegate;
                        break;
                    default:
                        break;
                }

                if (result.m_OriginalCompleteAction == result.m_OnCompleteCallbackDelegate) {
                    result.m_OriginalCompleteAction = null;
                }

                if (cancellationToken.CanBeCanceled) {
                    result.m_CancellationRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        x => {
                            var source = (TweenConfiguredSource)x;

                            switch (source.m_CancelBehaviour) {
                                case TweenCancelBehaviour.Kill:
                                default:
                                    source.m_Tween.Kill(false);
                                    break;
                                case TweenCancelBehaviour.KillAndCancelAwait:
                                    source.m_Canceled = true;
                                    source.m_Tween.Kill(false);
                                    break;
                                case TweenCancelBehaviour.KillWithCompleteCallback:
                                    source.m_Tween.Kill(true);
                                    break;
                                case TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                                    source.m_Canceled = true;
                                    source.m_Tween.Kill(true);
                                    break;
                                case TweenCancelBehaviour.Complete:
                                    source.m_Tween.Complete(false);
                                    break;
                                case TweenCancelBehaviour.CompleteAndCancelAwait:
                                    source.m_Canceled = true;
                                    source.m_Tween.Complete(false);
                                    break;
                                case TweenCancelBehaviour.CompleteWithSequenceCallback:
                                    source.m_Tween.Complete(true);
                                    break;
                                case TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                                    source.m_Canceled = true;
                                    source.m_Tween.Complete(true);
                                    break;
                                case TweenCancelBehaviour.CancelAwait:
                                    source.RestoreOriginalCallback();
                                    source.m_Core.TrySetCanceled(source.m_CancellationToken);
                                    break;
                            }
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                token = result.m_Core.Version;
                return result;
            }

            private void OnCompleteCallbackDelegate() {
                if (m_CancellationToken.IsCancellationRequested) {
                    if (m_CancelBehaviour == TweenCancelBehaviour.KillAndCancelAwait
                        || m_CancelBehaviour == TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait
                        || m_CancelBehaviour == TweenCancelBehaviour.CompleteAndCancelAwait
                        || m_CancelBehaviour == TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait
                        || m_CancelBehaviour == TweenCancelBehaviour.CancelAwait) {
                        m_Canceled = true;
                    }
                }

                if (m_Canceled) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                } else {
                    m_OriginalCompleteAction?.Invoke();
                    m_Core.TrySetResult(AsyncUnit.Default);
                }
            }

            private static void DoCancelBeforeCreate(Tween tween, TweenCancelBehaviour tweenCancelBehaviour) {
                switch (tweenCancelBehaviour) {
                    case TweenCancelBehaviour.Kill:
                    default:
                        tween.Kill(false);
                        break;
                    case TweenCancelBehaviour.KillAndCancelAwait:
                        tween.Kill(false);
                        break;
                    case TweenCancelBehaviour.KillWithCompleteCallback:
                        tween.Kill(true);
                        break;
                    case TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                        tween.Kill(true);
                        break;
                    case TweenCancelBehaviour.Complete:
                        tween.Complete(false);
                        break;
                    case TweenCancelBehaviour.CompleteAndCancelAwait:
                        tween.Complete(false);
                        break;
                    case TweenCancelBehaviour.CompleteWithSequenceCallback:
                        tween.Complete(true);
                        break;
                    case TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                        tween.Complete(true);
                        break;
                    case TweenCancelBehaviour.CancelAwait:
                        break;
                }
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    TryReturn();
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
                m_CancellationRegistration.Dispose();

                RestoreOriginalCallback();

                m_Tween = default;
                m_CancellationToken = default;
                m_OriginalCompleteAction = default;
                return s_Pool.TryPush(this);
            }

            private void RestoreOriginalCallback() {
                switch (m_CallbackType) {
                    case CallbackType.Kill:
                        m_Tween.onKill = m_OriginalCompleteAction;
                        break;
                    case CallbackType.Complete:
                        m_Tween.onComplete = m_OriginalCompleteAction;
                        break;
                    case CallbackType.Pause:
                        m_Tween.onPause = m_OriginalCompleteAction;
                        break;
                    case CallbackType.Play:
                        m_Tween.onPlay = m_OriginalCompleteAction;
                        break;
                    case CallbackType.Rewind:
                        m_Tween.onRewind = m_OriginalCompleteAction;
                        break;
                    case CallbackType.StepComplete:
                        m_Tween.onStepComplete = m_OriginalCompleteAction;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    internal sealed class PooledTweenCallback {
        private static readonly ConcurrentQueue<PooledTweenCallback> s_Pool = new();

        private readonly TweenCallback m_RunDelegate;

        private Action m_Continuation;

        private PooledTweenCallback() {
            m_RunDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCallback Create(Action continuation) {
            if (!s_Pool.TryDequeue(out var item)) {
                item = new PooledTweenCallback();
            }

            item.m_Continuation = continuation;
            return item.m_RunDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run() {
            var call = m_Continuation;
            m_Continuation = null;

            if (call != null) {
                s_Pool.Enqueue(this);
                call.Invoke();
            }
        }
    }
}

#endif