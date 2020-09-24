using Serilog;
using System;
using System.IO;
using MHLab.Patch.Core.IO;
using ILogger = MHLab.Patch.Core.Logging.ILogger;

namespace MHLab.Patch.Utilities.Logging
{
    public sealed class Logger : ILogger
    {
        private readonly Serilog.Core.Logger _logger;
        
        public Logger(string logfilePath)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logfilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void Debug(string messageTemplate, params object[] parameters)
        {
            _logger.Debug(messageTemplate, parameters);
        }

        public void Info(string messageTemplate, params object[] parameters)
        {
            _logger.Information(messageTemplate, parameters);
        }

        public void Warning(string messageTemplate, params object[] parameters)
        {
            _logger.Warning(messageTemplate, parameters);
        }

        public void Error(Exception exception, string messageTemplate, params object[] parameters)
        {
            _logger.Error(messageTemplate, parameters);
        }
    }
}
