# .NET DI 容器生命周期（Transient/Scoped/Singleton）

​	这段代码的核心是**演示 .NET 原生 DI 容器中三种生命周期（Transient/Scoped/Singleton）的创建、复用、释放规则**，同时结合 `IDisposable` 展示容器对可释放对象的管理逻辑。以下按「代码结构→逐段逐句解析→核心运行结果」展开，精准拆解每一行的作用。

## 一、代码整体结构梳理

| 模块                                | 作用                                                         |
| ----------------------------------- | ------------------------------------------------------------ |
| 接口定义（IAccount/IMessage/ITool） | 服务契约，用于 DI 注册时的「接口→实现」映射                  |
| 基类 Base（IDisposable）            | 所有实现类的父类，通过构造函数 / Dispose 打印生命周期，追踪对象创建 / 释放 |
| 实现类（Account/Message/Tool）      | 继承 Base 并实现对应接口，无额外逻辑，仅复用基类的生命周期打印 |
| Run 方法                            | 核心演示逻辑：创建根容器→创建子作用域→解析不同生命周期的服务→释放容器 |
| GetService<T> 方法                  | 辅助方法：重复解析同一服务，验证 Transient 每次新建、Scoped/Singleton 复用 |

## 二、逐句详细解析

### 1. 命名空间 & 类定义

```csharp
using Microsoft.Extensions.DependencyInjection; // 引入.NET DI核心命名空间

namespace IOC_TestLibs // 自定义命名空间
{
    internal class Example_004 // 内部类，仅当前程序集可见
    {
```

- 核心：引入 `Microsoft.Extensions.DependencyInjection` 是使用 .NET 原生 DI 的前提；`Example_004` 是封装所有演示逻辑的容器类。

### 2. 接口定义（服务契约）

```csharp
public interface IAccount { } // 账户服务契约
public interface IMessage { } // 消息服务契约
public interface ITool { }    // 工具服务契约
```

- 作用：DI 注册时通过「接口→实现类」映射解耦，后续解析服务时基于接口而非具体类；这里接口为空（仅作标记），实际项目中会定义方法。

### 3. 基类 Base（核心：追踪对象生命周期）

```csharp
public class Base : IDisposable // 实现IDisposable，支持容器自动释放
{
    public Base() // 构造函数：对象创建时执行
    {
        Console.WriteLine($"Created:{GetType().Name}已创建！");
        // GetType().Name：获取当前实例的实际类型名（如Account/Message/Tool）
    }

    public void Dispose() // 释放方法：对象销毁时执行
    {
        Console.WriteLine($"Disposed:{GetType().Name}已释放！");
    }
}
```

- 关键逻辑：
  - 构造函数：打印「XXX 已创建」，用于追踪 DI 容器何时创建对象；
  - `IDisposable`：实现该接口后，DI 容器会在「对象生命周期结束时」自动调用 `Dispose` 方法（核心：释放资源）；
  - `GetType().Name`：多态特性，子类实例调用时会显示子类名称（如 Account 实例调用时显示「Account」）。

### 4. 实现类（继承 Base + 实现接口）

```csharp
public class Account : Base, IAccount { } // 账户服务实现：继承Base，实现IAccount
public class Message : Base, IMessage { } // 消息服务实现：继承Base，实现IMessage
public class Tool : Base, ITool { }       // 工具服务实现：继承Base，实现ITool
```

- 作用：
  - 继承 `Base`：复用「创建 / 释放」的打印逻辑，无需每个类重复写；
  - 实现对应接口：为 DI 注册「接口→实现」提供映射依据；
  - 空类：仅作演示，实际项目中会实现接口的业务方法。

### 5. Run 方法（核心演示逻辑）

