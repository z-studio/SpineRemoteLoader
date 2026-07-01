using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// Spine 远程资源加载器的编排层：协调下载（<see cref="SpineRemoteFetcher"/>）、
    /// 资源构建（<see cref="SpineAssetFactory"/>）与内存缓存（<see cref="SpineRemoteCache"/>），
    /// 并管理共享资源的引用计数生命周期。
    /// 仅在主线程调用（内部字典无锁，且 Unity 对象创建必须在主线程）。
    /// </summary>
    public sealed class SpineRemoteLoader : ISpineRemoteLoader {
        private static SpineRemoteLoader s_Shared;

        /// <summary>
        /// 全局共享实例，方便快速调用。可被替换（set），便于注入自定义后端或在测试中替换。
        /// 业务层应尽量依赖 <see cref="ISpineRemoteLoader"/> 接口而非直接耦合此静态访问。
        /// </summary>
        public static SpineRemoteLoader Shared {
            get => s_Shared ??= new SpineRemoteLoader();
            set => s_Shared = value;
        }

        private readonly SpineRemoteCache m_Cache = new();
        private readonly Dictionary<string, TaskCompletionSource<SpineRemoteCacheEntry>> m_DownloadingTasks = new();

        private ISpineDownloader m_Downloader;
        private SpineRemoteFetcher m_Fetcher;

        public SpineRemoteLoader() : this(null) { }

        public SpineRemoteLoader(ISpineDownloader downloader) {
            m_Downloader = downloader ?? new UnityWebRequestSpineDownloader();
            m_Fetcher = new SpineRemoteFetcher(m_Downloader);
        }

        /// <summary>下载后端，可注入自定义实现（如 Best.HTTP）。设为 null 时回退到默认实现。</summary>
        public ISpineDownloader Downloader {
            get => m_Downloader;
            set {
                m_Downloader = value ?? new UnityWebRequestSpineDownloader();
                m_Fetcher = new SpineRemoteFetcher(m_Downloader);
            }
        }

        // 关闭域重载（Enter Play Mode without Domain Reload）时，静态状态不会自动清空，
        // 这里在进入播放模式时主动重置共享实例，避免缓存残留已销毁对象的引用。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedOnEnterPlayMode() {
            s_Shared?.ReleaseAll();
            s_Shared = null;
        }

        public async Awaitable<SpineRemoteLoadResult> LoadAndPlayAsync(SpineRemoteLoadOptions options) {
            var validationError = Validate(options, requireParent: true);

            if (validationError != null) {
                return SpineRemoteLoadResult.Fail(validationError);
            }

            var cacheKey = BuildCacheKey(options);

            try {
                var entry = await GetOrBuildEntryAsync(options, cacheKey);

                if (entry == null) {
                    return SpineRemoteLoadResult.Fail("Spine 资源下载或解析失败", cacheKey);
                }

                return CreateAndPlay(entry, options, cacheKey);
            } catch (OperationCanceledException) {
                return SpineRemoteLoadResult.Canceled(cacheKey);
            } catch (Exception e) {
                SpineRemoteLog.Error($"LoadAndPlayAsync 异常: {e}");
                return SpineRemoteLoadResult.Fail(e.Message, cacheKey);
            }
        }

        public async Awaitable<bool> PrewarmAsync(SpineRemoteLoadOptions options) {
            var validationError = Validate(options, requireParent: false);

            if (validationError != null) {
                SpineRemoteLog.Error(validationError);
                return false;
            }

            var cacheKey = BuildCacheKey(options);

            try {
                var entry = await GetOrBuildEntryAsync(options, cacheKey);
                return entry != null;
            } catch (OperationCanceledException) {
                return false;
            } catch (Exception e) {
                SpineRemoteLog.Error($"PrewarmAsync 异常: {e}");
                return false;
            }
        }

        public SpineRemoteLoadResult CreateInstance(string cacheKey, SpineRemoteLoadOptions options) {
            if (options == null) {
                return SpineRemoteLoadResult.Fail("options 不能为空", cacheKey);
            }

            if (!m_Cache.TryGet(cacheKey, out var entry)) {
                return SpineRemoteLoadResult.Fail($"缓存不存在: {cacheKey}", cacheKey);
            }

            if (options.parent == null) {
                return SpineRemoteLoadResult.Fail("Parent 不能为空", cacheKey);
            }

            return CreateAndPlay(entry, options, cacheKey);
        }

        public bool IsCached(string cacheKey) {
            return m_Cache.Contains(cacheKey);
        }

        public void Release(SpineRemoteLoadResult result) {
            if (result == null) {
                return;
            }

            if (result.gameObject != null) {
                UnityEngine.Object.Destroy(result.gameObject);
            }

            DestroyRuntimeAsset(result.skeletonDataAsset);
            DestroyRuntimeAsset(result.atlasAsset);

            if (result.materials != null) {
                foreach (var material in result.materials) {
                    DestroyRuntimeAsset(material);
                }
            }

            // 递减共享资源引用；归零（且不再被缓存持有）时自动销毁纹理等共享资源。
            result.entry?.Release();
        }

        public void ReleaseCache(string cacheKey) {
            m_Cache.Remove(cacheKey);
        }

        public void ReleaseAll() {
            m_Cache.Clear();
        }

        // ---------------------------------------------------------------------

        private async Awaitable<SpineRemoteCacheEntry> GetOrBuildEntryAsync(
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            if (options.useMemoryCache && m_Cache.TryGet(cacheKey, out var cached)) {
                options.progress?.Report(1f);
                return cached;
            }

            // 同一 cacheKey 正在下载时，复用同一个 TaskCompletionSource。
            // Awaitable 实例会被池化，不能安全地多次 await；Task 支持多个并发 awaiter。
            if (m_DownloadingTasks.TryGetValue(cacheKey, out var inFlight)) {
                return await inFlight.Task;
            }

            var tcs = new TaskCompletionSource<SpineRemoteCacheEntry>();
            m_DownloadingTasks[cacheKey] = tcs;

            try {
                var entry = await BuildEntryInternalAsync(options, cacheKey);

                if (entry != null && options.useMemoryCache && !m_Cache.Contains(cacheKey)) {
                    m_Cache.Add(entry);
                }

                tcs.TrySetResult(entry);
                return entry;
            } catch (OperationCanceledException) {
                tcs.TrySetCanceled();
                throw;
            } catch (Exception e) {
                tcs.TrySetException(e);
                throw;
            } finally {
                m_DownloadingTasks.Remove(cacheKey);
            }
        }

        private async Awaitable<SpineRemoteCacheEntry> BuildEntryInternalAsync(
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            var fetcher = m_Fetcher;
            var raw = await fetcher.FetchAsync(options);

            if (raw == null || !raw.IsValid()) {
                return null;
            }

            return SpineAssetFactory.BuildEntry(raw, options, cacheKey);
        }

        private SpineRemoteLoadResult CreateAndPlay(
            SpineRemoteCacheEntry entry,
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            var result = SpineAssetFactory.CreateInstance(entry, options, cacheKey);

            if (!result.success) {
                // 创建失败：若该条目未被缓存持有（refCount 仍为 0），主动销毁，避免共享资源泄漏。
                if (!m_Cache.Contains(cacheKey)) {
                    entry.DestroyShared();
                }

                return result;
            }

            entry.Retain();
            SpineAssetFactory.Play(result, options);
            return result;
        }

        private static string BuildCacheKey(SpineRemoteLoadOptions options) {
            // 键须覆盖所有影响共享条目内容的字段：url、格式、PMA 模式、自定义图集页地址。
            var sb = new StringBuilder();
            sb.Append(options.url).Append('|').Append(options.format).Append('|').Append(options.pmaMode);

            if (options.pageImageUrls != null && options.pageImageUrls.Length > 0) {
                sb.Append("|img:");

                for (var i = 0; i < options.pageImageUrls.Length; i++) {
                    if (i > 0) {
                        sb.Append(',');
                    }

                    sb.Append(options.pageImageUrls[i]);
                }
            }

            return sb.ToString();
        }

        private static string Validate(SpineRemoteLoadOptions options, bool requireParent) {
            if (options == null) {
                return "options 不能为空";
            }

            if (string.IsNullOrWhiteSpace(options.url)) {
                return "Url 不能为空";
            }

            if (requireParent && options.parent == null) {
                return "Parent 不能为空";
            }

            return null;
        }

        private static void DestroyRuntimeAsset(UnityEngine.Object asset) {
            if (asset != null) {
                UnityEngine.Object.Destroy(asset);
            }
        }
    }
}
