using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

namespace IOC_Base_Examples
{
    public class Example_001
    {
        public interface IAccount { }
        public interface IMessage { }

        public interface ITool { }

        public class Account : IAccount { }
        public class Message : IMessage { }

        public class Tool : ITool { }

        public static void Run()
        {
            var provider = new ServiceCollection()
                .AddSingleton<IAccount, Account>()
                .AddScoped<IMessage, Message>()
                .AddTransient<ITool, Tool>()
                .BuildServiceProvider();

            Debug.Assert(provider.GetService<IAccount>() is Account);
            Debug.Assert(provider.GetService<IMessage>() is Message);
            Debug.Assert(provider.GetService<ITool>() is Tool);


        }
    }
}
