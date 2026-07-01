# SpineRemoteLoader

通用的 Unity Spine 远程资源**动态下载与播放**库。把一组 Spine 导出文件（`.skel`/`.json` + `.atlas` + 一张或多张 `.png`）放到 CDN，运行时按 URL 拉取、缓存并播放，无需打进包体。

## 特性

- **两种渲染模式**：UI（`SkeletonGraphic`）与场景（`SkeletonAnimation`）
- **两种骨骼格式**：二进制 `.skel` 与文本 `.json`
- **多图集**：自动解析 `.atlas` 中的所有页并按页名下载、匹配
- **内存缓存**：避免重复下载与构建，支持并发去重
- **可插拔下载后端**：默认 `UnityWebRequest`，可注入自定义实现（如 Best.HTTP）
- **健壮网络层**：超时、失败重试、`CancellationToken` 取消、进度回调
- **正确的资源生命周期**：实例资源与共享资源分离，引用计数释放，无泄漏
- **PMA 自适应**：根据 `.atlas` 的 `pma` 字段自动设置预乘 Alpha
- **零侵入**：仅依赖 `spine-unity`，不绑定任何业务框架

## 依赖

- `com.esotericsoftware.spine.spine-unity` 4.2+

## 安装

### Git UPM（推荐）

在目标项目 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.zstudio.spineremoteloader": "https://github.com/z-studio/SpineRemoteLoader.git?path=Assets/SpineRemoteLoader"
  }
}
```

### 本地 UPM

将本包文件夹放入 `Packages/com.zstudio.spineremoteloader/`（文件夹名与 `package.json` 的 `name` 一致）。

### 拷贝到 Assets

将整个 `SpineRemoteLoader` 文件夹放入 `Assets/` 下即可，Unity 会按 `asmdef` 自动编译。


## 资源约定

URL 传**不含扩展名**的基础路径，库会按以下规则拉取：

```
{url}.skel        # 或 {url}.json
{url}.atlas
{baseDir}{pageName}.png   # baseDir 为 url 所在目录（含尾部 /）；pageName 来自 atlas 内的页声明
```

> 图集页图片与 `.atlas` 同目录；`.atlas` 内声明几张 `.png` 就会下载几张。

> **自定义图集页地址**：当服务器上 png 的实际文件名与 `.atlas` 内声明的页名不一致时（例如旧资源把图片挂在 `{url}.png`），可通过 `options.pageImageUrls` 按页顺序显式指定下载地址；某项为空则回退到内部拼接规则。纹理名仍取 atlas 页名，不影响 Spine 绑定。
>
> ```csharp
> options.pageImageUrls = new[] { $"{url}.png" }; // 单页：复刻“与骨骼同基名”的旧行为
> ```

## 访问方式

库同时支持两种用法，按需选择：

- **共享实例（便捷）**：`SpineRemoteLoader.Shared`，全局唯一、跨模块复用同一份缓存。`Shared` 可被替换（`SpineRemoteLoader.Shared = ...`），便于注入自定义后端或在测试中替换。进入 Play Mode 时会自动重置，兼容“关闭域重载”的工程设置。
- **独立实例（推荐用于可测试/多缓存域）**：`new SpineRemoteLoader(downloader)`，依赖 `ISpineRemoteLoader` 接口，DI 友好。业务层建议依赖接口而非直接耦合 `Shared`。

```csharp
ISpineRemoteLoader loader = new SpineRemoteLoader(myDownloader);
await loader.LoadAndPlayAsync(options);
```

## 快速开始

### 代码调用

```csharp
using ZStudio.SpineRemoteLoader;
using UnityEngine;

public sealed class Demo : MonoBehaviour {
    [SerializeField] 
    private Transform m_Parent;

    private SpineRemoteLoadResult m_Result;

    private async void Start() {
        m_Result = await SpineRemoteLoader.Shared.LoadAndPlayAsync(new SpineRemoteLoadOptions {
            url = "https://cdn.example.com/spine/hero",
            parent = m_Parent,
            animationName = "idle",
            loop = true,
            renderMode = ESpineRenderMode.Graphic,
            format = ESpineSkeletonFormat.Binary,
            cancellationToken = destroyCancellationToken,
            progress = new System.Progress<float>(p => Debug.Log($"加载进度 {p:P0}"))
        });

        if (!m_Result.success) {
            Debug.LogError(m_Result.error);
        }
    }

    private void OnDestroy() {
        // 销毁此实例的运行时资源（不影响缓存）
        SpineRemoteLoader.Shared.Release(m_Result);
    }
}
```

### 组件方式

把 `SpineRemotePlayer` 挂到物体上，在 Inspector 填 URL / 动画名，勾选 `Play On Awake`。组件销毁时会自动取消下载并释放实例资源。

### 预热

```csharp
await SpineRemoteLoader.Shared.PrewarmAsync(new SpineRemoteLoadOptions {
    url = "https://cdn.example.com/spine/hero"
});
```

## 缓存与释放

| 操作 | 说明 |
|---|---|
| `Release(result)` | 销毁单个实例的资源，递减引用计数 |
| `ReleaseCache(cacheKey)` | 释放内存缓存与共享纹理；若仍被实例引用则延迟到引用归零 |
| `ReleaseAll()` | 释放全部内存缓存 |

`cacheKey` 覆盖所有影响共享条目内容的字段（`url` + `format` + `pmaMode` + `pageImageUrls`），可从 `SpineRemoteLoadResult.cacheKey` 取得。

> 本库只做内存缓存，不落盘。若需要离线/跨启动复用，请在自定义 `ISpineDownloader` 中接入你自己的下载与缓存方案。

> **引用计数**：缓存条目的共享资源（纹理/文本）以引用计数管理。缓存自身持有 1 个引用，每个存活实例 +1；`Release` 实例与 `ReleaseCache` 各释放一份，归零时才真正销毁。因此可安全地"先 `ReleaseCache` 标记淘汰，待最后一个实例销毁后自动回收"。

> **线程模型**：本库仅支持在 **主线程** 调用（内部缓存字典无锁，且 Unity 对象创建必须在主线程）。

## 自定义下载后端

实现 `ISpineDownloader` 并注入，即可替换默认的 `UnityWebRequest`（例如复用项目里的 Best.HTTP）：

```csharp
public sealed class BestHttpSpineDownloader : ISpineDownloader {
    public async Awaitable<byte[]> GetBytesAsync(string url, int timeoutSeconds, CancellationToken ct) {
        // 用 Best.HTTP 发起请求，失败返回 null，取消时抛 OperationCanceledException
        ...
    }
}

// 注入（重试逻辑由库统一处理，实现里只需完成一次请求）
SpineRemoteLoader.Shared.Downloader = new BestHttpSpineDownloader();
```

## 日志

```csharp
SpineRemoteLog.sLevel = ESpineLogLevel.Verbose; // 默认 Warning
```

## 已知限制

- 仅支持 `.png` 图集页（与 spine-unity 运行时一致）。
- 直通 Alpha（straight-alpha）的图集在 `SkeletonAnimation` 模式下可能需要通过 `options.spineShader` 指定对应 shader。
