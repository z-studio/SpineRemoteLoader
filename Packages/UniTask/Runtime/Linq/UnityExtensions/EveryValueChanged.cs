using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TProperty> EveryValueChanged<TTarget, TProperty>(
            TTarget target,
            Func<TTarget, TProperty> propertySelector,
            PlayerLoopTiming monitorTiming = PlayerLoopTiming.Update,
            IEqualityComparer<TProperty> equalityComparer = null,
            bool cancelImmediately = false
        )
            where TTarget : class {
            var unityObject = target as UnityEngine.Object;
            var isUnityObject = target is UnityEngine.Object; // don't use (unityObject == null)

            if (isUnityObject) {
                return new EveryValueChangedUnityObject<TTarget, TProperty>(
                    target,
                    propertySelector,
                    equalityComparer ?? UnityEqualityComparer.GetDefault<TProperty>(),
                    monitorTiming,
                    cancelImmediately
                );
            } else {
                return new EveryValueChangedStandardObject<TTarget, TProperty>(
                    target,
                    propertySelector,
                    equalityComparer ?? UnityEqualityComparer.GetDefault<TProperty>(),
                    monitorTiming,
                    cancelImmediately
                );
            }
        }
    }

    internal sealed class EveryValueChangedUnityObject<TTarget, TProperty> : IUniTaskAsyncEnumerable<TProperty> {
        private readonly TTarget m_Target;
        private readonly Func<TTarget, TProperty> m_PropertySelector;
        private readonly IEqualityComparer<TProperty> m_EqualityComparer;
        private readonly PlayerLoopTiming m_MonitorTiming;
        private readonly bool m_CancelImmediately;

        public EveryValueChangedUnityObject(
            TTarget target,
            Func<TTarget, TProperty> propertySelector,
            IEqualityComparer<TProperty> equalityComparer,
            PlayerLoopTiming monitorTiming,
            bool cancelImmediately
        ) {
            m_Target = target;
            m_PropertySelector = propertySelector;
            m_EqualityComparer = equalityComparer;
            m_MonitorTiming = monitorTiming;
            m_CancelImmediately = cancelImmediately;
        }

        public IUniTaskAsyncEnumerator<TProperty> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerEveryValueChanged(
                m_Target,
                m_PropertySelector,
                m_EqualityComparer,
                m_MonitorTiming,
                cancellationToken,
                m_CancelImmediately
            );
        }

        private sealed class InnerEveryValueChanged : MoveNextSource, IUniTaskAsyncEnumerator<TProperty>, IPlayerLoopItem {
            private readonly TTarget m_InnerTarget;
            private readonly UnityEngine.Object m_TargetAsUnityObject;
            private readonly IEqualityComparer<TProperty> m_InnerEqualityComparer;
            private readonly Func<TTarget, TProperty> m_InnerPropertySelector;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;

            private bool m_First;
            private TProperty m_CurrentValue;
            private bool m_Disposed;

            public InnerEveryValueChanged(
                TTarget target,
                Func<TTarget, TProperty> propertySelector,
                IEqualityComparer<TProperty> equalityComparer,
                PlayerLoopTiming monitorTiming,
                CancellationToken cancellationToken,
                bool cancelImmediately
            ) {
                m_InnerTarget = target;
                m_TargetAsUnityObject = target as UnityEngine.Object;
                m_InnerPropertySelector = propertySelector;
                m_InnerEqualityComparer = equalityComparer;
                m_CancellationToken = cancellationToken;
                m_First = true;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (InnerEveryValueChanged)state;
                            source.mCompletionSource.TrySetCanceled(source.m_CancellationToken);
                        },
                        this
                    );
                }

                TaskTracker.TrackActiveTask(this, 2);
                PlayerLoopHelper.AddAction(monitorTiming, this);
            }

            public TProperty Current => m_CurrentValue;

            public UniTask<bool> MoveNextAsync() {
                if (m_Disposed) {
                    return CompletedTasks.False;
                }

                mCompletionSource.Reset();

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return new UniTask<bool>(this, mCompletionSource.Version);
                }

                if (m_First) {
                    m_First = false;

                    if (m_TargetAsUnityObject == null) {
                        return CompletedTasks.False;
                    }

                    m_CurrentValue = m_InnerPropertySelector(m_InnerTarget);
                    return CompletedTasks.True;
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_Disposed) {
                    m_CancellationTokenRegistration.Dispose();
                    m_Disposed = true;
                    TaskTracker.RemoveTracking(this);
                }

                return default;
            }

            public bool MoveNext() {
                if (m_Disposed || m_TargetAsUnityObject == null) {
                    mCompletionSource.TrySetResult(false);
                    DisposeAsync().Forget();
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                var nextValue = default(TProperty);

                try {
                    nextValue = m_InnerPropertySelector(m_InnerTarget);

                    if (m_InnerEqualityComparer.Equals(m_CurrentValue, nextValue)) {
                        return true;
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    DisposeAsync().Forget();
                    return false;
                }

                m_CurrentValue = nextValue;
                mCompletionSource.TrySetResult(true);
                return true;
            }
        }
    }

    internal sealed class EveryValueChangedStandardObject<TTarget, TProperty> : IUniTaskAsyncEnumerable<TProperty>
        where TTarget : class {
        private readonly WeakReference<TTarget> m_Target;
        private readonly Func<TTarget, TProperty> m_PropertySelector;
        private readonly IEqualityComparer<TProperty> m_EqualityComparer;
        private readonly PlayerLoopTiming m_MonitorTiming;
        private readonly bool m_CancelImmediately;

        public EveryValueChangedStandardObject(
            TTarget target,
            Func<TTarget, TProperty> propertySelector,
            IEqualityComparer<TProperty> equalityComparer,
            PlayerLoopTiming monitorTiming,
            bool cancelImmediately
        ) {
            m_Target = new WeakReference<TTarget>(target, false);
            m_PropertySelector = propertySelector;
            m_EqualityComparer = equalityComparer;
            m_MonitorTiming = monitorTiming;
            m_CancelImmediately = cancelImmediately;
        }

        public IUniTaskAsyncEnumerator<TProperty> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerEveryValueChanged(
                m_Target,
                m_PropertySelector,
                m_EqualityComparer,
                m_MonitorTiming,
                cancellationToken,
                m_CancelImmediately
            );
        }

        private sealed class InnerEveryValueChanged : MoveNextSource, IUniTaskAsyncEnumerator<TProperty>, IPlayerLoopItem {
            private readonly WeakReference<TTarget> m_InnerTarget;
            private readonly IEqualityComparer<TProperty> m_InnerEqualityComparer;
            private readonly Func<TTarget, TProperty> m_InnerPropertySelector;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;

            private bool m_First;
            private TProperty m_CurrentValue;
            private bool m_Disposed;

            public InnerEveryValueChanged(
                WeakReference<TTarget> target,
                Func<TTarget, TProperty> propertySelector,
                IEqualityComparer<TProperty> equalityComparer,
                PlayerLoopTiming monitorTiming,
                CancellationToken cancellationToken,
                bool cancelImmediately
            ) {
                m_InnerTarget = target;
                m_InnerPropertySelector = propertySelector;
                m_InnerEqualityComparer = equalityComparer;
                m_CancellationToken = cancellationToken;
                m_First = true;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (InnerEveryValueChanged)state;
                            source.mCompletionSource.TrySetCanceled(source.m_CancellationToken);
                        },
                        this
                    );
                }

                TaskTracker.TrackActiveTask(this, 2);
                PlayerLoopHelper.AddAction(monitorTiming, this);
            }

            public TProperty Current => m_CurrentValue;

            public UniTask<bool> MoveNextAsync() {
                if (m_Disposed) {
                    return CompletedTasks.False;
                }

                mCompletionSource.Reset();

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return new UniTask<bool>(this, mCompletionSource.Version);
                }

                if (m_First) {
                    m_First = false;

                    if (!m_InnerTarget.TryGetTarget(out var t)) {
                        return CompletedTasks.False;
                    }

                    m_CurrentValue = m_InnerPropertySelector(t);
                    return CompletedTasks.True;
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_Disposed) {
                    m_CancellationTokenRegistration.Dispose();
                    m_Disposed = true;
                    TaskTracker.RemoveTracking(this);
                }

                return default;
            }

            public bool MoveNext() {
                if (m_Disposed || !m_InnerTarget.TryGetTarget(out var t)) {
                    mCompletionSource.TrySetResult(false);
                    DisposeAsync().Forget();
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                var nextValue = default(TProperty);

                try {
                    nextValue = m_InnerPropertySelector(t);

                    if (m_InnerEqualityComparer.Equals(m_CurrentValue, nextValue)) {
                        return true;
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                    DisposeAsync().Forget();
                    return false;
                }

                m_CurrentValue = nextValue;
                mCompletionSource.TrySetResult(true);
                return true;
            }
        }
    }
}