namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Structure that defined the type of rule that was created.
    /// This object serves no purpose other than to give reference information.
    /// </summary>
    public struct NetworkRule
    {
        // The type of the rule.
        public NetworkRuleType Type;
        
        // The type of protocol the rule should use.
        public NetworkRuleProtocol Protocol;
        
        // The network that the rule should use, can be  stylized as 1.1.1.1 or 1.1.1.1-2.2.2.2.
        public string Network;
        
        // Reference to the interface name, not the type.
        public string Interface;
    }
}