using System;
using NetBase;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            BaseApplication.RegisterCommandHandler(
                "addrobot",
                "添加一个机器人，用法:addrobot token代表唯一登录标识",
                onAddRobot);

            BaseApplication.InitEvent += ApplicationInit;
            BaseApplication.UnInitEvent += ApplicationUnInit;
            BaseApplication.Run();
        }

        static bool ApplicationInit()
        {
            UserManager.Init();
            Log.Info("已启动，请执行GM命令。");
            return true;
        }

        static void ApplicationUnInit()
        {
            UserManager.UnInit();
        }

        static async void onAddRobot(CommandArgs args)
        {
            var token = args[0];
            if (string.IsNullOrEmpty(token))
            {
                Log.WriteCommand("缺少token参数");
                return;
            }

            var user = new IUser();
            var result = await user.LoginAsync(
                "8.210.206.80",
                16001,
                1,
                token);

            if (!string.IsNullOrEmpty(result))
            {
                Log.Error($"机器人登录失败：{result}");
                return;
            }

            Log.WriteCommand(
$@"机器人登录成功:
{user.Guid}_{user.UserId}_{user.Nickname}
Currency:{user.Currency}
BankCurrency:{user.BankCurrency}");
        }
    }
}
