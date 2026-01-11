# Example_007 逐句完整解析（Autofac 整合 .NET 原生 DI 容器）

​	这段代码是 **.NET 原生 DI 容器 + Autofac 容器混合使用的经典示例**，核心演示「将微软原生`ServiceCollection`的服务注册，合并到 Autofac 容器中、实现容器整合」的核心用法，也是实际项目中从原生 DI 迁移到 Autofac 的标准写法。

> 补充背景：Autofac 是.NET 生态中功能最强大的第三方 IOC 容器，比原生 DI 支持更多高级特性（属性注入、批量注册、生命周期更灵活等），项目中经常会做「原生 DI 注册 + Autofac 扩展注册」的整合，本代码就是这个场景的最小实践。

## 一、先说明代码核心背景

1. 代码中**大量注释的代码（`ITest`/`Test`类）** 是预留的扩展代码，本案例暂未启用，不影响主逻辑；
2. 本案例核心目的：**原生`ServiceCollection`注册部分服务 + Autofac 容器注册另一部分服务 → 整合为一个统一的容器 → 最终提供标准的`.NET IServiceProvider`供程序使用**；
3. 代码中`Base`基类的作用：无`IDisposable`，仅通过构造函数打印日志，追踪所有服务实例的「创建时机」。

------

## 二、逐句完整解析（按代码执行顺序）

### ✅ 1. 引用命名空间（核心 3 个）

```c#
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
```

- `Microsoft.Extensions.DependencyInjection`：.NET 原生 DI 容器的核心命名空间，提供`ServiceCollection`、`IServiceProvider`等核心类型；
- `Autofac`：Autofac 容器的核心命名空间，提供`ContainerBuilder`（Autofac 的服务注册构建器）、`IContainer`（Autofac 的容器）；
- `Autofac.Extensions.DependencyInjection`：**Autofac 和原生 DI 的「桥接包」核心命名空间**，提供关键的`AutofacServiceProvider`类，实现「Autofac 容器」向「.NET 标准 IServiceProvider」的转换，这是整合的核心依赖。

### ✅ 2. 命名空间 + 类声明

```C#
namespace IOC_Base_Examples
{
    internal class Example_007
    {
```

- `IOC_Base_Examples`：IOC 基础案例的命名空间；
- `internal class Example_007`：当前案例的容器类，`internal`修饰表示仅当前程序集可见，封装所有演示代码。

### ✅ 3. 定义服务契约（空接口，DI 标准写法）

```c#
public interface IAccount { }
public interface IMessage { }
public interface ITool { }
```

- 这三个都是**服务契约（接口）**，在 DI 容器中，我们始终「面向接口注册、面向接口解析」，而不是面向具体实现类，这是 DI 的核心解耦思想；
- 接口内容为空：仅做 DI 的「映射标记」，实际项目中会在接口中定义业务方法。

### ✅ 4. 定义基类`Base`（追踪实例创建）

```c#
public class Base
{
    public Base()
    {
        Console.WriteLine($"Created:{GetType().Name}已创建！");
    }
}
```

- 作用：所有服务实现类都继承这个基类，当子类实例化时，会**自动调用父类的无参构造函数**，打印当前实例的类名，实现「创建日志追踪」；
- 无析构 / 释放方法：本案例仅演示创建，不演示释放，所以没有实现`IDisposable`；
- 执行逻辑：只要是`Base`的子类（Account/Message/Tool）被实例化，就会触发这个打印。

### ✅ 5. 定义服务实现类（核心：继承 Base + 实现接口）

```c#
public class Account : Base, IAccount { }
public class Message : Base, IMessage { }
public class Tool : Base, ITool { }
```

​	这三个类是**服务的具体实现**，都是「无状态、无自定义构造函数」的简单类，编译器会自动生成默认的无参构造函数，解析规则如下：

