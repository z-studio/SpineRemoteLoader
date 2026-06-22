#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Internal {
    // Add, Remove, Enumerate with sweep. All operations are thread safe(in spinlock).
    internal class WeakDictionary<TKey, TValue>
        where TKey : class {
        private Entry[] m_Buckets;
        private int m_Size;
        private SpinLock m_Gate; // mutable struct(not readonly)

        private readonly float m_LoadFactor;
        private readonly IEqualityComparer<TKey> m_KeyEqualityComparer;

        public WeakDictionary(int capacity = 4, float loadFactor = 0.75f, IEqualityComparer<TKey> keyComparer = null) {
            var tableSize = CalculateCapacity(capacity, loadFactor);
            m_Buckets = new Entry[tableSize];
            m_LoadFactor = loadFactor;
            m_Gate = new SpinLock(false);
            m_KeyEqualityComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        }

        public bool TryAdd(TKey key, TValue value) {
            var lockTaken = false;

            try {
                m_Gate.Enter(ref lockTaken);
                return TryAddInternal(key, value);
            } finally {
                if (lockTaken) {
                    m_Gate.Exit(false);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value) {
            var lockTaken = false;

            try {
                m_Gate.Enter(ref lockTaken);

                if (TryGetEntry(key, out _, out var entry)) {
                    value = entry.Value;
                    return true;
                }

                value = default(TValue);
                return false;
            } finally {
                if (lockTaken) {
                    m_Gate.Exit(false);
                }
            }
        }

        public bool TryRemove(TKey key) {
            var lockTaken = false;

            try {
                m_Gate.Enter(ref lockTaken);

                if (TryGetEntry(key, out var hashIndex, out var entry)) {
                    Remove(hashIndex, entry);
                    return true;
                }

                return false;
            } finally {
                if (lockTaken) {
                    m_Gate.Exit(false);
                }
            }
        }

        private bool TryAddInternal(TKey key, TValue value) {
            var nextCapacity = CalculateCapacity(m_Size + 1, m_LoadFactor);

            TRY_ADD_AGAIN:

            if (m_Buckets.Length < nextCapacity) {
                // rehash
                var nextBucket = new Entry[nextCapacity];

                for (var i = 0; i < m_Buckets.Length; i++) {
                    var e = m_Buckets[i];

                    while (e != null) {
                        AddToBuckets(nextBucket, key, e.Value, e.Hash);
                        e = e.Next;
                    }
                }

                m_Buckets = nextBucket;
                goto TRY_ADD_AGAIN;
            } else {
                // add entry
                var successAdd = AddToBuckets(m_Buckets, key, value, m_KeyEqualityComparer.GetHashCode(key));

                if (successAdd) {
                    m_Size++;
                }

                return successAdd;
            }
        }

        private bool AddToBuckets(Entry[] targetBuckets, TKey newKey, TValue value, int keyHash) {
            var h = keyHash;
            var hashIndex = h & (targetBuckets.Length - 1);

            TRY_ADD_AGAIN:

            if (targetBuckets[hashIndex] == null) {
                targetBuckets[hashIndex] = new Entry {
                    Key = new WeakReference<TKey>(newKey, false),
                    Value = value,
                    Hash = h
                };

                return true;
            } else {
                // add to last.
                var entry = targetBuckets[hashIndex];

                while (entry != null) {
                    if (entry.Key.TryGetTarget(out var target)) {
                        if (m_KeyEqualityComparer.Equals(newKey, target)) {
                            return false; // duplicate
                        }
                    } else {
                        Remove(hashIndex, entry);

                        if (targetBuckets[hashIndex] == null) {
                            goto TRY_ADD_AGAIN; // add new entry
                        }
                    }

                    if (entry.Next != null) {
                        entry = entry.Next;
                    } else {
                        // found last
                        entry.Next = new Entry {
                            Key = new WeakReference<TKey>(newKey, false),
                            Value = value,
                            Hash = h
                        };

                        entry.Next.Prev = entry;
                    }
                }

                return false;
            }
        }

        private bool TryGetEntry(TKey key, out int hashIndex, out Entry entry) {
            var table = m_Buckets;
            var hash = m_KeyEqualityComparer.GetHashCode(key);
            hashIndex = hash & table.Length - 1;
            entry = table[hashIndex];

            while (entry != null) {
                if (entry.Key.TryGetTarget(out var target)) {
                    if (m_KeyEqualityComparer.Equals(key, target)) {
                        return true;
                    }
                } else {
                    // sweap
                    Remove(hashIndex, entry);
                }

                entry = entry.Next;
            }

            return false;
        }

        private void Remove(int hashIndex, Entry entry) {
            if (entry.Prev == null && entry.Next == null) {
                m_Buckets[hashIndex] = null;
            } else {
                if (entry.Prev == null) {
                    m_Buckets[hashIndex] = entry.Next;
                }

                if (entry.Prev != null) {
                    entry.Prev.Next = entry.Next;
                }

                if (entry.Next != null) {
                    entry.Next.Prev = entry.Prev;
                }
            }

            m_Size--;
        }

        public List<KeyValuePair<TKey, TValue>> ToList() {
            var list = new List<KeyValuePair<TKey, TValue>>(m_Size);
            ToList(ref list, false);
            return list;
        }

        // avoid allocate everytime.
        public int ToList(ref List<KeyValuePair<TKey, TValue>> list, bool clear = true) {
            if (clear) {
                list.Clear();
            }

            var listIndex = 0;
            var lockTaken = false;

            try {
                for (var i = 0; i < m_Buckets.Length; i++) {
                    var entry = m_Buckets[i];

                    while (entry != null) {
                        if (entry.Key.TryGetTarget(out var target)) {
                            var item = new KeyValuePair<TKey, TValue>(target, entry.Value);

                            if (listIndex < list.Count) {
                                list[listIndex++] = item;
                            } else {
                                list.Add(item);
                                listIndex++;
                            }
                        } else {
                            // sweap
                            Remove(i, entry);
                        }

                        entry = entry.Next;
                    }
                }
            } finally {
                if (lockTaken) {
                    m_Gate.Exit(false);
                }
            }

            return listIndex;
        }

        private static int CalculateCapacity(int collectionSize, float loadFactor) {
            var size = (int)(((float)collectionSize) / loadFactor);

            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size += 1;

            if (size < 8) {
                size = 8;
            }

            return size;
        }

        private class Entry {
            public WeakReference<TKey> Key;
            public TValue Value;
            public int Hash;
            public Entry Prev;
            public Entry Next;

            // debug only
            public override string ToString() {
                if (Key.TryGetTarget(out var target)) {
                    return target + "(" + Count() + ")";
                } else {
                    return "(Dead)";
                }
            }

            private int Count() {
                var count = 1;
                var n = this;

                while (n.Next != null) {
                    count++;
                    n = n.Next;
                }

                return count;
            }
        }
    }
}