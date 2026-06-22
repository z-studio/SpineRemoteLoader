using Spine.Unity;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    public sealed class SpineRemoteLoadResult {
        public bool success;
        public bool canceled;
        public string error;
        public string cacheKey;

        // 每个实例独有、需要随实例一起销毁的运行时资源。
        public SkeletonDataAsset skeletonDataAsset;
        public SpineAtlasAsset atlasAsset;
        public Material[] materials;
        public GameObject gameObject;
        public Component skeletonComponent;

        internal SpineRemoteCacheEntry entry;

        public SkeletonGraphic SkeletonGraphic => skeletonComponent as SkeletonGraphic;
        public SkeletonAnimation SkeletonAnimation => skeletonComponent as SkeletonAnimation;

        public static SpineRemoteLoadResult Fail(string error, string cacheKey = null) {
            return new SpineRemoteLoadResult {
                success = false,
                error = error,
                cacheKey = cacheKey
            };
        }

        public static SpineRemoteLoadResult Canceled(string cacheKey = null) {
            return new SpineRemoteLoadResult {
                success = false,
                canceled = true,
                error = "已取消",
                cacheKey = cacheKey
            };
        }
    }
}