1. `Account`：继承`Base` + 实现`IAccount`，DI 容器解析`IAccount`时，会创建`Account`实例；
2. `Message`：继承`Base` + 实现`IMessage`，DI 容器解析`IMessage`时，会创建`Message`实例；
3. `Tool`：继承`Base` + 实现`ITool`，DI 容器解析`ITool`时，会创建`Tool`实例；
4. 关键细节：因为都继承了`Base`，**每个类第一次被实例化时，控制台都会打印 `Created:XXX已创建！`**。

### ✅ 6. 注释的预留代码（说明）

```C#
//public interface ITest
//{
//    public IMessage Message { get; set; }
//}
//public class Test : ITest
//{
//    public IMessage Message { set; get; }
//    public Test(IAccount account, ITool tool)
//    {
//        Console.WriteLine($"Ctor:Test(IAccount,ITool)!");
//    }
//}
```

​	这部分是**预留的扩展代码，本案例未启用**，做个补充说明：

- 定义了`ITest`接口，包含一个可读写的`IMessage`属性；
- `Test`类的构造函数**依赖了`IAccount`和`ITool`**，这是典型的「构造函数注入」，如果取消注释，Autofac 容器会自动解析这两个依赖并注入；
- 这段代码注释后，不影响本案例的核心逻辑，只是作者预留的扩展。

### ✅ 7. 核心执行方法 `Run()`（重中之重，逐行解析）

​	这是本案例的**核心逻辑方法**，所有的「服务注册、容器整合、容器构建」都在这个方法中完成，**逐行拆解，无遗漏**：

```c#
public static void Run()
{
    // 步骤1：使用.NET原生DI容器的ServiceCollection注册服务
    var serviceCollection = new ServiceCollection()
        .AddSingleton<IAccount, Account>()
        .AddScoped<IMessage, Message>();
```

#### 步骤 1 解析：原生 DI 的服务注册

1. `new ServiceCollection()`：创建.NET 原生的「服务注册清单」，本质是一个存储服务注册信息的集合；
2. `.AddSingleton<IAccount, Account>()`：**原生 DI 注册 - 单例服务**，映射关系：`IAccount`接口 → `Account`实现类，生命周期：全局唯一实例，第一次解析时创建，之后一直复用；
3. `.AddScoped<IMessage, Message>()`：**原生 DI 注册 - 作用域服务**，映射关系：`IMessage`接口 → `Message`实现类，生命周期：同一个作用域内复用实例，不同作用域创建新实例；
4. 关键：此时这两个服务的注册信息，**只存在于原生的`serviceCollection`中，还未构建成可使用的容器**。

```c#
    // 步骤2：创建Autofac的容器构建器（核心对象）
    var containerBuilder = new ContainerBuilder();
```

#### 步骤 2 解析：初始化 Autofac 容器构建器

- `ContainerBuilder` 是 Autofac 的「服务注册核心对象」，和原生 DI 的`ServiceCollection`作用一致：**专门用于存储服务的注册信息、映射关系、生命周期**；
- 所有要交给 Autofac 管理的服务，都需要通过这个对象进行注册。

```
    // 步骤3：将原生DI的注册信息，合并/填充到Autofac的构建器中（核心整合API）
    containerBuilder.Populate(serviceCollection);
```

#### 步骤 3 解析：原生 DI → Autofac 的核心桥接

- **`Populate()` 是本案例的核心 API**，属于 Autofac 的扩展方法，作用是：**把.NET 原生`ServiceCollection`中已经注册的所有服务（IAccount/IMessage），完整的迁移、合并到 Autofac 的`ContainerBuilder`中**；
- 执行完这行代码后，Autofac 的构建器中，就已经包含了「原生 DI 注册的 2 个服务」；
- 核心意义：**实现了「原生 DI 的注册清单」和「Autofac 的注册清单」的合并**，这是「混合注册」的关键一步。

```c#
    // 步骤4：使用Autofac的方式，向构建器中注册额外的服务
    containerBuilder.RegisterType<Tool>().As<ITool>();
```

