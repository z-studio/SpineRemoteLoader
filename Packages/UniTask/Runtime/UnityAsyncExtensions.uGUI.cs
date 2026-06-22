#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if UNITASK_UGUI_SUPPORT
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cysharp.Threading.Tasks {
    public static partial class UnityAsyncExtensions {
        public static AsyncUnityEventHandler GetAsyncEventHandler(
            this UnityEvent unityEvent,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler(unityEvent, cancellationToken, false);
        }

        public static UniTask OnInvokeAsync(this UnityEvent unityEvent, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler(unityEvent, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> OnInvokeAsAsyncEnumerable(
            this UnityEvent unityEvent,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable(unityEvent, cancellationToken);
        }

        public static AsyncUnityEventHandler<T> GetAsyncEventHandler<T>(
            this UnityEvent<T> unityEvent,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<T>(unityEvent, cancellationToken, false);
        }

        public static UniTask<T> OnInvokeAsync<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler<T>(unityEvent, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<T> OnInvokeAsAsyncEnumerable<T>(
            this UnityEvent<T> unityEvent,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<T>(unityEvent, cancellationToken);
        }

        public static IAsyncClickEventHandler GetAsyncClickEventHandler(this Button button) {
            return new AsyncUnityEventHandler(button.onClick, button.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncClickEventHandler GetAsyncClickEventHandler(
            this Button button,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler(button.onClick, cancellationToken, false);
        }

        public static UniTask OnClickAsync(this Button button) {
            return new AsyncUnityEventHandler(button.onClick, button.GetCancellationTokenOnDestroy(), true)
                .OnInvokeAsync();
        }

        public static UniTask OnClickAsync(this Button button, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler(button.onClick, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> OnClickAsAsyncEnumerable(this Button button) {
            return new UnityEventHandlerAsyncEnumerable(button.onClick, button.GetCancellationTokenOnDestroy());
        }

        public static IUniTaskAsyncEnumerable<AsyncUnit> OnClickAsAsyncEnumerable(
            this Button button,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable(button.onClick, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<bool> GetAsyncValueChangedEventHandler(this Toggle toggle) {
            return new AsyncUnityEventHandler<bool>(
                toggle.onValueChanged,
                toggle.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<bool> GetAsyncValueChangedEventHandler(
            this Toggle toggle,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<bool>(toggle.onValueChanged, cancellationToken, false);
        }

        public static UniTask<bool> OnValueChangedAsync(this Toggle toggle) {
            return new AsyncUnityEventHandler<bool>(toggle.onValueChanged, toggle.GetCancellationTokenOnDestroy(), true)
                .OnInvokeAsync();
        }

        public static UniTask<bool> OnValueChangedAsync(this Toggle toggle, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler<bool>(toggle.onValueChanged, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<bool> OnValueChangedAsAsyncEnumerable(this Toggle toggle) {
            return new UnityEventHandlerAsyncEnumerable<bool>(
                toggle.onValueChanged,
                toggle.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<bool> OnValueChangedAsAsyncEnumerable(
            this Toggle toggle,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<bool>(toggle.onValueChanged, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Scrollbar scrollbar) {
            return new AsyncUnityEventHandler<float>(
                scrollbar.onValueChanged,
                scrollbar.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(
            this Scrollbar scrollbar,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<float>(scrollbar.onValueChanged, cancellationToken, false);
        }

        public static UniTask<float> OnValueChangedAsync(this Scrollbar scrollbar) {
            return new AsyncUnityEventHandler<float>(
                scrollbar.onValueChanged,
                scrollbar.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<float> OnValueChangedAsync(
            this Scrollbar scrollbar,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<float>(scrollbar.onValueChanged, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<float> OnValueChangedAsAsyncEnumerable(this Scrollbar scrollbar) {
            return new UnityEventHandlerAsyncEnumerable<float>(
                scrollbar.onValueChanged,
                scrollbar.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<float> OnValueChangedAsAsyncEnumerable(
            this Scrollbar scrollbar,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<float>(scrollbar.onValueChanged, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<Vector2> GetAsyncValueChangedEventHandler(
            this ScrollRect scrollRect
        ) {
            return new AsyncUnityEventHandler<Vector2>(
                scrollRect.onValueChanged,
                scrollRect.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<Vector2> GetAsyncValueChangedEventHandler(
            this ScrollRect scrollRect,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<Vector2>(scrollRect.onValueChanged, cancellationToken, false);
        }

        public static UniTask<Vector2> OnValueChangedAsync(this ScrollRect scrollRect) {
            return new AsyncUnityEventHandler<Vector2>(
                scrollRect.onValueChanged,
                scrollRect.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<Vector2> OnValueChangedAsync(
            this ScrollRect scrollRect,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<Vector2>(scrollRect.onValueChanged, cancellationToken, true)
                .OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<Vector2> OnValueChangedAsAsyncEnumerable(this ScrollRect scrollRect) {
            return new UnityEventHandlerAsyncEnumerable<Vector2>(
                scrollRect.onValueChanged,
                scrollRect.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<Vector2> OnValueChangedAsAsyncEnumerable(
            this ScrollRect scrollRect,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<Vector2>(scrollRect.onValueChanged, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Slider slider) {
            return new AsyncUnityEventHandler<float>(
                slider.onValueChanged,
                slider.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(
            this Slider slider,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<float>(slider.onValueChanged, cancellationToken, false);
        }

        public static UniTask<float> OnValueChangedAsync(this Slider slider) {
            return new AsyncUnityEventHandler<float>(
                slider.onValueChanged,
                slider.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<float> OnValueChangedAsync(this Slider slider, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler<float>(slider.onValueChanged, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<float> OnValueChangedAsAsyncEnumerable(this Slider slider) {
            return new UnityEventHandlerAsyncEnumerable<float>(
                slider.onValueChanged,
                slider.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<float> OnValueChangedAsAsyncEnumerable(
            this Slider slider,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<float>(slider.onValueChanged, cancellationToken);
        }

        public static IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(this InputField inputField) {
            return new AsyncUnityEventHandler<string>(
                inputField.onEndEdit,
                inputField.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(
            this InputField inputField,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<string>(inputField.onEndEdit, cancellationToken, false);
        }

        public static UniTask<string> OnEndEditAsync(this InputField inputField) {
            return new AsyncUnityEventHandler<string>(
                inputField.onEndEdit,
                inputField.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<string> OnEndEditAsync(this InputField inputField, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler<string>(inputField.onEndEdit, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<string> OnEndEditAsAsyncEnumerable(this InputField inputField) {
            return new UnityEventHandlerAsyncEnumerable<string>(
                inputField.onEndEdit,
                inputField.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<string> OnEndEditAsAsyncEnumerable(
            this InputField inputField,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<string>(inputField.onEndEdit, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(
            this InputField inputField
        ) {
            return new AsyncUnityEventHandler<string>(
                inputField.onValueChanged,
                inputField.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(
            this InputField inputField,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<string>(inputField.onValueChanged, cancellationToken, false);
        }

        public static UniTask<string> OnValueChangedAsync(this InputField inputField) {
            return new AsyncUnityEventHandler<string>(
                inputField.onValueChanged,
                inputField.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<string> OnValueChangedAsync(
            this InputField inputField,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<string>(inputField.onValueChanged, cancellationToken, true)
                .OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<string> OnValueChangedAsAsyncEnumerable(this InputField inputField) {
            return new UnityEventHandlerAsyncEnumerable<string>(
                inputField.onValueChanged,
                inputField.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<string> OnValueChangedAsAsyncEnumerable(
            this InputField inputField,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<string>(inputField.onValueChanged, cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<int> GetAsyncValueChangedEventHandler(this Dropdown dropdown) {
            return new AsyncUnityEventHandler<int>(
                dropdown.onValueChanged,
                dropdown.GetCancellationTokenOnDestroy(),
                false
            );
        }

        public static IAsyncValueChangedEventHandler<int> GetAsyncValueChangedEventHandler(
            this Dropdown dropdown,
            CancellationToken cancellationToken
        ) {
            return new AsyncUnityEventHandler<int>(dropdown.onValueChanged, cancellationToken, false);
        }

        public static UniTask<int> OnValueChangedAsync(this Dropdown dropdown) {
            return new AsyncUnityEventHandler<int>(
                dropdown.onValueChanged,
                dropdown.GetCancellationTokenOnDestroy(),
                true
            ).OnInvokeAsync();
        }

        public static UniTask<int> OnValueChangedAsync(this Dropdown dropdown, CancellationToken cancellationToken) {
            return new AsyncUnityEventHandler<int>(dropdown.onValueChanged, cancellationToken, true).OnInvokeAsync();
        }

        public static IUniTaskAsyncEnumerable<int> OnValueChangedAsAsyncEnumerable(this Dropdown dropdown) {
            return new UnityEventHandlerAsyncEnumerable<int>(
                dropdown.onValueChanged,
                dropdown.GetCancellationTokenOnDestroy()
            );
        }

        public static IUniTaskAsyncEnumerable<int> OnValueChangedAsAsyncEnumerable(
            this Dropdown dropdown,
            CancellationToken cancellationToken
        ) {
            return new UnityEventHandlerAsyncEnumerable<int>(dropdown.onValueChanged, cancellationToken);
        }
    }

    public interface IAsyncClickEventHandler : IDisposable {
        UniTask OnClickAsync();
    }

    public interface IAsyncValueChangedEventHandler<T> : IDisposable {
        UniTask<T> OnValueChangedAsync();
    }

    public interface IAsyncEndEditEventHandler<T> : IDisposable {
        UniTask<T> OnEndEditAsync();
    }

    // for TMP_PRO

    public interface IAsyncEndTextSelectionEventHandler<T> : IDisposable {
        UniTask<T> OnEndTextSelectionAsync();
    }

    public interface IAsyncTextSelectionEventHandler<T> : IDisposable {
        UniTask<T> OnTextSelectionAsync();
    }

    public interface IAsyncDeselectEventHandler<T> : IDisposable {
        UniTask<T> OnDeselectAsync();
    }

    public interface IAsyncSelectEventHandler<T> : IDisposable {
        UniTask<T> OnSelectAsync();
    }

    public interface IAsyncSubmitEventHandler<T> : IDisposable {
        UniTask<T> OnSubmitAsync();
    }

    internal class TextSelectionEventConverter : UnityEvent<(string, int, int)>, IDisposable {
        private readonly UnityEvent<string, int, int> m_InnerEvent;
        private readonly UnityAction<string, int, int> m_InvokeDelegate;

        public TextSelectionEventConverter(UnityEvent<string, int, int> unityEvent) {
            m_InnerEvent = unityEvent;
            m_InvokeDelegate = InvokeCore;

            m_InnerEvent.AddListener(m_InvokeDelegate);
        }

        private void InvokeCore(string item1, int item2, int item3) {
            Invoke((item1, item2, item3));
        }

        public void Dispose() {
            m_InnerEvent.RemoveListener(m_InvokeDelegate);
        }
    }

    public class AsyncUnityEventHandler : IUniTaskSource, IDisposable, IAsyncClickEventHandler {
        private static Action<object> s_CancellationCallback = CancellationCallback;

        private readonly UnityAction m_Action;
        private readonly UnityEvent m_UnityEvent;

        private CancellationToken m_CancellationToken;
        private CancellationTokenRegistration m_Registration;
        private bool m_IsDisposed;
        private bool m_CallOnce;

        private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

        public AsyncUnityEventHandler(UnityEvent unityEvent, CancellationToken cancellationToken, bool callOnce) {
            m_CancellationToken = cancellationToken;

            if (cancellationToken.IsCancellationRequested) {
                m_IsDisposed = true;
                return;
            }

            m_Action = Invoke;
            m_UnityEvent = unityEvent;
            m_CallOnce = callOnce;

            unityEvent.AddListener(m_Action);

            if (cancellationToken.CanBeCanceled) {
                m_Registration = cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
            }

            TaskTracker.TrackActiveTask(this, 3);
        }

        public UniTask OnInvokeAsync() {
            m_Core.Reset();

            if (m_IsDisposed) {
                m_Core.TrySetCanceled(this.m_CancellationToken);
            }

            return new UniTask(this, m_Core.Version);
        }

        private void Invoke() {
            m_Core.TrySetResult(AsyncUnit.Default);
        }

        private static void CancellationCallback(object state) {
            var self = (AsyncUnityEventHandler)state;
            self.Dispose();
        }

        public void Dispose() {
            if (!m_IsDisposed) {
                m_IsDisposed = true;
                TaskTracker.RemoveTracking(this);
                m_Registration.Dispose();

                if (m_UnityEvent != null) {
                    m_UnityEvent.RemoveListener(m_Action);
                }

                m_Core.TrySetCanceled(m_CancellationToken);
            }
        }

        UniTask IAsyncClickEventHandler.OnClickAsync() {
            return OnInvokeAsync();
        }

        void IUniTaskSource.GetResult(short token) {
            try {
                m_Core.GetResult(token);
            } finally {
                if (m_CallOnce) {
                    Dispose();
                }
            }
        }

        UniTaskStatus IUniTaskSource.GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        UniTaskStatus IUniTaskSource.UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }
    }

    public class AsyncUnityEventHandler<T> : IUniTaskSource<T>,
                                             IDisposable,
                                             IAsyncValueChangedEventHandler<T>,
                                             IAsyncEndEditEventHandler<T>,
                                             IAsyncEndTextSelectionEventHandler<T>,
                                             IAsyncTextSelectionEventHandler<T>,
                                             IAsyncDeselectEventHandler<T>,
                                             IAsyncSelectEventHandler<T>,
                                             IAsyncSubmitEventHandler<T> {
        private static Action<object> s_CancellationCallback = CancellationCallback;

        private readonly UnityAction<T> m_Action;
        private readonly UnityEvent<T> m_UnityEvent;

        private CancellationToken m_CancellationToken;
        private CancellationTokenRegistration m_Registration;
        private bool m_IsDisposed;
        private bool m_CallOnce;

        private UniTaskCompletionSourceCore<T> m_Core;

        public AsyncUnityEventHandler(UnityEvent<T> unityEvent, CancellationToken cancellationToken, bool callOnce) {
            m_CancellationToken = cancellationToken;

            if (cancellationToken.IsCancellationRequested) {
                m_IsDisposed = true;
                return;
            }

            m_Action = Invoke;
            m_UnityEvent = unityEvent;
            m_CallOnce = callOnce;

            unityEvent.AddListener(m_Action);

            if (cancellationToken.CanBeCanceled) {
                m_Registration = cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
            }

            TaskTracker.TrackActiveTask(this, 3);
        }

        public UniTask<T> OnInvokeAsync() {
            m_Core.Reset();

            if (m_IsDisposed) {
                m_Core.TrySetCanceled(this.m_CancellationToken);
            }

            return new UniTask<T>(this, m_Core.Version);
        }

        private void Invoke(T result) {
            m_Core.TrySetResult(result);
        }

        private static void CancellationCallback(object state) {
            var self = (AsyncUnityEventHandler<T>)state;
            self.Dispose();
        }

        public void Dispose() {
            if (!m_IsDisposed) {
                m_IsDisposed = true;
                TaskTracker.RemoveTracking(this);
                m_Registration.Dispose();

                if (m_UnityEvent != null) {
                    // Dispose inner delegate for TextSelectionEventConverter
                    if (m_UnityEvent is IDisposable disp) {
                        disp.Dispose();
                    }

                    m_UnityEvent.RemoveListener(m_Action);
                }

                m_Core.TrySetCanceled();
            }
        }

        UniTask<T> IAsyncValueChangedEventHandler<T>.OnValueChangedAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncEndEditEventHandler<T>.OnEndEditAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncEndTextSelectionEventHandler<T>.OnEndTextSelectionAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncTextSelectionEventHandler<T>.OnTextSelectionAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncDeselectEventHandler<T>.OnDeselectAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncSelectEventHandler<T>.OnSelectAsync() {
            return OnInvokeAsync();
        }

        UniTask<T> IAsyncSubmitEventHandler<T>.OnSubmitAsync() {
            return OnInvokeAsync();
        }

        T IUniTaskSource<T>.GetResult(short token) {
            try {
                return m_Core.GetResult(token);
            } finally {
                if (m_CallOnce) {
                    Dispose();
                }
            }
        }

        void IUniTaskSource.GetResult(short token) {
            ((IUniTaskSource<T>)this).GetResult(token);
        }

        UniTaskStatus IUniTaskSource.GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        UniTaskStatus IUniTaskSource.UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }
    }

    public class UnityEventHandlerAsyncEnumerable : IUniTaskAsyncEnumerable<AsyncUnit> {
        private readonly UnityEvent m_UnityEvent;
        private readonly CancellationToken m_CancellationToken1;

        public UnityEventHandlerAsyncEnumerable(UnityEvent unityEvent, CancellationToken cancellationToken) {
            m_UnityEvent = unityEvent;
            m_CancellationToken1 = cancellationToken;
        }

        public IUniTaskAsyncEnumerator<AsyncUnit> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            if (m_CancellationToken1 == cancellationToken) {
                return new UnityEventHandlerAsyncEnumerator(
                    m_UnityEvent,
                    m_CancellationToken1,
                    CancellationToken.None
                );
            } else {
                return new UnityEventHandlerAsyncEnumerator(m_UnityEvent, m_CancellationToken1, cancellationToken);
            }
        }

        private class UnityEventHandlerAsyncEnumerator : MoveNextSource, IUniTaskAsyncEnumerator<AsyncUnit> {
            private static readonly Action<object> s_Cancel1 = OnCanceled1;
            private static readonly Action<object> s_Cancel2 = OnCanceled2;

            private readonly UnityEvent m_InnerUnityEvent;
            private CancellationToken m_InnerCancellationToken1;
            private CancellationToken m_CancellationToken2;

            private UnityAction m_UnityAction;
            private CancellationTokenRegistration m_Registration1;
            private CancellationTokenRegistration m_Registration2;
            private bool m_IsDisposed;

            public UnityEventHandlerAsyncEnumerator(
                UnityEvent unityEvent,
                CancellationToken cancellationToken1,
                CancellationToken cancellationToken2
            ) {
                m_InnerUnityEvent = unityEvent;
                m_InnerCancellationToken1 = cancellationToken1;
                m_CancellationToken2 = cancellationToken2;
            }

            public AsyncUnit Current => default;

            public UniTask<bool> MoveNextAsync() {
                m_InnerCancellationToken1.ThrowIfCancellationRequested();
                m_CancellationToken2.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_UnityAction == null) {
                    m_UnityAction = Invoke;

                    TaskTracker.TrackActiveTask(this, 3);
                    m_InnerUnityEvent.AddListener(m_UnityAction);

                    if (m_InnerCancellationToken1.CanBeCanceled) {
                        m_Registration1 = m_InnerCancellationToken1.RegisterWithoutCaptureExecutionContext(s_Cancel1, this);
                    }

                    if (m_CancellationToken2.CanBeCanceled) {
                        m_Registration2 = m_CancellationToken2.RegisterWithoutCaptureExecutionContext(s_Cancel2, this);
                    }
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void Invoke() {
                mCompletionSource.TrySetResult(true);
            }

            private static void OnCanceled1(object state) {
                var self = (UnityEventHandlerAsyncEnumerator)state;

                try {
                    self.mCompletionSource.TrySetCanceled(self.m_InnerCancellationToken1);
                } finally {
                    self.DisposeAsync().Forget();
                }
            }

            private static void OnCanceled2(object state) {
                var self = (UnityEventHandlerAsyncEnumerator)state;

                try {
                    self.mCompletionSource.TrySetCanceled(self.m_CancellationToken2);
                } finally {
                    self.DisposeAsync().Forget();
                }
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    m_Registration1.Dispose();
                    m_Registration2.Dispose();
                    m_InnerUnityEvent.RemoveListener(m_UnityAction);

                    mCompletionSource.TrySetCanceled();
                }

                return default;
            }
        }
    }

    public class UnityEventHandlerAsyncEnumerable<T> : IUniTaskAsyncEnumerable<T> {
        private readonly UnityEvent<T> m_UnityEvent;
        private readonly CancellationToken m_CancellationToken1;

        public UnityEventHandlerAsyncEnumerable(UnityEvent<T> unityEvent, CancellationToken cancellationToken) {
            m_UnityEvent = unityEvent;
            m_CancellationToken1 = cancellationToken;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            if (m_CancellationToken1 == cancellationToken) {
                return new UnityEventHandlerAsyncEnumerator(
                    m_UnityEvent,
                    m_CancellationToken1,
                    CancellationToken.None
                );
            } else {
                return new UnityEventHandlerAsyncEnumerator(m_UnityEvent, this.m_CancellationToken1, cancellationToken);
            }
        }

        private class UnityEventHandlerAsyncEnumerator : MoveNextSource, IUniTaskAsyncEnumerator<T> {
            private static readonly Action<object> s_Cancel1 = OnCanceled1;
            private static readonly Action<object> s_Cancel2 = OnCanceled2;

            private readonly UnityEvent<T> m_InnerUnityEvent;
            private CancellationToken m_InnerCancellationToken1;
            private CancellationToken m_CancellationToken2;

            private UnityAction<T> m_UnityAction;
            private CancellationTokenRegistration m_Registration1;
            private CancellationTokenRegistration m_Registration2;
            private bool m_IsDisposed;

            public UnityEventHandlerAsyncEnumerator(
                UnityEvent<T> unityEvent,
                CancellationToken cancellationToken1,
                CancellationToken cancellationToken2
            ) {
                m_InnerUnityEvent = unityEvent;
                m_InnerCancellationToken1 = cancellationToken1;
                m_CancellationToken2 = cancellationToken2;
            }

            public T Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_InnerCancellationToken1.ThrowIfCancellationRequested();
                m_CancellationToken2.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_UnityAction == null) {
                    m_UnityAction = Invoke;

                    TaskTracker.TrackActiveTask(this, 3);
                    m_InnerUnityEvent.AddListener(m_UnityAction);

                    if (m_InnerCancellationToken1.CanBeCanceled) {
                        m_Registration1 = m_InnerCancellationToken1.RegisterWithoutCaptureExecutionContext(s_Cancel1, this);
                    }

                    if (m_CancellationToken2.CanBeCanceled) {
                        m_Registration2 = m_CancellationToken2.RegisterWithoutCaptureExecutionContext(s_Cancel2, this);
                    }
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void Invoke(T value) {
                Current = value;
                mCompletionSource.TrySetResult(true);
            }

            private static void OnCanceled1(object state) {
                var self = (UnityEventHandlerAsyncEnumerator)state;

                try {
                    self.mCompletionSource.TrySetCanceled(self.m_InnerCancellationToken1);
                } finally {
                    self.DisposeAsync().Forget();
                }
            }

            private static void OnCanceled2(object state) {
                var self = (UnityEventHandlerAsyncEnumerator)state;

                try {
                    self.mCompletionSource.TrySetCanceled(self.m_CancellationToken2);
                } finally {
                    self.DisposeAsync().Forget();
                }
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    m_Registration1.Dispose();
                    m_Registration2.Dispose();

                    if (m_InnerUnityEvent is IDisposable disp) {
                        disp.Dispose();
                    }

                    m_InnerUnityEvent.RemoveListener(m_UnityAction);

                    mCompletionSource.TrySetCanceled();
                }

                return default;
            }
        }
    }
}

#endif