using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
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

        private readonly Dictionary<string, SpineRemoteCacheEntry> m_Cache = new();
        private readonly Dictionary<string, UniTask<SpineRemoteCacheEntry>> m_DownloadingTasks = new();

        private ISpineDownloader m_Downloader;

        public SpineRemoteLoader() : this(null) { }

        public SpineRemoteLoader(ISpineDownloader downloader) {
            m_Downloader = downloader ?? new UnityWebRequestSpineDownloader();
        }

        /// <summary>下载后端，可注入自定义实现（如 Best.HTTP）。设为 null 时回退到默认实现。</summary>
        public ISpineDownloader Downloader {
            get => m_Downloader;
            set => m_Downloader = value ?? new UnityWebRequestSpineDownloader();
        }

        // 关闭域重载（Enter Play Mode without Domain Reload）时，静态状态不会自动清空，
        // 这里在进入播放模式时主动重置共享实例，避免缓存残留已销毁对象的引用。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedOnEnterPlayMode() {
            s_Shared?.ReleaseAll();
            s_Shared = null;
        }

        public async UniTask<SpineRemoteLoadResult> LoadAndPlayAsync(SpineRemoteLoadOptions options) {
            var validationError = Validate(options);

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

        public async UniTask<bool> PrewarmAsync(SpineRemoteLoadOptions options) {
            var validationError = ValidateForDownload(options);

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

        public UniTask<SpineRemoteLoadResult> CreateInstanceAsync(string cacheKey, SpineRemoteLoadOptions options) {
            if (!m_Cache.TryGetValue(cacheKey, out var entry)) {
                return UniTask.FromResult(SpineRemoteLoadResult.Fail($"缓存不存在: {cacheKey}", cacheKey));
            }

            if (options.parent == null) {
                return UniTask.FromResult(SpineRemoteLoadResult.Fail("Parent 不能为空", cacheKey));
            }

            return UniTask.FromResult(CreateAndPlay(entry, options, cacheKey));
        }

        public bool TryGetCache(string cacheKey, out SpineRemoteCacheEntry entry) {
            return m_Cache.TryGetValue(cacheKey, out entry);
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

            var entry = result.entry;

            if (entry == null) {
                return;
            }

            entry.refCount = Mathf.Max(0, entry.refCount - 1);

            if (entry.refCount > 0) {
                return;
            }

            var isCached = !string.IsNullOrEmpty(result.cacheKey)
                           && m_Cache.TryGetValue(result.cacheKey, out var cached)
                           && ReferenceEquals(cached, entry);

            if (isCached) {
                // 已缓存：仅在被显式 ReleaseCache 标记后才销毁共享资源。
                if (entry.pendingRelease) {
                    entry.DestroyShared();
                    m_Cache.Remove(result.cacheKey);
                }
            } else {
                // 未进入缓存的临时资源由最后一个实例负责销毁。
                entry.DestroyShared();
            }
        }

        public void ReleaseCache(string cacheKey) {
            if (!m_Cache.TryGetValue(cacheKey, out var entry)) {
                return;
            }

            if (entry.refCount > 0) {
                entry.pendingRelease = true;
                SpineRemoteLog.Info($"缓存仍被 {entry.refCount} 个实例引用，将在引用归零后释放: {cacheKey}");
                return;
            }

            entry.DestroyShared();
            m_Cache.Remove(cacheKey);
        }

        public void ReleaseAll() {
            var keys = new List<string>(m_Cache.Keys);

            foreach (var key in keys) {
                ReleaseCache(key);
            }
        }

        // ---------------------------------------------------------------------

        private async UniTask<SpineRemoteCacheEntry> GetOrBuildEntryAsync(
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            if (options.useMemoryCache && m_Cache.TryGetValue(cacheKey, out var cached)) {
                options.progress?.Report(1f);
                return cached;
            }

            if (m_DownloadingTasks.TryGetValue(cacheKey, out var inFlight)) {
                return await inFlight;
            }

            // Preserve() 允许同一下载任务被多个并发调用方安全 await。
            var task = BuildEntryInternalAsync(options, cacheKey).Preserve();
            m_DownloadingTasks[cacheKey] = task;

            try {
                var entry = await task;

                if (entry != null && options.useMemoryCache && !m_Cache.ContainsKey(cacheKey)) {
                    m_Cache[cacheKey] = entry;
                }

                return entry;
            } finally {
                m_DownloadingTasks.Remove(cacheKey);
            }
        }

        private async UniTask<SpineRemoteCacheEntry> BuildEntryInternalAsync(
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            var raw = await FetchRawDataAsync(options);

            if (raw == null || !raw.IsValid()) {
                return null;
            }

            return BuildEntryFromRaw(raw, options, cacheKey);
        }

        private async UniTask<SpineRemoteRawData> FetchRawDataAsync(SpineRemoteLoadOptions options) {
            options.progress?.Report(0f);

            var downloader = m_Downloader;
            var token = options.cancellationToken;
            var skeletonExtension = options.format == ESpineSkeletonFormat.Binary ? "skel" : "json";
            var baseDir = GetBaseDirectory(options.url);

            var skeletonBytes = await SpineRemoteDownloader.DownloadBytesAsync(
                downloader,
                $"{options.url}.{skeletonExtension}",
                options.timeoutSeconds,
                options.retryCount,
                options.retryIntervalSeconds,
                token
            );

            if (skeletonBytes == null || skeletonBytes.Length == 0) {
                return null;
            }

            options.progress?.Report(0.15f);

            var atlasBytes = await SpineRemoteDownloader.DownloadBytesAsync(
                downloader,
                $"{options.url}.atlas",
                options.timeoutSeconds,
                options.retryCount,
                options.retryIntervalSeconds,
                token
            );

            if (atlasBytes == null || atlasBytes.Length == 0) {
                return null;
            }

            options.progress?.Report(0.3f);

            var atlasText = Encoding.UTF8.GetString(atlasBytes);
            var pageNames = SpineAtlasPageParser.GetPageNames(atlasText);

            if (pageNames.Count == 0) {
                SpineRemoteLog.Error($"atlas 未解析到任何图集页: {options.url}.atlas");
                return null;
            }

            var raw = new SpineRemoteRawData {
                skeletonExtension = skeletonExtension,
                skeletonBytes = skeletonBytes,
                atlasText = atlasText
            };

            for (var i = 0; i < pageNames.Count; i++) {
                var pageName = pageNames[i];

                var pageBytes = await SpineRemoteDownloader.DownloadBytesAsync(
                    downloader,
                    $"{baseDir}{pageName}.png",
                    options.timeoutSeconds,
                    options.retryCount,
                    options.retryIntervalSeconds,
                    token
                );

                if (pageBytes == null || pageBytes.Length == 0) {
                    return null;
                }

                raw.pages.Add(new SpineRemoteRawData.SpineRemotePage(pageName, pageBytes));
                options.progress?.Report(0.3f + 0.7f * (i + 1) / pageNames.Count);
            }

            options.progress?.Report(1f);
            return raw;
        }

        private static SpineRemoteCacheEntry BuildEntryFromRaw(
            SpineRemoteRawData raw,
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            var textures = new Texture2D[raw.pages.Count];

            for (var i = 0; i < raw.pages.Count; i++) {
                var page = raw.pages[i];
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);

                if (!texture.LoadImage(page.pngBytes)) {
                    SpineRemoteLog.Error($"图集纹理解码失败: {page.name}.png");

                    for (var j = 0; j < i; j++) {
                        UnityEngine.Object.Destroy(textures[j]);
                    }

                    return null;
                }

                texture.name = page.name;
                textures[i] = texture;
            }

            var skeletonData = new TextAsset(raw.skeletonBytes) {
                name = $"runtime.{raw.skeletonExtension}"
            };

            var atlasTextAsset = new TextAsset(raw.atlasText) {
                name = "runtime.atlas"
            };

            var premultiplyAlpha = ResolvePremultiplyAlpha(options, raw.atlasText);

            return new SpineRemoteCacheEntry(cacheKey, skeletonData, atlasTextAsset, textures, premultiplyAlpha);
        }

        private SpineRemoteLoadResult CreateAndPlay(
            SpineRemoteCacheEntry entry,
            SpineRemoteLoadOptions options,
            string cacheKey
        ) {
            var shader = ResolveShader(options);

            if (shader == null) {
                return SpineRemoteLoadResult.Fail("找不到 Spine Shader", cacheKey);
            }

            var source = new Material(shader);

            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                entry.atlasText,
                entry.textures,
                source,
                true,
                renameMaterial: true
            );

            UnityEngine.Object.Destroy(source);

            var scale = options.scale > 0f ? options.scale : 0.01f;
            var skeletonAsset = SkeletonDataAsset.CreateRuntimeInstance(entry.skeletonData, atlasAsset, true, scale);

            GameObject go;
            Component skeletonComponent;
            var materials = new List<Material>();

            if (atlasAsset.materials != null) {
                materials.AddRange(atlasAsset.materials);
            }

            if (options.renderMode == ESpineRenderMode.Animation) {
                var skeletonAnimation = SkeletonAnimation.NewSkeletonAnimationGameObject(skeletonAsset);

                if (options.parent != null) {
                    skeletonAnimation.transform.SetParent(options.parent, false);
                }

                go = skeletonAnimation.gameObject;
                skeletonComponent = skeletonAnimation;
            } else {
                var graphicMaterial = new Material(shader);

                var skeletonGraphic = SkeletonGraphic.NewSkeletonGraphicGameObject(
                    skeletonAsset,
                    options.parent,
                    graphicMaterial
                );

                skeletonGraphic.raycastTarget = false;
                skeletonGraphic.MeshGenerator.settings.pmaVertexColors = entry.premultiplyAlpha;
                skeletonGraphic.MeshGenerator.settings.canvasGroupCompatible = !entry.premultiplyAlpha;
                materials.Add(graphicMaterial);
                go = skeletonGraphic.gameObject;
                skeletonComponent = skeletonGraphic;
            }

            ResetTransform(go.transform);
            PlayAnimation(skeletonComponent, options);

            entry.refCount++;

            return new SpineRemoteLoadResult {
                success = true,
                cacheKey = cacheKey,
                skeletonDataAsset = skeletonAsset,
                atlasAsset = atlasAsset,
                materials = materials.ToArray(),
                gameObject = go,
                skeletonComponent = skeletonComponent,
                entry = entry
            };
        }

        private static void PlayAnimation(Component skeletonComponent, SpineRemoteLoadOptions options) {
            var animationState = GetAnimationState(skeletonComponent);
            var skeletonData = GetSkeletonData(skeletonComponent);

            if (animationState == null || skeletonData == null) {
                SpineRemoteLog.Error("无法获取 AnimationState / SkeletonData");
                return;
            }

            var animationName = options.animationName;

            if (string.IsNullOrEmpty(animationName)) {
                if (skeletonData.Animations.Count == 0) {
                    SpineRemoteLog.Error("Spine 动画列表为空");
                    return;
                }

                animationName = skeletonData.Animations.Items[0].Name;
            }

            if (skeletonData.FindAnimation(animationName) == null) {
                SpineRemoteLog.Error($"找不到动画: {animationName}");
                return;
            }

            animationState.SetAnimation(0, animationName, options.loop);
        }

        private static Spine.AnimationState GetAnimationState(Component skeletonComponent) {
            return skeletonComponent switch {
                SkeletonGraphic graphic => graphic.AnimationState,
                SkeletonAnimation animation => animation.AnimationState,
                _ => null
            };
        }

        private static SkeletonData GetSkeletonData(Component skeletonComponent) {
            return skeletonComponent switch {
                SkeletonGraphic graphic => graphic.Skeleton?.Data,
                SkeletonAnimation animation => animation.Skeleton?.Data,
                _ => null
            };
        }

        private static Shader ResolveShader(SpineRemoteLoadOptions options) {
            if (options.spineShader != null) {
                return options.spineShader;
            }

            var shaderName = options.renderMode == ESpineRenderMode.Animation
                ? "Spine/Skeleton"
                : "Spine/SkeletonGraphic";

            var shader = Shader.Find(shaderName);

            if (shader == null) {
                SpineRemoteLog.Error($"找不到 Shader: {shaderName}，请通过 options.spineShader 指定。");
            }

            return shader;
        }

        private static bool ResolvePremultiplyAlpha(SpineRemoteLoadOptions options, string atlasText) {
            return options.pmaMode switch {
                ESpinePmaMode.ForceOn => true,
                ESpinePmaMode.ForceOff => false,
                _ => SpineAtlasPageParser.DetectPremultiplyAlpha(atlasText)
            };
        }

        private static string GetBaseDirectory(string url) {
            var trimmed = url.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed.Substring(0, lastSlash + 1) : string.Empty;
        }

        private static string BuildCacheKey(SpineRemoteLoadOptions options) {
            return $"{options.url}|{options.format}";
        }

        private static string Validate(SpineRemoteLoadOptions options) {
            if (options == null) {
                return "options 不能为空";
            }

            if (string.IsNullOrWhiteSpace(options.url)) {
                return "Url 不能为空";
            }

            if (options.parent == null) {
                return "Parent 不能为空";
            }

            return null;
        }

        private static string ValidateForDownload(SpineRemoteLoadOptions options) {
            if (options == null) {
                return "options 不能为空";
            }

            if (string.IsNullOrWhiteSpace(options.url)) {
                return "Url 不能为空";
            }

            return null;
        }

        private static void ResetTransform(Transform transform) {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void DestroyRuntimeAsset(UnityEngine.Object asset) {
            if (asset != null) {
                UnityEngine.Object.Destroy(asset);
            }
        }
    }
}