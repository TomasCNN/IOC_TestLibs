using Autofac;
using Autofac.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_007_Plus
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