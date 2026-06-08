using System;

namespace Attributes
{
    /// <summary>
    ///     Marks a class as a console command that should be auto-discovered and
    ///     registered by <c>CommandCollector.CollectCommands</c> at startup.
    ///     The decorated class must extend <c>ACommand</c> and have a public
    ///     parameterless constructor so it can be instantiated via reflection.
    /// </summary>
    /// <example>
    ///     <code>
    /// [ConsoleCommand]
    /// public class ClearCommand : ACommand&lt;NoArgs&gt;
    /// {
    ///     public override string CommandName => "clear";
    ///     // ...
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConsoleCommandAttribute : Attribute
    {
    }
}