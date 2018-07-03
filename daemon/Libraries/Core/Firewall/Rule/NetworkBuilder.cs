using ServiceStack;
using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Network Builder
    ///
    /// This class just serves the purpose to keep classes shorter with quick functions that can be called to do iterable-like objects.
    /// All functions are intended to be static.
    /// </summary>
    public class NetworkBuilder
    {
        /// <summary>
        /// Compile a template using the provided network rule.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="networkRule"></param>
        /// <param name="interfaceInformation"></param>
        /// <returns></returns>
        public static string BuildTemplate(string template, NetworkRule networkRule, InterfaceInformation interfaceInformation)
        {
            // Replace all data in the template.
            template = template.ReplaceAll("{type}", networkRule.Type.ToString().ToUpper());
            template = template.ReplaceAll("{network}", networkRule.Network);
            template = template.ReplaceAll("{interface-address}", interfaceInformation.Address);
            template = template.ReplaceAll("{interface-name}", interfaceInformation.Name);
            template = template.ReplaceAll("{protocol}", networkRule.Protocol.ToString().ToLower());

            // Return the modified template to the user.
            return template;
        }

        /// <summary>
        /// Build a BOG-Standard instance of ProcessOptions to save space.
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static ProcessOptions BuildProcessOptions(string executable, bool root)
        {
            return new ProcessOptions()
            {
                Executable = executable,
                InvokeAsSuperuser = root
            };
        }
    }
}