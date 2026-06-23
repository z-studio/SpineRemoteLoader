using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 内存缓存条目：持有可在多个实例间共享的资源（骨骼/图集文本与纹理），并以引用计数管理生命周期。
    /// 引用来源 = 缓存本身（被缓存持有时 +1）+ 每个存活的播放实例（+1）。计数归零即销毁共享资源。
    /// 每个播放实例会基于这些共享资源各自构建 SkeletonDataAsset / SpineAtlasAsset / Material。
    /// 仅在主线程访问。
    /// </summary>
    public sealed class SpineRemoteCacheEntry {
        public readonly string cacheKey;
        public readonly TextAsset skeletonData;
        public readonly TextAsset atlasText;
        public readonly Texture2D[] textures;
        public readonly bool premultiplyAlpha;

        private bool m_Destroyed;

        public int RefCount { get; private set; }

        public SpineRemoteCacheEntry(
            string cacheKey,
            TextAsset skeletonData,
            TextAsset atlasText,
            Texture2D[] textures,
            bool premultiplyAlpha
        ) {
            this.cacheKey = cacheKey;
            this.skeletonData = skeletonData;
            this.atlasText = atlasText;
            this.textures = textures;
            this.premultiplyAlpha = premultiplyAlpha;
        }

        internal void Retain() {
            RefCount++;
        }

        /// <summary>递减引用计数；归零时销毁共享资源并返回 true。</summary>
        internal bool Release() {
            if (RefCount > 0) {
                RefCount--;
            }

            if (RefCount == 0) {
                DestroyShared();
                return true;
            }

            return false;
        }

        internal void DestroyShared() {
            if (m_Destroyed) {
                return;
            }

            m_Destroyed = true;

            if (skeletonData != null) {
                Object.Destroy(skeletonData);
            }

            if (atlasText != null) {
                Object.Destroy(atlasText);
            }

            if (textures != null) {
                foreach (var texture in textures) {
                    if (texture != null) {
                        Object.Destroy(texture);
                    }
                }
            }
        }
    }
}
