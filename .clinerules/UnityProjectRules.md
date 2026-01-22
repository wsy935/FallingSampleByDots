# Unity 项目开发规则

## 代码规范

### 核心要求
- 代码的编写需要尽可能的遵循SOLID原则

### 命名约定
- **类名**：使用 PascalCase（如 `PlayerController`, `GameManager`）
- **方法名**：使用 PascalCase（如 `MovePlayer()`, `Initialize()`）
- **变量名**：使用 camelCase（如 `playerHealth`, `moveSpeed`）
- **私有字段**：使用 camelCase，可选下划线前缀（如 `_rigidbody`, `healthValue`）
- **常量**：使用 UPPER_SNAKE_CASE（如 `MAX_HEALTH`, `DEFAULT_SPEED`）
- **ScriptableObject**：以 SO 或 Data 结尾（如 `PlayerDataSO`, `WeaponConfig`）

### 文件组织
- 每个 MonoBehaviour/Component 单独一个文件
- 文件名必须与类名完全匹配
- 使用命名空间避免冲突（推荐项目名作为根命名空间）
- 按功能分类组织文件夹（如 Scripts/Player, Scripts/Enemy, Scripts/UI）

## Unity 特定最佳实践

### ECS/DOTS 开发
- 优先使用 Job System 进行多线程计算
- Component 应该只包含数据，不包含逻辑
- System 负责处理所有业务逻辑
- 使用 Burst 编译器优化性能关键代码
- 避免在 Job 中使用托管类型

### 性能优化
- 缓存 GetComponent 结果，避免重复调用
- 使用 Object Pooling 管理频繁创建/销毁的对象
- 避免在 Update() 中进行重复的复杂计算
- 使用 StringBuilder 代替字符串拼接
- 合理使用 FixedUpdate, Update, LateUpdate
- 注意 GC Alloc，减少不必要的内存分配

### MonoBehaviour 生命周期
- **Awake**：初始化自身引用和数据
- **Start**：初始化需要引用其他对象的逻辑
- **OnEnable/OnDisable**：注册/注销事件监听
- **OnDestroy**：清理资源，取消订阅

### 资源管理
- 使用 Addressables 或 AssetBundle 管理资源
- ScriptableObject 用于配置数据
- 及时释放不再使用的资源
- 使用 Resources.UnloadUnusedAssets() 清理内存

## 架构模式

### 推荐模式
- **MVC/MVP**：UI 和逻辑分离
- **事件驱动**：使用 UnityEvent 或自定义事件系统
- **依赖注入**：使用 Zenject/VContainer 等框架
- **状态模式**：管理复杂状态转换
- **对象池模式**：管理频繁创建的对象

### DOTS 项目特定
- 遵循 Data-Oriented 设计原则
- 优先使用 IComponentData 而非 class Component
- System 应保持单一职责
- 使用 SystemBase 或 ISystem（新版本）
- 合理划分 SystemGroup

## 序列化
- 使用 `[SerializeField]` 暴露私有字段
- 避免序列化公共字段，除非必要
- 使用 `[HideInInspector]` 隐藏不需要编辑的公共字段
- 复杂数据结构使用 ScriptableObject

## 调试与测试
- 使用 Debug.Log/LogWarning/LogError 适当输出日志
- 使用 Gizmos 可视化调试信息
- 在编辑器模式下添加自定义 Inspector
- 使用条件编译符区分开发和发布代码 (#if UNITY_EDITOR)

## 安全与质量
- 避免使用 GameObject.Find，使用引用或标签
- 空引用检查，避免 NullReferenceException
- 使用 try-catch 处理可能的异常
- 注释关键逻辑和复杂算法
- 避免硬编码，使用配置文件

## 版本控制
- .meta 文件必须包含在版本控制中
- Library、Temp、Obj 文件夹加入 .gitignore
- 场景文件使用文本序列化（Edit > Project Settings > Editor > Asset Serialization: Force Text）
- 提交前测试场景是否正常加载

## 项目特定规则（FallingSampleByDots）
- 使用 Unity DOTS 架构
- Pixel 相关逻辑放在 Assets/Script/Pixel/ 目录
- SimulationSystem 负责像素模拟核心逻辑
- 使用 ScriptableObject 定义像素类型配置
- 渲染逻辑与模拟逻辑分离
