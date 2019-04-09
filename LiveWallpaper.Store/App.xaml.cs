﻿using EasyMvvm;
using LiveWallpaper.Server;
using LiveWallpaper.Store.ViewModels;
using LiveWallpaper.Store.Views;
using MultiLanguageManager;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;

namespace LiveWallpaper.Store
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public App()
        {
            //异常捕获
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            //多语言初始化
            string appDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string path = Path.Combine(appDir, "Assets\\Languages");
            LanService.Init(new JsonDB(path), true, "zh");

            //mvvm初始化
            IocContainer container = new IocContainer();
            container
                .Singleton<LocalServer>()
                .Singleton<WallpapersViewModel>()
                .Singleton<SettingViewModel>()
                .Singleton<AppMenuViewModel>();

            var inputs = GetInputs();
            if (inputs == null || inputs.Count() == 0)
            {
                var result = MessageBox.Show("请从《巨应动态壁纸》启动本程序，是否打开下载链接？", "警告", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    Process.Start("https://www.mscoder.cn/product/livewallpaper/");
                }
                Shutdown(0);
                return;
            }
            AppManager appManager = new AppManager();
            if (inputs.ContainsKey("wallpaper"))
            {
                string saveDir = inputs["wallpaper"];
                SetSaveDir(appManager, saveDir);
            }
            container.Instance(appManager);

            EasyManager.Initialize(container, new StoreNavigator());
            EasyManager.Associate<WallpapersView, WallpapersViewModel>();
            EasyManager.Associate<SettingView, SettingViewModel>();
        }

        internal async void SetSaveDir(AppManager app, string dir)
        {
            var tmp = await app.LoadConfig();
            tmp.General.WallpaperSaveDir = dir;
            await app.SaveConfig(tmp);
        }

        private Dictionary<string, string> GetInputs()
        {
            Dictionary<string, string> inputs = new Dictionary<string, string>();
            try
            {
                var args = Environment.GetCommandLineArgs();

                if (args.Length > 1)
                {
                    Uri argUri;
                    if (Uri.TryCreate(args[1], UriKind.Absolute, out argUri))
                    {
                        var decoder = new WwwFormUrlDecoder(argUri.Query);
                        if (decoder.Any())
                        {
                            foreach (var entry in decoder)
                            {
                                inputs[entry.Name] = entry.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return inputs;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            logger.Error(ex);
            MessageBox.Show(ex.Message);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            logger.Error(ex);
            MessageBox.Show(ex.Message);
        }
    }
}
