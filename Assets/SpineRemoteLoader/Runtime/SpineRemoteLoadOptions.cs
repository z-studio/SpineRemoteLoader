using System;
using System.Threading;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    public sealed class SpineRemoteLoadOptions {
        /// <summary>资源基础 URL，不含扩展名。例如 https://cdn.example.com/spine/hero</summary>
        public string url;

        /// <summary>
        /// 可选：自定义图集页图片的下载 URL（按 atlas 中页的顺序）。
        /// 某一项非空时，该页图片从指定 URL 下载，否则回退到内部拼接的 {baseDir}{页名}.png。
        /// 仅改变"从哪里下载"，纹理名仍取 atlas 中声明的页名，以保证 Spine 正确绑定。
        /// 适用于服务器上 png 文件名与 atlas 声明页名不一致的场景（如旧资源把 png 挂在 {url}.png）。
        /// </summary>
        public string[] pageImageUrls;

        /// <summary>实例挂载父节点</summary>
        public Transform parent;

        /// <summary>要播放的动画名。为空时播放第一个动画。</summary>
        public string animationName;

        /// <summary>是否循环播放</summary>
        public bool loop = true;

        /// <summary>渲染模式：UI 用 Graphic，场景用 Animation</summary>
        public ESpineRenderMode renderMode = ESpineRenderMode.Graphic;

        /// <summary>骨骼数据格式，默认二进制 .skel</summary>
        public ESpineSkeletonFormat format = ESpineSkeletonFormat.Binary;

        /// <summary>Spine Shader。为空时按 RenderMode 自动查找。</summary>
        public Shader spineShader;

        /// <summary>骨骼缩放，大于 0 时覆盖 SkeletonDataAsset 默认缩放。</summary>
        public float scale = 0f;

        /// <summary>预乘 Alpha 处理模式</summary>
        public ESpinePmaMode pmaMode = ESpinePmaMode.Auto;

        /// <summary>下载超时（秒）</summary>
        public int timeoutSeconds = 30;

        /// <summary>失败重试次数（不含首次）</summary>
        public int retryCount = 2;

        /// <summary>每次重试的间隔（秒）</summary>
        public float retryIntervalSeconds = 1f;

        /// <summary>是否使用内存缓存，避免重复构建</summary>
        public bool useMemoryCache = true;

        /// <summary>下载进度回调（0~1），可选</summary>
        public IProgress<float> progress;

        /// <summary>取消令牌，可选</summary>
        public CancellationToken cancellationToken = default;
    }
}
