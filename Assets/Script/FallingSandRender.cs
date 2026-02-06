using Pixel;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallingSandRender : MonoBehaviour
{
    public static FallingSandRender Instance { get; private set; }
    SpriteRenderer sr;
    Texture2D tex;

    public int pixelPerUnit = 100;
    public bool showDirtyChunks = true;
    public Color dirtyChunkColor = new Color(1f, 0f, 0f, 0.3f); // 半透明红色
    
    public Texture2D Tex => tex;    
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        var fsw = FallingSandWorld.Instance;
        sr = GetComponent<SpriteRenderer>();
        var worldConfig = fsw.WorldConfig;        
        tex = new(worldConfig.width, worldConfig.height, TextureFormat.RGBA32, false,true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };        
        sr.sprite = Sprite.Create(tex, new(0, 0, worldConfig.width,worldConfig.height), new(0.5f, 0.5f), pixelPerUnit);
    }

    private void OnGUI()
    {
        if (!showDirtyChunks) return;

        var fsw = FallingSandWorld.Instance;
        if (fsw == null) return;

        var dirtyChunkManager = fsw.DirtyChunkManager;
        var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
        if (!dirtyChunks.IsCreated || dirtyChunks.Length == 0) return;

        var worldConfig = fsw.WorldConfig;
        
        // 获取相机和 sprite 的世界边界
        Camera cam = Camera.main;
        if (cam == null) return;

        // 计算 sprite 在世界空间中的位置和大小
        Vector3 worldPos = transform.position;
        float worldWidth = worldConfig.width / (float)pixelPerUnit;
        float worldHeight = worldConfig.height / (float)pixelPerUnit;
        
        // 计算左下角位置（因为 sprite pivot 是 0.5, 0.5）
        Vector3 worldBottomLeft = worldPos - new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, 0);

        // 设置绘制颜色
        GUI.color = dirtyChunkColor;

        // 遍历所有 DirtyChunk 并绘制
        for (int i = 0; i < dirtyChunks.Length; i++)
        {
            var chunk = dirtyChunks[i];
            if (!chunk.isDirty) continue;

            Pixel.Rect rect = chunk.rect;

            // 将像素坐标转换为世界坐标
            Vector3 worldChunkBottomLeft = worldBottomLeft + new Vector3(
                rect.x / (float)pixelPerUnit,
                rect.y / (float)pixelPerUnit,
                0
            );

            Vector3 worldChunkTopRight = worldBottomLeft + new Vector3(
                (rect.x + rect.width) / (float)pixelPerUnit,
                (rect.y + rect.height) / (float)pixelPerUnit,
                0
            );

            // 转换为屏幕坐标
            Vector3 screenBottomLeft = cam.WorldToScreenPoint(worldChunkBottomLeft);
            Vector3 screenTopRight = cam.WorldToScreenPoint(worldChunkTopRight);

            // Unity GUI 坐标系原点在左上角，Y 轴向下
            float screenX = screenBottomLeft.x;
            float screenY = Screen.height - screenTopRight.y; // 反转 Y 坐标
            float screenWidth = screenTopRight.x - screenBottomLeft.x;
            float screenHeight = screenTopRight.y - screenBottomLeft.y;

            // 绘制矩形框（使用 GUI.Box）
            UnityEngine.Rect screenRect = new UnityEngine.Rect(screenX, screenY, screenWidth, screenHeight);
            GUI.Box(screenRect, "");
                        
            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY - 1, screenWidth, 1), ""); // 上边
            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY + screenHeight - 1, screenWidth, 1), ""); // 下边
            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY, 1, screenHeight), ""); // 左边
            GUI.Box(new UnityEngine.Rect(screenX + screenWidth - 1, screenY, 1, screenHeight), ""); // 右边                        
        }

        // 重置 GUI 颜色
        GUI.color = Color.white;

        // 显示 DirtyChunk 数量信息
        GUI.Label(new UnityEngine.Rect(10, 10, 300, 20), $"DirtyChunks Count: {dirtyChunks.Length}");
    }
}
