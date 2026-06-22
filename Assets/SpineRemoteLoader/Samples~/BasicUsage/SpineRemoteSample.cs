using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader.Samples {
    /// <summary>
    /// 最小示例：下载并播放一个远程 Spine 动画，并在销毁时正确释放。
    /// </summary>
    public sealed class SpineRemoteSample : MonoBehaviour {
        [Header("远程资源基础 URL（不含扩展名）")]
        [SerializeField] private string m_Url = "https://cdn.example.com/spine/hero";

        [SerializeField] private string m_AnimationName = "idle";
        [SerializeField] private ESpineRenderMode m_RenderMode = ESpineRenderMode.Graphic;
        [SerializeField] private Transform m_Parent;

        private SpineRemoteLoadResult m_Result;

        private async UniTaskVoid Start() {
            SpineRemoteLog.sLevel = ESpineLogLevel.Info;

            var progress = new System.Progress<float>(p => SpineRemoteLog.Info($"加载进度: {p:P0}"));

            m_Result = await SpineRemoteLoader.Shared.LoadAndPlayAsync(new SpineRemoteLoadOptions {
                url = m_Url,
                parent = m_Parent != null ? m_Parent : transform,
                animationName = m_AnimationName,
                renderMode = m_RenderMode,
                loop = true,
                progress = progress,
                cancellationToken = this.GetCancellationTokenOnDestroy()
            });

            if (!m_Result.success) {
                SpineRemoteLog.Error($"加载失败: {m_Result.error}");
            }
        }

        private void OnDestroy() {
            SpineRemoteLoader.Shared.Release(m_Result);
        }
    }
}
