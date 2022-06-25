using System;
using NetBase;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            BaseApplication.InitEvent += ApplicationInit;
            BaseApplication.UnInitEvent += ApplicationUnInit;
            BaseApplication.Run();
        }

        static bool ApplicationInit()
        {
            Log.Info("初始化");
            return true;
        }

        static void ApplicationUnInit()
        {

        }
    }
}
