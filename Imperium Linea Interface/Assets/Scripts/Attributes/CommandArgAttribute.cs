using System;

namespace Attributes
{
    /// <summary>
    ///     Marks a field or property on a command args struct as a positional
    ///     command-line argument. Used by <c>CommandArgumentParser</c> to map
    ///     raw string tokens to the correct struct members by index.
    /// </summary>
    /// <example>
    ///     <code>
    /// public struct MyArgs
    /// {
    ///     [CommandArg(0)]           public string RequiredName;
    ///     [CommandArg(1, "hello")]  public string OptionalGreeting;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CommandArgAttribute : Attribute
    {
        /// <summary>
        ///     The value assigned to this argument when it is omitted from input.
        ///     Only meaningful when <see cref="Optional" /> is true.
        /// </summary>
        public readonly object DefaultValue;

        /// <summary>
        ///     Zero-based position of this argument in the token array passed to
        ///     the command. Lower indices are parsed first.
        /// </summary>
        public readonly int Index;

        /// <summary>
        ///     True when a default value was supplied, meaning the argument may be
        ///     omitted without causing a parse error.
        /// </summary>
        public readonly bool Optional;

        /// <summary>
        ///     Marks this member as a required positional argument at
        ///     <paramref name="index" />. The parser will treat a missing token
        ///     at this position as an error.
        /// </summary>
        /// <param name="index">Zero-based position in the token array.</param>
        public CommandArgAttribute(int index)
        {
            Index = index;
            Optional = false;
            DefaultValue = null;
        }

        /// <summary>
        ///     Marks this member as an optional positional argument at
        ///     <paramref name="index" />. If the token is absent, the parser
        ///     assigns <paramref name="defaultValue" /> instead.
        /// </summary>
        /// <param name="index">Zero-based position in the token array.</param>
        /// <param name="defaultValue">
        ///     Fallback value used when the argument is not provided.
        ///     Must be assignable to the type of the decorated member.
        /// </param>
        public CommandArgAttribute(int index, object defaultValue)
        {
            Index = index;
            Optional = true;
            DefaultValue = defaultValue;
        }
    }
}