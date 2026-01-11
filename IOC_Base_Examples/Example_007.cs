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

        //public interface ITest
        //{
        //    public IMessage Message { get; set; }
        //}

        public class Account : Base, IAccount { }
        public class Message : Base, IMessage { }

        public class Tool : Base, ITool { }

        //public class Test : ITest
        //{
        //    public IMessage Message { set; get; }
        //    public Test(IAccount account, ITool tool)
        //    {
        //        Console.WriteLine($"Ctor:Test(IAccount,ITool)!");
        //    }
        //}
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
        }
    }
}
