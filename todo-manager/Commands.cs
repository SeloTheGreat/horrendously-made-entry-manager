using System;
using System.Collections.Generic;
using System.Linq;

namespace Commands
{
    struct CommandArgument
    {
        public string ArgName;
        public string Value;
        public CommandArgumentRule Rule;
    }

    struct CommandArgumentRule
    {
        public bool Required;
        public string NamedValue;
        public string ArgName;
        public string Description;
    }
    
    struct CommandItem
    {
        public string Name;
        public string Description;
        public bool YieldsUntilInput;
        public bool RestartsTaskAfterCompletion;
        public Func<ConsoleRender.ListViewer, List<CommandArgument>, Dictionary<string, CommandArgument>, string> OnExecuted;
        public CommandArgumentRule[] OptionalArguments;
        public CommandArgumentRule[] PositionalArguments;
    }

    internal class CommandHandler
    {
        Func<char, bool> newLineCountPredicate = (c) =>
        {
            return c == '\n';
        };

        public Dictionary<string, CommandItem> All;

        bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            s = s.Trim().ToLower();
            if (s == "true" || s == "t" || s == "1" || s == "on")
                return true;
            else if (s == "false" || s == "f" || s == "0" || s == "off")
                return false;
            return false;
        }

        int CountNewlines(string s)
        {
            return s.Count(newLineCountPredicate);
        }