```csharp
public static void Run() // 静态方法，可直接调用（无需实例化Example_004）
{
    // 第一步：创建根容器（ServiceProvider），并用using包裹（自动释放）
    using (var root = new ServiceCollection() // 创建服务注册清单
        // 注册Transient服务：每次解析都新建实例
        .AddTransient<IAccount, Account>()
        // 注册Scoped服务：同一作用域内解析复用实例
        .AddScoped<IMessage, Message>()
        // 注册Singleton服务：全局（根容器）仅创建一次，所有作用域复用
        .AddSingleton<ITool, Tool>()
        // 构建根容器（ServiceProvider）：注册清单→可执行的DI容器
        .BuildServiceProvider())
    {
        // 第二步：创建子作用域（Scope），并用using包裹（自动释放）
        using (var scope = root.CreateScope())
        {
            var child = scope.ServiceProvider; // 子作用域的服务提供器（子容器）
            child.GetServices<IAccount>();     // 解析Transient服务（IAccount）
            child.GetServices<IMessage>();     // 解析Scoped服务（IMessage）
            child.GetService<ITool>();         // 解析Singleton服务（ITool）
            Console.WriteLine("释放子容器！");  // 标记子容器即将释放
        } // 子作用域using结束：自动释放Scoped对象，Transient对象随解析时机释放
        Console.WriteLine("释放根容器！");      // 标记根容器即将释放
    } // 根容器using结束：自动释放Singleton对象
```

#### 关键逐句拆解：

- `new ServiceCollection()`：创建「服务注册清单」，用于记录所有服务的「接口→实现」映射和生命周期；
- `.AddTransient<IAccount, Account>()`：注册 **Transient（瞬时）** 服务：
  - 规则：每次调用 `GetService/IAccount>` 都会新建 `Account` 实例；
  - 释放：无固定释放时机，实例不再被引用时由 GC 回收（但实现 `IDisposable` 时，容器会在「作用域释放 / 根容器释放」时调用 `Dispose`）；
- `.AddScoped<IMessage, Message>()`：注册 **Scoped（作用域）** 服务：
  - 规则：同一作用域（`scope`）内多次解析，复用同一个 `Message` 实例；不同作用域新建实例；
  - 释放：作用域（`scope`）释放时（`using` 结束），自动调用 `Dispose`；
- `.AddSingleton<ITool, Tool>()`：注册 **Singleton（单例）** 服务：
  - 规则：全局（根容器）仅创建一次 `Tool` 实例，所有作用域 / 解析操作复用；
  - 释放：根容器释放时（`using` 结束），自动调用 `Dispose`；
- `.BuildServiceProvider()`：将「注册清单」转换为「可执行的根容器（ServiceProvider）」，此时容器具备解析服务的能力；
- `using (var root = ...)`：根容器实现 `IDisposable`，`using` 包裹后自动调用 `Dispose`，释放 Singleton 对象；
- `var scope = root.CreateScope()`：基于根容器创建「子作用域」，Scoped 服务的生命周期绑定到该作用域；
- `var child = scope.ServiceProvider`：获取子作用域的服务提供器（「子容器」），用于解析该作用域内的服务；
- `child.GetServices<IAccount>()`：解析 `IAccount` 服务（返回 `IEnumerable<IAccount>`）：
  - 因是 Transient，会新建 `Account` 实例，触发 `Base` 构造函数（打印「Created:Account 已创建！」）；
- `child.GetServices<IMessage>()`：解析 `IMessage` 服务：
  - 因是 Scoped，在当前子作用域内新建 `Message` 实例，触发构造函数（打印「Created:Message 已创建！」）；
- `child.GetService<ITool>()`：解析 `ITool` 服务：
  - 因是 Singleton，全局首次解析，新建 `Tool` 实例，触发构造函数（打印「Created:Tool 已创建！」）；
- `Console.WriteLine("释放子容器！")`：标记子作用域即将释放；
- 子作用域 `using` 结束：触发 Scoped 服务（Message）的 `Dispose`（打印「Disposed:Message 已释放！」）；
- `Console.WriteLine("释放根容器！")`：标记根容器即将释放；
- 根容器 `using` 结束：触发 Singleton 服务（Tool）的 `Dispose`（打印「Disposed:Tool 已释放！」）；
- Transient 服务（Account）：因 `GetServices` 返回的实例无外部引用，GC 回收时触发 `Dispose`（演示中可能因控制台程序退出快，打印不明显）。

### 6. 注释掉的代码（补充演示多作用域）

