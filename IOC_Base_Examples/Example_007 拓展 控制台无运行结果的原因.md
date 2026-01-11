# Example_007 拓展 控制台无运行结果的原因

​	这个问题问到了核心点上，这段 `Example_007` 代码运行后**完全没有控制台输出**，是**100% 正常现象**，也是新手接触 Autofac/.NET DI 最容易踩的坑，我会把「无输出的核心原因」+「逐点解释」+「完整修复代码」+「扩展输出效果」全部讲清楚，内容循序渐进，保证你彻底理解。

## 一、✅ 核心原因（最关键，只有这两点，缺一不可）

​	这段代码**只做了【服务注册】+【容器构建】，完全没有写【服务解析】的代码**，这是无输出的唯一根源，拆解为 2 个核心点：

### 1. 基类`Base`的打印日志，触发条件是「**服务实例被创建时**」

​	代码中`Base`类的构造函数里的`Console.WriteLine`，**不是定义了就会执行**：

```c#
public class Base
{
    public Base()
    {
        Console.WriteLine($"Created:{GetType().Name}已创建！");
    }
}
```

​	它的执行规则是：**只有当`Base`的子类（Account/Message/Tool）被`new`出来、创建实例对象的时候，才会执行父类 Base 的构造函数，打印日志**。

- 你的代码里**没有任何一行代码去创建这些子类的实例**；
- 自然不会触发打印，控制台就是空白的。

### 2. .NET/Autofac 的 DI 容器，全部遵循「**延迟实例化 / 懒加载**」原则

​	这是 DI 容器的**核心设计规则**，也是无输出的核心原因：

> 不管是.NET 原生 DI 的`ServiceCollection`，还是 Autofac 的`ContainerBuilder`，**执行注册、执行 Build 构建容器时，都不会创建任何服务的实例**。

​	容器在执行 `AddSingleton/AddScoped/RegisterType/Build` 这些方法时，**只做一件事**：把「接口→实现类」「生命周期」这些映射关系，记录到容器的注册清单里，仅此而已。

- 服务实例的创建，**只会发生在【解析服务】的那一刻**（调用`GetService/GetRequiredService/Resolve`时）；
- 你的代码到最后一行 `new AutofacServiceProvider(container)` 就结束了，**没有任何解析服务的代码**，所以没有任何服务被实例化，没有任何日志输出。



## 二、✅ 补充：代码里的「生命周期注册」不会产生输出

​	你代码里注册的这些生命周期，也不会触发输出：

```c#
.AddSingleton<IAccount, Account>() // 只记录映射关系，不创建实例
.AddScoped<IMessage, Message>()     // 只记录映射关系，不创建实例
.RegisterType<Tool>().As<ITool>()   // 只记录映射关系，不创建实例
```

​	哪怕是`Singleton`单例服务，也**不会在容器构建时创建**，而是**第一次解析的时候才创建**，之后复用这个实例。



## 三、✅ 完整修复代码（直接复制可用，带详细注释）

​	我在你原代码的基础上，**只增加【服务解析】的代码**，不修改任何原有逻辑，运行后就能看到完整的控制台输出，完美解决「无输出」问题。

```c#
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_007
    {
        public interface IAccount { }
        public interface IMessage { }
        public interface ITool { }

        public class Base
        {
            public Base()
            {
                Console.WriteLine($"Created:{GetType().Name}已创建！");
            }
        }

        public class Account : Base, IAccount { }
        public class Message : Base, IMessage { }
        public class Tool : Base, ITool { }

        public static void Run()
        {
            // ========= 原代码 开始 =========
            var serviceCollection = new ServiceCollection()
                .AddSingleton<IAccount, Account>()
                .AddScoped<IMessage, Message>();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(serviceCollection);
            containerBuilder.RegisterType<Tool>().As<ITool>();
            var container = containerBuilder.Build();
            // 注意：原代码变量名拼写错误 privider → provider，已修正
            IServiceProvider provider = new AutofacServiceProvider(container);
            // ========= 原代码 结束 =========

            // ========= 新增：服务解析代码（核心！触发实例创建+打印日志） =========
            Console.WriteLine("===== 1. 解析【单例服务】IAccount =====");
            // 第一次解析：创建实例，触发打印
            var account1 = provider.GetRequiredService<IAccount>();
            // 第二次解析：复用单例实例，不会再次打印
            var account2 = provider.GetRequiredService<IAccount>();
            Console.WriteLine($"两次解析是否为同一个实例：{ReferenceEquals(account1, account2)}\n");

            Console.WriteLine("===== 2. 解析【作用域服务】IMessage =====");
            // Scoped服务必须在子作用域中解析（原生DI/Autofac通用规则）
            using (var scope = provider.CreateScope())
            {
                var scopeProvider = scope.ServiceProvider;
                // 第一次解析：创建实例，触发打印
                var msg1 = scopeProvider.GetRequiredService<IMessage>();
                // 第二次解析：同作用域复用实例，不会再次打印
                var msg2 = scopeProvider.GetRequiredService<IMessage>();
                Console.WriteLine($"同作用域内两次解析是否为同一个实例：{ReferenceEquals(msg1, msg2)}\n");
            }

            Console.WriteLine("===== 3. 解析【瞬时服务】ITool =====");
            // Autofac的RegisterType默认是瞬时（InstancePerDependency），等价原生AddTransient
            // 第一次解析：创建实例，触发打印
            var tool1 = provider.GetRequiredService<ITool>();
            // 第二次解析：创建新实例，再次触发打印
            var tool2 = provider.GetRequiredService<ITool>();
            Console.WriteLine($"两次解析是否为同一个实例：{ReferenceEquals(tool1, tool2)}\n");
        }
    }
}
```



