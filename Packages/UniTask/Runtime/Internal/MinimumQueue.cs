#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.CompilerServices;

namespace Cysharp.Threading.Tasks.Internal {
    // optimized version of Standard Queue<T>.
    internal class MinimumQueue<T> {
        private const int k_MinimumGrow = 4;
        private const int k_GrowFactor = 200;

        private T[] m_Array;
        private int m_Head;
        private int m_Tail;
        private int m_Size;

        public MinimumQueue(int capacity) {
            if (capacity < 0) {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            m_Array = new T[capacity];
            m_Head = m_Tail = m_Size = 0;
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Size;
        }

        public T Peek() {
            if (m_Size == 0) {
                ThrowForEmptyQueue();
            }

            return m_Array[m_Head];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item) {
            if (m_Size == m_Array.Length) {
                Grow();
            }

            m_Array[m_Tail] = item;
            MoveNext(ref m_Tail);
            m_Size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue() {
            if (m_Size == 0) {
                ThrowForEmptyQueue();
            }

            int head = m_Head;
            T[] array = m_Array;
            T removed = array[head];
            array[head] = default(T);
            MoveNext(ref m_Head);
            m_Size--;
            return removed;
        }

        private void Grow() {
            int newcapacity = (int)((long)m_Array.Length * (long)k_GrowFactor / 100);

            if (newcapacity < m_Array.Length + k_MinimumGrow) {
                newcapacity = m_Array.Length + k_MinimumGrow;
            }

            SetCapacity(newcapacity);
        }

        private void SetCapacity(int capacity) {
            T[] newarray = new T[capacity];

            if (m_Size > 0) {
                if (m_Head < m_Tail) {
                    Array.Copy(m_Array, m_Head, newarray, 0, m_Size);
                } else {
                    Array.Copy(m_Array, m_Head, newarray, 0, m_Array.Length - m_Head);
                    Array.Copy(m_Array, 0, newarray, m_Array.Length - m_Head, m_Tail);
                }
            }

            m_Array = newarray;
            m_Head = 0;
            m_Tail = (m_Size == capacity) ? 0 : m_Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(ref int index) {
            int tmp = index + 1;

            if (tmp == m_Array.Length) {
                tmp = 0;
            }

            index = tmp;
        }

        private void ThrowForEmptyQueue() {
            throw new InvalidOperationException("EmptyQueue");
        }
    }
}