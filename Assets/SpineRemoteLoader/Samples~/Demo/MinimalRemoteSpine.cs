using System;
using System.Threading;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace ZStudio.SpineRemoteLoader.Samples {
    /// <summary>
    /// 极简单文件版：不带缓存/抽象/引用计数，直接下载并播放一个远程 Spine（单图集页、UI 模式）。
    /// 适合"就一个地方用一次"的轻量场景；需要复用/健壮性请改用 SpineRemoteLoader 库本体。
    /// 资源约定：{url}.skel + {url}.atlas + {url}.png（不含扩展名传 url）。
    /// </summary>
    public sealed class MinimalRemoteSpine : MonoBehaviour {
        [SerializeField] private string m_Url = "https://cdn.example.com/spine/hero";

        private async void Start() {
            await LoadAsync(m_Url, transform, destroyCancellationToken);
        }

        public static async Awaitable<SkeletonGraphic> LoadAsync(string url, Transform parent, CancellationToken ct = default) {
            var skelBytes = await GetBytes($"{url}.skel", ct);
            var atlasBytes = await GetBytes($"{url}.atlas", ct);
            var pngBytes = await GetBytes($"{url}.png", ct);

            if (skelBytes == null || atlasBytes == null || pngBytes == null) {
                Debug.LogError($"[MinimalRemoteSpine] 下载失败: {url}");
                return null;
            }

            var atlasText = System.Text.Encoding.UTF8.GetString(atlasBytes);

            var texture = new Texture2D(2, 2);
            texture.LoadImage(pngBytes);
            // 纹理名必须等于 atlas 中声明的页名（去扩展名），否则 spine 无法绑定。
            texture.name = FirstPageName(atlasText);

            var shader = Shader.Find("Spine/SkeletonGraphic");
            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                new TextAsset(atlasText), new[] { texture }, shader, true);
            var skeletonData = SkeletonDataAsset.CreateRuntimeInstance(
                new TextAsset(skelBytes) { name = "remote.skel" }, atlasAsset, true);

            var graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(skeletonData, parent, new Material(shader));
            graphic.transform.localPosition = Vector3.zero;
            graphic.raycastTarget = false;

            var animations = graphic.Skeleton.Data.Animations;
            if (animations.Count > 0) {
                graphic.AnimationState.SetAnimation(0, animations.Items[0].Name, true);
            }

            return graphic;
        }

        private static async Awaitable<byte[]> GetBytes(string url, CancellationToken ct) {
            using var request = UnityWebRequest.Get(url);

            try {
                await Awaitable.FromAsyncOperation(request.SendWebRequest(), ct);
            } catch (OperationCanceledException) {
                throw;
            } catch (System.Exception) {
                return null;
            }

            return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.data : null;
        }

        // 取 atlas 中第一张图集页名（去扩展名），规则与 spine-unity 运行时一致。
        private static string FirstPageName(string atlasText) {
            foreach (var raw in atlasText.Replace("\r", "").Split('\n')) {
                var line = raw.Trim();
                if (line.EndsWith(".png") && !line.Contains(":")) {
                    return line.Substring(0, line.Length - 4);
                }
            }

            return "remote";
        }
    }
}
