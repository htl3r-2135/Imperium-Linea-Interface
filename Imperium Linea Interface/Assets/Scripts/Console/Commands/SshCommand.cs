using Abstract.Console;
using Attributes;

namespace Console.Commands
{
    /// <summary>
    ///     Arguments for the ssh command.
    /// </summary>
    public class SshArgs
    {
        [CommandArg(1)] public string Host;
        [CommandArg(2)] public string Password;

        /// <summary>
        ///     User, Host and Password. (Do nothing, as command is just used for help).
        /// </summary>
        [CommandArg(0)] public string User;
    }

    [ConsoleCommand]
    public class SshCommand : ACommand<SshArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "ssh";

        /// <inheritdoc />
        public override string ShortDescription => "Connects to a given ssh Server.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Connects to a given ssh Server.\n\n" +
            "Usage: ssh [user]@[host] [password]\n\n" +
            "Arguments:\n" +
            "  user   Username to connect on the host." +
            "  host   IP-Address of the SSH Server to connect to." +
            "  password  Password for the user.";

        /// <summary>
        ///     Does nothing, as Execution is handled in Screen.cs
        /// </summary>
        protected override void Execute(SshArgs args)
        {
        }
    }
}