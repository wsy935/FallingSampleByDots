# ECS中如何使用ScriptableObject (SO) - 完整指南

## 📋 目录

1. [核心概念](#核心概念)
2. [三种主要方法](#三种主要方法)
3. [实际使用示例](#实际使用示例)
4. [最佳实践](#最佳实践)
5. [常见问题](#常见问题)

---

## 核心概念

### 为什么ECS不能直接使用ScriptableObject?

ECS (Entity Component System) 是**纯数据驱动**的架构:

- ✅ ECS组件必须是**struct结构体**
- ✅ 必须实现`IComponentData`或`IBufferElementData`接口
- ❌ 不能包含**引用类型**(如ScriptableObject)
- ❌ 不能包含**托管对象**(Managed Objects)

ScriptableObject是**引用类型**(class),无法直接用于ECS组件。

### 解决方案: Baker烘焙机制

**Baker**在编辑时或运行时初始化阶段,将ScriptableObject的**数据**提取出来,转换为ECS可用的**纯数据结构**。

```
ScriptableObject (引用类型) → Baker → IComponentData (值类型) → 存储到Entity
```

---

## 三种主要方法

### 方法1: 单个Entity配置 (Per-Entity Configuration)

**适用场景**: 每个Entity需要不同的配置

```csharp
// 1. 创建Authoring组件 (MonoBehaviour)
public class PixelSOAuthoring : MonoBehaviour
{
    public PixelSO pixelSO;  // 在Inspector中引用SO
}

// 2. 创建Baker进行转换
public class PixelSOBaker : Baker<PixelSOAuthoring>
{
    public override void Bake(PixelSOAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        // 将SO的数据复制到ECS组件
        AddComponent(entity, new PixelConfig
        {
            type = authoring.pixelSO.type,
            color = authoring.pixelSO.color,
            interactionMask = authoring.pixelSO.interactionMask
        });
    }
}

// 3. 定义ECS组件 (纯数据结构)
public struct PixelConfig : IComponentData
{
    public PixelType type;
    public Color32 color;
    public PixelType interactionMask;
}
```

**使用方式**:

1. 在Scene中创建GameObject
2. 添加`PixelSOAuthoring`组件
3. 在Inspector中指定`PixelSO`资源
4. 运行时自动转换为Entity和`PixelConfig`组件

---

### 方法2: 全局单例配置 (Singleton Configuration)

**适用场景**: 多个System需要共享同一份配置

```csharp
// 1. 创建Manager组件
public class PixelSOManager : MonoBehaviour
{
    public PixelSet pixelSet;  // 包含多个SO的集合
}

// 2. Baker转换为Singleton
public class PixelSOManagerBaker : Baker<PixelSOManager>
{
    public override void Bake(PixelSOManager authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        // 添加Singleton标记组件
        AddComponent(entity, new PixelConfigSingleton());

        // 将数组数据存入Buffer
        var buffer = AddBuffer<PixelConfigElement>(entity);
        foreach (var pixel in authoring.pixelSet.pixels)
        {
            buffer.Add(new PixelConfigElement
            {
                type = pixel.type,
                color = pixel.color,
                interactionMask = pixel.interactionMask
            });
        }
    }
}

// 3. 在System中访问Singleton
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 获取Singleton Entity
        var singletonEntity = SystemAPI.GetSingletonEntity<PixelConfigSingleton>();
        var configBuffer = SystemAPI.GetBuffer<PixelConfigElement>(singletonEntity);

        // 使用配置数据
        foreach (var config in configBuffer)
        {
            // 处理每种像素类型的配置
        }
    }
}
```

---

### 方法3: Buffer数组配置 (Dynamic Array Data)

**适用场景**: SO包含数组数据,需要动态数量的配置

```csharp
// SO定义
[CreateAssetMenu]
public class PixelSet : ScriptableObject
{
    public PixelSO[] pixels;  // 数组
}

// Baker转换为Buffer
public class PixelSetBaker : Baker<PixelSOAuthoring>
{
    public override void Bake(PixelSOAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        // Buffer可以存储动态数量的元素
        var buffer = AddBuffer<PixelConfigBuffer>(entity);

        foreach (var pixel in authoring.pixelSet.pixels)
        {
            buffer.Add(new PixelConfigBuffer
            {
                type = pixel.type,
                color = pixel.color
            });
        }
    }
}

// Buffer元素定义
public struct PixelConfigBuffer : IBufferElementData
{
    public PixelType type;
    public Color32 color;
}

// 在System中使用
foreach (var buffer in SystemAPI.Query<DynamicBuffer<PixelConfigBuffer>>())
{
    for (int i = 0; i < buffer.Length; i++)
    {
        var config = buffer[i];
        // 使用配置
    }
}
```

---

## 实际使用示例

### 完整流程示例: 像素沙盒游戏

#### 步骤1: 创建ScriptableObject

```csharp
// Assets/Script/Pixel/PixelSO.cs
[CreateAssetMenu(fileName = "Pixel", menuName = "SO/Pixel")]
public class PixelSO : ScriptableObject
{
    public PixelType type;
    public Color32 color;
    public PixelType interactionMask;
}

[CreateAssetMenu(fileName = "PixelSet", menuName = "SO/PixelSet")]
public class PixelSet : ScriptableObject
{
    public PixelSO[] pixels;
}
```

#### 步骤2: 在Editor中创建SO资源

1. 右键 → Create → SO → Pixel
2. 创建多个像素类型: Sand.asset, Water.asset, Wall.asset
3. 创建PixelSet → 添加所有像素引用

#### 步骤3: 设置全局配置

```csharp
// 在Scene中创建空GameObject,命名为"PixelConfig"
// 添加PixelSOManager组件,指定PixelSet资源
```

#### 步骤4: 在System中使用

```csharp
public partial struct PixelSimulationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 获取全局配置
        var configEntity = SystemAPI.GetSingletonEntity<PixelConfigSingleton>();
        var pixelConfigs = SystemAPI.GetBuffer<PixelConfigElement>(configEntity);

        // 遍历所有像素块
        foreach (var (chunk, buffer) in 
            SystemAPI.Query<RefRO<PixelChunk>, DynamicBuffer<PixelBuffer>>())
        {
            // 处理每个像素
            for (int i = 0; i < buffer.Length; i++)
            {
                var pixel = buffer[i].data;

                // 根据类型查找配置
                if (PixelConfigElement.TryGetConfig(pixelConfigs, pixel.type, out var config))
                {
                    // 使用config.color进行渲染
                    // 使用config.interactionMask判断交互
                }
            }
        }
    }
}
```

---

## 最佳实践

### ✅ DO (推荐做法)

1. **使用Baker进行数据转换**
   
   ```csharp
   // ✅ 正确: 通过Baker提取数据
   AddComponent(entity, new Config { value = authoring.so.value });
   ```

2. **Singleton用于全局配置**
   
   ```csharp
   // ✅ 正确: 全局配置使用Singleton
   var config = SystemAPI.GetSingleton<GlobalConfig>();
   ```

3. **只提取需要的数据**
   
   ```csharp
   // ✅ 正确: 只提取运行时需要的字段
   public struct PixelConfig : IComponentData
   {
       public PixelType type;  // 运行时需要
       public Color32 color;    // 运行时需要
       // 不包含Editor专用字段
   }
   ```

4. **使用Buffer存储数组**
   
   ```csharp
   // ✅ 正确: 数组数据用Buffer
   var buffer = AddBuffer<ConfigElement>(entity);
   foreach (var item in authoring.array)
   {
       buffer.Add(new ConfigElement { data = item });
   }
   ```

### ❌ DON'T (避免做法)

1. **不要在ECS组件中引用SO**
   
   ```csharp
   // ❌ 错误: 不能在struct中存储引用类型
   public struct WrongConfig : IComponentData
   {
       public PixelSO pixelSO;  // 编译错误!
   }
   ```

2. **不要在System中直接访问SO**
   
   ```csharp
   // ❌ 错误: 破坏ECS纯数据原则
   public partial struct BadSystem : ISystem
   {
       public PixelSO pixelSO;  // 不应该直接引用SO
   }
   ```

3. **不要在Job中访问SO**
   
   ```csharp
   // ❌ 错误: Job必须是纯数据,不能访问托管对象
   [BurstCompile]
   public struct BadJob : IJobEntity
   {
       public PixelSO config;  // 编译错误!
   }
   ```

---

## 常见问题

### Q1: 运行时可以修改SO数据吗?

**A**: 可以,但不推荐。修改SO不会自动同步到ECS组件,需要手动更新。建议在ECS组件中直接修改数据。

```csharp
// 如果必须修改,需要重新Baker或手动同步
SystemAPI.SetComponent(entity, new PixelConfig { 
    type = newType  // 直接修改ECS组件
});
```

### Q2: Baker何时执行?

**A**: 

- **SubScene**: 每次SubScene重新烘焙时
- **GameObject**: 进入Play模式时或运行时转换时
- **手动触发**: 修改Authoring组件时

### Q3: 如何调试Baker转换结果?

**A**: 使用Entity Inspector

1. Window → Entities → Hierarchy
2. 选择Entity查看组件
3. 验证数据是否正确转换

### Q4: SO和ECS性能对比?

**A**:

- **SO查询**: 较慢,涉及引用查找
- **ECS组件**: 极快,连续内存布局
- **建议**: 初始化时用SO,运行时用ECS组件

### Q5: 多个Entity可以共享同一份配置吗?

**A**: 可以!使用Singleton或Shared Component

```csharp
// 方法1: Singleton (推荐)
var config = SystemAPI.GetSingleton<GlobalConfig>();

// 方法2: Shared Component (相同配置的Entity会分组)
public struct SharedPixelConfig : ISharedComponentData
{
    public PixelType type;
}
```

---

## 总结

### 核心要点

1. **ECS不能直接使用SO** → 必须通过Baker转换
2. **Baker将数据复制到ECS组件** → SO → struct
3. **三种模式**: Per-Entity, Singleton, Buffer
4. **运行时只用ECS组件** → 获得最佳性能

### 选择指南

| 场景           | 推荐方法            | 组件类型                              |
| ------------ | --------------- | --------------------------------- |
| 每个Entity不同配置 | Per-Entity      | IComponentData                    |
| 全局共享配置       | Singleton       | IComponentData + Singleton Entity |
| 动态数组数据       | Buffer          | IBufferElementData                |
| 大量Entity相同配置 | SharedComponent | ISharedComponentData              |

---

## 相关文件

- `PixelSO.cs` - ScriptableObject定义
- `PixelSOAuthoring.cs` - Per-Entity配置示例
- `PixelSOManager.cs` - Singleton配置示例
- `PixelData.cs` - ECS组件定义

---

**创建日期**: 2026-01-15  
**Unity版本**: 2023.2+  
**ECS版本**: Entities 1.0+
