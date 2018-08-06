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
using System.Collections.Generic;

namespace Spectero.daemon.Models.Opaque.Responses
{
    public class CloudAPIResponse<T>
    {
        public List<string> errors;
        public T result;
        public string message;
        public string version;
    }

    public class Node
    {
        public long id;
        public string ip;
        public string protocol;
        public string status;
        public string created_at;
        public string user_id;
        public string market_model;
    }

    public class Engagement
    {
        public long engagement_id;
        public string username;
        public string password;
        public string sync_timestamp;
        public string cert;
        public string cert_key;
    }
}