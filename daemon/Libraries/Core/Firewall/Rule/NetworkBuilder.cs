using ServiceStack;

namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    public class NetworkBuilder
    {
        public static string BuildTemplate(string template, NetworkRule networkRule)
        {
            // Replace all data in the template.
            template = template.ReplaceAll("{type}", networkRule.Type.ToString().ToUpper());
            template = template.ReplaceAll("{network}", networkRule.Network);
            template = template.ReplaceAll("{interface}", networkRule.Interface);

            // Return the modified template to the user.
            return template;
        }

        
    }
}