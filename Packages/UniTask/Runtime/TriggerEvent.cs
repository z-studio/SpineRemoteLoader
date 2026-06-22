using System;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public interface ITriggerHandler<T> {
        void OnNext(T value);
        void OnError(Exception ex);
        void OnCompleted();
        void OnCanceled(CancellationToken cancellationToken);

        // set/get from TriggerEvent<T>
        ITriggerHandler<T> Prev { get; set; }
        ITriggerHandler<T> Next { get; set; }
    }

    // be careful to use, itself is struct.
    public struct TriggerEvent<T> {
        private ITriggerHandler<T> m_Head; // head.prev is last
        private ITriggerHandler<T> m_IteratingHead;
        private ITriggerHandler<T> m_IteratingNode;

        private void LogError(Exception ex) {
            UnityEngine.Debug.LogException(ex);
        }

        public void SetResult(T value) {
            if (m_IteratingNode != null) {
                throw new InvalidOperationException("Can not trigger itself in iterating.");
            }

            var h = m_Head;

            while (h != null) {
                m_IteratingNode = h;

                try {
                    h.OnNext(value);
                } catch (Exception ex) {
                    LogError(ex);
                    Remove(h);
                }

                // If `h` itself is removed by OnNext, h.Next is null.
                // Therefore, instead of looking at h.Next, the `iteratingNode` reference itself is replaced.
                h = h == m_IteratingNode ? h.Next : m_IteratingNode;
            }

            m_IteratingNode = null;

            if (m_IteratingHead != null) {
                Add(m_IteratingHead);
                m_IteratingHead = null;
            }
        }

        public void SetCanceled(CancellationToken cancellationToken) {
            if (m_IteratingNode != null) {
                throw new InvalidOperationException("Can not trigger itself in iterating.");
            }

            var h = m_Head;

            while (h != null) {
                m_IteratingNode = h;

                try {
                    h.OnCanceled(cancellationToken);
                } catch (Exception ex) {
                    LogError(ex);
                }

                var next = h == m_IteratingNode ? h.Next : m_IteratingNode;
                m_IteratingNode = null;
                Remove(h);
                h = next;
            }

            m_IteratingNode = null;

            if (m_IteratingHead != null) {
                Add(m_IteratingHead);
                m_IteratingHead = null;
            }
        }

        public void SetCompleted() {
            if (m_IteratingNode != null) {
                throw new InvalidOperationException("Can not trigger itself in iterating.");
            }

            var h = m_Head;

            while (h != null) {
                m_IteratingNode = h;

                try {
                    h.OnCompleted();
                } catch (Exception ex) {
                    LogError(ex);
                }

                var next = h == m_IteratingNode ? h.Next : m_IteratingNode;
                m_IteratingNode = null;
                Remove(h);
                h = next;
            }

            m_IteratingNode = null;

            if (m_IteratingHead != null) {
                Add(m_IteratingHead);
                m_IteratingHead = null;
            }
        }

        public void SetError(Exception exception) {
            if (m_IteratingNode != null) {
                throw new InvalidOperationException("Can not trigger itself in iterating.");
            }

            var h = m_Head;

            while (h != null) {
                m_IteratingNode = h;

                try {
                    h.OnError(exception);
                } catch (Exception ex) {
                    LogError(ex);
                }

                var next = h == m_IteratingNode ? h.Next : m_IteratingNode;
                m_IteratingNode = null;
                Remove(h);
                h = next;
            }

            m_IteratingNode = null;

            if (m_IteratingHead != null) {
                Add(m_IteratingHead);
                m_IteratingHead = null;
            }
        }

        public void Add(ITriggerHandler<T> handler) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            // zero node.
            if (m_Head == null) {
                m_Head = handler;
                return;
            }

            if (m_IteratingNode != null) {
                if (m_IteratingHead == null) {
                    m_IteratingHead = handler;
                    return;
                }

                var last = m_IteratingHead.Prev;

                if (last == null) {
                    // single node.
                    m_IteratingHead.Prev = handler;
                    m_IteratingHead.Next = handler;
                    handler.Prev = m_IteratingHead;
                } else {
                    // multi node
                    m_IteratingHead.Prev = handler;
                    last.Next = handler;
                    handler.Prev = last;
                }
            } else {
                var last = m_Head.Prev;

                if (last == null) {
                    // single node.
                    m_Head.Prev = handler;
                    m_Head.Next = handler;
                    handler.Prev = m_Head;
                } else {
                    // multi node
                    m_Head.Prev = handler;
                    last.Next = handler;
                    handler.Prev = last;
                }
            }
        }

        public void Remove(ITriggerHandler<T> handler) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var prev = handler.Prev;
            var next = handler.Next;

            if (next != null) {
                next.Prev = prev;
            }

            if (handler == m_Head) {
                m_Head = next;
            }

            // when handler is head, prev indicate last so don't use it.
            else if (prev != null) {
                prev.Next = next;
            }

            if (handler == m_IteratingNode) {
                m_IteratingNode = next;
            }

            if (handler == m_IteratingHead) {
                m_IteratingHead = next;
            }

            if (m_Head != null) {
                if (m_Head.Prev == handler) {
                    if (prev != m_Head) {
                        m_Head.Prev = prev;
                    } else {
                        m_Head.Prev = null;
                    }
                }
            }

            if (m_IteratingHead != null) {
                if (m_IteratingHead.Prev == handler) {
                    if (prev != m_IteratingHead.Prev) {
                        m_IteratingHead.Prev = prev;
                    } else {
                        m_IteratingHead.Prev = null;
                    }
                }
            }

            handler.Prev = null;
            handler.Next = null;
        }
    }
}