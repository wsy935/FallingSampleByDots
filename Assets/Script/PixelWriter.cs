using Pixel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 像素写入器 - 支持在世界中写入像素
/// </summary>
public class PixelWriter : MonoBehaviour
{
    [Header("写入设置")]
    [SerializeField] private PixelType pixelType = PixelType.Sand;
    [SerializeField] private int brushSize = 3;
    [SerializeField] private KeyCode drawKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode eraseKey = KeyCode.Mouse1;

    private Camera mainCamera;
    private FallingSandWorld fsw;
    private EntityManager entityManager;

    private void Start()
    {
        mainCamera = Camera.main;
        fsw = FallingSandWorld.Instance;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Update()
    {
        // 绘制像素
        if (Input.GetKey(drawKey))
        {
            Vector2 worldPos = GetMouseWorldPosition();
            WritePixel(worldPos, pixelType);
        }

        // 擦除像素
        if (Input.GetKey(eraseKey))
        {
            Vector2 worldPos = GetMouseWorldPosition();
            WritePixel(worldPos, PixelType.Empty);
        }

        // 数字键切换像素类型
        if (Input.GetKeyDown(KeyCode.Alpha1)) pixelType = PixelType.Empty;
        if (Input.GetKeyDown(KeyCode.Alpha2)) pixelType = PixelType.Sand;
        if (Input.GetKeyDown(KeyCode.Alpha3)) pixelType = PixelType.Water;
        if (Input.GetKeyDown(KeyCode.Alpha4)) pixelType = PixelType.Wall;

        // 鼠标滚轮调整笔刷大小
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            brushSize = Mathf.Clamp(brushSize + (int)Mathf.Sign(scroll), 1, 20);
        }
    }

    /// <summary>
    /// 获取鼠标在世界中的位置（像素坐标）
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        // 转换为像素坐标（假设sprite的pivot在中心）
        float pixelPerUnit = 64f; // 与 FallingSandRender 保持一致
        int pixelX = Mathf.FloorToInt((worldPos.x + fsw.WorldWidth / (2f * pixelPerUnit)) * pixelPerUnit);
        int pixelY = Mathf.FloorToInt((worldPos.y + fsw.WorldHeight / (2f * pixelPerUnit)) * pixelPerUnit);
        
        return new Vector2(pixelX, pixelY);
    }

    /// <summary>
    /// 在指定世界坐标写入像素（支持笔刷）
    /// </summary>
    public void WritePixel(Vector2 worldPos, PixelType type)
    {
        int centerX = (int)worldPos.x;
        int centerY = (int)worldPos.y;

        // 使用圆形笔刷
        for (int dy = -brushSize; dy <= brushSize; dy++)
        {
            for (int dx = -brushSize; dx <= brushSize; dx++)
            {
                // 圆形判断
                if (dx * dx + dy * dy > brushSize * brushSize)
                    continue;

                int x = centerX + dx;
                int y = centerY + dy;

                WritePixelAt(x, y, type);
            }
        }
    }

    /// <summary>
    /// 在指定像素坐标写入单个像素
    /// </summary>
    public void WritePixelAt(int x, int y, PixelType type)
    {
        // 边界检查
        if (x < 0 || x >= fsw.WorldWidth || y < 0 || y >= fsw.WorldHeight)
            return;

        // 计算chunk坐标和局部坐标
        int chunkX = x / fsw.ChunkEdge;
        int chunkY = y / fsw.ChunkEdge;
        int localX = x % fsw.ChunkEdge;
        int localY = y % fsw.ChunkEdge;

        // 查找对应的chunk entity
        var query = entityManager.CreateEntityQuery(typeof(PixelChunk), typeof(PixelBuffer));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            var chunk = entityManager.GetComponentData<PixelChunk>(entity);
            
            // 找到目标chunk
            if (chunk.pos.x == chunkX && chunk.pos.y == chunkY)
            {
                var buffer = entityManager.GetBuffer<PixelBuffer>(entity);
                
                // 计算chunk内索引（包含border）
                int chunkIdx = fsw.GetChunkIdx(localX + fsw.ChunkBorder, localY + fsw.ChunkBorder);
                
                // 写入像素
                buffer[chunkIdx] = new PixelBuffer { type = type };
                
                // 标记chunk为dirty以触发模拟
                chunk.isDirty = true;
                entityManager.SetComponentData(entity, chunk);
                
                break;
            }
        }

        entities.Dispose();
    }

    /// <summary>
    /// 在指定矩形区域填充像素
    /// </summary>
    public void FillRect(int startX, int startY, int width, int height, PixelType type)
    {
        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                WritePixelAt(x, y, type);
            }
        }
    }

    /// <summary>
    /// 清空整个世界
    /// </summary>
    public void ClearWorld()
    {
        FillRect(0, 0, fsw.WorldWidth, fsw.WorldHeight, PixelType.Empty);
    }

    private void OnGUI()
    {
        // 显示当前状态
        GUI.Box(new Rect(10, 10, 250, 120), "Pixel Writer");
        GUI.Label(new Rect(20, 35, 230, 20), $"Current Type: {pixelType}");
        GUI.Label(new Rect(20, 55, 230, 20), $"Brush Size: {brushSize}");
        GUI.Label(new Rect(20, 75, 230, 20), $"Left Click: Draw | Right Click: Erase");
        GUI.Label(new Rect(20, 95, 230, 20), $"1-4: Change Type | Scroll: Brush Size");
        GUI.Label(new Rect(20, 115, 230, 20), $"Mouse Pos: {GetMouseWorldPosition()}");
    }
}
