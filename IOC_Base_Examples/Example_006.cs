using Autofac;
using Autofac.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    internal class Example_006
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
            var serviceCollection = new ServiceCollection()
                .AddSingleton<Base, Account>()
                .AddScoped<Base, Message>();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.Populate(serviceCollection);

            containerBuilder.RegisterType<Tool>().As<ITool>();

            var container = containerBuilder.Build();

            IServiceProvider privider = new AutofacServiceProvider(container);


        }
    }
}
