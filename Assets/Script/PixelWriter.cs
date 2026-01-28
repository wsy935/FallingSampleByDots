using System.Collections.Generic;
using Pixel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 像素写入器 - 支持在世界中写入像素
/// </summary>
public class PixelWriter : MonoBehaviour
{
    [Header("写入设置")]
    [SerializeField] private PixelType pixelType = PixelType.Sand;
    [SerializeField] private int brushSize = 3;

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
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        // 绘制像素
        if (mouse.leftButton.isPressed)
        {
            Vector2 worldPos = GetMouseWorldPosition();
            WritePixel(worldPos, pixelType);
        }

        // 擦除像素
        if (mouse.rightButton.isPressed)
        {
            Vector2 worldPos = GetMouseWorldPosition();
            WritePixel(worldPos, PixelType.Empty);
        }

        if (keyboard[Key.F].isPressed) FillAll();        

        // 数字键切换像素类型        
        if (keyboard[Key.Digit1].isPressed) pixelType = PixelType.Sand;
        if (keyboard[Key.Digit2].isPressed) pixelType = PixelType.Water;
        if (keyboard[Key.Digit3].isPressed) pixelType = PixelType.Wall;

        // 鼠标滚轮调整笔刷大小
        float scroll = mouse.scroll.value.y;
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
        Vector2 mousePos = Mouse.current.position.value;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        // 转换为像素坐标（假设sprite的pivot在中心）
        float pixelPerUnit = FallingSandRender.Instance.pixelPerUnit;
        // float pixelPerUnit = FallingSandComputeRender.Instance.pixelPerUnit;
        int pixelX = Mathf.FloorToInt((worldPos.x + fsw.WorldWidth / (2f * pixelPerUnit)) * pixelPerUnit);
        int pixelY = Mathf.FloorToInt((worldPos.y + fsw.WorldHeight / (2f * pixelPerUnit)) * pixelPerUnit);

        return new Vector2(pixelX, pixelY);
    }

    private void FillAll()
    {
        var query = entityManager.CreateEntityQuery(typeof(PixelChunk), typeof(PixelBuffer));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);        
        foreach (var entity in entities)
        {
            var buffer = entityManager.GetBuffer<PixelBuffer>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                var temp = buffer[i];
                temp.type = pixelType;
                buffer[i] = temp;
            }
                
        }
    }

    /// <summary>
    /// 在指定世界坐标写入像素（支持笔刷）
    /// </summary>
    public void WritePixel(Vector2 worldPos, PixelType type)
    {
        int centerX = (int)worldPos.x;
        int centerY = (int)worldPos.y;

        Dictionary<int2, List<int>> idxMap = new();
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
                if (x < 0 || x >= fsw.WorldWidth || y < 0 || y >= fsw.WorldHeight)
                    continue;
                // 计算chunk坐标和局部坐标
                int chunkX = x / fsw.ChunkEdge;
                int chunkY = y / fsw.ChunkEdge;
                int localX = x % fsw.ChunkEdge;
                int localY = y % fsw.ChunkEdge;
                int2 chunkPos = new(chunkX, chunkY);
                if (idxMap.TryGetValue(chunkPos, out var idxs))
                {
                    idxs.Add(fsw.GetChunkIdx(localX, localY));
                }
                else
                {
                    idxMap[chunkPos] = new() { fsw.GetChunkIdx(localX, localY) };
                }
            }
        }
        // 查找对应的chunk entity
        var query = entityManager.CreateEntityQuery(typeof(PixelChunk), typeof(PixelBuffer));
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            var chunk = entityManager.GetComponentData<PixelChunk>(entity);
            if (idxMap.TryGetValue(chunk.pos, out var idxs))
            {
                var buffer = entityManager.GetBuffer<PixelBuffer>(entity);

                foreach (var idx in idxs)
                {
                    buffer[idx] = new PixelBuffer { type = type };
                }

                // 标记chunk为dirty以触发模拟
                chunk.isDirty = true;
                entityManager.SetComponentData(entity, chunk);
            }
        }
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
