using System;
using System.Linq;
using System.Reflection;
using Attributes;

namespace Utility.Console
{
    public static class CommandArgumentParser
    {
        public static T Parse<T>(string[] args) where T : new()
        {
            GameLogger.Instance.LogDebug(
                $"Parsing args for {typeof(T).Name}: [{string.Join(", ", args)}]",
                "Parser"
            );

            object instance = new T();

            var members = typeof(T)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<CommandArgAttribute>() != null);

            foreach (var member in members)
            {
                var attr = member.GetCustomAttribute<CommandArgAttribute>();
                object value;

                try
                {
                    if (attr.Index < args.Length)
                    {
                        var raw = args[attr.Index];

                        GameLogger.Instance.LogDebug(
                            $"Arg[{attr.Index}] -> {member.Name} = \"{raw}\"",
                            "Parser"
                        );

                        value = ConvertTo(raw, GetMemberType(member));
                    }
                    else if (attr.Optional)
                    {
                        GameLogger.Instance.LogDebug(
                            $"Arg[{attr.Index}] missing, using default for {member.Name}: {attr.DefaultValue}",
                            "Parser"
                        );

                        value = attr.DefaultValue;
                    }
                    else
                    {
                        GameLogger.Instance.LogError(
                            $"Missing required argument at index {attr.Index} for {typeof(T).Name}",
                            "Parser"
                        );

                        throw new Exception($"Missing required argument at index {attr.Index}");
                    }

                    SetValue(member, instance, value);
                }
                catch (Exception ex)
                {
                    GameLogger.Instance.LogError(
                        $"Failed parsing argument '{member.Name}': {ex.Message}",
                        "Parser"
                    );

                    throw;
                }
            }

            return (T)instance;
        }

        private static Type GetMemberType(MemberInfo m)
        {
            return m switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => throw new Exception("Unsupported member")
            };
        }

        private static void SetValue(MemberInfo m, object obj, object value)
        {
            GameLogger.Instance.LogDebug(
                $"Setting {m.Name} = {value}",
                "Parser"
            );

            switch (m)
            {
                case FieldInfo f: f.SetValue(obj, value); break;
                case PropertyInfo p: p.SetValue(obj, value); break;
            }
        }

        private static object ConvertTo(string input, Type type)
        {
            try
            {
                object result;

                if (type == typeof(int))
                {
                    result = int.Parse(input);
                }
                else if (type == typeof(float))
                {
                    result = float.Parse(input);
                }
                else if (type == typeof(bool))
                {
                    result = bool.Parse(input);
                }
                else if (type == typeof(string))
                {
                    result = input;
                }
                else
                {
                    GameLogger.Instance.LogError(
                        $"Unsupported argument type: {type.Name}",
                        "Parser"
                    );

                    throw new Exception($"Unsupported argument type {type.Name}");
                }

                GameLogger.Instance.LogDebug(
                    $"Converted \"{input}\" -> {result} ({type.Name})",
                    "Parser"
                );

                return result;
            }
            catch (Exception ex)
            {
                GameLogger.Instance.LogError(
                    $"Conversion failed: \"{input}\" -> {type.Name} ({ex.Message})",
                    "Parser"
                );

                throw;
            }
        }
    }
}