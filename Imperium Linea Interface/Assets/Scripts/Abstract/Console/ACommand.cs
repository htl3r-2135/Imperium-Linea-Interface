using Utility.Console;

namespace Abstract.Console
{
    /// <summary>
    ///     Contract for all console commands. Every command exposes a name (used
    ///     for lookup and dispatch), a short one-line description (for command
    ///     listings), and a long description (for individual help pages).
    /// </summary>
    internal interface ICommand
    {
        /// <summary>The token the user types to invoke this command (e.g. "help", "clear").</summary>
        string CommandName { get; }

        /// <summary>One-line summary shown in command listings.</summary>
        string ShortDescription { get; }

        /// <summary>Full help text shown when the user requests details for this command.</summary>
        string LongDescription { get; }

        /// <summary>
        ///     Executes the command with the given tokenised arguments.
        ///     The first element (args[0]) is typically the command name itself;
        ///     subclasses should document their expected argument layout.
        /// </summary>
        void Execute(string[] args);
    }

    /// <summary>
    ///     Non-generic base class for all commands.
    ///     Provides the shared metadata properties and forces subclasses to
    ///     implement <see cref="Execute(string[])" />.
    /// </summary>
    public abstract class ACommand : ICommand
    {
        /// <inheritdoc />
        public abstract string CommandName { get; }

        /// <inheritdoc />
        public abstract string ShortDescription { get; }

        /// <inheritdoc />
        public abstract string LongDescription { get; }

        /// <inheritdoc />
        public abstract void Execute(string[] args);
    }

    /// <summary>
    ///     Strongly-typed command base class that handles argument parsing
    ///     automatically via <see cref="CommandArgumentParser" />.
    ///     Subclasses only need to implement <see cref="Execute(TArgs)" /> with
    ///     a concrete args struct.
    /// </summary>
    /// <typeparam name="TArgs">
    ///     A struct whose fields are decorated with <c>[CommandArg]</c> attributes.
    ///     Use <see cref="NoArgs" /> for commands that accept no arguments.
    /// </typeparam>
    public abstract class ACommand<TArgs> : ACommand where TArgs : new()
    {
        /// <summary>
        ///     Entry point called by the command dispatcher with raw string tokens.
        ///     Handles two cases:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="NoArgs" /> — skips parsing and invokes the typed
        ///                 overload with a default instance.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Any other <typeparamref name="TArgs" /> — delegates to
        ///                 <see cref="CommandArgumentParser.Parse{T}" /> then invokes the
        ///                 typed overload.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        public override void Execute(string[] args)
        {
            // Commands that take no arguments bypass the parser entirely.
            if (typeof(TArgs) == typeof(NoArgs))
            {
                Execute((TArgs)(object)new NoArgs());
                return;
            }

            var parsed = CommandArgumentParser.Parse<TArgs>(args);
            Execute(parsed);
        }

        /// <summary>
        ///     Typed execution method. Subclasses implement their logic here;
        ///     all argument parsing has already been handled by the base class.
        /// </summary>
        protected abstract void Execute(TArgs args);
    }

    /// <summary>
    ///     Sentinel type used as the <c>TArgs</c> type parameter for commands
    ///     that accept no arguments. Allows the generic pipeline to remain uniform
    ///     while avoiding unnecessary parser invocations.
    /// </summary>
    public struct NoArgs
    {
    }
}