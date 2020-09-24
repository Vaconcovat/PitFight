using System;
using System.IO;
using MHLab.Patch.Core.Client;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Launcher.Scripts.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MHLab.Patch.Launcher.Scripts
{
    public sealed class LauncherUpdater : LauncherBase
    {
        public int SceneToLoad;
        
        private PatcherUpdater _patcherUpdater;
        
        protected override void Initialize(UpdatingContext context)
        {
            _patcherUpdater = new PatcherUpdater(context);
            _patcherUpdater.Downloader.DownloadComplete += Data.DownloadComplete;
            _patcherUpdater.Downloader.ProgressChanged += Data.DownloadProgressChanged;
            
            context.RegisterUpdateStep(_patcherUpdater);

            context.Runner.PerformedStep += (sender, updater) =>
            {
                if (context.IsDirty(out var reasons))
                {
                    context.Logger.Debug("Context is set to dirty: updater restart required. The files {DirtyFiles} have been replaced.", reasons);
                    UpdateRestartNeeded();
                }
            };
        }

        protected override string UpdateProcessName => "Launcher updating";

        protected override void OverrideSettings(ILauncherSettings settings)
        {
            string rootPath = string.Empty;
            
#if UNITY_EDITOR
            rootPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), LauncherData.WorkspaceFolderName, "TestLauncher");
            Directory.CreateDirectory(rootPath);
#elif UNITY_STANDALONE_WIN
            rootPath = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
#elif UNITY_STANDALONE_LINUX
            rootPath = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
#elif UNITY_STANDALONE_OSX
            rootPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName).FullName;
#endif
            
            settings.RootPath = rootPath;
        }
        
        protected override void UpdateStarted()
        {
            Data.StartTimer();
        }
        
        protected override void UpdateCompleted()
        {
            Data.Dispatcher.Invoke(() =>
            {
                Data.ProgressBar.Progress = 1;
                Data.ProgressPercentage.text = "100%";
            });

            var repairer = new Repairer(Context);
            var updater = new Updater(Context);

            if (repairer.IsRepairNeeded() || updater.IsUpdateAvailable())
            {
                UpdateRestartNeeded();
                return;
            }
            
            Data.Log(Context.LocalizedMessages.UpdateProcessCompleted);
            Context.Logger.Info($"===> [{UpdateProcessName}] process COMPLETED! <===");
            StartGameScene();
        }

        private void StartGameScene()
        {
            Data.Dispatcher.Invoke(() => 
            {
                SceneManager.LoadScene(SceneToLoad);
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
            
            EnsureExecutePrivileges(PathsManager.Combine(Context.Settings.RootPath, Data.LauncherExecutableName));
            
            ApplicationStarter.StartApplication(Path.Combine(Context.Settings.RootPath, Data.LauncherExecutableName), "");
            
            Data.Dispatcher.Invoke(Application.Quit);
        }

        public void GenerateDebugReport()
        {
            GenerateDebugReport("debug_report_pregame.txt");
        }
    }
}