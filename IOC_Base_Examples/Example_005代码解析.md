# Example_005 逐句解析：.NET DI 容器的构造函数解析与生命周期

​	这段代码核心演示 **.NET 原生 DI 容器的两个关键特性**：

1. DI 容器自动解析构造函数依赖（参数注入）；
2. 不同生命周期服务的注册与解析逻辑；
3. 构造函数重载时的「参数匹配规则」（核心坑点）。

​	以下按代码结构逐句拆解，覆盖每个成员的作用、执行逻辑和运行结果。

## 一、基础结构：命名空间 & 类声明

```csharp
using Microsoft.Extensions.DependencyInjection; // 引入.NET DI核心命名空间

namespace IOC_Base_Examples // DI基础示例命名空间
{
    internal class Example_005 // 内部类，仅当前程序集可见
    {
```

### 解析：

- `Microsoft.Extensions.DependencyInjection` 是 .NET 原生 DI 容器的核心依赖，提供 `ServiceCollection`/`ServiceProvider` 等核心类型；
- `Example_005` 是封装 DI 演示逻辑的容器类，`internal` 修饰符表示仅当前程序集可访问。

## 二、接口定义（服务契约）

```csharp
public interface IAccount { } // 账户服务契约（空接口，仅作DI映射标记）
public interface IMessage { } // 消息服务契约
public interface ITool { }    // 工具服务契约
public interface ITest { }    // 测试服务契约（核心演示类的接口）
```

### 解析：

- 所有接口均为「空接口」，仅作为 DI 注册时「接口→实现类」的映射契约（实际项目中会定义业务方法）；
- `ITest` 是核心演示接口，其实现类 `Test` 包含多个重载构造函数，用于验证 DI 容器的构造函数解析规则。

## 三、基类 Base（IDisposable 生命周期追踪）

```csharp
public class Base : IDisposable // 实现IDisposable，支持容器自动释放
{
    public Base() // 构造函数：对象创建时执行
    {
        Console.WriteLine($"Created:{GetType().Name}已创建！");
    }

    public void Dispose() // 释放方法：对象销毁时执行
    {
        Console.WriteLine($"Disposed:{GetType().Name}已释放！");
    }
}
```

### 解析：

- 设计意图：通过构造函数 /`Dispose` 打印日志，追踪对象的「创建 / 释放」生命周期；
- 注意点：**本段代码中 `Base` 类未被任何实现类继承**（`Account`/`Message`/`Tool`/`Test` 均未继承 `Base`），因此 `Base` 的构造函数 /`Dispose` 不会被执行（代码设计上的小瑕疵）；
- `IDisposable`：实现该接口后，DI 容器会在对象生命周期结束时自动调用 `Dispose`（但本示例中无类继承 `Base`，因此无实际效果）。

## 四、服务实现类（核心：Test 类的重载构造函数）

### 1. 基础实现类（无依赖）

```csharp
public class Account : IAccount { } // 账户服务实现：仅实现IAccount，无构造函数
public class Message : IMessage { } // 消息服务实现：仅实现IMessage，无构造函数
public class Tool : ITool { }       // 工具服务实现：仅实现ITool，无构造函数
```

### 解析：

- 这三个类均为「无状态、无依赖」的简单实现类，构造函数为默认无参构造函数（编译器自动生成）；
- DI 容器解析这些类时，会直接调用无参构造函数创建实例。

### 2. Test 类（核心：重载构造函数 + 依赖注入）

```csharp
public class Test : ITest
{
    // 构造函数1：依赖IAccount
    public Test(IAccount account)
    {
        Console.WriteLine($"Ctor:Test(IAccount)!");
    }

    // 构造函数2：依赖IAccount + IMessage
    public Test(IAccount account, IMessage message)
    {
        Console.WriteLine($"Ctor:Test(IAccount,IMessage)!");
    }

    // 构造函数3：依赖IAccount + IMessage + ITool（注意：参数写错，应为ITool而非ITest）
    public Test(IAccount account, IMessage message, ITool tool)
    {
        Console.WriteLine($"Ctor:Test(IAccount,IMessage,ITest)!"); // 日志写错，应为ITool
    }
}
```