## 四、✅ 运行后完整的控制台输出结果

```
===== 1. 解析【单例服务】IAccount =====
Created:Account已创建！
两次解析是否为同一个实例：True

===== 2. 解析【作用域服务】IMessage =====
Created:Message已创建！
同作用域内两次解析是否为同一个实例：True

===== 3. 解析【瞬时服务】ITool =====
Created:Tool已创建！
Created:Tool已创建！
两次解析是否为同一个实例：False
```

------

## 五、✅ 输出结果逐行解析（对应生命周期 + 实例化规则）

​	结合输出和代码，彻底理解「解析→实例化→打印」的逻辑，这是 DI 的核心知识点：

### 1. 单例服务 `IAccount`

- 输出：只打印 1 次 `Created:Account已创建！`
- 原因：`AddSingleton` 是全局单例，**第一次解析时创建实例，之后所有解析都复用这个实例**，所以第二次解析不会再次打印。
- 验证：`ReferenceEquals(account1, account2)` 返回 `True`，证明是同一个实例。

### 2. 作用域服务 `IMessage`

- 输出：只打印 1 次 `Created:Message已创建！`
- 原因：`AddScoped` 是作用域单例，**同一个作用域内，第一次解析创建实例，之后复用**，第二次解析不会再次打印。
- 验证：`ReferenceEquals(msg1, msg2)` 返回 `True`，证明是同一个实例。
- 注意：Scoped 服务**严禁直接从根容器解析**，必须创建子作用域 `provider.CreateScope()`，否则会降级为单例，造成资源泄漏。

### 3. 瞬时服务 `ITool`

- 输出：打印 2 次 `Created:Tool已创建！`
- 原因：**Autofac 的默认注册生命周期是「瞬时（InstancePerDependency）」**，等价于.NET 原生的`AddTransient`，**每一次解析都会创建一个全新的实例**，所以两次解析打印两次日志。
- 验证：`ReferenceEquals(tool1, tool2)` 返回 `False`，证明是两个不同的实例。



## 六、✅ 额外补充 3 个高频知识点（针对这段代码，必记）

### 知识点 1：Autofac 注册的默认生命周期

```c#
containerBuilder.RegisterType<Tool>().As<ITool>();
```

​	这行代码**没有显式指定生命周期**，Autofac 的默认规则是：**InstancePerDependency（瞬时）** → 等价于原生 DI 的`AddTransient`，这是你这段代码的隐藏知识点。

​	如果需要指定其他生命周期，写法如下：

```c#
// 等价原生 AddSingleton → 单例
containerBuilder.RegisterType<Tool>().As<ITool>().SingleInstance();
// 等价原生 AddScoped → 作用域
containerBuilder.RegisterType<Tool>().As<ITool>().InstancePerLifetimeScope();
```

### 知识点 2：`Populate`的核心作用

```c#
containerBuilder.Populate(serviceCollection);
```

​	这行代码是「原生 DI」和「Autofac」整合的核心，作用是：**把原生`ServiceCollection`里注册的所有服务，完整复制到 Autofac 的容器构建器中**，之后这些服务就归 Autofac 管理了，生命周期规则完全生效。

### 知识点 3：`AutofacServiceProvider`的意义

```C#
IServiceProvider provider = new AutofacServiceProvider(container);
```

​	这行代码的价值是：**把 Autofac 的容器包装成.NET 标准的`IServiceProvider`**，你的业务代码中，依然可以用原生的`GetRequiredService<T>()`解析服务，**不需要写任何 Autofac 的专属语法**，实现「无缝整合」。

------

## 七、✅ 总结（无输出的根本原因 + 核心规则）

### ✔️ 无输出的根本原因

> 你的代码**只有「服务注册 + 容器构建」，没有「服务解析」，没有任何服务被实例化，因此没有触发 Base 类的打印日志**。

### ✔️ DI 容器的核心规则（必背）

1. 容器的「注册 / 构建」阶段，只记录映射关系，**不创建任何服务实例**；
2. 服务实例的创建，**只发生在「解析服务」的那一刻**；
3. 所有的打印 / 初始化逻辑，都会在实例创建时触发；
4. Autofac 注册默认是「瞬时生命周期」，原生 DI 需要显式指定。

### ✔️ 一句话记住

**注册不实例化，解析才实例化；不解析，无实例，无输出。**