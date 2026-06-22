using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 挂载到场景中的便捷组件，可在 Inspector 中配置远程 Spine 并自动加载播放。
    /// 组件销毁时会自动取消进行中的下载并释放实例资源。
    /// </summary>
    public sealed class SpineRemotePlayer : MonoBehaviour {
        [SerializeField]
        private string m_Url;

        [SerializeField]
        private string m_AnimationName;

        [SerializeField]
        private bool m_Loop = true;

        [SerializeField]
        private ESpineRenderMode m_RenderMode = ESpineRenderMode.Graphic;

        [SerializeField]
        private ESpineSkeletonFormat m_Format = ESpineSkeletonFormat.Binary;

        [SerializeField]
        private bool m_PlayOnAwake = true;

        [SerializeField]
        private bool m_UseMemoryCache = true;

        public SpineRemoteLoadResult LastResult { get; private set; }

        private void Awake() {
            if (m_PlayOnAwake) {
                PlayAsync().Forget();
            }
        }

        public async UniTask<SpineRemoteLoadResult> PlayAsync(IProgress<float> progress = null) {
            ReleaseCurrentInstance();

            var options = new SpineRemoteLoadOptions {
                url = m_Url,
                parent = transform,
                animationName = m_AnimationName,
                loop = m_Loop,
                renderMode = m_RenderMode,
                format = m_Format,
                useMemoryCache = m_UseMemoryCache,
                progress = progress,
                cancellationToken = this.GetCancellationTokenOnDestroy()
            };

            LastResult = await SpineRemoteLoader.Instance.LoadAndPlayAsync(options);

            if (!LastResult.success && !LastResult.canceled) {
                SpineRemoteLog.Error($"播放失败: {LastResult.error}");
            }

            return LastResult;
        }

        public void ReleaseCache() {
            if (!string.IsNullOrEmpty(LastResult?.cacheKey)) {
                SpineRemoteLoader.Instance.ReleaseCache(LastResult.cacheKey);
            }
        }

        private void ReleaseCurrentInstance() {
            if (LastResult is { success: true }) {
                SpineRemoteLoader.Instance.Release(LastResult);
                LastResult = null;
            }
        }

        private void OnDestroy() {
            ReleaseCurrentInstance();
        }
    }
}