### 关键解析（DI 构造函数解析核心）：

1. **构造函数重载**：`Test` 定义了 3 个重载构造函数，均依赖其他服务接口，是 DI 容器「自动解析依赖」的核心演示点；
2. **参数错误（代码瑕疵）**：
   - 第三个构造函数的参数是 `ITool tool`，但日志写的是 `ITest`（笔误），实际运行时日志会输出 `Ctor:Test(IAccount,IMessage,ITest)!`，需注意区分；
3. **DI 构造函数选择规则**：
   - DI 容器会优先选择「参数数量最多、且所有参数都能从容器中解析」的构造函数；
   - 若某构造函数的参数无法解析（如缺少注册），容器会降级选择参数更少的构造函数；
   - 若所有构造函数的参数都无法解析，容器会抛 `InvalidOperationException`。

## 五、Run 方法（核心：DI 注册 + 解析）

```csharp
public static void Run()
{
    var test = new ServiceCollection() // 1. 创建服务注册清单
        .AddTransient<IAccount, Account>() // 2. 注册Transient服务：IAccount → Account
        .AddScoped<IMessage, Message>()    // 3. 注册Scoped服务：IMessage → Message
        .AddSingleton<ITest, Test>()       // 4. 注册Singleton服务：ITest → Test
        .BuildServiceProvider()            // 5. 构建根容器（ServiceProvider）
        .GetService<ITest>();              // 6. 从容器解析ITest服务
}
```

### 逐句拆解：

#### 1. `new ServiceCollection()`

- 创建「服务注册清单」（`ServiceCollection`），用于记录所有「接口→实现类」的映射关系和生命周期；
- 本质是一个 `List<ServiceDescriptor>`，每个 `ServiceDescriptor` 描述一个服务的注册信息。

#### 2. `.AddTransient<IAccount, Account>()`

- 注册 **Transient（瞬时）** 服务：
  - 映射关系：`IAccount` 接口 → `Account` 实现类；
  - 生命周期规则：每次解析 `IAccount` 都会新建 `Account` 实例；
  - 适用场景：无状态、轻量级服务（如工具类）。

#### 3. `.AddScoped<IMessage, Message>()`

- 注册 **Scoped（作用域）** 服务：
  - 映射关系：`IMessage` 接口 → `Message` 实现类；
  - 生命周期规则：同一作用域内解析 `IMessage` 复用实例，不同作用域新建；
  - 注意：本示例中未创建子作用域，直接从根容器解析，Scoped 服务会降级为 Singleton。

#### 4. `.AddSingleton<ITest, Test>()`

- 注册 **Singleton（单例）** 服务：
  - 映射关系：`ITest` 接口 → `Test` 实现类；
  - 生命周期规则：全局仅创建一次 `Test` 实例，所有解析操作复用；
  - 核心：`Test` 的构造函数依赖 `IAccount`/`IMessage`/`ITool`，DI 容器会自动解析这些依赖并注入。

#### 5. `.BuildServiceProvider()`

- 将「服务注册清单」转换为「可执行的 DI 容器」（`ServiceProvider`）；
- 容器初始化时，不会立即创建服务实例（除非注册时指定 `AddSingleton<T>(new T())`），而是「延迟解析」（首次 `GetService` 时创建）。

#### 6. `.GetService<ITest>()`

- 从根容器解析 `ITest` 服务（核心执行逻辑）：
  - 步骤 1：容器发现 `ITest` 映射到 `Test`，且是 Singleton 服务；
  - 步骤 2：容器遍历 `Test` 的所有构造函数，寻找「参数最多且可解析」的构造函数：
    - 构造函数 3：参数 `IAccount`（已注册）、`IMessage`（已注册）、`ITool`（未注册）→ 无法解析；
    - 构造函数 2：参数 `IAccount`（已注册）、`IMessage`（已注册）→ 所有参数可解析；
    - 构造函数 1：参数更少，降级备选；
  - 步骤 3：容器自动解析 `IAccount` 和 `IMessage` 的实例，注入到 `Test` 的第二个构造函数，创建 `Test` 实例；
  - 步骤 4：返回 `Test` 实例，赋值给 `test` 变量。

## 六、运行结果 & 核心结论

