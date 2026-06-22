using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 内存缓存条目，持有可在多个实例间共享的资源（骨骼/图集文本与纹理）。
    /// 每个播放实例会基于这些共享资源各自构建 SkeletonDataAsset / SpineAtlasAsset / Material。
    /// </summary>
    public sealed class SpineRemoteCacheEntry {
        public readonly string cacheKey;
        public readonly TextAsset skeletonData;
        public readonly TextAsset atlasText;
        public readonly Texture2D[] textures;
        public readonly bool premultiplyAlpha;

        internal int refCount;
        internal bool pendingRelease;

        public int RefCount => refCount;

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

        internal void DestroyShared() {
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