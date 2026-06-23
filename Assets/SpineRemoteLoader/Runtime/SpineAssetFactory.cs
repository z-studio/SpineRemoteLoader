using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 负责把渲染无关的 <see cref="SpineRemoteRawData"/> 转换为可共享的缓存条目，
    /// 以及基于缓存条目创建可播放的 Unity 实例（GameObject + Skeleton 组件 + 每实例运行时资源）。
    /// 仅在主线程调用。
    /// </summary>
    internal static class SpineAssetFactory {
        /// <summary>由原始字节构建共享缓存条目（解码纹理、生成 TextAsset、解析 PMA）。失败返回 null。</summary>
        public static SpineRemoteCacheEntry BuildEntry(
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
                        Object.Destroy(textures[j]);
                    }

                    return null;
                }

                // 纹理名必须等于 atlas 声明的页名（去扩展名），SpineAtlasAsset 按名绑定。
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

        /// <summary>
        /// 基于共享缓存条目创建一个播放实例。仅创建对象，不递增引用计数、不播放动画（由调用方负责）。
        /// 失败时返回 success=false 的结果，且不会残留需要外部释放的对象。
        /// </summary>
        public static SpineRemoteLoadResult CreateInstance(
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

            Object.Destroy(source);

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

        public static void Play(SpineRemoteLoadResult result, SpineRemoteLoadOptions options) {
            var animationState = GetAnimationState(result.skeletonComponent);
            var skeletonData = GetSkeletonData(result.skeletonComponent);

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

        private static void ResetTransform(Transform transform) {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