### 1. 实际运行输出

```plaintext
Ctor:Test(IAccount,IMessage)!
```

### 2. 结果解析：

- 容器选择了 `Test` 的第二个构造函数（`Test(IAccount, IMessage)`），原因是：
  - 第三个构造函数依赖 `ITool`，但代码中未注册 `ITool → Tool`，因此无法解析；
  - 第二个构造函数的 `IAccount`/`IMessage` 均已注册，是「参数最多且可解析」的构造函数；
- `Base` 类的日志未输出，因为 `Account`/`Message`/`Test` 均未继承 `Base`；
- 无 `Dispose` 日志输出，因为：
  - 根容器未被 `using` 包裹，未触发释放；
  - 即使包裹，`Test`/`Account`/`Message` 未实现 `IDisposable`，容器也不会调用 `Dispose`。

### 3. 关键补充（修正代码后的运行结果）

若在 `Run` 方法中补充注册 `ITool`：

```csharp
var test = new ServiceCollection()
    .AddTransient<IAccount, Account>()
    .AddScoped<IMessage, Message>()
    .AddTransient<ITool, Tool>() // 新增：注册ITool → Tool
    .AddSingleton<ITest, Test>()
    .BuildServiceProvider()
    .GetService<ITest>();
```

此时运行输出：

```plaintext
Ctor:Test(IAccount,IMessage,ITest)! // 日志笔误，实际依赖的是ITool
```

原因：第三个构造函数的所有参数（`IAccount`/`IMessage`/`ITool`）均可解析，容器优先选择该构造函数。

## 七、代码瑕疵修正 & 扩展

### 1. 修正 `Base` 类继承（让生命周期日志生效）

```csharp
// 修正后：实现类继承Base
public class Account : Base, IAccount { }
public class Message : Base, IMessage { }
public class Tool : Base, ITool { }
public class Test : Base, ITest { /* 构造函数不变 */ }
```

修正后运行（补充 `ITool` 注册 + `using` 包裹容器）：

```csharp
public static void Run()
{
    using (var provider = new ServiceCollection()
        .AddTransient<IAccount, Account>()
        .AddScoped<IMessage, Message>()
        .AddTransient<ITool, Tool>()
        .AddSingleton<ITest, Test>()
        .BuildServiceProvider())
    {
        var test = provider.GetService<ITest>();
    }
}
```

输出结果：

```plaintext
Created:Account已创建！    // 解析IAccount时创建
Created:Message已创建！    // 解析IMessage时创建
Created:Tool已创建！       // 解析ITool时创建
Created:Test已创建！       // 解析ITest时创建
Ctor:Test(IAccount,IMessage,ITest)! // 构造函数日志
Disposed:Test已释放！      // 根容器释放→Singleton的Test释放
Disposed:Tool已释放！      // Transient的Tool随容器释放
Disposed:Message已释放！   // Scoped的Message随根容器释放（降级为Singleton）
Disposed:Account已释放！   // Transient的Account随容器释放
```

### 2. 修正 `Test` 构造函数日志笔误

```csharp
// 修正后
public Test(IAccount account, IMessage message, ITool tool)
{
    Console.WriteLine($"Ctor:Test(IAccount,IMessage,ITool)!");
}
```

## 八、核心知识点总结

| 知识点           | 核心规则                                                     |
| ---------------- | ------------------------------------------------------------ |
| DI 构造函数选择  | 优先选择「参数数量最多、且所有参数可解析」的构造函数；参数无法解析则降级；无可用构造函数则抛异常 |
| 生命周期影响     | 根容器解析 Scoped 服务 → 降级为 Singleton；Transient 每次解析新建；Singleton 全局唯一 |
| IDisposable 释放 | 容器会自动调用实现 `IDisposable` 服务的 `Dispose`，但需用 `using` 包裹容器 / 作用域 |
| 服务注册缺失     | 依赖的服务未注册 → 容器无法解析该构造函数，降级选择其他构造函数 |

​	这段代码的核心价值是演示「DI 容器的构造函数自动解析规则」，这是 .NET 依赖注入的核心机制，理解后可避免因构造函数重载 / 服务注册缺失导致的解析异常。