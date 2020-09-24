using System;
using System.IO;
using MHLab.Patch.Core.Client;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Launcher.Scripts.Utilities;
using UnityEngine;

namespace MHLab.Patch.Launcher.Scripts
{
    public sealed class Launcher : LauncherBase
    {
        private Repairer _repairer;
        private Updater _updater;
        
        protected override void Initialize(UpdatingContext context)
        {
            _repairer = new Repairer(context);
            _repairer.Downloader.DownloadComplete += Data.DownloadComplete;
            _repairer.Downloader.ProgressChanged += Data.DownloadProgressChanged;

            _updater = new Updater(context);
            _updater.Downloader.DownloadComplete += Data.DownloadComplete;
            _updater.Downloader.ProgressChanged += Data.DownloadProgressChanged;
            
            context.RegisterUpdateStep(_repairer);
            context.RegisterUpdateStep(_updater);

            context.Runner.PerformedStep += (sender, updater) =>
            {
                if (context.IsDirty(out var reasons))
                {
                    context.Logger.Debug("Context is set to dirty: updater restart required. The files {DirtyFiles} have been replaced.", reasons);
                    UpdateRestartNeeded();
                }
            };
        }

        protected override string UpdateProcessName => "Game updating";

        protected override void OverrideSettings(ILauncherSettings settings)
        {
            string rootPath = string.Empty;
            
#if UNITY_EDITOR
            rootPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), LauncherData.WorkspaceFolderName, "TestLauncher");
            Directory.CreateDirectory(rootPath);
#elif UNITY_STANDALONE_WIN
            rootPath = Directory.GetParent(Application.dataPath).FullName;
#elif UNITY_STANDALONE_LINUX
            rootPath = Directory.GetParent(Application.dataPath).FullName;
#elif UNITY_STANDALONE_OSX
            rootPath = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
#endif
            
            settings.RootPath = rootPath;
        }
        
        protected override void UpdateStarted()
        {
            Data.StartTimer();
        }
        
        protected override void UpdateCompleted()
        {
            Data.Log(Context.LocalizedMessages.UpdateProcessCompleted);
            Context.Logger.Info($"===> [{UpdateProcessName}] process COMPLETED! <===");
            
            Data.Dispatcher.Invoke(() =>
            {
                Data.ProgressBar.Progress = 1;
                Data.ProgressPercentage.text = "100%";
            });
            
            EnsureExecutePrivileges(PathsManager.Combine(Context.Settings.GetGamePath(), Data.GameExecutableName));
            EnsureExecutePrivileges(PathsManager.Combine(Context.Settings.RootPath, Data.LauncherExecutableName));
            
            Data.Dispatcher.Invoke(() =>
            {
                Invoke(nameof(StartGame), 1.5f);
            });
        }

        protected override void UpdateFailed(Exception e)
        {
            Data.Log(Context.LocalizedMessages.UpdateProcessFailed);
            Context.Logger.Error(e, $"===> [{UpdateProcessName}] process FAILED! <===");
        }

        protected override void UpdateRestartNeeded()
        {
            Data.Log(Context.LocalizedMessages.UpdateRestartNeeded);
            Context.Logger.Info($"===> [{UpdateProcessName}] process INCOMPLETE: restart is needed! <===");
            ApplicationStarter.StartApplication(Path.Combine(Context.Settings.RootPath, Data.LauncherExecutableName), "");
            
            Data.Dispatcher.Invoke(Application.Quit);
        }

        private void StartGame()
        {
            var filePath = PathsManager.Combine(Context.Settings.GetGamePath(), Data.GameExecutableName);
            ApplicationStarter.StartApplication(filePath, $"{Context.Settings.LaunchArgumentParameter}={Context.Settings.LaunchArgumentValue}");
            Application.Quit();
        }

        public void GenerateDebugReport()
        {
            GenerateDebugReport("debug_report_launcher.txt");
        }
    }
}