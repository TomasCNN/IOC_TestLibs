using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_005
    {
        public interface IAccount { }
        public interface IMessage { }

        public interface ITool { }

        public interface ITest { }



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

        public class Account : IAccount { }
        public class Message : IMessage { }
        public class Tool : ITool { }
        public class Test : ITest
        {
            public Test(IAccount account)
            {
                Console.WriteLine($"Ctor:Test(IAccount)!");
            }

            public Test(IAccount account, IMessage message)
            {
                Console.WriteLine($"Ctor:Test(IAccount,IMessage)!");
            }

            public Test(IAccount account, IMessage message, ITool tool)
            {
                Console.WriteLine($"Ctor:Test(IAccount,IMessage,ITest)!");
            }
        }



        public static void Run()
        {
            var test = new ServiceCollection()
                .AddTransient<IAccount, Account>()
                .AddScoped<IMessage, Message>()
                .AddSingleton<ITest, Test>()
                .BuildServiceProvider()
                .GetService<ITest>();
        }
    }
}
