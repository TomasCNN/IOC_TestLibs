using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_002
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
            var services = new ServiceCollection()
                .AddSingleton<Base, Account>()
                .AddScoped<Base, Message>()
                .AddTransient<Base, Tool>()
                .BuildServiceProvider()
                .GetServices<Base>().ToList();


            Debug.Assert(services.OfType<IAccount>().Any());
            Debug.Assert(services.OfType<IMessage>().Any());
            Debug.Assert(services.OfType<ITool>().Any());
        }
    }
}
