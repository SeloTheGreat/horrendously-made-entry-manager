using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ConsoleRender;
using Commands;
using System.Threading.Tasks;

namespace entry_manager
{
    internal class Program
    {
        static CommandHandler CmdHandler = new CommandHandler();

        static void Main(string[] args)
        {
            SaveFile.Initialize();

            Console.Title = "Entry Manager";

            WindowsConsole.WC_AddOutputConsoleMode(WindowsOutputConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING);

            ListViewer viewer = new ListViewer(
                new List<string>(new string[]
                {
                    "L0",
                    "L1",
                    "L2",
                    /*"L3",
                    "L4",
                    "L5",
                    "L6",
                    "L7",
                    "L8",
                    "L9",
                    "L10",
                    "L11",
                    "L12",
                    "L13",
                    "L14",
                    "L15",*/
                })
                );

            viewer.OnCommandTriggered = Viewer_CommandTriggered;

            viewer.StartUpdateLoop();
        }

        private static void Viewer_CommandTriggered(ListViewer sender, string e)
        {
            if (string.IsNullOrWhiteSpace(e))
            {
                sender.ErrorToLogInterface("Command was empty!");
                return;
            }

            e = e.Replace("\\\"", "`");

            List<CommandArgument> positionalArguments = new List<CommandArgument>();
            Dictionary<string, CommandArgument> namedArguments = new Dictionary<string, CommandArgument>();

            CommandItem cmd;

            {
                //old regex was /=|\/?\w+|"".*?""/ which also captured the / parts of the arguments
                MatchCollection matchedArgs = Regex.Matches(e, @"=|[A-Za-z0-9_\-]+|[""'].*?[""']");

                {
                    Match item = matchedArgs[0];
                    bool got = CmdHandler.All.TryGetValue(item.Value.ToLower(), out cmd);
                    if (!got)
                    {
                        sender.ErrorToLogInterface("Invalid command. Got " + item.Value);
                        return;
                    }
                }

                for (int i = 1; i < matchedArgs.Count; i++)
                {
                    Match item = matchedArgs[i];

                    bool initOptionalRule = false;
                    CommandArgumentRule optionalRule = default(CommandArgumentRule);
                    if (cmd.OptionalArguments != null)
                    {
                        foreach (CommandArgumentRule argRule in cmd.OptionalArguments)
                            if (argRule.ArgName == item.Value) { optionalRule = argRule; initOptionalRule = true; break; }
                    }

                    if (i < matchedArgs.Count - 2 && matchedArgs[i + 1].Value == "=")
                    {
                        if (cmd.OptionalArguments == null)
                        {
                            sender.ErrorToLogInterface("There are no named arguments for cmdlet " + cmd.Name);
                            return;
                        }

                        namedArguments.Add(item.Value, new CommandArgument
                        {
                            ArgName = item.Value.Trim('"', '\''),
                            Value = matchedArgs[i + 2].Value.Trim('"', '\''),
                            Rule = optionalRule,
                        });
                        i += 2;
                    }
                    else if (initOptionalRule)
                    {
                        string trimmed = item.Value.Trim('"', '\'');
                        namedArguments.Add(item.Value, new CommandArgument
                        {
                            ArgName = trimmed,
                            Value = trimmed,
                            Rule = optionalRule,
                        });
                    }
                    else
                    {
                        if (cmd.PositionalArguments == null || positionalArguments.Count >= cmd.PositionalArguments.Length)
                        {
                            sender.ErrorToLogInterface("Too many positional arguments found for cmdlet " + cmd.Name);
                            return;
                        }
                        CommandArgumentRule rule = cmd.PositionalArguments[positionalArguments.Count];
                        positionalArguments.Add(new CommandArgument
                        {
                            ArgName = rule.ArgName,
                            Value = item.Value.Trim('"', '\''),
                            Rule = rule
                        });

                        initOptionalRule = true;
                    }

                    if (!initOptionalRule)
                    {
                        sender.ErrorToLogInterface("No valid rule found for argument " + item.Value);
                        return;
                    }
                }
            }

            if (cmd.RestartsTaskAfterCompletion)
                sender.Close();

            string errorStr = cmd.OnExecuted?.Invoke(sender, positionalArguments, namedArguments);

            if (errorStr == "EXIT_P")
            {
                sender.FullClose();
                return;
            }

            if (!string.IsNullOrEmpty(errorStr))
            {
                sender.ErrorToLogInterface(errorStr);
            }
            else
            {
                if (ListViewerSettings.loud)
                    Task.Run(sender.BeepHigh);

                if (cmd.YieldsUntilInput)
                    Console.ReadKey(true);
                //sender.PrintToLogInterface($"Executed command '{cmd.Name}'");

                if (ListViewerSettings.loud)
                    Task.Run(sender.BeepLow);
            }

            if (cmd.RestartsTaskAfterCompletion)
                sender.StartUpdateLoop();
        }
    }
}
