namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// For each type of firewall, please define it in here for organization's sake.
    /// </summary>
    public class NetworkRuleTemplates
    {
        public const string SNAT = "-t nat POSTROUTING -p TCP -o {interface} -J SNAT --to {address}";
        public const string MASQUERADE = "POSTROUTING -S {network} -o {interface} -J MASQUERADE";
    }
}