        public CommandHandler()
        {
            All = new Dictionary<string, CommandItem>();

            {
                CommandItem i = new CommandItem
                {
                    Name = "help",
                    Description = "Displays help information for other commands, or opens a general help panel if no arguments are given",
                    YieldsUntilInput = true,
                    RestartsTaskAfterCompletion = true,
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { ArgName = "cmdlet", }
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count > 0)
                        {
                            CommandArgument arg = positional[0];

                            bool success = All.TryGetValue(arg.Value, out CommandItem cmdlet);
                            if (!success)
                                return $"Invalid cmdlet name '{arg.Value}' for help command";

                            string descriptor = $"Cmdlet:\n\t{cmdlet.Name}\nUsage:\n\t{cmdlet.Name} ";
                            string positionalArgsDescriptor = "Positional Arguments:\n\t";
                            string namedArgsDescriptor = "Named Arguments:\n\t";

                            if (cmdlet.PositionalArguments != null)
                                foreach (CommandArgumentRule rule in cmdlet.PositionalArguments)
                                {
                                    descriptor += $"<{rule.ArgName}"
                                        + (rule.NamedValue != null ? $" = {rule.NamedValue}>" : ">")
                                        + (rule.Required ? " " : "? ");
                                    positionalArgsDescriptor += $"* {rule.ArgName} [{(rule.Required ? "required" : "optional")}] {(rule.NamedValue == null ? "" : $"[type = {rule.NamedValue}]")}\n\t\t{rule.Description}\n\t";
                                }
                            if (cmdlet.OptionalArguments != null)
                                foreach (CommandArgumentRule rule in cmdlet.OptionalArguments)
                                {
                                    descriptor += $"[{rule.ArgName}"
                                        + (rule.NamedValue == null ? "]" : $" = {rule.NamedValue}]")
                                        + (rule.Required ? " " : "? ");
                                    namedArgsDescriptor += $"* {rule.ArgName} [{(rule.Required ? "required" : "optional")}] {(rule.NamedValue == null ? "" : $"[type = {rule.NamedValue}]")}\n\t\t{rule.Description}\n\t";
                                }

                            string printable = $"{descriptor}\nDescription:\n\t{cmdlet.Description}\n\n{(cmdlet.PositionalArguments != null ? positionalArgsDescriptor + "\n" : "")}{(cmdlet.OptionalArguments != null ? namedArgsDescriptor + "\n" : "")}";

                            int bufferHeight = CountNewlines(printable) + 1;
                            Console.WindowHeight = Math.Min(bufferHeight, Console.LargestWindowHeight);
                            Console.BufferHeight = bufferHeight;

                            Console.Write(printable);
                        }
                        else
                        {
                            string cmdlets = "";
                            foreach (KeyValuePair<string, CommandItem> pair in All)
                            {
                                CommandItem v = pair.Value;
                                cmdlets += $"* {v.Name}\n\t{v.Description}\n";
                            }

                            string printable = $@"--- Navigation ---

* UP ARROW: Selects the element above, if there is one
* DOWN ARROW: Selects the element below, if there is one

* PAGE UP: Selects the element {ConsoleRender.ListViewerSettings.pageupjmp} elements above, or until there are no elements above left
* PAGE DOWN: Selects the element {ConsoleRender.ListViewerSettings.pagedownjmp} elements below, or until there are no elements below left

* HOME: Selects the first element
* END: Selects the last element

* NUMBER KEYS: Pressing a number searches for an entry with the according index.
If any other non-number key is pressed, then the selection will try to travel to the current search number.

--- COMMANDS ---
Here is a list off all avaliable cmdlets, to get more info on them, type in 'help <cmdlet>' (cmdlet being the command name)

{cmdlets}";
                            int bufferHeight = CountNewlines(printable) + 1;
                            Console.WindowHeight = Math.Min(bufferHeight, Console.LargestWindowHeight);
                            Console.BufferHeight = bufferHeight;

                            Console.Write(printable);
                        }
                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "conf",
                    Description = @"Configure or view settings",
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { Required = true, ArgName = "setting-name", Description = @"The name of the setting.
                * Settings list:
                    loud <BOOLEAN> DEFAULT=[false]: Enables or disables sound feedback from interactions
                    page-down-jmp <UNSIGNED_INT> DEFAULT=[5] : The amount of rows you will jump downwards when the Page Down key is pressed
                    page-up-jmp <UNSIGNED_INT> DEFAULT=[5]: The amount of rows you will jump upwards when the Page Down key is pressed" },
                        new CommandArgumentRule { ArgName = "value", Description = @"The value of the setting, leave blank to log the current setting value.
                * Interpreting Values:
                    * to set a setting to it's default value, the value must be 'def'
                    BOOLEAN: ['t' 'true' '1' 'on'] MEANS true, ['f' 'false' '0' 'off'] MEANS false
                    UNSIGNED_INT: [x] x IS A number, AND x > 0" }
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count < 1)
                            return "Not enough arguments for conf command";

                        string name = positional[0].Value;

                        string v = "";
                        if (positional.Count > 1)
                            v = positional[1].Value;

                        /*if (!viewer.Settings.Settings.ContainsKey(name))
                            return "Invalid setting name, type in 'help conf' to see all avaliable settings to configure";

                        dynamic newVal = viewer.Settings[name];
                        Type t = newVal.GetType();

                        if (v.ToLowerInvariant() != "def")
                        {

                        }
                        if (t == typeof(bool))
                            newVal = ParseBool(v);
                        else if (t == typeof(uint))
                        {
                            bool success = uint.TryParse(v, out uint result);
                        }*/

                        if (name == "loud")
                        {
                            if (v != string.Empty)
                                ConsoleRender.ListViewerSettings.loud = ParseBool(v);
                            viewer.PrintToLogInterface($"'loud' setting value = {ConsoleRender.ListViewerSettings.loud.ToString()}");

                            SaveFile.SaveData["SET_loud"] = ConsoleRender.ListViewerSettings.loud.ToString();
                        }
                        else if (name == "page-down-jmp")
                        {
                            bool success = uint.TryParse(v, out uint result);
                            if (!success && v != string.Empty)
                                result = 5;

                            if (v != string.Empty)
                                ConsoleRender.ListViewerSettings.pagedownjmp = result;
                            viewer.PrintToLogInterface($"'page-down-jmp' setting value = {ConsoleRender.ListViewerSettings.pagedownjmp.ToString()}");

                            SaveFile.SaveData["SET_page-down-jmp"] = ConsoleRender.ListViewerSettings.pagedownjmp.ToString();
                        }
                        else if (name == "page-up-jmp")
                        {
                            bool success = uint.TryParse(v, out uint result);
                            if (!success && v != string.Empty)
                                result = 5;

                            if (v != string.Empty)
                                ConsoleRender.ListViewerSettings.pageupjmp = result;
                            viewer.PrintToLogInterface($"'page-up-jmp' setting value = {ConsoleRender.ListViewerSettings.pageupjmp.ToString()}");

                            SaveFile.SaveData["SET_page-up-jmp"] = ConsoleRender.ListViewerSettings.pageupjmp.ToString();
                        }
                        else
                            return "Invalid setting name, type in 'help conf' to see all avaliable settings to configure";

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "add",
                    Description = @"Adds an entry to the current list of entries.",
                    RestartsTaskAfterCompletion = true,
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { Required = true, ArgName = "entry-name", Description = "The name of the entry" },
                        new CommandArgumentRule { ArgName = "entry-index", NamedValue = "UNSIGNED_INT", Description = "The index to shove the entry to, leave blank to add it to the end of the list" }
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count < 1)
                            return "Not enough arguments for 'add' command";

                        CommandArgument name = positional[0];

                        if (positional.Count > 1)
                        {
                            bool parsesuccess = uint.TryParse(positional[1].Value, out uint index);
                            if (!parsesuccess)
                                return "Invalid number format for 'entry-index' argument";
                            if (index >= viewer.ViewingList.Count)
                                return "Number index given for 'entry-index' was too big";
                            viewer.ViewingList.Insert((int)index, name.Value);
                        }
                        else
                            viewer.ViewingList.Add(name.Value);

                        viewer.PrintToLogInterface($"Added entry '{name.Value}'");

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "rem",
                    Description = @"Removes an entry from the current list of entries.",
                    RestartsTaskAfterCompletion = true,
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { ArgName = "entry-identifier", Description = "The identifier of the entry, if left blank, the last entry is removed" }
                    },
                    OptionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { ArgName = "n", Description = "Interprets the entry-identifier as the name of the entry instead of the index of the entry" }
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (viewer.ViewingList.Count == 0)
                            return "There are no elements to remove";

                        string removedEntry;
                        int index;

                        if (positional.Count < 1)
                        {
                            index = viewer.ViewingList.Count - 1;
                            removedEntry = viewer.ViewingList[index];
                            viewer.ViewingList.RemoveAt(index);

                            viewer.PrintToLogInterface($"Removed entry '{removedEntry}'");
                            return "";
                        }

                        CommandArgument identifier = positional[0];

                        if (named.ContainsKey("n"))
                        {
                            index = viewer.ViewingList.IndexOf(identifier.Value);
                            if (index < 0)
                                return $"Entry of name {identifier.Value} couldn't be found";

                            removedEntry = viewer.ViewingList[index];
                            viewer.ViewingList.RemoveAt(index);
                        }
                        else
                        {
                            bool parseSuccess = uint.TryParse(identifier.Value, out uint result);
                            if (!parseSuccess)
                                return "Invalid number format for the first positional 'entry-identifier' argument";
                            if (result >= viewer.ViewingList.Count)
                                return "Number index given for 'entry-identifier' was too big";
                            index = (int)result;
                            removedEntry = viewer.ViewingList[index];
                            viewer.ViewingList.RemoveAt(index);
                        }

                        viewer.PrintToLogInterface($"Removed entry '{removedEntry}'");

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "clone",
                    Description = @"Clones an entry from the current list of entries.",
                    RestartsTaskAfterCompletion = true,
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { Required = true, ArgName = "entry-identifier", Description = "The identifier of the entry" },
                    },
                    OptionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { NamedValue = "UNSIGNED_INT", ArgName = "to", Description = "The index to shove the entry into, leave blank to add it to the end of the list" },
                        new CommandArgumentRule { ArgName = "n", Description = "Interprets the entry-identifier as the name of the entry instead of the index of the entry" },
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count < 1)
                            return "Not enough arguments for 'clone' command";

                        CommandArgument identifier = positional[0];

                        string entry;
                        if (named.ContainsKey("n"))
                        {
                            bool contains = viewer.ViewingList.Contains(identifier.Value);
                            if (!contains)
                                return $"An entry with the name of {identifier.Value} couldn't be found";
                            entry = identifier.Value;
                        }
                        else
                        {
                            bool parsesuccess = uint.TryParse(identifier.Value, out uint result);
                            if (!parsesuccess)
                                return "Invalid number format for 'to' argument";
                            if (result >= viewer.ViewingList.Count)
                                return "Number index given for 'to' was too big";
                            entry = viewer.ViewingList[(int)result];
                        }

                        int index = -1;
                        if (named.TryGetValue("to", out CommandArgument toIndex))
                        {
                            bool parsesuccess = uint.TryParse(toIndex.Value, out uint result);
                            if (!parsesuccess)
                                return "Invalid number format for 'to' argument";
                            if (result >= viewer.ViewingList.Count)
                                return "Number index given for 'to' was too big";
                            index = (int)result;
                        }

                        if (index >= 0)
                            viewer.ViewingList.Insert(index, entry);
                        else
                            viewer.ViewingList.Add(entry);

                        viewer.PrintToLogInterface($"Cloned entry '{entry}'");

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "rev",
                    Description = @"Reverses the current list of entries.",
                    RestartsTaskAfterCompletion = true,
                    OptionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { NamedValue = "UNSIGNED_INT", ArgName = "from", Description = "The index to reverse the entries from, leave blank to reverse from the 0th index" },
                        new CommandArgumentRule { NamedValue = "UNSIGNED_INT", ArgName = "count", Description = "The amount of entries to reverse, leave blank to reverse the rest of the list" },
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        bool arg1Success = named.TryGetValue("p", out CommandArgument argFrom);
                        bool arg2Success = named.TryGetValue("s", out CommandArgument argSize);

                        int from = 0;
                        if (arg1Success)
                        {
                            bool uintSuccess = uint.TryParse(argFrom.Value, out uint result);
                            if (uintSuccess)
                                from = (int)result;
                            else
                                return "Invalid number format for the 'p' argument";
                        }

                        int count = viewer.ViewingList.Count - from;
                        if (arg2Success)
                        {
                            bool uintSuccess = uint.TryParse(argSize.Value, out uint result);
                            if (uintSuccess)
                                count = (int)result;
                            else
                                return "Invalid number format for the 's' argument";
                            if (count > viewer.ViewingList.Count - from)
                                return "Reverse amount number for the 's' argument was too big";
                        }

                        viewer.ViewingList.Reverse(from, count);

                        viewer.PrintToLogInterface($"Reversed entry list from {from} to {from + count - 1}");

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "swap",
                    Description = @"Swaps the position of 2 entries from the current list of entries.",
                    RestartsTaskAfterCompletion = true,
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { Required = true, ArgName = "entry-identifier-1", Description = "The identifier of the first entry to swap with the second entry" },
                        new CommandArgumentRule { Required = true, ArgName = "entry-identifier-2", Description = "The identifier of the second entry to swap with the first entry" },
                    },
                    OptionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { ArgName = "n", Description = "Interprets the entry identifiers as the name of the entries instead of the index of the entries" },
                    },

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count < 2)
                            return "Not enough arguments for 'swap' command";

                        string identity1 = positional[0].Value;
                        string identity2 = positional[1].Value;

                        if (named.ContainsKey("n"))
                        {
                            int index1 = viewer.ViewingList.IndexOf(identity1);
                            if (index1 < 0)
                                return $"Entry of name {identity1} couldn't be found";
                            int index2 = viewer.ViewingList.IndexOf(identity2);
                            if (index2 < 0)
                                return $"Entry of name {identity2} couldn't be found";
                            string temp = viewer.ViewingList[index1];
                            viewer.ViewingList[index1] = viewer.ViewingList[index2];
                            viewer.ViewingList[index2] = temp;
                        }
                        else
                        {
                            bool parseSuccess1 = uint.TryParse(identity1, out uint result1);
                            if (!parseSuccess1)
                                return "Invalid number format for 'entry-identifier-1' argument";
                            if (result1 >= viewer.ViewingList.Count)
                                return "Number index given for 'entry-identifier-1' was too big";
                            int index1 = (int)result1;
                            bool parseSuccess2 = uint.TryParse(identity2, out uint result2);
                            if (!parseSuccess2)
                                return "Invalid number format for 'entry-identifier-2' argument";
                            if (result2 >= viewer.ViewingList.Count)
                                return "Number index given for 'entry-identifier-2' was too big";
                            int index2 = (int)result2;
                            string temp = viewer.ViewingList[index1];
                            viewer.ViewingList[index1] = viewer.ViewingList[index2];
                            viewer.ViewingList[index2] = temp;
                        }

                        viewer.PrintToLogInterface($"Swapped entries '{identity1}' and '{identity2}'");

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "mode",
                    Description = @"Sets the list viewer mode",
                    PositionalArguments = new CommandArgumentRule[]
                    {
                        new CommandArgumentRule { Required = true, NamedValue = "'visual' | 'filesys'", ArgName = "mode-name", Description = @"The name of the mode to set the list viewer to.
                * Mode list:
                    visual: Opens a simulated directory
                    filesys: Opens an interactive directory at the specified path" },
                        new CommandArgumentRule { ArgName = "path", Description = "The path to open the 'filesys' mode at, leave blank to open at the default set directory" }
                    },
                    

                    OnExecuted = (viewer, positional, named) =>
                    {
                        if (positional.Count < 1)
                            return "Not enough arguments for 'mode' command";

                        string modeName = positional[0].Value;

                        if (modeName == "visual")
                        {

                        }
                        else if (modeName == "filesys")
                        {

                        }
                        else
                            return "Invalid mode name given, type in 'help mode' to see all avaliable modes";

                        return "";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "cleardata-and-exit",
                    Description = @"Clears all data associated with this program, and exits",

                    OnExecuted = (viewer, positional, named) =>
                    {
                        SaveFile.SaveData.Clear();
                        SaveFile.FullSave();
                        return "EXIT_P";
                    }
                };
                All.Add(i.Name, i);
            }
            {
                CommandItem i = new CommandItem
                {
                    Name = "exit",
                    Description = @"Exits the process",

                    OnExecuted = (viewer, positional, named) =>
                    {
                        return "EXIT_P";
                    }
                };
                All.Add(i.Name, i);
            }
        }
    }
}
