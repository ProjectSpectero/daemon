using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public enum Commands
    {
        [Command(typeof(ScopedAuthentication), Description = "Service specific authentication helper, meant for invocation by 3rd party binaries.")]
        auth,

        [Command(typeof(ConnectToSpecteroCloud), Description = "Automagic connection to the Spectero Cloud")]
        connect,

        [Command(typeof(DisconnectFromSpecteroCloud), Description = "Disconnect from the Spectero Cloud (client only)")]
        disconnect,

        [Command(typeof(SetGlobalProperty), Description = "Sets various system properties, consult the docs for details.")]
        env,

        [Command(typeof(GetSystemHeartbeat), Description = "Check if the Daemon is online and connectible")]
        heartbeat,

        [Command(typeof(ManuallyConnectToSpecteroCloud), Description = "Manually Connect Daemon to Spectero Cloud")]
        manual,

        [Command(typeof(ViewCloudConnectivityStatus), Description = "See the current state of connectivity to the Spectero Cloud")]
        status,

        [Command(typeof(Shell), Description = "Invokes the Spectero Shell")]
        shell,

        [Command(typeof(Version), Description = "Shows the Spectero Installer and Linked Daemon Versions")]
        version
    }
}