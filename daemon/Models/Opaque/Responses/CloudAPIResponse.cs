using System;
using System.Collections.Generic;

namespace Spectero.daemon.Models.Responses
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