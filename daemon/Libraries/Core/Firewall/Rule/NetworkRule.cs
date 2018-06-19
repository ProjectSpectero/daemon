namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Structure that defined the type of rule that was created.
    /// This object serves no purpose other than to give reference information.
    /// </summary>
    public struct NetworkRule
    {
        public NetworkRuleType Type;
        public NetworkRuleProtocol Protocol;
        public string Network;
        public string Interface;
    }
}