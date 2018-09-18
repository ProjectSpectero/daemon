/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Models.Opaque
{
    public class TaskDescriptor : OpaqueBase, IProcessTrackable
    {
        // Unique identifier for this task.
        public string Id { get; set; }
        
        // The exact type of task that's being created
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskType Type { get; set; }
        
        // This one tracks the task's own status
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskStatus Status { get; set; }
        
        // If the task has an underlying process (3rd party), this one tracks that one's status.
        // This is also used to synchronize with IProcessRunner.
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceState ProcessStatus { get; set; }
        
        // The payload in string form, this will be selectively parsed to get what we need out of it.
        public TaskPayload Payload { get; set; }
        
        // If this is a task that requires managing an external process
        public CommandHolder Command { get; set; }
        
        public ServiceState GetState() => ProcessStatus;
    }
    
    public enum TaskType
    {
        // ReSharper disable once InconsistentNaming
        ConnectToOpenVPNServer,
        SetAsSystemProxy
    }
    
    public enum TaskStatus
    {
        Pending,
        Running,
        Finished
    }
}