using Cysharp.Threading.Tasks;

namespace ZStudio.SpineRemoteLoader {
    public interface ISpineRemoteLoader {
        /// <summary>下载（或命中缓存）并创建一个播放实例。</summary>
        UniTask<SpineRemoteLoadResult> LoadAndPlayAsync(SpineRemoteLoadOptions options);

        /// <summary>仅下载并写入缓存，不创建实例。用于预热。</summary>
        UniTask<bool> PrewarmAsync(SpineRemoteLoadOptions options);

        /// <summary>基于已缓存资源直接创建一个新实例（不触发网络）。</summary>
        UniTask<SpineRemoteLoadResult> CreateInstanceAsync(string cacheKey, SpineRemoteLoadOptions options);

        bool TryGetCache(string cacheKey, out SpineRemoteCacheEntry entry);

        /// <summary>销毁某个实例的运行时资源并递减引用计数。</summary>
        void Release(SpineRemoteLoadResult result);

        /// <summary>释放内存缓存（含共享纹理）。仍有实例引用时会延迟到引用归零后释放。</summary>
        void ReleaseCache(string cacheKey);

        void ReleaseAll();
    }
}