```csharp
//var root = new ServiceCollection()
//    .AddTransient<IAccount, Account>()
//    .AddScoped<IMessage, Message>()
//    .AddSingleton<ITool, Tool>()
//    .BuildServiceProvider();

//var child1 = root.CreateScope().ServiceProvider; // 子作用域1
//var child2 = root.CreateScope().ServiceProvider; // 子作用域2

//GetService<IAccount>(child1); // 子作用域1解析Transient（新建Account）
//GetService<IMessage>(child1); // 子作用域1解析Scoped（新建Message）
//GetService<ITool>(child1);    // 子作用域1解析Singleton（新建Tool）
//Console.WriteLine();
//GetService<IAccount>(child2); // 子作用域2解析Transient（新建Account）
//GetService<IMessage>(child2); // 子作用域2解析Scoped（新建Message）
//GetService<ITool>(child2);    // 子作用域2解析Singleton（复用已创建的Tool）
```

- 核心意图：演示「多作用域下的生命周期差异」：
  - Transient：child1/child2 解析时各新建 Account 实例（共 2 个）；
  - Scoped：child1/child2 各新建 Message 实例（共 2 个）；
  - Singleton：child1 首次解析新建 Tool，child2 复用（仅 1 个）；
- 未执行：仅作注释补充，强化生命周期理解。

### 7. GetService<T> 辅助方法

```csharp
public static void GetService<T>(IServiceProvider provider)
{
    provider.GetService<T>(); // 第一次解析T服务
    provider.GetService<T>(); // 第二次解析T服务
}
```

- 作用：重复解析同一服务，验证生命周期规则：
  - Transient：两次解析→新建 2 个实例（打印 2 次「Created:Account 已创建！」）；
  - Scoped：两次解析→复用 1 个实例（仅打印 1 次「Created:Message 已创建！」）；
  - Singleton：两次解析→复用 1 个实例（仅打印 1 次「Created:Tool 已创建！」）。

## 三、核心运行结果 & 解释

### 1. 实际运行输出（主逻辑）

```plaintext
Created:Account已创建！  // child.GetServices<IAccount>() → Transient新建
Created:Message已创建！  // child.GetServices<IMessage>() → Scoped新建
Created:Tool已创建！     // child.GetService<ITool>() → Singleton首次新建
释放子容器！
Disposed:Message已释放！ // 子作用域释放→Scoped对象释放
释放根容器！
Disposed:Tool已释放！    // 根容器释放→Singleton对象释放
Disposed:Account已释放！ // Transient对象GC回收→释放（可能延迟/不显示，因控制台退出快）
```

### 2. 关键结论（生命周期核心规则）

| 生命周期  | 解析规则                         | 释放时机                           |
| --------- | -------------------------------- | ---------------------------------- |
| Transient | 每次解析新建实例                 | 无固定时机（GC 回收 / 容器释放时） |
| Scoped    | 同一作用域内复用，不同作用域新建 | 所属作用域释放时（using 结束）     |
| Singleton | 全局仅创建一次，所有作用域复用   | 根容器释放时（using 结束）         |

## 四、补充说明（易忽略细节）

1. `GetServices<T>()` vs `GetService<T>()`：
   - `GetServices<T>()`：返回 `IEnumerable<T>`，即使无注册也返回空集合（不报错）；
   - `GetService<T>()`：返回单个实例，无注册时返回 `null`；
   - 演示中用 `GetServices` 是为了「解析但不报错」（空接口无实现也不会抛异常）。
2. `using` 包裹容器 / 作用域的必要性：
   - 若不使用 `using`，容器 / 作用域不会自动释放，`IDisposable` 的 `Dispose` 方法不会被调用，导致资源泄漏（如数据库连接、文件句柄）。
3. 根容器解析 Scoped 服务的风险：
   - 若直接用 `root.GetService<IMessage>()`（根容器解析 Scoped），Scoped 服务会降级为 Singleton（根容器的作用域是全局），易导致资源泄漏 ——**Scoped 服务必须在子作用域内解析**。

## 总结

​	这段代码通过「基类追踪生命周期 + 三种注册方式 + 作用域管理」，完整演示了 .NET 原生 DI 容器的核心规则：

- Transient：“即用即建，用完即弃”；

- Scoped：“作用域内复用，作用域释放销毁”；

- Singleton：“全局唯一，容器销毁才释放”。

  

  核心价值是帮助理解 DI 容器如何管控对象的创建和释放，是工业级应用中「资源管理 + 依赖解耦」的基础。