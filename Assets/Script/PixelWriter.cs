using System.Collections.Generic;
using Pixel;
using Unity.Collections;
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
    private NativeArray<PixelType> buffer;
    private FallingSandWorld fsw;        

    private void Start()
    {
        mainCamera = Camera.main;
        fsw = FallingSandWorld.Instance;
        buffer = fsw.PixelBuffer;
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
        var worldConfig = fsw.WorldConfig;
        int pixelX = Mathf.FloorToInt(worldPos.x * pixelPerUnit + (worldConfig.width/2));
        int pixelY = Mathf.FloorToInt(worldPos.y * pixelPerUnit + (worldConfig.height/2));

        return new Vector2(pixelX, pixelY);
    }

    private void FillAll()
    {
        var worldConfig = fsw.WorldConfig;
        for(int i = 0; i < worldConfig.height; i++)
        {
            for(int j = 0; j < worldConfig.width; j++)
            {
                int idx = worldConfig.CoordsToIdx(j, i);
                buffer[idx] = PixelType.Sand;
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
                if (!fsw.WorldConfig.IsInWorld(x, y))
                    continue;
                int idx = fsw.WorldConfig.CoordsToIdx(x, y);
                buffer[idx] = type;
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
