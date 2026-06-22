using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    /// <summary>
    /// Lightweight IProgress[T] factory.
    /// </summary>
    public static class Progress {
        public static IProgress<T> Create<T>(Action<T> handler) {
            if (handler == null) {
                return NullProgress<T>.Instance;
            }

            return new AnonymousProgress<T>(handler);
        }

        public static IProgress<T> CreateOnlyValueChanged<T>(Action<T> handler, IEqualityComparer<T> comparer = null) {
            if (handler == null) {
                return NullProgress<T>.Instance;
            }

            return new OnlyValueChangedProgress<T>(handler, comparer ?? UnityEqualityComparer.GetDefault<T>());
        }

        private sealed class NullProgress<T> : IProgress<T> {
            public static readonly IProgress<T> Instance = new NullProgress<T>();

            NullProgress() { }

            public void Report(T value) { }
        }

        private sealed class AnonymousProgress<T> : IProgress<T> {
            private readonly Action<T> m_Action;

            public AnonymousProgress(Action<T> action) {
                m_Action = action;
            }

            public void Report(T value) {
                m_Action(value);
            }
        }

        private sealed class OnlyValueChangedProgress<T> : IProgress<T> {
            private readonly Action<T> m_Action;
            private readonly IEqualityComparer<T> m_Comparer;
            private bool m_IsFirstCall;
            private T m_LatestValue;

            public OnlyValueChangedProgress(Action<T> action, IEqualityComparer<T> comparer) {
                m_Action = action;
                m_Comparer = comparer;
                m_IsFirstCall = true;
            }

            public void Report(T value) {
                if (m_IsFirstCall) {
                    m_IsFirstCall = false;
                } else if (m_Comparer.Equals(value, m_LatestValue)) {
                    return;
                }

                m_LatestValue = value;
                m_Action(value);
            }
        }
    }
}