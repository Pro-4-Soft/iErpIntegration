using Pro4Soft.iErpIntegration.Infrastructure;

namespace Pro4Soft.iErpIntegration
{
    class Program
    {
        static void Main(string[] args)
        {
            Singleton<EntryPoint>.Instance.Initialize(args);
            Singleton<EntryPoint>.Instance.Startup();
        }
    }
}
