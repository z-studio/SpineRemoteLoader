using System.Collections.Generic;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 内存缓存：按 cacheKey 存储 <see cref="SpineRemoteCacheEntry"/>，并为每个被缓存的条目持有一份引用计数。
    /// 仅在主线程访问。
    /// </summary>
    internal sealed class SpineRemoteCache {
        private readonly Dictionary<string, SpineRemoteCacheEntry> m_Entries = new();

        public bool TryGet(string cacheKey, out SpineRemoteCacheEntry entry) {
            return m_Entries.TryGetValue(cacheKey, out entry);
        }

        public bool Contains(string cacheKey) {
            return m_Entries.ContainsKey(cacheKey);
        }

        public int RefCountOf(string cacheKey) {
            return m_Entries.TryGetValue(cacheKey, out var entry) ? entry.RefCount : 0;
        }

        /// <summary>加入缓存并持有一份引用。已存在同键时忽略。</summary>
        public void Add(SpineRemoteCacheEntry entry) {
            if (entry == null || m_Entries.ContainsKey(entry.cacheKey)) {
                return;
            }

            m_Entries[entry.cacheKey] = entry;
            entry.Retain();
        }

        /// <summary>移出缓存并释放缓存持有的那份引用（仍被实例引用时不会立即销毁）。</summary>
        public void Remove(string cacheKey) {
            if (!m_Entries.TryGetValue(cacheKey, out var entry)) {
                return;
            }

            m_Entries.Remove(cacheKey);
            entry.Release();
        }

        public void Clear() {
            var keys = new List<string>(m_Entries.Keys);

            foreach (var key in keys) {
                Remove(key);
            }
        }
    }
}
