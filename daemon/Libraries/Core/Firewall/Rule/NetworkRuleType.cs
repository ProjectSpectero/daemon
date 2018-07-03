namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Enumerator that defines a set of trackable types.
    /// </summary>
    public enum NetworkRuleType
    {
        Masquerade, // MASQUERADE
        SourceNetworkAddressTranslation // SNAT
    }
}