#### 步骤 4 解析：Autofac 原生的服务注册方式

1. 这是**Autofac 独有的注册语法**，和原生 DI 的`AddXXX`写法不同，语法规则：`RegisterType<实现类>().As<服务接口>()`；
2. 映射关系：`ITool`接口 → `Tool`实现类；
3. ✅ 重点：**Autofac 的注册默认生命周期是「瞬时 (InstancePerDependency)」**，等价于原生 DI 的`AddTransient`，即**每次解析都会创建新实例**；
4. 执行完这行代码后，Autofac 的构建器中，现在包含了**3 个服务**：原生 DI 来的`IAccount(单例)`、`IMessage(作用域)` + Autofac 注册的`ITool(瞬时)`；
5. 补充：Autofac 支持指定生命周期，比如：
   - 单例：`containerBuilder.RegisterType<Tool>().As<ITool>().SingleInstance();` (等价原生`AddSingleton`)
   - 作用域：`containerBuilder.RegisterType<Tool>().As<ITool>().InstancePerLifetimeScope();` (等价原生`AddScoped`)

```C#
    // 步骤5：构建Autofac的最终容器（IContainer）
    var container = containerBuilder.Build();
```

#### 步骤 5 解析：构建 Autofac 容器

- `Build()` 是 Autofac 构建器的核心方法，执行后会把`ContainerBuilder`中所有的服务注册信息，编译为一个**可执行的 Autofac 容器（IContainer）**；
- 此时`container`就是 Autofac 的完整容器，已经可以直接通过 Autofac 的方式解析服务（`container.Resolve<ITool>()`）；
- 关键：**容器构建时，不会创建任何服务实例**，所有服务都是「延迟实例化」—— 第一次解析时才会创建实例。

```c#
    // 步骤6：将Autofac容器，转换为.NET标准的IServiceProvider（终极整合）
    IServiceProvider privider = new AutofacServiceProvider(container);
}
```

#### 步骤 6 解析：最终整合，实现「无缝切换」

1. `AutofacServiceProvider` 是本案例的**终极核心类**，来自`Autofac.Extensions.DependencyInjection`包；

2. 作用：**将 Autofac 的`IContainer`容器，包装成.NET 标准的`IServiceProvider`接口实例**；

3. 核心价值：

   - 你的业务代码中，**不需要任何改动**，依然使用.NET 原生的`GetService<T>()`/`GetRequiredService<T>()`解析服务；
   - 底层的容器实现却是功能更强的 Autofac，完美实现「原生语法 + Autofac 内核」的结合；
   - 这也是「项目从原生 DI 迁移到 Autofac」的最佳实践：业务层无感知，只改容器构建逻辑。

   

4. 代码笔误：变量名`privider` → 正确拼写是`provider`，不影响运行。

------

## 三、补充：扩展代码（让案例可运行 + 看到效果）

​	原代码只是完成了「注册和容器构建」，没有写**服务解析的代码**，所以运行后控制台无任何输出。这里补充完整的`Run()`方法代码（在最后一行后追加），运行后就能看到所有服务的创建日志，帮助你理解执行逻辑：

```c#
public static void Run()
{
    var serviceCollection = new ServiceCollection()
        .AddSingleton<IAccount, Account>()
        .AddScoped<IMessage, Message>();

    var containerBuilder = new ContainerBuilder();
    containerBuilder.Populate(serviceCollection);
    containerBuilder.RegisterType<Tool>().As<ITool>();
    var container = containerBuilder.Build();
    IServiceProvider provider = new AutofacServiceProvider(container);

    // ===== 追加：服务解析代码 =====
    Console.WriteLine("===== 解析单例服务IAccount =====");
    var account1 = provider.GetRequiredService<IAccount>();
    var account2 = provider.GetRequiredService<IAccount>();
    Console.WriteLine($"是否同一个实例：{ReferenceEquals(account1, account2)}\n");

    Console.WriteLine("===== 解析作用域服务IMessage =====");
    using(var scope = provider.CreateScope())
    {
        var msg1 = scope.ServiceProvider.GetRequiredService<IMessage>();
        var msg2 = scope.ServiceProvider.GetRequiredService<IMessage>();
        Console.WriteLine($"作用域内是否同一个实例：{ReferenceEquals(msg1, msg2)}\n");
    }

    Console.WriteLine("===== 解析瞬时服务ITool =====");
    var tool1 = provider.GetRequiredService<ITool>();
    var tool2 = provider.GetRequiredService<ITool>();
    Console.WriteLine($"是否同一个实例：{ReferenceEquals(tool1, tool2)}");
}
```

