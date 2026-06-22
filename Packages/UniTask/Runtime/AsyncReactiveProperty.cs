using System;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public interface IReadOnlyAsyncReactiveProperty<T> : IUniTaskAsyncEnumerable<T> {
        T Value { get; }
        IUniTaskAsyncEnumerable<T> WithoutCurrent();
        UniTask<T> WaitAsync(CancellationToken cancellationToken = default);
    }

    public interface IAsyncReactiveProperty<T> : IReadOnlyAsyncReactiveProperty<T> {
        new T Value { get; set; }
    }

    [Serializable]
    public class AsyncReactiveProperty<T> : IAsyncReactiveProperty<T>, IDisposable {
        private TriggerEvent<T> m_TriggerEvent;
        
        [UnityEngine.SerializeField]
        private T m_LatestValue;

        public T Value {
            get => m_LatestValue;
            set {
                m_LatestValue = value;
                m_TriggerEvent.SetResult(value);
            }
        }

        public AsyncReactiveProperty(T value) {
            m_LatestValue = value;
            m_TriggerEvent = default;
        }

        public IUniTaskAsyncEnumerable<T> WithoutCurrent() {
            return new WithoutCurrentEnumerable(this);
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken) {
            return new Enumerator(this, cancellationToken, true);
        }

        public void Dispose() {
            m_TriggerEvent.SetCompleted();
        }

        public static implicit operator T(AsyncReactiveProperty<T> value) {
            return value.Value;
        }

        public override string ToString() {
            if (s_IsValueType) {
                return m_LatestValue.ToString();
            }

            return m_LatestValue?.ToString();
        }

        public UniTask<T> WaitAsync(CancellationToken cancellationToken = default) {
            return new UniTask<T>(WaitAsyncSource.Create(this, cancellationToken, out var token), token);
        }

        private static bool s_IsValueType;

        static AsyncReactiveProperty() {
            s_IsValueType = typeof(T).IsValueType;
        }

        private sealed class WaitAsyncSource : IUniTaskSource<T>, ITriggerHandler<T>, ITaskPoolNode<WaitAsyncSource> {
            private static Action<object> s_CancellationCallback = CancellationCallback;

            private static TaskPool<WaitAsyncSource> s_Pool;
            private WaitAsyncSource m_NextNode;
            ref WaitAsyncSource ITaskPoolNode<WaitAsyncSource>.NextNode => ref m_NextNode;

            static WaitAsyncSource() {
                TaskPool.RegisterSizeGetter(typeof(WaitAsyncSource), () => s_Pool.Size);
            }

            private AsyncReactiveProperty<T> m_Parent;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private UniTaskCompletionSourceCore<T> m_Core;

            private WaitAsyncSource() { }

            public static IUniTaskSource<T> Create(
                AsyncReactiveProperty<T> parent,
                CancellationToken cancellationToken,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitAsyncSource();
                }

                result.m_Parent = parent;
                result.m_CancellationToken = cancellationToken;

                if (cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, result);
                }

                result.m_Parent.m_TriggerEvent.Add(result);

                TaskTracker.TrackActiveTask(result, 3);

                token = result.m_Core.Version;
                return result;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_CancellationTokenRegistration.Dispose();
                m_CancellationTokenRegistration = default;
                m_Parent.m_TriggerEvent.Remove(this);
                m_Parent = null;
                m_CancellationToken = default;
                return s_Pool.TryPush(this);
            }

            private static void CancellationCallback(object state) {
                var self = (WaitAsyncSource)state;
                self.OnCanceled(self.m_CancellationToken);
            }

            // IUniTaskSource

            public T GetResult(short token) {
                try {
                    return m_Core.GetResult(token);
                } finally {
                    TryReturn();
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            // ITriggerHandler

            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public void OnCanceled(CancellationToken cancellationToken) {
                m_Core.TrySetCanceled(cancellationToken);
            }

            public void OnCompleted() {
                // Complete as Cancel.
                m_Core.TrySetCanceled(CancellationToken.None);
            }

            public void OnError(Exception ex) {
                m_Core.TrySetException(ex);
            }

            public void OnNext(T value) {
                m_Core.TrySetResult(value);
            }
        }

        private sealed class WithoutCurrentEnumerable : IUniTaskAsyncEnumerable<T> {
            private readonly AsyncReactiveProperty<T> m_Parent;

            public WithoutCurrentEnumerable(AsyncReactiveProperty<T> parent) {
                m_Parent = parent;
            }

            public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
                return new Enumerator(m_Parent, cancellationToken, false);
            }
        }

        private sealed class Enumerator : MoveNextSource, IUniTaskAsyncEnumerator<T>, ITriggerHandler<T> {
            private static Action<object> s_CancellationCallback = CancellationCallback;

            private readonly AsyncReactiveProperty<T> m_Parent;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;
            private T m_Value;
            private bool m_IsDisposed;
            private bool m_FirstCall;

            public Enumerator(
                AsyncReactiveProperty<T> parent,
                CancellationToken cancellationToken,
                bool publishCurrentValue
            ) {
                m_Parent = parent;
                m_CancellationToken = cancellationToken;
                m_FirstCall = publishCurrentValue;

                parent.m_TriggerEvent.Add(this);
                TaskTracker.TrackActiveTask(this, 3);

                if (cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
                }
            }

            public T Current => m_Value;

            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public UniTask<bool> MoveNextAsync() {
                // raise latest value on first call.
                if (m_FirstCall) {
                    m_FirstCall = false;
                    m_Value = m_Parent.Value;
                    return CompletedTasks.True;
                }

                mCompletionSource.Reset();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    m_Parent.m_TriggerEvent.Remove(this);
                }

                return default;
            }

            public void OnNext(T value) {
                m_Value = value;
                mCompletionSource.TrySetResult(true);
            }

            public void OnCanceled(CancellationToken cancellationToken) {
                DisposeAsync().Forget();
            }

            public void OnCompleted() {
                mCompletionSource.TrySetResult(false);
            }

            public void OnError(Exception ex) {
                mCompletionSource.TrySetException(ex);
            }

            private static void CancellationCallback(object state) {
                var self = (Enumerator)state;
                self.DisposeAsync().Forget();
            }
        }
    }

    public class ReadOnlyAsyncReactiveProperty<T> : IReadOnlyAsyncReactiveProperty<T>, IDisposable {
        private TriggerEvent<T> m_TriggerEvent;

        private T m_LatestValue;
        private IUniTaskAsyncEnumerator<T> m_Enumerator;

        public T Value => m_LatestValue;

        public ReadOnlyAsyncReactiveProperty(
            T initialValue,
            IUniTaskAsyncEnumerable<T> source,
            CancellationToken cancellationToken
        ) {
            m_LatestValue = initialValue;
            ConsumeEnumerator(source, cancellationToken).Forget();
        }

        public ReadOnlyAsyncReactiveProperty(IUniTaskAsyncEnumerable<T> source, CancellationToken cancellationToken) {
            ConsumeEnumerator(source, cancellationToken).Forget();
        }

        private async UniTaskVoid ConsumeEnumerator(IUniTaskAsyncEnumerable<T> source, CancellationToken cancellationToken) {
            m_Enumerator = source.GetAsyncEnumerator(cancellationToken);

            try {
                while (await m_Enumerator.MoveNextAsync()) {
                    var value = m_Enumerator.Current;
                    m_LatestValue = value;
                    m_TriggerEvent.SetResult(value);
                }
            } finally {
                await m_Enumerator.DisposeAsync();
                m_Enumerator = null;
            }
        }

        public IUniTaskAsyncEnumerable<T> WithoutCurrent() {
            return new WithoutCurrentEnumerable(this);
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken) {
            return new Enumerator(this, cancellationToken, true);
        }

        public void Dispose() {
            if (m_Enumerator != null) {
                m_Enumerator.DisposeAsync().Forget();
            }

            m_TriggerEvent.SetCompleted();
        }

        public static implicit operator T(ReadOnlyAsyncReactiveProperty<T> value) {
            return value.Value;
        }

        public override string ToString() {
            if (s_IsValueType) {
                return m_LatestValue.ToString();
            }

            return m_LatestValue?.ToString();
        }

        public UniTask<T> WaitAsync(CancellationToken cancellationToken = default) {
            return new UniTask<T>(WaitAsyncSource.Create(this, cancellationToken, out var token), token);
        }

        private static bool s_IsValueType;

        static ReadOnlyAsyncReactiveProperty() {
            s_IsValueType = typeof(T).IsValueType;
        }

        private sealed class WaitAsyncSource : IUniTaskSource<T>, ITriggerHandler<T>, ITaskPoolNode<WaitAsyncSource> {
            private static Action<object> s_CancellationCallback = CancellationCallback;

            private static TaskPool<WaitAsyncSource> s_Pool;
            private WaitAsyncSource m_NextNode;
            ref WaitAsyncSource ITaskPoolNode<WaitAsyncSource>.NextNode => ref m_NextNode;

            static WaitAsyncSource() {
                TaskPool.RegisterSizeGetter(typeof(WaitAsyncSource), () => s_Pool.Size);
            }

            private ReadOnlyAsyncReactiveProperty<T> m_Parent;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private UniTaskCompletionSourceCore<T> m_Core;

            private WaitAsyncSource() { }

            public static IUniTaskSource<T> Create(
                ReadOnlyAsyncReactiveProperty<T> parent,
                CancellationToken cancellationToken,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new WaitAsyncSource();
                }

                result.m_Parent = parent;
                result.m_CancellationToken = cancellationToken;

                if (cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, result);
                }

                result.m_Parent.m_TriggerEvent.Add(result);

                TaskTracker.TrackActiveTask(result, 3);

                token = result.m_Core.Version;
                return result;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_CancellationTokenRegistration.Dispose();
                m_CancellationTokenRegistration = default;
                m_Parent.m_TriggerEvent.Remove(this);
                m_Parent = null;
                m_CancellationToken = default;
                return s_Pool.TryPush(this);
            }

            private static void CancellationCallback(object state) {
                var self = (WaitAsyncSource)state;
                self.OnCanceled(self.m_CancellationToken);
            }

            // IUniTaskSource

            public T GetResult(short token) {
                try {
                    return m_Core.GetResult(token);
                } finally {
                    TryReturn();
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            // ITriggerHandler

            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public void OnCanceled(CancellationToken cancellationToken) {
                m_Core.TrySetCanceled(cancellationToken);
            }

            public void OnCompleted() {
                // Complete as Cancel.
                m_Core.TrySetCanceled(CancellationToken.None);
            }

            public void OnError(Exception ex) {
                m_Core.TrySetException(ex);
            }

            public void OnNext(T value) {
                m_Core.TrySetResult(value);
            }
        }

        private sealed class WithoutCurrentEnumerable : IUniTaskAsyncEnumerable<T> {
            private readonly ReadOnlyAsyncReactiveProperty<T> m_Parent;

            public WithoutCurrentEnumerable(ReadOnlyAsyncReactiveProperty<T> parent) {
                m_Parent = parent;
            }

            public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
                return new Enumerator(m_Parent, cancellationToken, false);
            }
        }

        private sealed class Enumerator : MoveNextSource, IUniTaskAsyncEnumerator<T>, ITriggerHandler<T> {
            private static Action<object> s_CancellationCallback = CancellationCallback;

            private readonly ReadOnlyAsyncReactiveProperty<T> m_Parent;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_CancellationTokenRegistration;
            private T m_Value;
            private bool m_IsDisposed;
            private bool m_FirstCall;

            public Enumerator(
                ReadOnlyAsyncReactiveProperty<T> parent,
                CancellationToken cancellationToken,
                bool publishCurrentValue
            ) {
                m_Parent = parent;
                m_CancellationToken = cancellationToken;
                m_FirstCall = publishCurrentValue;

                parent.m_TriggerEvent.Add(this);
                TaskTracker.TrackActiveTask(this, 3);

                if (cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
                }
            }

            public T Current => m_Value;
            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public UniTask<bool> MoveNextAsync() {
                // raise latest value on first call.
                if (m_FirstCall) {
                    m_FirstCall = false;
                    m_Value = m_Parent.Value;
                    return CompletedTasks.True;
                }

                mCompletionSource.Reset();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    mCompletionSource.TrySetCanceled(m_CancellationToken);
                    m_Parent.m_TriggerEvent.Remove(this);
                }

                return default;
            }

            public void OnNext(T value) {
                m_Value = value;
                mCompletionSource.TrySetResult(true);
            }

            public void OnCanceled(CancellationToken cancellationToken) {
                DisposeAsync().Forget();
            }

            public void OnCompleted() {
                mCompletionSource.TrySetResult(false);
            }

            public void OnError(Exception ex) {
                mCompletionSource.TrySetException(ex);
            }

            private static void CancellationCallback(object state) {
                var self = (Enumerator)state;
                self.DisposeAsync().Forget();
            }
        }
    }

    public static class StateExtensions {
        public static ReadOnlyAsyncReactiveProperty<T> ToReadOnlyAsyncReactiveProperty<T>(
            this IUniTaskAsyncEnumerable<T> source,
            CancellationToken cancellationToken
        ) {
            return new ReadOnlyAsyncReactiveProperty<T>(source, cancellationToken);
        }

        public static ReadOnlyAsyncReactiveProperty<T> ToReadOnlyAsyncReactiveProperty<T>(
            this IUniTaskAsyncEnumerable<T> source,
            T initialValue,
            CancellationToken cancellationToken
        ) {
            return new ReadOnlyAsyncReactiveProperty<T>(initialValue, source, cancellationToken);
        }
    }
}