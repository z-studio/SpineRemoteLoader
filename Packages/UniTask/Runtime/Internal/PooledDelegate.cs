using System;
using System.Runtime.CompilerServices;

namespace Cysharp.Threading.Tasks.Internal {
    internal sealed class PooledDelegate<T> : ITaskPoolNode<PooledDelegate<T>> {
        private static TaskPool<PooledDelegate<T>> s_Pool;

        private PooledDelegate<T> m_NextNode;
        public ref PooledDelegate<T> NextNode => ref m_NextNode;

        static PooledDelegate() {
            TaskPool.RegisterSizeGetter(typeof(PooledDelegate<T>), () => s_Pool.Size);
        }

        private readonly Action<T> m_RunDelegate;
        private Action m_Continuation;

        private PooledDelegate() {
            m_RunDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T> Create(Action continuation) {
            if (!s_Pool.TryPop(out var item)) {
                item = new PooledDelegate<T>();
            }

            item.m_Continuation = continuation;
            return item.m_RunDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run(T _) {
            var call = m_Continuation;
            m_Continuation = null;

            if (call != null) {
                s_Pool.TryPush(this);
                call.Invoke();
            }
        }
    }
}