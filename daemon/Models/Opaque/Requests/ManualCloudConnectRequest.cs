namespace Spectero.daemon.Models.Opaque.Requests
{
    public class ManualCloudConnectRequest
    {
        public bool force { get; set; }
        public long NodeId { get; set; }
        public string NodeKey { get; set; }

    }
}