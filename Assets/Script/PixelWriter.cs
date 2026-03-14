using System;
using System.Collections.Generic;
using Pixel;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Rect = Pixel.Rect;
/// <summary>
/// 像素写入器 - 支持在世界中写入像素
/// </summary>
public class PixelWriter : MonoBehaviour
{
    [Header("写入设置")]
    [SerializeField] private PixelType pixelType = PixelType.Sand;
    [SerializeField] private int brushSize = 3;

    void OnEnable()
    {
        Keyboard.current.onTextInput += SetPixelType;
    }

    void OnDisable()
    {
        Keyboard.current.onTextInput -= SetPixelType;
    }

    private void SetPixelType(char c)
    {
        if (char.IsDigit(c))
        {
            int val = c - '0';
            val = val < 1 ? 1 : val;
            pixelType = (PixelType)val;
        }
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

        // 鼠标滚轮调整笔刷大小
        float scroll = mouse.scroll.value.y;
        if (scroll != 0)
        {
            brushSize = Mathf.Clamp(brushSize + (int)Mathf.Sign(scroll), 1, 20);
        }

        if (keyboard[Key.F].isPressed) FillAll();
        if (keyboard[Key.A].isPressed) WritePixel(new(255, 255), pixelType, brushSize);
        if (keyboard[Key.S].isPressed) WritePixel(new(255, 255), pixelType, 1);
    }

    /// <summary>
    /// 获取鼠标在世界中的位置（像素坐标）
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.value;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        var worldConfig = FallingSandWorld.Instance.WorldConfig;

        // 转换为像素坐标（假设sprite的pivot在中心）
        float pixelPerUnit = FallingSandRender.Instance.pixelPerUnit;
        int pixelX = Mathf.FloorToInt(worldPos.x * pixelPerUnit + (worldConfig.width / 2));
        int pixelY = Mathf.FloorToInt(worldPos.y * pixelPerUnit + (worldConfig.height / 2));

        return new Vector2(pixelX, pixelY);
    }

    private void FillAll()
    {
        var worldConfig = FallingSandWorld.Instance.WorldConfig;
        var buffer = FallingSandWorld.Instance.PixelBuffer;
        var chunks = FallingSandWorld.Instance.Chunks;

        for (int i = 0; i < worldConfig.height; i++)
        {
            for (int j = 0; j < worldConfig.width; j++)
            {
                int idx = worldConfig.CoordsToIdx(j, i);
                buffer[idx] = PixelData.NewPixel(pixelType, FallingSandWorld.Instance.PixelLookup.GetConfig(pixelType));
            }
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            chunk.forceDiryFrame = Time.frameCount;
            chunks[i] = chunk;
        }
    }

    /// <summary>
    /// 在指定世界坐标写入像素（支持笔刷）
    /// </summary>
    public void WritePixel(Vector2 worldPos, PixelType type, int size = -1)
    {
        int centerX = (int)worldPos.x;
        int centerY = (int)worldPos.y;
        var worldConfig = FallingSandWorld.Instance.WorldConfig;
        var buffer = FallingSandWorld.Instance.PixelBuffer;
        var chunks = FallingSandWorld.Instance.Chunks;

        // 使用圆形笔刷
        size = size == -1 ? brushSize : size;
        for (int dy = -size; dy <= size; dy++)
        {
            for (int dx = -size; dx <= size; dx++)
            {
                // 圆形判断
                if (dx * dx + dy * dy > size * size)
                    continue;

                int x = centerX + dx;
                int y = centerY + dy;
                if (!worldConfig.IsInWorld(x, y))
                    continue;
                int idx = worldConfig.CoordsToIdx(x, y);
                buffer[idx] = PixelData.NewPixel(type, FallingSandWorld.Instance.PixelLookup.GetConfig(type));

                int chunkIdx = worldConfig.GetChunkIdxByWorld(x, y);
                var chunk = chunks[chunkIdx];
                chunk.forceDiryFrame = Time.frameCount;
                chunks[chunkIdx] = chunk;
            }
        }
    }

    private void OnGUI()
    {
        // 显示当前状态
        GUI.Box(new UnityEngine.Rect(10, 10, 250, 120), "Pixel Writer");
        GUI.Label(new UnityEngine.Rect(20, 35, 230, 20), $"Current Type: {pixelType}");
        GUI.Label(new UnityEngine.Rect(20, 55, 230, 20), $"Brush Size: {brushSize}");
        GUI.Label(new UnityEngine.Rect(20, 75, 230, 20), $"Left Click: Draw | Right Click: Erase");
        GUI.Label(new UnityEngine.Rect(20, 95, 230, 20), $"1-4: Change Type | Scroll: Brush Size");
        GUI.Label(new UnityEngine.Rect(20, 115, 230, 20), $"Mouse Pos: {GetMouseWorldPosition()}");
    }
}
