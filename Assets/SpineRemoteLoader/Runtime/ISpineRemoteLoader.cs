using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// Spine 远程加载器接口。所有方法仅应在主线程调用。
    /// </summary>
    public interface ISpineRemoteLoader {
        /// <summary>下载（或命中缓存）并创建一个播放实例。</summary>
        Awaitable<SpineRemoteLoadResult> LoadAndPlayAsync(SpineRemoteLoadOptions options);

        /// <summary>仅下载并写入缓存，不创建实例。用于预热。</summary>
        Awaitable<bool> PrewarmAsync(SpineRemoteLoadOptions options);

        /// <summary>基于已缓存资源同步创建一个新实例（不触发网络）。缓存不存在时返回失败结果。</summary>
        SpineRemoteLoadResult CreateInstance(string cacheKey, SpineRemoteLoadOptions options);

        /// <summary>指定 cacheKey 是否已在内存缓存中。</summary>
        bool IsCached(string cacheKey);

        /// <summary>销毁某个实例的运行时资源并递减共享资源引用计数。</summary>
        void Release(SpineRemoteLoadResult result);

        /// <summary>从缓存移除条目并释放其缓存引用；仍有实例引用时会延迟到引用归零后销毁共享资源。</summary>
        void ReleaseCache(string cacheKey);

        void ReleaseAll();
    }
}
