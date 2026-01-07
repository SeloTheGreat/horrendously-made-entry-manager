using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleRender
{
    internal readonly struct RenderGlyphs
    {
        public const char VerticalPipe = '│';
        public const char HorizontalPipe = '─';
        public const char Intersect = '┼';
        public const char ForkLeft = '├';
        public const char ForkRight = '┤';

        public const char TopLeft = '┌';
        public const char TopRight = '┐';
        public const char BottomLeft = '└';
        public const char BottomRight = '┘';
    }

    internal static class ListViewerSettings
    {
        public static bool loud = false;
        public static uint pageupjmp = 5;
        public static uint pagedownjmp = 5;
    }

    internal delegate void ListViewerCallback<TArgs>(ListViewer sender, TArgs e);

    internal class ListViewer
    {
        public List<string> ViewingList;
        public bool IsClosed;

        public ListViewerCallback<string> OnItemOpened;
        public ListViewerCallback<string> OnCommandTriggered;

        public int CommandInputRow;
        public int LogOutputRow;

        const int seperatorLength = 60;

        int previousBufferHeight;
        int previousWindowHeight;
        int currentInputRow;

        public int previousSelection;
        public int currentSelection;

        string defaultCmdString = "[PRESS 'Insert' to use CMD] [PRESS 'Space' to open entry]";
        string currentLog = "Here is a tip, type in 'help' at CMD and press enter!";
        bool currentLogIsError;

        public readonly Action BeepLow = () =>
        {
            Console.Beep(800, 100);
        };
        public readonly Action BeepHigh = () =>
        {
            Console.Beep(1200, 80);
        };
        public readonly Action BeepBurstHigh = () =>
        {
            Console.Beep(1000, 90);
            Console.Beep(700, 70);
        };
        public readonly Action BeepBurstLow = () =>
        {
            Console.Beep(700, 90);
            Console.Beep(400, 70);
        };

        public ListViewer(List<string> viewingList)
        {
            Console.CursorVisible = false;
            this.ViewingList = viewingList;
            this.IsClosed = false;

            this.previousBufferHeight = Console.BufferHeight;
            this.previousWindowHeight = Console.WindowHeight;

            {
                ListViewerSettings.loud = SaveFile.SaveData.TryGetValue("SET_loud", out string res) ? bool.Parse(res) : ListViewerSettings.loud;
                ListViewerSettings.pageupjmp = SaveFile.SaveData.TryGetValue("SET_page-up-jmp", out res) ? uint.Parse(res) : ListViewerSettings.pageupjmp;
                ListViewerSettings.pagedownjmp = SaveFile.SaveData.TryGetValue("SET_page-down-jmp", out res) ? uint.Parse(res) : ListViewerSettings.pagedownjmp;
            }

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                this.FullClose();
                Console.WriteLine("PROCESS ENDED, PRESS ANY BUTTONS TO KILL THIS PROCESS...");
                e.Cancel = true;
            };
        }

        void WriteAtClear(string printable, int row = 0)
        {
            int oldRow = Console.CursorTop;
            Console.CursorTop = row;
            Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
            Console.Write(printable);
            Console.CursorTop = oldRow;
        }

        public void ErrorToLogInterface(string log)
        {
            currentLog = log;
            currentLogIsError = true;
            WriteAtClear($"{RenderGlyphs.VerticalPipe} LOG: \x1b[31m{log}\x1b[0m", LogOutputRow);

            if (ListViewerSettings.loud)
                Task.Run(BeepBurstLow);
        }
        public void PrintToLogInterface(string log)
        {
            currentLog = log;
            currentLogIsError = false;
            WriteAtClear($"{RenderGlyphs.VerticalPipe} LOG: \x1b[32m{log}\x1b[0m", LogOutputRow);
        }

        public void MainRender()
        {
            bool isEmpty = ViewingList.Count == 0;

            string seperator = new string(RenderGlyphs.HorizontalPipe, seperatorLength - 1);

            Console.WriteLine(RenderGlyphs.TopLeft + seperator);
            Console.WriteLine(RenderGlyphs.VerticalPipe + " Selected > " + (isEmpty ? "..." : ViewingList[currentSelection]));
            Console.WriteLine(RenderGlyphs.ForkLeft + seperator);

            bool setToDefaultNextIter = false;

            for (int i = 0; i < ViewingList.Count; i++)
            {
                string printable = string.Format("{0} [{1}] > {2}",
                    RenderGlyphs.VerticalPipe,
                    i,
                    ViewingList[i]
                );

                printable += new string(' ', Math.Max(seperatorLength - printable.Length, 0));

                if (i == currentSelection)
                {
                    currentInputRow = Console.CursorTop;
                    Console.Write("\x1b[7m");
                    Console.WriteLine(printable);
                    setToDefaultNextIter = true;
                }
                else
                {
                    if (setToDefaultNextIter)
                    {
                        Console.Write("\x1b[0m");
                        setToDefaultNextIter = false;
                    }
                    Console.WriteLine(printable);
                }
            }

            if (setToDefaultNextIter)
                Console.Write("\x1b[0m");

            Console.WriteLine(RenderGlyphs.ForkLeft + seperator);
            Console.WriteLine($"{RenderGlyphs.VerticalPipe} {defaultCmdString}");
            CommandInputRow = Console.CursorTop - 1;
            Console.WriteLine(RenderGlyphs.ForkLeft + seperator);
            Console.WriteLine($"{RenderGlyphs.VerticalPipe} LOG: {(currentLogIsError ? "\x1b[31m" : "\x1b[32m")}{currentLog}\x1b[0m");
            LogOutputRow = Console.CursorTop - 1;
            Console.WriteLine(RenderGlyphs.BottomLeft + seperator);
        }

        void PreRender()
        {
            Console.CursorVisible = false;
        }

        void Render()
        {
            currentInputRow = 3 + currentSelection;

            if (ViewingList.Count == 0)
                return;

            if (ViewingList.Count > 1)
            {
                WriteAtClear(string.Format("\x1b[0m{0} [{1}] > {2}",
                    RenderGlyphs.VerticalPipe,
                    previousSelection,
                    ViewingList[previousSelection]
                ), 3 + previousSelection);
            }

            string printable = string.Format("{0} [{1}] > {2}",
                RenderGlyphs.VerticalPipe,
                currentSelection,
                ViewingList[currentSelection]
            );

            printable = "\x1b[7m" + printable + new string(' ', Math.Max(seperatorLength - printable.Length, 0)) + "\x1b[0m";

            WriteAtClear(printable, 3 + currentSelection);

            WriteAtClear(RenderGlyphs.VerticalPipe + " Selected > " + ViewingList[currentSelection], 1);
        }

        void PostRender()
        {
            Console.CursorTop = currentInputRow;
            Console.CursorLeft = 0;

            ConsoleKeyInfo info = Console.ReadKey(true);

            bool flag_doBeep = true;

            previousSelection = currentSelection;

            if (info.Key == ConsoleKey.UpArrow || info.Key == ConsoleKey.LeftArrow)
                currentSelection = Math.Max(currentSelection - 1, 0);
            else if (info.Key == ConsoleKey.DownArrow || info.Key == ConsoleKey.RightArrow)
                currentSelection = Math.Min(currentSelection + 1, ViewingList.Count - 1);
            else if (info.Key == ConsoleKey.PageUp)
                currentSelection = Math.Max(currentSelection - (int)ListViewerSettings.pageupjmp, 0);
            else if (info.Key == ConsoleKey.PageDown)
                currentSelection = Math.Min(currentSelection + (int)ListViewerSettings.pagedownjmp, ViewingList.Count - 1);
            else if (info.Key == ConsoleKey.End)
                currentSelection = ViewingList.Count - 1;
            else if (info.Key == ConsoleKey.Home)
                currentSelection = 0;
            else if (info.Key == ConsoleKey.Spacebar)
            {
                OnItemOpened?.Invoke(this, ViewingList[currentSelection]);
            }
            else if (info.Key == ConsoleKey.Insert)
            {

                if (ListViewerSettings.loud)
                    Task.Run(BeepHigh);

                string writeAtPrintable = $"{RenderGlyphs.VerticalPipe} CMD > :";
                WriteAtClear(writeAtPrintable, CommandInputRow);

                Console.CursorVisible = true;
                Console.SetCursorPosition(writeAtPrintable.Length, CommandInputRow);

                string result = Console.ReadLine();

                OnCommandTriggered?.Invoke(this, result);

                if (IsClosed)
                    return;
                WriteAtClear($"{RenderGlyphs.VerticalPipe} {defaultCmdString}", CommandInputRow);
                Console.CursorVisible = false;
                flag_doBeep = false;
            }
            else if (info.Key == ConsoleKey.C)
            {
                OnCommandTriggered?.Invoke(this, $"clone {currentSelection}");
                flag_doBeep = false;
            }
            else if (info.Key == ConsoleKey.D)
            {
                OnCommandTriggered?.Invoke(this, $"rem {currentSelection}");
                flag_doBeep = false;
            }
            else
            {
                ConsoleKeyInfo currentInfo = info;
                uint totalResult = 0;

                while (true)
                {
                    uint result;
                    bool parsesuccess = uint.TryParse(currentInfo.KeyChar.ToString(), out result);

                    if (parsesuccess)
                    {
                        if (ListViewerSettings.loud)
                            Task.Run(BeepHigh);

                        totalResult = totalResult * 10 + result;
                        WriteAtClear(string.Format("{0} Searching [{1}...]", RenderGlyphs.VerticalPipe, totalResult), 1);
                    }
                    else
                    {
                        if (currentInfo != info)
                        {
                            currentSelection = (int)Math.Min(totalResult, ViewingList.Count - 1);
                            if (ListViewerSettings.loud)
                                Task.Run(BeepBurstHigh);
                            flag_doBeep = false;
                        }
                        break;
                    }
                    currentInfo = Console.ReadKey(true);
                }
            }

            if (ListViewerSettings.loud && flag_doBeep)
                Task.Run(BeepLow);
        }

        public void StartUpdateLoop()
        {
            IsClosed = false;

            Console.Clear();

            int bufferHeight = ViewingList.Count + 9;
            Console.WindowHeight = Math.Min(bufferHeight, Console.LargestWindowHeight); //TODO, DO VISUAL SCROLLING FOR RENDER
            Console.BufferHeight = bufferHeight;

            currentSelection = Math.Min(Math.Max(currentSelection, 0), ViewingList.Count - 1);
            previousSelection = Math.Min(Math.Max(previousSelection, 0), ViewingList.Count - 1);

            this.MainRender();
            while (!IsClosed)
            {
                this.PreRender();
                this.Render();
                this.PostRender();
            }
        }

        public void Close()
        {
            IsClosed = true;
            Console.Clear();
            Console.CursorVisible = true;
        }

        public void SetPreviousData()
        {
            Console.CursorVisible = true;
            Console.WindowHeight = previousWindowHeight;
            Console.BufferHeight = previousBufferHeight;
        }

        public void FullClose()
        {
            Close();
            SetPreviousData();
            SaveFile.FullSave();
        }
    }
}