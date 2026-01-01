using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_004
    {
        public interface IAccount { }
        public interface IMessage { }

        public interface ITool { }

        public class Base : IDisposable
        {
            public Base()
            {
                Console.WriteLine($"Created:{GetType().Name}已创建！");
            }

            public void Dispose()
            {
                Console.WriteLine($"Disposed:{GetType().Name}已释放！");
            }
        }

        public class Account : Base, IAccount { }
        public class Message : Base, IMessage { }

        public class Tool : Base, ITool { }

        public static void Run()
        {
            using (var root = new ServiceCollection()
                .AddTransient<IAccount, Account>()
                .AddScoped<IMessage, Message>()
                .AddSingleton<ITool, Tool>()
                .BuildServiceProvider())
            {
                using (var scope = root.CreateScope())
                {
                    var child = scope.ServiceProvider;
                    child.GetServices<IAccount>();
                    child.GetServices<IMessage>();
                    child.GetService<ITool>();
                    Console.WriteLine("释放子容器！");
                }
                Console.WriteLine("释放根容器！");
            }
        }
    }
}
