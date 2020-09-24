using System;
using System.IO;
using System.Threading.Tasks;
using MHLab.Patch.Core.Client;
using MHLab.Patch.Core.Client.IO;
using MHLab.Patch.Core.IO;
using MHLab.Patch.Core.Utilities;
using MHLab.Patch.Launcher.Scripts.Localization;
using MHLab.Patch.Launcher.Scripts.Utilities;
using MHLab.Patch.Utilities;
using MHLab.Patch.Utilities.Serializing;
using UnityEngine;

namespace MHLab.Patch.Launcher.Scripts
{
    public abstract class LauncherBase : MonoBehaviour
    {
        public LauncherData Data;
        
        protected UpdatingContext Context;
        protected abstract string UpdateProcessName { get; }
        
        private ILauncherSettings CreateSettings()
        {
            var settings = new LauncherSettings();
            settings.RemoteUrl = Data.RemoteUrl;
            settings.PatchDownloadAttempts = 3;
            settings.AppDataPath = Application.persistentDataPath;
            
            OverrideSettings(settings);

            return settings;
        }

        protected abstract void OverrideSettings(ILauncherSettings settings);

        protected UpdatingContext CreateContext(ILauncherSettings settings)
        {
            var progress = new ProgressReporter();
            progress.ProgressChanged.AddListener(Data.UpdateProgressChanged);
            
            var context = new UpdatingContext(settings, progress);
            context.Logger = new MHLab.Patch.Utilities.Logging.Logger(settings.GetLogsFilePath());
            context.Serializer = new NewtonsoftSerializer();
            context.LocalizedMessages = new EnglishUpdaterLocalizedMessages();

            return context;
        }
        
        private void Initialize(ILauncherSettings settings)
        {
            Context = CreateContext(settings);
            
            Initialize(Context);
        }

        protected abstract void Initialize(UpdatingContext context);
        
        protected void GenerateDebugReport(string path)
        {
            var system = DebugHelper.GetSystemInfo();
            var report = Debugger.GenerateDebugReport(Context.Settings, system, new NewtonsoftSerializer());
            
            File.WriteAllText(path, report);
        }

        private void Awake()
        {
            Initialize(CreateSettings());
            Data.ResetComponents();
        }
        
        private void Start()
        {
            if (FilesManager.IsDirectoryWritable(Context.Settings.GetLogsDirectoryPath()))
            {
                StartUpdateProcess();
            }
            else
            {
                Data.Log(Context.LocalizedMessages.LogsFileNotWritable);
                Data.Dialog.ShowDialog(Context.LocalizedMessages.LogsFileNotWritable, Context.Settings.GetLogsFilePath(), 
                    Application.Quit,
                    StartUpdateProcess);
            }
        }

        private void StartUpdateProcess()
        {
            try
            {
                Context.Logger.Info($"===> [{UpdateProcessName}] process STARTED! <===");
                
                if (!NetworkChecker.IsNetworkAvailable())
                {
                    Data.Log(Context.LocalizedMessages.NotAvailableNetwork);
                    Data.Dialog.ShowCloseDialog(Context.LocalizedMessages.NotAvailableNetwork, string.Empty, Application.Quit);
                    Context.Logger.Error(null, $"[{UpdateProcessName}] process FAILED! Network is not available or connectivity is low/weak... Check your connection!");
                    return;
                }

                if (!NetworkChecker.IsRemoteServiceAvailable(Context.Settings.GetRemoteBuildsIndexUrl()))
                {
                    Data.Log(Context.LocalizedMessages.NotAvailableServers);
                    Data.Dialog.ShowCloseDialog(Context.LocalizedMessages.NotAvailableServers, string.Empty, Application.Quit);
                    Context.Logger.Error(null, $"[{UpdateProcessName}] process FAILED! Our servers are not responding... Wait some minutes and retry!");
                    return;
                }

                Context.Initialize();

                Task.Run(CheckForUpdates);
            }
            catch (Exception ex)
            {
                Data.Log(Context.LocalizedMessages.UpdateProcessFailed);
                Context.Logger.Error(ex, $"===> [{UpdateProcessName}] process FAILED! <===");
            }
        }
        
        private void CheckForUpdates()
        {
            UpdateStarted();

            try
            {
                Context.Update();

                UpdateCompleted();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                UpdateFailed(ex);
            }
            finally
            {
                Data.StopTimer();
            }
        }

        protected abstract void UpdateStarted();
        
        protected abstract void UpdateCompleted();
        
        protected abstract void UpdateFailed(Exception e);
        
        protected abstract void UpdateRestartNeeded();
        
        protected void EnsureExecutePrivileges(string filePath)
        {
            try
            {
                PrivilegesSetter.EnsureExecutePrivileges(filePath);
            }
            catch (Exception ex)
            {
                Context.Logger.Error(ex, "Unable to set executing privileges on {FilePath}.", filePath);
            }
        }
    }
}