### ✅ 扩展代码运行结果（控制台输出）

```
===== 解析单例服务IAccount =====
Created:Account已创建！
是否同一个实例：True

===== 解析作用域服务IMessage =====
Created:Message已创建！
作用域内是否同一个实例：True

===== 解析瞬时服务ITool =====
Created:Tool已创建！
Created:Tool已创建！
是否同一个实例：False
```

### 结果解析：

1. `IAccount`是单例：第一次解析创建实例，第二次解析复用，只打印一次创建日志；
2. `IMessage`是作用域：作用域内解析两次复用实例，只打印一次创建日志；
3. `ITool`是瞬时：Autofac 默认注册是瞬时，每次解析都创建新实例，打印两次创建日志。

------

## 四、本案例核心知识点总结（必记）

### ✅ 核心 1：Autofac 与 .NET 原生 DI 整合的标准流程

```
1. 创建原生 ServiceCollection → 注册部分服务
2. 创建 Autofac ContainerBuilder → 调用 Populate() 合并原生注册信息
3. 用 Autofac 语法注册额外服务
4. 调用 Build() 构建 Autofac 的 IContainer
5. 用 AutofacServiceProvider 包装为 .NET 标准 IServiceProvider
6. 业务层通过 IServiceProvider 解析所有服务（无感知）
```

### ✅ 核心 2：关键 API 作用

1. `containerBuilder.Populate(serviceCollection)`：原生 DI → Autofac 注册信息迁移；
2. `containerBuilder.RegisterType<T>().As<I>()`：Autofac 原生注册语法，默认瞬时生命周期；
3. `new AutofacServiceProvider(container)`：Autofac 容器 → .NET 标准 IServiceProvider 转换；

### ✅ 核心 3：生命周期对应关系

| .NET 原生 DI |                   Autofac 注册语法                   |     生命周期含义     |
| :----------: | :--------------------------------------------------: | :------------------: |
| AddTransient |              RegisterType<T>().As<I>()               | 瞬时（每次解析新建） |
|  AddScoped   | RegisterType<T>().As<I>().InstancePerLifetimeScope() |  作用域（同域复用）  |
| AddSingleton |      RegisterType<T>().As<I>().SingleInstance()      |   单例（全局唯一）   |

### ✅ 核心 4：为什么要整合？

1. 项目中部分组件是基于原生 DI 开发的，直接复用即可；
2. Autofac 支持原生 DI 没有的高级特性（属性注入、批量注册、条件注册等）；
3. 业务层无需改动代码，完美兼容原生 DI 的解析语法；
4. 这是.NET 项目中「轻量 DI → 重量级 DI」的平滑迁移方案。

------

## 五、额外补充：Autofac 相对原生 DI 的优势

​	本案例是整合使用，补充一点优势帮你理解为什么要学 Autofac：

1. 支持**属性注入**（原生 DI 只支持构造函数注入）；
2. 支持**批量注册**（比如一次性注册某个程序集下的所有服务）；
3. 支持**条件注册**（根据条件动态注册不同的实现类）；
4. 支持**装饰器模式**（对已注册的服务进行增强，无需修改原代码）；
5. 生命周期管理更灵活，支持更多自定义生命周期。