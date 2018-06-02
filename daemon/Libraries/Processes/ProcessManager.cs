using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Processes
{
    public class ProcessManager
    {
        private string _initializer;
        private readonly ILogger<object> _logger;
        private List<Command> _listOfCommands = new List<Command>();

        /// <summary>
        /// Class Constructor with dependency injection.
        /// </summary>
        /// <param name="initializer"></param>
        /// <param name="logger"></param>
        public ProcessManager(string initializer, ILogger<object> logger)
        {
            // Inherit dependency injection.
            _initializer = initializer;
            _logger = logger;

            
        }

        


        /// <summary>
        /// Set the reference name of the reason why this ProcessManager was created.
        /// </summary>
        /// <param name="initializer"></param>
        public void SetInitializer(string initializer)
        {
            _initializer = initializer;
            _logger.LogInformation("Initializer has changed to " + initializer);
        }

        /// <summary>
        /// Get the reference name of the reason why the ProcessManager was created.
        /// </summary>
        /// <returns></returns>
        public string GetInitializer() => _initializer;
    }
}