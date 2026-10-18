using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Win32;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using NetCoreAudio;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using SixLabors.ImageSharp.Processing;
#pragma warning disable SYSLIB0014

namespace MyLanguage
{
    internal class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static IntPtr _cachedHandle = IntPtr.Zero;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 0x04;

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(uint sessionId, out IntPtr phToken);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken, string lpApplicationName, string lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread); [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public long Luid;
            public uint Attributes;
        }
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";
        const uint THREAD_TERMINATE = 0x0001;
        public static FileSystemWatcher watcher;
        private static BlockingCollection<string> logQueue = [];
        public static string aliasname;
        public static string aliasvalue;
        public static bool IsDebugModeEnabled;
        [STAThread]
        static async Task Main(string[] args)
        {
            bool STATUS = EnableDebugPrivilege();
            if (STATUS == true) { if (IsDebugModeEnabled) Console.WriteLine("[DEBUG] Successfully high up process rights."); }
            else
            {
                Console.WriteLine("Failed to high up process rights. Some features will not available.");
            }
            bool isdirectoryexists = Directory.Exists("C:\\WINNT");
            if (isdirectoryexists)
            {
                Console.WriteLine("Old version of Windows is detected.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            IntPtr hWnd = GetConsoleWindow();
            const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            int cornerPreference = 1;
            DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
            const int GWL_STYLE = -16;
            const int WS_THICKFRAME = 0x00040000;

            int style = GetWindowLong(hWnd, GWL_STYLE);

            style &= ~WS_THICKFRAME;
            SetWindowLong(hWnd, GWL_STYLE, style);

            const uint SWP_FRAMECHANGED = 0x0020;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOZORDER = 0x0004;
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            string myUniqueId = "Console_" + Guid.NewGuid().ToString();
            Console.Title = myUniqueId;
            Thread.Sleep(100);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "MyLanguage! Platform: .NET 8.0";
            bool AllowLogs = false;
            bool DisableWarnings = false;
            bool DisableWelcomeText = false;
            if (args.Length > 0)
            {
                if (args[0] == "-nowarnings") { DisableWarnings = true; }
                if (args[0] == "-debugmode") { IsDebugModeEnabled = true; }
                if (args[0] == "-calcmode")
                {
                    while (true)
                    {
                        string something = Console.ReadLine();
                        string[] parts = something.Split(' ');
                        string op = parts[1];
                        BigInteger a = int.Parse(parts[0]);
                        BigInteger b = int.Parse(parts[2]);
                        switch (op)
                        {
                            case "+":
                                BigInteger result = a + b;
                                Console.WriteLine(result);
                                break;
                            case "-":
                                BigInteger result1 = a - b;
                                Console.WriteLine(result1);
                                break;
                            case "*":
                                BigInteger result2 = a * b;
                                Console.WriteLine(result2);
                                break;
                            case "/":
                                BigInteger result3 = a / b;
                                Console.WriteLine(result3);
                                break;

                        }
                    }
                }
                if (args[0] == "-disablewelcometext") { DisableWelcomeText = true; }
                if (args.Length > 1)
                {
                    if (args[1] == "-nowarnings") { DisableWarnings = true; }
                    if (args[1] == "-debugmode") { IsDebugModeEnabled = true; }
                    if (args[1] == "-calcmode")
                    {
                        while (true)
                        {
                            string something = Console.ReadLine();
                            string[] parts = something.Split(' ');
                            string op = parts[1];
                            BigInteger a = int.Parse(parts[0]);
                            BigInteger b = int.Parse(parts[2]);
                            switch (op)
                            {
                                case "+":
                                    BigInteger result = a + b;
                                    Console.WriteLine(result);
                                    break;
                                case "-":
                                    BigInteger result1 = a - b;
                                    Console.WriteLine(result1);
                                    break;
                                case "*":
                                    BigInteger result2 = a * b;
                                    Console.WriteLine(result2);
                                    break;
                                case "/":
                                    BigInteger result3 = a / b;
                                    Console.WriteLine(result3);
                                    break;

                            }
                        }
                    }
                    if (args[1] == "-disablewelcometext")
                    {
                        DisableWelcomeText = true;
                    }
                }
            }
            if (IsDebugModeEnabled)
            {
                if (!DisableWarnings) Console.WriteLine("WARNING: Debug Mode can cause errors because of testing. It's recommended to turn it off. \nIf you have enabled this and your computer has been damaged in any way, \nthe developer is not liable for any consequences.");
            }
            if (DisableWelcomeText == false) { Console.WriteLine("Welcome to MyLanguage program!"); }
            while (true)
            {
                Console.Write($"{Environment.CurrentDirectory}> ");
                List<string> commands = new List<string>();
                string userinput = Console.ReadLine();

            start_checks:

                if (IsDebugModeEnabled)
                {
                    Console.WriteLine($"[DEBUG] Symbols typed: {userinput.Length}, Text: '{userinput}'");
                }
                if (IsDebugModeEnabled)
                {
                    Console.WriteLine($"[DEBUG] userinput = '{userinput}'");
                }

                string EasterEggVariable = "there is no easter eggs lol";
                bool IsEasterEggEnabled = false;

                if (IsEasterEggEnabled) Console.WriteLine(EasterEggVariable);

                if (userinput.StartsWith("print "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.Create("logs.txt").Close();
                        File.AppendAllText("logs.txt", "Command print executed\n");
                    }
                    string arg = userinput.Substring(6);
                    Console.WriteLine(arg);
                }

                if (userinput == "all commands" || userinput == "history")
                {
                    commands.Add(userinput);
                    Console.Write("Please choose a way how to print your command histroy: [array/logfile]: ");
                    string way = Console.ReadLine();
                    if (way.Equals("array", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var i in commands)
                        {

                            Console.WriteLine(i);
                        }
                    }
                    else if (way.Equals("logfile", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] commands1 = File.ReadAllLines("logs.txt");
                        for (int i = 0; i < commands1.Length; i++)
                        {
                            commands1[i] = commands1[i].Replace("executed", "");
                            commands1[i] = commands1[i].Replace("Command ", "");
                        }
                        foreach (var h in commands1)
                        {
                            Console.WriteLine(h);
                        }
                    }
                }

                if (userinput == "close program")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command close program executed\n");
                        File.AppendAllText("logs.txt", "Program closed successfully");
                    }
                    Environment.Exit(0);
                }

                if (userinput.StartsWith("do "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Substring(3).Split(' ');
                    int user_number = Convert.ToInt32(parts[0]);
                    if (parts.Length > 2)
                    {
                        for (var i = 0; i < user_number; i++)
                        {
                            foreach (var w in parts[1..])
                            {
                                Console.WriteLine(w);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < user_number; i++)
                        {
                            Console.WriteLine(parts[1]);
                        }
                    }
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command do executed\n");
                    }
                }

                if (userinput == "clear")
                {
                    commands.Add(userinput);
                    Console.Clear();
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command clear executed\n");
                    }
                }

                if (userinput.StartsWith("read "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command read executed\n");
                    }
                    string filepath = userinput.Substring(5).Trim();
                    if (File.Exists(filepath))
                    {
                        string filecontent = File.ReadAllText(filepath);
                        Console.WriteLine(filecontent);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("No such file or directory is invalid! \nProgram is only support reading files from executable file directory.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                if (userinput == "all files")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command all files executed\n");
                    }
                    string directory = Directory.GetCurrentDirectory();
                    string[] files = Directory.GetFiles(directory);

                    foreach (string file in files)
                    {
                        Console.WriteLine(file);
                    }
                }

                if (userinput == "help" || userinput == "help 1")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("print [text] - Print text.");
                    Console.WriteLine("read [filename.extension] - Read file (please do not try this \non executable (.exe) files).");
                    Console.WriteLine("all files - Show all files in current directory.");
                    Console.WriteLine("clear - Clear console.");
                    Console.WriteLine("delete [filename.extension] - Delete file.");
                    Console.WriteLine("create [filename.extension] - Create file.");
                    Console.WriteLine("user add [username] - Create a user in your OS.");
                    Console.WriteLine("user remove [username] - Remove a user in your OS.");
                    Console.WriteLine("Page 1 -- Type \"help 2\" for next page.");
                }

                if (userinput == "help 2")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("shutdown pc - Shutdown your OS.");
                    Console.WriteLine("shutdown pc timer [timer] - Shutdown your OS by the timer (seconds).");
                    Console.WriteLine("restart pc - Restart your OS.");
                    Console.WriteLine("restart pc timer [timer] - Restart your OS by the timer (seconds).");
                    Console.WriteLine("kill process [processname] - Kill a process by it's name.");
                    Console.WriteLine("open process [processname] - Create a process by it's name.");
                    Console.WriteLine("task manager - A integrated task manager (no one virus will detect it lol)");
                    Console.WriteLine("color [foreground or background] [colorname] - Color your console.");
                    Console.WriteLine("info - Prints info about this app. (try it, it's cool)");
                    Console.WriteLine("ping [dns.adress or domain] - Pings domain or DNS Server");
                    Console.WriteLine("Page 2 -- Type \"help 3\" for next page.");
                }

                if (userinput == "help 3")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("systeminfo (or pcinfo) - Give you information about your PC.");
                    Console.WriteLine("allow logs - By this you allow logging what commands you type (example: command pcinfo executed).");
                    Console.WriteLine("deny logs - By this you deny logging what commands you type.");
                    Console.WriteLine("clear logs - By this you clean logs file.");
                    Console.WriteLine("redactor - Give you C# (CSharp) redactor, where you can type C# (CSharp) code.");
                    Console.WriteLine("disable warnings - Disable all program warnings. (not recommended to turn off warnings)");
                    Console.WriteLine("disable warnings [flag, true or false or y or n] - Disable/Enable all program warnings. \n(not recommended to turn off warnings)");
                    Console.WriteLine("enable warnings - Enable all warnings.");
                    Console.WriteLine("enable warnings [flag] - Enable/Disable all program warnings. (not recommended to turn off warnings)");
                    Console.WriteLine("administrator permission [set/remove] [appname] - Set/Remove administrator rights for/from process.");
                    Console.WriteLine("currentprocessinfo - Prints all current process info available in Environment method. (apart from process name)");
                    Console.WriteLine("cmd - Open command prompt.");
                    Console.WriteLine("powershell - Open powershell.");
                    Console.WriteLine("Page 3 -- Type \"help 4\" for next page.");
                }

                if (userinput == "help 4")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("theme [themename] - Allow you to change console theme. \nAll themes:\nmatrix\nrainbow\nverity/veritymatrix");
                    Console.WriteLine("cd [directoryname] - Direct you to your directory (Directory you type).");
                    Console.WriteLine("beep [int] [int] - Beep. (Yes, just beep)");
                    Console.WriteLine("regedit add [path] [regszordwordname] - Add a regedit value in whatever directory.");
                    Console.WriteLine("regedit remove [path] [whatevertype] - Remove a regedit value in whatever directory.");
                    Console.WriteLine("regedit edit [path] [regszdwordname] - Edit a regedit value in whatever directory.");
                    Console.WriteLine("access/grant/icacls [filename] [groupnameorusername] - Give full rights for group/user.");
                    Console.WriteLine("myip - Show your IP Address.");
                    Console.WriteLine("network connections - Show all network connections.");
                    Console.WriteLine("size [int] [int] - Size your console by [int] [int] values.");
                    Console.WriteLine("writeatfirstline [filename] [text] - Write your text at first line in your requested file.");
                    Console.WriteLine("writeatlastline [filename] [text] - Write your text at last line in your requested file.");
                    Console.WriteLine("Page 4 -- Type \"help 5\" for next page.");
                }

                if (userinput == "help 5")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("hash [filename] - Print your file hash.");
                    Console.WriteLine("clear garbage - Call C# GC (Garbage Collector) Method and clear all garbage.");
                    Console.WriteLine("properties [filename] - Show all available information available in C# (CSharp) Environment method.");
                    Console.WriteLine("service add [name] [path] [displayname] [service_type] [start_type] - Create a service with your arguments.");
                    Console.WriteLine("C:\\... Open a directory that starts with C:\\.");
                    Console.WriteLine("D:\\... Open a directory that starts with D:\\.");
                    Console.WriteLine("calc - Open calculator mode. (Also can be run when you start this program with -calcmode argument).");
                    Console.WriteLine("where [pidorprocessnamewithoutextension] - Show you the process directory.");
                    Console.WriteLine("kill process-tree [processname] - Kill original process and its children.");
                    Console.WriteLine("license - Show program license.");
                    Console.WriteLine("MoveConsole [int] [int] - Move your console by the coordinates.");
                    Console.WriteLine("size [int] [int] - Size your console by the coordinates.");
                    Console.WriteLine("dotnet [arguments] - Run a dotnet task with your arguments.");
                    Console.WriteLine("Page 5 -- Type \"help 6\" for next page.");
                }

                if (userinput == "help 6")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("speaker [text] - Speak your text by standard Windows Voice.");
                    Console.WriteLine("download [url] [filename] - Download your file from requested URL.");
                    Console.WriteLine("generate random password - Generate random password.");
                    Console.WriteLine("hex [filename] - Read file by HEX way.");
                    Console.WriteLine("[name].exe - Start executable file by its name.");
                    Console.WriteLine("[name].txt - Open text document file with Windows Notepad.");
                    Console.WriteLine("filecompare text [file1] [file2] - Compares two files by text.");
                    Console.WriteLine("filecompare bytes [file1] [file2] - Compares one two files by bytes.");
                    Console.WriteLine("filecompare textandbytes [file1] [file2] - Compares two files by text and bytes.");
                    Console.WriteLine("game - A C# redactor where you must type a valid text. If:\nYou type empty string - You get a BSOD.\nYou type invalid code - You get a BSOD.\nYou type to close program by any way (If you type game) - You get a BSOD.");
                    Console.WriteLine("copy [file1] [file2] - Copy file to file.");
                    Console.WriteLine("rename [file1] [file2] - Renames file name to your file name.");
                    Console.WriteLine("snake game - Give you a snake game (be careful, if you out of bounds you get a crash)");
                    Console.WriteLine("runassystem [filepath] - Run a process without window as NT AUTHORITY\\SYSTEM user. (SYSTEM user has more priviliges than Administrator if you don't know, \nand you cannot log in to it.");
                    Console.WriteLine("tasks - Give you a task menu.");
                    Console.WriteLine("findstr [string] [file] - Find's a string in a file.");
                    Console.WriteLine("Page 6 -- Type \"help 7\" for next page.");
                }

                if (userinput == "help 7")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("close after [timer] - Close program after typed seconds.");
                    Console.WriteLine("all drivers - Print all drives.");
                    Console.WriteLine("attribute/attr [attributes] [file] - Gives your attributes to your files.");
                    Console.WriteLine("service remove [servicename] - Removes serivce by its name.");
                    Console.WriteLine("service stop [servicename] - Stop service by its name.");
                    Console.WriteLine("service delete [servicename] - Delete service by its name.");
                    Console.WriteLine("cleartemp/cleantempfolder/tempdel - \"Clean %systemroot%\\Temp\" folder and \"%TEMP%\" folder also.");
                    Console.WriteLine("send [ip] [port] [text] - This will connect to device with your IP Address and your port and convert text into bytes, and send it to your device.");
                    Console.WriteLine("compile [name.cs] [namewithoutexeextension] - Compiles .exe file with using DOTNET and program file.");
                    Console.WriteLine("dllinject [processname] [dlllibrary] - Injects dll file to exe file.");
                    Console.WriteLine("battoexe [batfile.bat] [exefile] - Converts bat file to exe file.");
                    Console.WriteLine("forcekill [processname] - Force kills process by two different ways. (Can be selected after typing this command)");
                    Console.WriteLine("videototext [file.mp4] - Converts mp4 file to text and plays it in console.");
                    Console.WriteLine("Page 7 -- Type \"help 8\" for next page.");
                }

                if (userinput == "help 8")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    Console.WriteLine("Commands:");
                    Console.WriteLine("alias [aliasname] [commandname] - Make an alias for any command.");
                }

                if (userinput.StartsWith("help"))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command help executed\n");
                    }
                    string[] allCommands = {
        // --- Page 1 ---
        "print [text] - Print text.",
        "read [filename.extension] - Read file (please do not try this on executable (.exe) files).",
        "all files - Show all files in current directory.",
        "clear - Clear console.",
        "delete [filename.extension] - Delete file.",
        "create [filename.extension] - Create file.",
        "user add [username] - Create a user in your OS.",
        "user remove [username] - Remove a user in your OS.",

        // --- Page 2 ---
        "shutdown pc - Shutdown your OS.",
        "shutdown pc timer [timer] - Shutdown your OS by the timer (seconds).",
        "restart pc - Restart your OS.",
        "restart pc timer [timer] - Restart your OS by the timer (seconds).",
        "kill process [processname] - Kill a process by it's name.",
        "open process [processname] - Create a process by it's name.",
        "task manager - A integrated task manager (no one virus will detect it lol)",
        "color [foreground or background] [colorname] - Color your console.",
        "info - Prints info about this app. (try it, it's cool)",
        "ping [dns.adress or domain] - Pings domain or DNS Server",

        // --- Page 3 ---
        "systeminfo (or pcinfo) - Give you information about your PC.",
        "allow logs - By this you allow logging what commands you type (example: command pcinfo executed).",
        "deny logs - By this you deny logging what commands you type.",
        "clear logs - By this you clean logs file.",
        "redactor - Give you C# (CSharp) redactor, where you can type C# (CSharp) code.",
        "disable warnings - Disable all program warnings. (not recommended to turn off warnings)",
        "disable warnings [flag, true or false or y or n] - Disable/Enable all program warnings. (not recommended to turn off warnings)",
        "enable warnings - Enable all warnings.",
        "enable warnings [flag] - Enable/Disable all program warnings. (not recommended to turn off warnings)",
        "administrator permission [set/remove] [appname] - Set/Remove administrator rights for/from process.",
        "currentprocessinfo - Prints all current process info available in Environment method. (apart from process name)",
        "cmd - Open command prompt.",
        "powershell - Open powershell.",

        // --- Page 4 ---
        "theme [themename] - Allow you to change console theme. All themes: matrix, rainbow, verity/veritymatrix",
        "cd [directoryname] - Direct you to your directory (Directory you type).",
        "beep [int] [int] - Beep. (Yes, just beep)",
        "regedit add [path] [regszordwordname] - Add a regedit value in whatever directory.",
        "regedit remove [path] [whatevertype] - Remove a regedit value in whatever directory.",
        "regedit edit [path] [regszdwordname] - Edit a regedit value in whatever directory.",
        "access/grant/icacls [filename] [groupnameorusername] - Give full rights for group/user.",
        "myip - Show your IP Address.",
        "network connections - Show all network connections.",
        "size [int] [int] - Size your console by [int] [int] values.",
        "writeatfirstline [filename] [text] - Write your text at first line in your requested file.",
        "writeatlastline [filename] [text] - Write your text at last line in your requested file.",

        // --- Page 5 ---
        "hash [filename] - Print your file hash.",
        "clear garbage - Call C# GC (Garbage Collector) Method and clear all garbage.",
        "properties [filename] - Show all available information available in C# (CSharp) Environment method.",
        "service add [name] [path] [displayname] [service_type] [start_type] - Create a service with your arguments.",
        "C:\\... Open a directory that starts with C:\\.",
        "D:\\... Open a directory that starts with D:\\.",
        "calc - Open calculator mode. (Also can be run when you start this program with -calcmode argument).",
        "where [pidorprocessnamewithoutextension] - Show you the process directory.",
        "kill process-tree [processname] - Kill original process and its children.",
        "license - Show program license.",
        "MoveConsole [int] [int] - Move your console by the coordinates.",
        "size [int] [int] - Size your console by the coordinates.",
        "dotnet [arguments] - Run a dotnet task with your arguments.",

        // --- Page 6 ---
        "speaker [text] - Speak your text by standard Windows Voice.",
        "download [url] [filename] - Download your file from requested URL.",
        "generate random password - Generate random password.",
        "hex [filename] - Read file by HEX way.",
        "[name].exe - Start executable file by its name.",
        "[name].txt - Open text document file with Windows Notepad.",
        "filecompare text [file1] [file2] - Compares two files by text.",
        "filecompare bytes [file1] [file2] - Compares one two files by bytes.",
        "filecompare textandbytes [file1] [file2] - Compares two files by text and bytes.",
        "game - A C# redactor where you must type a valid text. If: You type empty string - You get a BSOD. You type invalid code - You get a BSOD. You type to close program by any way (If you type game) - You get a BSOD.",
        "copy [file1] [file2] - Copy file to file.",
        "rename [file1] [file2] - Renames file name to your file name.",
        "snake game - Give you a snake game (be careful, if you out of bounds you get a crash)",
        "runassystem [filepath] - Run a process without window as NT AUTHORITY\\SYSTEM user. (SYSTEM user has more privileges than Administrator if you don't know, and you cannot log in to it.",
        "tasks - Give you a task menu.",
        "findstr [string] [file] - Find's a string in a file.",

        // --- Page 7 ---
        "close after [timer] - Close program after typed seconds.",
        "all drivers - Print all drives.",
        "attribute/attr [attributes] [file] - Gives your attributes to your files.",
        "service remove [servicename] - Removes serivce by its name.",
        "service stop [servicename] - Stop service by its name.",
        "service delete [servicename] - Delete service by its name.",
        "cleartemp/cleantempfolder/tempdel - \"Clean %systemroot%\\Temp\" folder and \"%TEMP%\" folder also.",
        "send [ip] [port] [text] - This will connect to device with your IP Address and your port and convert text into bytes, and send it to your device.",
        "compile [name.cs] [namewithoutexeextension] - Compiles .exe file with using DOTNET and program file.",
        "dllinject [processname] [dlllibrary] - Injects dll file to exe file.",
        "battoexe [batfile.bat] [exefile] - Converts bat file to exe file.",
        "forcekill [processname] - Force kills process by two different ways. (Can be selected after typing this command)",
        "videototext [file.mp4] - Converts mp4 file to text and plays it in console.",

        // --- Page 8 ---
        "alias [aliasname] [commandname] - Make an alias for any command."
    };

                    int pageSize = 10;
                    int totalPages = (int)Math.Ceiling((double)allCommands.Length / pageSize);
                    int currentPage = 1;
                    string[] inputParts = userinput.Split(' ');
                    if (inputParts.Length > 1 && int.TryParse(inputParts[1], out int startPage))
                    {
                        if (startPage >= 1 && startPage <= totalPages) currentPage = startPage;
                    }
                    bool originalCursorVisible = Console.CursorVisible;
                    Console.CursorVisible = false;
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("=== MyLanguage Interactive Help ===");
                        Console.WriteLine("Use Up/Down or Left/Right Arrows to turn pages. Press Enter or Esc to return to CLI.\n");
                        int startIndex = (currentPage - 1) * pageSize;
                        int endIndex = Math.Min(startIndex + pageSize, allCommands.Length);
                        for (int i = startIndex; i < endIndex; i++)
                        {
                            Console.WriteLine(" • " + allCommands[i]);
                        }
                        Console.WriteLine($"\n--- Page {currentPage} of {totalPages} ---");
                        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                        if (keyInfo.Key == ConsoleKey.DownArrow || keyInfo.Key == ConsoleKey.RightArrow)
                        {
                            if (currentPage < totalPages) currentPage++;
                        }
                        else if (keyInfo.Key == ConsoleKey.UpArrow || keyInfo.Key == ConsoleKey.LeftArrow)
                        {
                            if (currentPage > 1) currentPage--;
                        }
                        else if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                        {
                            Console.Clear();
                            Console.CursorVisible = originalCursorVisible;
                            break;
                        }
                    }
                }

                if (userinput == "cleartemp" || userinput == "cleartempfolder" || userinput == "tempdel")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command cleartemp/cleartempfolder/tempdel executed");
                    }
                    Process.Start("cmd.exe", $"/c del /s /q {Environment.ExpandEnvironmentVariables("%TMP%")}\\*.*");
                    Process.Start("cmd.exe", $"/c del /s /q {Environment.ExpandEnvironmentVariables("%systemroot%")}\\Temp\\*.*");
                }

                if (userinput.StartsWith("delete "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command delete executed\n");
                    }
                    string[] parts = userinput.Substring(7).Split(' ');
                    string file = parts[0];

                    if (File.Exists(file))
                    {
                        if (parts.Length > 1)
                        {
                            string answer = parts[1];
                            if (answer == "y" || answer == "Y")
                            {
                                File.Delete(file);
                            }
                            if (answer == "n" || answer == "N")
                            {
                                userinput = Console.ReadLine();
                            }
                        }
                        else
                        {
                            Console.Write("Are you sure? [Y/N]: ");
                            string answer1 = Console.ReadLine();
                            if (answer1 == "y" || answer1 == "Y")
                            {
                                File.Delete(file);
                            }
                            if (answer1 == "n" || answer1 == "N")
                            {
                                userinput = Console.ReadLine();
                            }
                        }

                    }

                }
                if (userinput.StartsWith("create "))
                {
                    commands.Add(userinput);
                    if (AllowLogs) File.AppendAllText("logs.txt", "Command create executed\n");
                    string[] parts = userinput[7..].Split(' ');
                    string filename = parts[0];
                    string filetext = string.Join(" ", parts[1..]);
                    File.WriteAllText(filename, filetext);
                }

                if (userinput.StartsWith("user add "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command user add executed\n");
                    }
                    string username = userinput.Substring(9);

                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = $"/c net user {username} /add";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput.StartsWith("user remove "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command user remove executed\n");
                    }
                    string username = userinput.Substring(12);

                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = $"/c net user {username} /remove";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput.StartsWith("shutdown pc "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command shutdown pc executed\n");
                    }
                    string[] parts = userinput.Substring(12).Split(' ');
                    if (parts.Length > 1)
                    {
                        string timer = parts[1];
                        ProcessStartInfo CMDProcess = new ProcessStartInfo();
                        CMDProcess.CreateNoWindow = true;
                        CMDProcess.FileName = "cmd.exe";
                        CMDProcess.Arguments = $"/c shutdown -s -t {timer}";
                        CMDProcess.UseShellExecute = false;
                        Process.Start(CMDProcess);
                    }
                }

                if (userinput == "shutdown pc")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command shutdown pc executed\n");
                    }
                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = $"/c shutdown -s -t 0";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput == "cancel pc shutdown")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command cancel pc shutdown executed\n");
                    }
                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = "/c shutdown -a";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput.StartsWith("restart pc "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command restart pc executed\n");
                    }
                    string[] parts = userinput.Substring(11).Split(' ');
                    if (parts.Length > 1)
                    {
                        string timer = parts[1];
                        ProcessStartInfo CMDProcess = new ProcessStartInfo();
                        CMDProcess.CreateNoWindow = true;
                        CMDProcess.FileName = "cmd.exe";
                        CMDProcess.Arguments = $"/c shutdown -s -t {timer}";
                        CMDProcess.UseShellExecute = false;
                        Process.Start(CMDProcess);
                    }
                }

                if (userinput == "restart pc")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command restart pc executed\n");
                    }
                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = $"/c shutdown -r -t 0";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput == "cancel pc restart")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command cancel pc restart executed\n");
                    }
                    ProcessStartInfo CMDProcess = new ProcessStartInfo();
                    CMDProcess.CreateNoWindow = true;
                    CMDProcess.FileName = "cmd.exe";
                    CMDProcess.Arguments = "/c shutdown -a";
                    CMDProcess.UseShellExecute = false;
                    Process.Start(CMDProcess);
                }

                if (userinput.StartsWith("kill process "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command kill process executed\n");
                    }
                    string processname = userinput.Substring(13);
                    if (processname.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        processname = processname.Substring(0, processname.Length - 4);
                    }
                    Process[] processes = Process.GetProcessesByName(processname);
                    foreach (Process process in processes)
                    {
                        process.Kill();
                    }
                }

                if (userinput.StartsWith("open process "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command open process executed\n");
                    }
                    string processname = userinput.Substring(13);
                    Process.Start(processname);
                }

                if (userinput == "task manager")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command task manager executed\n");
                    }
                    Process[] processes = Process.GetProcesses();
                    foreach (Process process in processes)
                    {
                        Console.WriteLine($"Process Name: {process.ProcessName} Process ID: {process.Id}");
                    }
                }
                if (userinput.StartsWith("color ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command color executed\n");
                    }
                    string[] parts = userinput.Substring(6).Split(' ');

                    if (IsDebugModeEnabled) Console.WriteLine($"DEBUG: parts[0] hello i am = '{parts[0]}'");
                    if (IsDebugModeEnabled) Console.WriteLine($"DEBUG: parts[1] hello i am = '{parts[1]}'");

                    if (parts[0] == "foreground")
                    {
                        string color = parts[1];
                        if (color == "red")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }

                        if (color == "green")
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        if (color == "blue")
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                        }

                        if (color == "white")
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                        }

                        if (color == "black")
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                        }

                        if (color == "pink")
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                        }
                    }

                    if (parts[0] == "background")
                    {
                        string color = parts[1];
                        Console.WriteLine("WARNING: There seem to be problems with Console.BackgroundColor method in Windows. If it's not \nworking properly, it's recommended to type \"clear\" command.");
                        if (color == "red")
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                        }

                        if (color == "green")
                        {
                            Console.BackgroundColor = ConsoleColor.Green;
                        }

                        if (color == "blue")
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                        }

                        if (color == "white")
                        {
                            Console.BackgroundColor = ConsoleColor.White;
                        }

                        if (color == "black")
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                        }

                        if (color == "pink")
                        {
                            Console.BackgroundColor = ConsoleColor.Magenta;
                        }
                    }
                }

                if (userinput.ToLower().Trim() == "info")
                {
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command info executed\n");
                    }
                    if (!IsDebugModeEnabled)
                    {
                        Console.WriteLine(@"
 __  __       _                                     
|  \/  |     | |                                    
| \  / |_   _| |    __ _ _ __   __ _ _   _  __ _   ____   ___ 
| |\/| | | | | |   / _` | '_ \ / _` | | | |/ _` | / _` \ / _ \
| |  | | |_| | |__| (_| | | | | (_| | |_| | (_| || (_| | | __/
|_|  |_|\__, |_____\__,_|_| |_|\__, |\__,_|\__,_| \__, |  \___|
          _/ |                   _ / |              __||
         |___/                   |___/              |__/             

Info:
Developer: Zilyak Stepan (Stepan 360)
Version: I am not counting versions
Mail: I want to be privacy
Thanks for using/watching!
");
                        Console.Write("Please rate this product from 0/10! (if you want of course, if you don't want you \ncan type idontwant): ");
                        var rating = Console.ReadLine();
                        if (rating == "idontwant")
                        {
                            Console.WriteLine("OK...");
                        }

                        else if (int.TryParse(rating, out int number1))
                        {
                            if (number1 == 0)
                            {
                                Console.WriteLine(";(");
                            }
                            if (number1 >= 10)
                            {
                                Console.WriteLine("omg why are you give so high rating? but thanks!");
                            }
                            if (number1 <= 10)
                            {
                                Console.WriteLine("thanks! your мнение very значимо for us!");
                            }
                        }

                    }
                    else
                    {
                        Console.WriteLine("test123");
#pragma warning disable CA1416
                        Console.Beep(1000, 100);
                        Console.WriteLine("Checked. Command working properly. Turning off debug mode.");
                        IsDebugModeEnabled = false;
                    }
                }

                if (userinput.StartsWith("ping "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command ping executed\n");
                    }
                    string pingtarget = userinput.Substring(5);
                    try
                    {
                        Ping PingSender = new Ping();
                        PingReply _PingReply = PingSender.Send(pingtarget, 2000);

                        if (_PingReply.Status == IPStatus.Success)
                        {
                            Console.WriteLine($"Success! Time: {_PingReply.RoundtripTime}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception caused. Reason: {e.Message}");
                    }
                }

                if (userinput == "pcinfo" || userinput == "systeminfo")
                {
                    commands.Add(userinput);
                    Process process = Process.GetCurrentProcess();
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command pcinfo/systeminfo executed\n");
                    }
                    Console.WriteLine("OS Info:");
                    Console.WriteLine($"OS Version: {Environment.OSVersion}");
                    Console.WriteLine($"Machine name: {Environment.MachineName}");
                    Console.WriteLine($"Username: {Environment.UserName}");
                    Console.WriteLine($"Has Shutdown Started: {Environment.HasShutdownStarted}");
                    Console.WriteLine($"Time after last system start (in milliseconds): {Environment.TickCount}");
                    Console.WriteLine($"Processor Cores: {Environment.ProcessorCount}");
                    Console.WriteLine($"Is 64 Bit OS: {Environment.Is64BitOperatingSystem}");
                    Console.WriteLine($"Logical Drives:");
                    foreach (var i in Environment.GetLogicalDrives())
                    {
                        Console.WriteLine(i);
                    }
                    TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
                    Console.WriteLine($"Total Processor Time: {process.TotalProcessorTime}");
                    Console.WriteLine($"User Processor Time: {process.UserProcessorTime}");
                    Console.WriteLine($"PC Uptime: {uptime.Days} Days, {uptime.Hours} Hours, {uptime.Minutes} Minutes, {uptime.Seconds} Seconds");
                }

                if (userinput == "fun 1")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command fun 1 executed\n");
                    }
                    if (IsDebugModeEnabled)
                    {
                        Console.Clear();
                        while (true)
                        {
                            Random rnd = new Random();
                            int randomColorNumber = rnd.Next(1, 16);
                            Console.BackgroundColor = (ConsoleColor)randomColorNumber;
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            var text = rnd.Next(short.MinValue, short.MaxValue).ToString();
                            Console.WriteLine(text + text + text + text + text + text + text);
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true).Key;
                                if (key == ConsoleKey.C)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }

                if (userinput == "fun 2")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command fun 2 executed\n");
                    }
                    if (IsDebugModeEnabled)
                    {
                        await Task.Run(() => Process.Start("mshta", "vbscript:Execute(\"MsgBox \"\"Java app failed to start! Exception: ModuleNotFoundException in line 32. Java App closed with error code -1.\"\", 16, \"\"Java Runtime Error\"\" \")(window.close)"));
                        Thread.Sleep(500);
                        Process.Start("mshta", "vbscript:Execute(\"MsgBox \"\"Java app failed to start! Exception: ModuleNotFoundException in line 32. Java App closed with error code -1.\"\", 16, \"\"Java Runtime Error\"\" \")(window.close)");
                        Thread.Sleep(500);
                        Process.Start("mshta", "vbscript:Execute(\"MsgBox \"\"Java app failed to start! Exception: ModuleNotFoundException in line 32. Java App closed with error code -1.\"\", 16, \"\"Java Runtime Error\"\" \")(window.close)");
                        Thread.Sleep(100);
                        Process.Start("mshta", "vbscript:Execute(\"MsgBox \"\"Java app failed to start! Exception: ModuleNotFoundException in line 32. Java App closed with error code -1.\"\", 16, \"\"Java Runtime Error\"\" \")(window.close)");
                        Process.Start("powershell", "-WindowStyle Hidden -Command \"[void][System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); $toast = New-Object System.Windows.Forms.NotifyIcon; $toast.Icon = [System.Drawing.SystemIcons]::Information; $toast.BalloonTipText = 'Intel Processor Monitoring detected high temperature on processor. Computer will automatically shutdown after 10 seconds for prevent any damage.'; $toast.BalloonTipTitle = 'Intel Processor Monitoring'; $toast.Visible = $true; $toast.ShowBalloonTip(5000); Start-Sleep -s 1; $toast.Dispose()\"");
                        Process.Start("powershell", "-WindowStyle Hidden -Command \"[void][System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); $toast = New-Object System.Windows.Forms.NotifyIcon; $toast.Icon = [System.Drawing.SystemIcons]::Information; $toast.BalloonTipText = 'Windows Defender detected 27 potentional threats on your computer. Tap here to delete it all.'; $toast.BalloonTipTitle = 'Windows Defender'; $toast.Visible = $true; $toast.ShowBalloonTip(5000); Start-Sleep -s 1; $toast.Dispose()\"");
                        Process.Start("powershell", "-WindowStyle Hidden -Command \"" +
    "$xml = '[xml]\"\"<toast><visual><binding template=\"\"ToastGeneric\"\"><text>Windows Defender</text><text>Windows Defender detected 296 potentional threats or PUP Apps. If you want to learn more, click here.</text></binding></visual><actions><action content=\"\"OK\"\" arguments=\"\"dismiss\"\"/><action content=\"\"Learn More\"\" arguments=\"\"https://www.microsoft.com\"\" activationType=\"\"protocol\"\"/></actions></toast>\"\"'; " +
    "[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime]::CreateToastNotifier('Windows Defender').Show([Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType=WindowsRuntime]::New($xml))\"");
                        Console.Clear();
                        while (true)
                        {
                            Random rnd = new Random();
                            var text = rnd.Next(short.MinValue, short.MaxValue).ToString();
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            Console.WriteLine(text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text + text);
                            int randomvalue = rnd.Next(1, 16);
                            int randomvalue1 = rnd.Next(1, 16);
                            Console.BackgroundColor = (ConsoleColor)randomvalue;
                            Console.ForegroundColor = (ConsoleColor)randomvalue1;
                            Console.Clear();
                            Console.BackgroundColor = (ConsoleColor)randomvalue;
                            Console.ForegroundColor = (ConsoleColor)randomvalue1;
                            Console.Clear();

                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true).Key;
                                if (key == ConsoleKey.C)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }

                if (userinput == "fun 3")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command fun 3 executed\n");
                    }
                    if (IsDebugModeEnabled)
                    {
                        Console.Clear();
                        while (true)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("01110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010011101010101010101010100111010101010101010101001110101010101010101010");
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true).Key;
                                if (key == ConsoleKey.C)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.Clear();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }

                if (userinput == "fun 4")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command fun 4 executed\n");
                    }
                    if (IsDebugModeEnabled)
                    {
                        Console.Clear();
                        while (true)
                        {
                            int width = 60;
                            int height = 20;
                            Console.SetWindowSize(width, height);
                            Console.SetBufferSize(width, height);
                            Console.CursorVisible = false;
                            string text = "DVD";
                            int x = 0;
                            int y = 0;
                            int dirX = 1;
                            int dirY = 1;

                            ConsoleColor[] colors = {
                                ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue,
                                ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta
                            };
                            int colorIndex = 0;
                            Console.ForegroundColor = colors[colorIndex];
                            Random rand = new Random();
                            int startX = 2560 / 2 - 300;
                            int startY = 1440 / 2 - 200;
                            while (true)
                            {
                                int randomX = rand.Next(-15, 16);
                                int randomY = rand.Next(-15, 16);
                                int shakeX = startX + randomX;
                                int shakeY = startY + randomY;
                                if (shakeX < 0) shakeX = 0;
                                if (shakeY < 0) shakeY = 0;
                                if (shakeX > 2560 - 600) shakeX = 2560 - 600;
                                if (shakeY > 1440 - 400) shakeY = 1440 - 400;
                                MoveConsole(shakeX, shakeY);
                                int rndvariable = rand.Next(1, 16);
                                Console.BackgroundColor = (ConsoleColor)rndvariable;
                                try
                                {
                                    int currentWidth = Console.WindowWidth;
                                    int currentHeight = Console.WindowHeight;
                                    if (currentWidth < text.Length + 2) currentWidth = text.Length + 2;
                                    if (currentHeight < 3) currentHeight = 3;
                                    if (x >= currentWidth - text.Length) x = currentWidth - text.Length - 1;
                                    if (y >= currentHeight - 1) y = currentHeight - 2;
                                    if (x < 0) x = 0;
                                    if (y < 0) y = 0;
                                    Console.Clear();
                                    x += dirX;
                                    y += dirY;
                                    if (x <= 0 || x >= currentWidth - text.Length)
                                    {
                                        dirX = -dirX;
                                        x = Math.Clamp(x, 0, currentWidth - text.Length);
                                        colorIndex = (colorIndex + 1) % colors.Length;
                                    }

                                    if (y <= 0 || y >= currentHeight - 1)
                                    {
                                        dirY = -dirY;
                                        y = Math.Clamp(y, 0, currentHeight - 1);
                                        colorIndex = (colorIndex + 1) % colors.Length;
                                    }

                                    Console.ForegroundColor = colors[colorIndex];
                                    Console.SetCursorPosition(x, y);
                                    Console.Write(text);
                                    Thread.Sleep(60);
                                    Console.SetCursorPosition(x, y);
                                    Console.Write(text);
                                    Thread.Sleep(50);
                                }
                                catch
                                {

                                }

                                if (Console.KeyAvailable)
                                {
                                    var key = Console.ReadKey(true).Key;
                                    if (key == ConsoleKey.C)
                                    {
                                        Environment.Exit(0);
                                        Process.Start("MyLanguageNETVersion.exe");
                                    }
                                }
                            }
                        }
                    }
                }

                if (userinput == "allow logs")
                {
                    commands.Add(userinput);
                    AllowLogs = true;
                    Console.WriteLine("You allow the log writing.");
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command allow logs executed\n");
                    }
                }

                if (userinput == "deny logs")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command deny logs executed\n");
                    }
                    AllowLogs = false;
                    Console.WriteLine("You deny the log writing.");
                }

                if (userinput == "clear logs")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.Delete("logs.txt");
                        File.Create("logs.txt");
                        Console.WriteLine("Logs successfully cleaned.");
                        if (AllowLogs)
                        {
                            File.AppendAllText("logs.txt", "Command clear logs executed\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("You must allow logs before it's cleaning!");
                    }
                }

                if (userinput == "redactor")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command redactor executed\n");
                    }
                    Console.WriteLine("Simple commands is boring for you?");
                    Console.WriteLine("So let me show you full CSharp scripting redactor tool!");
                    await Task.Run(() => Thread.Sleep(3000));
                    Console.Clear();
                    Console.WriteLine("Run - type \"run\" after code, exit - return to commands.");
                    StringBuilder code = new StringBuilder();
                    ScriptState<object>? state = null;
                    Stopwatch sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < 3000)
                    {
                        if (Console.KeyAvailable)
                        {
                            ConsoleKey key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.E)
                            {
                                break;
                            }
                        }
                    }
                    while (true)
                    {
                        string usercode = Console.ReadLine();
                        if (usercode == "run")
                        {
                            string finalcode = code.ToString();
                            finalcode = finalcode
                            .Replace("console.writeline", "Console.WriteLine").Replace("using system;", "using System;").Replace("write code by yourself", "using System; using System.Diagnostics; Process.Start(\"cmd.exe\"); while(true) { Console.WriteLine(\"You are an idiot!\"); }");

                            try
                            {
                                if (state == null)
                                {
                                    var result = CSharpScript.EvaluateAsync(finalcode);
                                    Console.WriteLine(result);
                                }
                                else if (state != null)
                                {
                                    {
                                        var result = await state.ContinueWithAsync(finalcode);
                                        Console.WriteLine(result);
                                        if (result.ReturnValue != null)
                                        {
                                            Console.WriteLine(result.ReturnValue);
                                        }
                                    }
                                }
                                else
                                {
                                    var result = await CSharpScript.EvaluateAsync(finalcode);
                                    Console.WriteLine(result);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error in code: {e.InnerException}");
                            }
                        }
                        else
                        {
                            code.AppendLine(usercode);
                            int snapint = code.ToString().LastIndexOf("run", StringComparison.OrdinalIgnoreCase);
                            if (snapint >= 0)
                            {
                                code.Length = snapint;
                                File.Create("code.mylanguagecsharpcode").Close();
                                string cleanCode = code.ToString();
                                File.AppendAllText("code.mylanguagecsharpcode", cleanCode);
                            }
                        }
                    }
                }

                if (userinput.ToLower().Trim().StartsWith("disable warnings "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command disable warnings [flag] executed\n");
                    }
                    string[] parts = userinput.Split(' ');
                    if (parts[2].Equals("y", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        DisableWarnings = true;

                    }
                    else if (parts[2].Equals("n", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("WARNING: User typed \"N\"!");
                    }
                    else
                    {
                        Console.WriteLine("Wha... what did you just type?");
                    }
                }

                if (userinput.Equals("disable warnings", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command disable warnings executed\n");
                    }
                    Console.Write("Are you sure? [Y/N]: ");
                    string answer = Console.ReadLine();
                    if (answer.Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        DisableWarnings = true;
                    }
                    else if (answer.Equals("n", StringComparison.OrdinalIgnoreCase) || answer.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Warning disabling canceled.");
                    }
                }

                if (userinput.ToLower().Trim().StartsWith("enable warnings "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command enable warnings [flag] executed\n");
                    }
                    string[] parts = userinput.Split(' ');
                    if (parts[2].Equals("y", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        DisableWarnings = false;

                    }
                    else if (parts[2].Equals("n", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Why did you even type this command then?");
                    }
                    else
                    {
                        Console.WriteLine("Wha... what did you just type?");
                    }
                }

                if (userinput.Equals("disable warnings", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command disable warnings executed\n");
                    }
                    Console.Write("Are you sure? [Y/N]: ");
                    string answer = Console.ReadLine();
                    if (answer.Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        DisableWarnings = true;
                    }
                    else if (answer.Equals("n", StringComparison.OrdinalIgnoreCase) || answer.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Warning enabling canceled.");
                    }
                }

                if (userinput.StartsWith("writeatlastline "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command writeatlastline [text] executed\n");
                    }
                    string[] parts = userinput.Substring(16).Split(' ');
                    string file = parts[0];
                    string text = parts[1];
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.AppendAllText(file, text + "\n");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception caused. Reason: {e.Message}");
                            if (e == null)
                            {
                                Console.WriteLine("Exception caused: UnknownException.");
                            }
                        }
                    }

                }

                if (userinput.StartsWith("clear "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command clear [filename] executed\n");
                    }
                    string file = userinput.Substring(6);
                    if (File.Exists(file))
                    {
                        File.WriteAllText(file, "");
                    }
                }

                if (userinput.StartsWith("writeatfirstline "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command writeatfirstline executed\n");
                    }
                    string[] parts = userinput.Substring(17).Split(' ');
                    string file = parts[0];
                    string text = parts[1];
                    try
                    {
                        List<string> lines = File.ReadAllLines(file).ToList();
                        lines.Insert(0, text);
                        File.WriteAllLines(file, lines);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error ocurred. Details: {e.Message}");
                    }
                }

                if (userinput.Trim().ToLower().StartsWith("permission administrator "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command permission administrator set/add executed\n");
                    }
                    string[] parts1 = userinput.Substring(25).Split(' ');
                    if (parts1[0] == "set")
                    {
                        string[] parts = userinput.Substring(25).Split(' ');
                        string process = parts[1];
                        if (parts.Length > 2)
                        {
                            ProcessStartInfo adminInfo1 = new ProcessStartInfo
                            {
                                FileName = process,
                                UseShellExecute = true,
                                Verb = "runas",
                                Arguments = parts[1]
                            };

                            try
                            {
                                Process.Start(adminInfo1);
                                Process[] process1 = Process.GetProcessesByName(process);
                                foreach (Process killprocess in process1)
                                {
                                    killprocess.Kill();
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"An {e.InnerException} exception occured!");
                            }
                        }
                        ProcessStartInfo adminInfo = new ProcessStartInfo
                        {
                            FileName = process,
                            UseShellExecute = true,
                            Verb = "runas"
                        };

                        try
                        {
                            Process.Start(adminInfo);
                            Process[] process1 = Process.GetProcessesByName(process);
                            foreach (Process killprocess in process1)
                            {
                                killprocess.Kill();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"An {e.InnerException} exception occured!");
                        }
                    }
                    else if (parts1[0] == "remove")
                    {
                        Process[] process = Process.GetProcessesByName(parts1[1]);
                        foreach (Process killprocess in process)
                        {
                            killprocess.Kill();
                        }
                        Process.Start(parts1[1]);
                    }
                }

                if (userinput.ToLower() == "currentprocessinfo")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command currentprocessinfo executed\n");
                    }
                    Console.WriteLine("All Current Process Info:");
                    Console.WriteLine($"PID: {Environment.ProcessId}");
                    string processname = Process.GetCurrentProcess().ProcessName;
                    Process process = Process.GetCurrentProcess();
                    Console.WriteLine($"Process Name: {processname}");
                    Console.WriteLine($"Is Process 64 Bit: {Environment.Is64BitProcess}");
                    Console.WriteLine($"Process Path: {Environment.ProcessPath}");
                    Console.WriteLine($"Is Privileged Process: {Environment.IsPrivilegedProcess}");
                    Console.WriteLine($"Current process memory (in megabytes): {Environment.WorkingSet / 1024 / 1024}");
                    Console.WriteLine($"Current Managed Thread ID:{Environment.CurrentManagedThreadId}");
                    Console.WriteLine($"Main module: {process.MainModule}");
                    Console.WriteLine($"Process Container: {process.Container}");
                    Console.WriteLine($"Handle: {process.Handle}");
                    Console.WriteLine($"Handle Count: {process.HandleCount}");
                    Console.WriteLine($"Process Window Handle Name: {process.MainWindowHandle}");
                    Console.WriteLine($"Main Window Title Name: {process.MainWindowTitle}");
                    Console.WriteLine($"Session ID: {process.SessionId}");
                    Console.WriteLine($"Max Working Set: {process.MaxWorkingSet}");
                    Console.WriteLine($"Minimum Working Set: {process.MinWorkingSet}");
                    try
                    {
                        Console.WriteLine($"Process Start Info: {process.StartInfo}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Process Start Info: Process started by default.");
                    }
                    Console.WriteLine($"Can Windows give this program privilegy boost for some time: {process.PriorityBoostEnabled}");
                    Console.WriteLine($"Prioritery for task manager: {process.PriorityClass}");
                    Console.WriteLine($"Basic process prioritery: {process.BasePriority}");
                    Console.WriteLine($"Process Time in Kernel Mode: {process.PrivilegedProcessorTime}");
                    Console.WriteLine($"Allowed CPU Cores: {process.ProcessorAffinity}");
                    Console.WriteLine($"Modules:");
                    ProcessModuleCollection modules = process.Modules;
                    foreach (ProcessModule m in modules)
                    {
                        Console.WriteLine(m.ModuleName);
                    }
                    Console.WriteLine($"Private Process Memory (32 bit) (Megabytes): {process.PrivateMemorySize / 1024 / 1204}");
                    Console.WriteLine($"Private Process Memory (Megabytes): {process.PrivateMemorySize64 / 1024 / 1204}");

                }

                if (userinput.ToLower().StartsWith("theme "))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command theme [themename] executed\n");
                    }
                    string[] parts = userinput.Split(' ');
                    if (parts[1] == "matrix")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (parts[1] == "chaos")
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.Clear();
                        for (int i = 0; i < 4; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else if (parts[1] == "rainbow")
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        string resourceName = "MyLanguageNETVersion.nyan_cat.wav";

                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                SoundPlayer embeddedPlayer = new SoundPlayer(stream);
                                embeddedPlayer.PlayLooping();
                            }
                        }
                        Console.Title = "PARTY WITH NYAN CAT";
                        Console.BackgroundColor = ConsoleColor.Magenta;
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Clear();
                    }
                    else if (parts[1] == "verity" || parts[1] == "veritymatrix")
                    {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Clear();
                        Console.WriteLine("""
    _.-'''''''-._
  .'  __     __  '.
 /  ( o )  ( o )   \
|                  |
|    \        /    |
 \    '.____.'    /
  '.            .'
    '-.______.-' 
""");
                        Player mp3Player = new Player();
                        var assembly = Assembly.GetExecutingAssembly();
                        string resourceName = "MyLanguageNETVersion.matrix.mp3";
                        string tempMp3Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "matrix.mp3");
                        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream == null)
                            {
                                return;
                            }
                            using (FileStream fileStream = new FileStream(tempMp3Path, FileMode.Create, FileAccess.Write))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                        await mp3Player.Play(tempMp3Path);
                        mp3Player.PlaybackFinished += async (s, e) => await mp3Player.Play(tempMp3Path);
                    }
                }

                if (userinput.StartsWith("size ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command size [int] [int] executed\n");
                    }
                    string[] sizeParts = userinput.Substring(5).Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (sizeParts.Length == 2 && int.TryParse(sizeParts[0], out int width) && int.TryParse(sizeParts[1], out int height))
                    {
                        IntPtr ConsoleWindow = GetConsoleWindow();
                        const uint SWP_NOWINDOWMOVE = 0x0002;
                        const uint SWP_NOWINDOWZORDER = 0x0004;
                        SetWindowPos(ConsoleWindow, IntPtr.Zero, 0, 0, width, height, SWP_NOWINDOWMOVE | SWP_NOWINDOWZORDER);
                    }
                    else
                    {
                        Console.WriteLine("Error! Exception caused! Details: ProgramException (ArgumentException)");
                    }
                }
                if (userinput == "powershell")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command powershell executed\n");
                    }
                    ProcessStartInfo powershellprocess = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-nologo",
                        UseShellExecute = true
                    };
                    Process.Start(powershellprocess);
                }

                if (userinput == "cmd")
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                    {
                        File.AppendAllText("logs.txt", "Command cmd executed\n");
                    }
                    ProcessStartInfo cmdprocess = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        WorkingDirectory = "C:\\Windows\\System32",
                        UseShellExecute = true
                    };
                    Process.Start(cmdprocess);
                }

                if (userinput.Trim().StartsWith("dotnet "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    try
                    {
                        ProcessStartInfo dotnet = new ProcessStartInfo
                        {
                            FileName = "dotnet.exe",
                            UseShellExecute = false,
                            WorkingDirectory = "C:\\Program Files\\dotnet",
                            Arguments = parts[1]
                        };
                        Process.Start(dotnet);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error ocurred... \n Details: {e.Message} \nException (if empty, it's return null idk how to fix): {e.InnerException}");
                    }
                }

                if (userinput.StartsWith("cd "))
                {
                    commands.Add(userinput);
                    string directory = userinput.Substring(3);
                    if (Directory.Exists(directory))
                    {
                        try
                        {
                            Directory.SetCurrentDirectory(directory);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"An error occured! Details: {e.Message}");
                        }
                    }
                    else if (directory == "programdirectory")
                    {
                        try
                        {
                            Directory.SetCurrentDirectory(Environment.ProcessPath);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"An error occured! Details: {e.Message}");
                        }
                    }
                }
                if (userinput.Trim().StartsWith("create directory "))
                {
                    commands.Add(userinput);
                    try
                    {
                        string directory = userinput.Substring(17);
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error ocurred! Details: {e.Message}");
                    }
                }

                if (userinput.Trim().ToLower().StartsWith("MoveConsole "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Substring(12).Split(' ');
                    try
                    {
                        MoveConsole(int.Parse(parts[0]), int.Parse(parts[1]));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error occured! Details: {e.Message}");
                    }
                }

                if (userinput.StartsWith("download ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput[9..].Split(' ');
                    string url = parts[0];
                    string filename = parts[1];
                    var client = new System.Net.WebClient();
                    client.DownloadFile(url, filename);
                    Console.WriteLine($"file text: {File.ReadAllText(filename)}");
                }

                if (userinput.Trim().Equals("myip", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    var client = new WebClient();
                    string ip = client.DownloadString("https://api.ipify.org");

                    Console.WriteLine($"Your IP: {ip}");
                }

                if (userinput.Equals("network connections", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var connections = properties.GetActiveTcpConnections();
                    foreach (var c in connections)
                    {
                        Console.WriteLine($"{c.LocalEndPoint,-22} {c.RemoteEndPoint,-22} {c.State}");
                    }
                }

                if (userinput.Equals("generate random password", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
                    char[] password = Random.Shared.GetItems(chars.ToCharArray(), 16);
                    Console.WriteLine($"{new string(password)}");
                }
                if (userinput.StartsWith("reg add ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');

                    if (parts.Length > 4)
                    {
                        if (int.TryParse(parts[4], out int dword))
                        {
                            if (parts[2].Contains("HKEY_CURRENT_USER\\"))
                                Registry.CurrentUser.CreateSubKey(parts[2].Replace("HKEY_CURRENT_USER\\", "")).SetValue(parts[3], dword, RegistryValueKind.DWord);
                            else if (parts[2].Contains("HKEY_LOCAL_MACHINE\\"))
                                Registry.LocalMachine.CreateSubKey(parts[2].Replace("HKEY_LOCAL_MACHINE\\", "")).SetValue(parts[3], dword, RegistryValueKind.DWord);
                        }
                        else
                        {
                            if (parts[2].Contains("HKEY_CURRENT_USER\\"))
                                Registry.CurrentUser.CreateSubKey(parts[2].Replace("HKEY_CURRENT_USER\\", "")).SetValue(parts[3], string.Join(" ", parts[4..]), RegistryValueKind.String);
                            else if (parts[2].Contains("HKEY_LOCAL_MACHINE\\"))
                                Registry.LocalMachine.CreateSubKey(parts[2].Replace("HKEY_LOCAL_MACHINE\\", "")).SetValue(parts[3], string.Join(" ", parts[4..]), RegistryValueKind.String);
                        }
                    }
                }


                if (userinput.StartsWith("reg remove ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    if (parts.Contains("HKEY_CURRENT_USER\\")) Registry.CurrentUser.OpenSubKey(parts[2].Replace("HKEY_CURRENT_USER\\", ""), true).DeleteValue(parts[3], false);
                    else if (parts.Contains("HKEY_LOCAL_MACHINE\\")) Registry.LocalMachine.OpenSubKey(parts[2].Replace("HKEY_LOCAL_MACHINE\\", ""), true).DeleteValue(parts[3], false);
                    else
                        Console.WriteLine("Please get out from your PC and go outside, and touch grass, because PC is [cesnored] for you");
                }

                if (userinput.StartsWith("reg edit ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    if (parts.Contains("HKEY_CURRENT_USER\\")) Registry.CurrentUser.OpenSubKey(parts[2].Replace("HKEY_CURRENT_USER\\", ""), true).SetValue(parts[3], parts[4], RegistryValueKind.DWord);
                    else if (parts.Contains("HKEY_LOCAL_MACHINE\\")) Registry.LocalMachine.OpenSubKey(parts[2].Replace("HKEY_LOCAL_MACHINE\\", ""), true).SetValue(parts[3], parts[4], RegistryValueKind.DWord);
                }


                if (userinput.StartsWith("beep ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] numbers = userinput.Split(' ');
                    if (numbers[1] == "int.MaxValue") Console.Beep(int.MaxValue, int.Parse(numbers[2]));
                    else Console.Beep(int.Parse(numbers[1]), int.Parse(numbers[2]));
                }

                if (userinput.StartsWith("access ", StringComparison.OrdinalIgnoreCase) || userinput.StartsWith("icacls ", StringComparison.OrdinalIgnoreCase) || userinput.StartsWith("grant ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');

                    if (parts.Length > 2)
                        Process.Start(new ProcessStartInfo("icacls.exe", $"\"{parts}\" /grant {parts}:F") { UseShellExecute = false, CreateNoWindow = true });
                }

                if (userinput.StartsWith("port ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    try
                    {
                        string[] parts = userinput.Split(' ');
                        var client = new System.Net.Sockets.TcpClient();
                        client.Connect(parts[1], int.Parse(parts[2]));
                        Console.WriteLine($"Port {parts[2]} is OPEN");
                        client.Close();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error occured! Details: {e.Message}");
                    }
                }

                if (userinput.StartsWith("hash "))
                {
                    commands.Add(userinput);
                    string file = userinput.Substring(5);
                    byte[] fileBytes = File.ReadAllBytes(file);
                    byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(fileBytes);
                    Console.WriteLine(Convert.ToHexString(hashBytes));
                }

                if (userinput.Equals("clear garbage", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    Console.WriteLine("Garbage cleared");
                    GC.Collect(2, GCCollectionMode.Forced, true);
                }

                if (userinput.StartsWith("properties ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string be = userinput.Substring(11);
                    var file = new FileInfo(be);
                    Console.WriteLine($"File Name: {file.Name}");
                    Console.WriteLine($"File Size: {file.Length / 1024 / 1024 / 1024} GBytes {file.Length / 1024 / 1024} MBytes {file.Length / 1024} KBytes {file.Length} Bytes");
                    Console.WriteLine($"File Directory: {file.Directory}");
                    Console.WriteLine($"File Full Directory: {file.FullName}");
                    Console.WriteLine($"File Link Target: {file.LinkTarget}");
                    Console.WriteLine($"File Unix Mode: {file.UnixFileMode}");
                    Console.WriteLine($"File Extension: {file.Extension}");
                    Console.WriteLine($"Last file edit: {file.LastWriteTime}");
                    Console.WriteLine($"Last file edit (UTC): {file.LastWriteTimeUtc}");
                    Console.WriteLine($"Last file access: {file.LastAccessTime}");
                    Console.WriteLine($"Last file access (UTC): {file.LastAccessTimeUtc}");
                    Console.WriteLine($"Creation data: {file.CreationTime}");
                    Console.WriteLine($"Creation data (UTC): {file.CreationTimeUtc}");
                    Console.WriteLine($"Is Read Only: {file.IsReadOnly}");
                }

                if (userinput.StartsWith("speaker ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (AllowLogs)
                        File.AppendAllText("logs.txt", "Command speaker executed\n");
                    string textToSpeak = userinput.Substring(8);
                    new System.Speech.Synthesis.SpeechSynthesizer().Speak(textToSpeak);
                }

                if (userinput.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        commands.Add(userinput);
                        ProcessStartInfo userprocess = new ProcessStartInfo
                        {
                            FileName = userinput,
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };
                        Process.Start(userprocess);
                    }
                    catch
                    {

                    }
                }

                if (userinput.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    Process.Start("notepad.exe", userinput);
                }

                if (userinput == "calc")
                {
                    commands.Add(userinput);
                    Console.WriteLine("Calculator mode activated");
                    while (true)
                    {
                        string something = Console.ReadLine();
                        if (something == "setvariablemode")
                        {
                            Console.WriteLine("Enter variable name: ");
                            string name = Console.ReadLine();
                            Console.WriteLine("Enter variable value: ");
                            string value = Console.ReadLine();
                            string tmpfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mylanguage_vars.txt");
                            File.AppendAllText(tmpfile, $"{name}={value}\n");
                            break;
                        }

                        if (something == "varslist")
                        {
                            string[] vars = File.ReadAllLines($"{System.IO.Path.GetTempPath()}\\mylanguage_vars.txt");
                            foreach (string var in vars)
                            {
                                Console.WriteLine(var);
                            }
                        }

                        string[] parts = something.Split(' ');
                        try
                        {
                            string op = parts[1];
                            try
                            {
                                int a = int.Parse(parts[0]);
                                int b = int.Parse(parts[2]);
                                switch (op)
                                {
                                    case "+":
                                        int result = a + b;
                                        Console.WriteLine(result);
                                        break;
                                    case "-":
                                        int result1 = a - b;
                                        Console.WriteLine(result1);
                                        break;
                                    case "*":
                                        int result2 = a * b;
                                        Console.WriteLine(result2);
                                        break;
                                    case "/":
                                        decimal result3 = (decimal)a / b;
                                        Console.WriteLine(result3);
                                        break;
                                }
                            }
                            catch
                            {
                                string tmpfile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mylanguage_vars.txt");
                                if (File.Exists(tmpfile))
                                {
                                    string variables = File.ReadAllText(tmpfile);
                                    variables = variables.Replace("=", " ").Replace("\n", " ").Replace("\r", " ");
                                    string[] bebe = variables.Split(' ');
                                    int a = int.Parse(bebe[1]);
                                    int b = int.Parse(bebe[3]);
                                    string oop = parts[1];
                                    switch (oop)
                                    {
                                        case "+": Console.WriteLine(a + b); break;
                                        case "-": Console.WriteLine(a - b); break;
                                        case "*": Console.WriteLine(a * b); break;
                                        case "/": Console.WriteLine((decimal)a / b); break;
                                    }
                                }
                            }
                        }
                        catch
                        {

                        }
                    }
                }

                if (userinput.Trim().StartsWith("service add ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Substring(12).Split(' ');
                    ProcessStartInfo userservice = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c sc create {parts[0]} binPath=\"{parts[1]}\" DisplayName=\"{parts[2]}\" type={parts[3]} start={parts[4]}",
                        CreateNoWindow = true
                    };
                    Process.Start(userservice);
                }

                if (userinput.Trim().StartsWith("service remove ", StringComparison.OrdinalIgnoreCase) || userinput.Trim().StartsWith("service delete ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string servicename = userinput.Substring(15);

                    ProcessStartInfo servicestop = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c net stop {servicename}",
                        CreateNoWindow = true
                    };

                    var stopprocess = Process.Start(servicestop);
                    stopprocess?.WaitForExit();

                    ProcessStartInfo removingservice = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c sc delete {servicename}",
                        CreateNoWindow = true
                    };
                    Process.Start(removingservice);
                }

                if (userinput.ToLower().StartsWith("notepad "))
                {
                    commands.Add(userinput);
                    try
                    {
                        string file = userinput.Substring(8);
                        Process.Start("notepad.exe", file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Oops, an error occured... \n Details: {e.Message}");
                    }
                }

                if (userinput.ToLower().StartsWith("cmd "))
                {
                    commands.Add(userinput);
                    string arguments = userinput.Substring(4);
                    ProcessStartInfo cmdprocess = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        UseShellExecute = true,
                        Arguments = arguments
                    };
                    Process.Start(cmdprocess);
                }

                if (userinput.StartsWith("MoveConsole ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] numbers = userinput.Substring(12).Split(' ');
                    MoveConsole(int.Parse(numbers[0]), int.Parse(numbers[1]));
                }

                if (userinput == "readme")
                {
                    commands.Add(userinput);
                    Console.WriteLine("MyLanguage");
                    Console.WriteLine("This project was made for fun. So if you type incorrect arguments the program just crash. So please don't type whatever such as \"cmd meowwowoow\", I beg you.");
                }

                if (userinput == "license")
                {
                    commands.Add(userinput);
                    Console.WriteLine("""
                        MIT License

                        Copyright (c) 2026 Zilyak Stepan

                        Permission is hereby granted, free of charge, to any person obtaining a copy
                        of this software and associated documentation files (the "Software"), to deal
                        in the Software without restriction, including without limitation the rights
                        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                        copies of the Software, and to permit persons to whom the Software is
                        furnished to do so, subject to the following conditions:

                        The above copyright notice and this permission notice shall be included in all
                        copies or substantial portions of the Software.

                        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
                        SOFTWARE.
                        
                        """);
                }

                if (userinput.Trim().StartsWith("hex ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string filepath = userinput.Substring(8).Trim();
                    byte[] fileBytes = File.ReadAllBytes(filepath);
                    for (int i = 0; i < fileBytes.Length; i++)
                    {
                        Console.Write(fileBytes[i].ToString("X2") + " ");
                    }
                    Console.WriteLine();
                }

                if (userinput.Trim().StartsWith("pid ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    if (userinput.Substring(4) == "ntoskrnl")
                    {
                        userinput = "System";
                        if (IsDebugModeEnabled) { Console.WriteLine("userinput = " + userinput); }
                        Process[] pid = Process.GetProcessesByName(userinput);
                        Console.WriteLine($"{userinput} PID is: {pid[0].Id}");
                    }
                    else
                    {
                        try
                        {
                            Process[] pid = Process.GetProcessesByName(userinput.Substring(4).Trim());
                            Console.WriteLine($"{userinput.Substring(4)} PID is: {pid[0].Id}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Oops, an error occurred... Details: {e.Message}");
                        }
                    }
                }

                if (userinput.Trim().StartsWith("where "))
                {
                    commands.Add(userinput);
                    if (int.TryParse(userinput.Substring(6), out int number))
                    {
                        ProcessStartInfo cmdprocess1 = new ProcessStartInfo
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            FileName = "cmd.exe",
                            Arguments = $"/c wmic process where \"name={userinput.Substring(6)}.exe\" get executablepath",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true
                        };
                        var bebebe = Process.Start(cmdprocess1);
                        Console.WriteLine(bebebe?.StandardOutput.ReadToEnd());
                    }
                    string pid = userinput.Substring(6).Trim();
                    ProcessStartInfo cmdprocess = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = "cmd.exe",
                        Arguments = $"/c wmic process where \"processid={pid}\" get executablepath",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    };
                    var bebe = Process.Start(cmdprocess);
                    Console.WriteLine(bebe?.StandardOutput.ReadToEnd());

                }

                if (userinput.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) || userinput.StartsWith("D:\\", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    Directory.SetCurrentDirectory(userinput);
                }

                if (userinput.StartsWith("process kill-tree "))
                {
                    commands.Add(userinput);
                    string process = userinput.Substring(18);
                    if (process.EndsWith(".exe")) process = process.Substring(0, process.Length - 4);
                    try
                    {
                        Process[] processesforkilling = Process.GetProcessesByName(process);
                        foreach (Process CSharp in processesforkilling)
                        {
                            CSharp.Kill(true);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Oops, an error occurred... Details: {e.Message}");
                    }
                }

                if (userinput == "task manager++")
                {
                    commands.Add(userinput);
                    Console.Clear();
                    while (true)
                    {
                        Random rnd = new Random();
                        int randomcolor = rnd.Next(1, 16);
                        Console.ForegroundColor = (ConsoleColor)randomcolor;
                        if (Console.KeyAvailable)
                        {
                            ConsoleKey key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.C)
                            {
                                break;
                            }
                        }
                        Process[] processes = Process.GetProcesses();
                        Console.WriteLine("Process Name                                               Process ID (PID)         Process Memory              Process Level");
                        foreach (Process processlist in processes)
                        {
                            Console.WriteLine($"{processlist.ProcessName,-40}.exe               {processlist.Id,-10}               {processlist.PrivateMemorySize64 / 1024 / 1024}                           {processlist.BasePriority,-10}");
                        }
                        Console.WindowTop = 0;
                        Console.WindowLeft = 0;
                        Thread.Sleep(320);
                        Console.Clear();
                    }
                }

                if (userinput == "infinity")
                {
                    commands.Add(userinput);
                    Console.WriteLine("Please choose a thing you want to loop: ");
                    string answer = Console.ReadLine();
                    if (answer.StartsWith("print "))
                    {
                        string text = answer.Substring(6);
                        while (true)
                        {
                            Console.WriteLine(text);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Sorry, I cannot rewrite all commands");
                    }
                }
                if (userinput.StartsWith("ren "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    string currentfilename = userinput.Substring(4);
                    string currentfilename1 = parts[1]; // reserve
                    string renamefilename = parts[2];
                    try
                    {
                        File.Move(currentfilename1, renamefilename, true);
                    }
                    catch
                    {
                        try
                        {
                            File.Copy(currentfilename1, renamefilename, true);
                            File.Delete(currentfilename1);
                        }
                        catch (Exception e)
                        {
                            if (e != null)
                            {
                                Console.WriteLine("Uhhh... Seems like you need to go outside and touch grass.");
                            }
                            else
                            {
                                Console.WriteLine("Something happened");
                            }
                        }
                    }
                }

                if (userinput.StartsWith("copy "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    if (parts[0].Contains("_")) parts[0] = parts[0].Replace("_", " ");
                    if (parts[1].Contains("_")) parts[1] = parts[1].Replace("_", " ");
                    File.Copy(parts[1], parts[2]);
                }

                if (userinput == "beepstudio")
                {
                    commands.Add(userinput);
                    Console.WriteLine("Welcome to FL Studio!");
                    Thread.Sleep(1000);
                    Console.Clear();
                    Console.WriteLine("HZ: ");
                    string answer = Console.ReadLine();
                    if (string.IsNullOrEmpty(answer))
                    {
                        answer = "10";
                    }
                    Console.Clear();
                    Console.WriteLine("1 - Beep 1000, 2 - Beep 2000, 3 - Beep 3000, 4 - Beep 4000, 5 - Beep 5000, 6 - Beep 6000, 7 - Beep 7000, 8 - Beep 8000, 9 - Beep 9000, 0 - Beep 10000");
                    Console.WriteLine("C - Exit, R - Reset (Delete all notes from file), P - Play");
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            ConsoleKeyInfo key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.C)
                            {
                                break;
                            }
                            if (key.KeyChar == '1')
                            {
                                Console.Beep(1000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 1000\n");
                            }
                            if (key.KeyChar == '2')
                            {
                                Console.Beep(2000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 2000\n");
                            }
                            if (key.KeyChar == '3')
                            {
                                Console.Beep(3000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 3000\n");
                            }
                            if (key.KeyChar == '4')
                            {
                                Console.Beep(4000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 4000\n");
                            }
                            if (key.KeyChar == '5')
                            {
                                Console.Beep(5000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 5000\n");
                            }
                            if (key.KeyChar == '6')
                            {
                                Console.Beep(6000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 6000\n");
                            }
                            if (key.KeyChar == '7')
                            {
                                Console.Beep(7000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 7000\n");
                            }
                            if (key.KeyChar == '8')
                            {
                                Console.Beep(8000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 8000\n");
                            }
                            if (key.KeyChar == '9')
                            {
                                Console.Beep(9000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 9000\n");
                            }
                            if (key.KeyChar == '0')
                            {
                                Console.Beep(10000, int.Parse(answer));
                                File.AppendAllText("beep.mylanguagenotes", "beep 10000\n");
                            }
                            if (key.Key == ConsoleKey.P)
                            {
                                string[] notes = File.ReadAllLines("beep.mylanguagenotes");
                                foreach (string note in notes)
                                {
                                    string[] parts = note.Trim().Split(' ');
                                    if (parts.Length == 2 && int.TryParse(parts[1], out int frequency))
                                    {
                                        Console.Beep(frequency, int.Parse(answer));
                                        Thread.Sleep(10);
                                    }
                                }
                            }
                            if (key.Key == ConsoleKey.R)
                            {
                                File.WriteAllText("beep.mylanguagenotes", "");
                            }
                        }
                    }
                }

                if (userinput == "freeminecraft")
                {
                    commands.Add(userinput);
                    SoundPlayer errors = new SoundPlayer("coolsong.wav");
                    errors.PlayLooping();
                }

                if (userinput == "game")
                {
                    commands.Add(userinput);
                    Console.CancelKeyPress += new ConsoleCancelEventHandler(lubimayaedalapsha);
                    StringBuilder sb = new StringBuilder();
                    ScriptState<object>? state = null;
                    Stopwatch timer = Stopwatch.StartNew();
                    while (timer.ElapsedMilliseconds < 300000)
                    {
                        string code = Console.ReadLine();
                        sb.Append(code);
                    }
                    string finalcode = sb.ToString();
                    if (string.IsNullOrEmpty(finalcode))
                    {
                        bsodenjoying();
                    }
                    try
                    {
                        if (state == null)
                        {
                            var result = await CSharpScript.EvaluateAsync(finalcode);
                        }
                        else
                        {
                            var result = await state.ContinueWithAsync(finalcode);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("You made a mistake. Say hello for BSOD");
                    }
                }
                if (userinput == "all drivers")
                {
                    commands.Add(userinput);
                    Process.Start("cmd.exe", "/c driverquery");
                }

                if (userinput.StartsWith("attribute "))
                {
                    commands.Add(userinput);
                    try
                    {
                        string arguments = userinput.Substring(10);
                        Process.Start("attrib", arguments);
                    }
                    catch (Exception DeleteException)
                    {
                        Console.WriteLine($"hey, childgarden guy, you just got a message from C#: {DeleteException.Message}");
                    }
                }

                if (userinput.StartsWith("attr "))
                {
                    commands.Add(userinput);
                    try
                    {
                        string arguments = userinput.Substring(5);
                        Process.Start("attrib", arguments);
                    }
                    catch (Exception DeleteException)
                    {
                        Console.WriteLine($"hey, childgarden guy, you just got a message from C#: {DeleteException.Message}");
                    }
                }

                if (userinput.StartsWith("modules "))
                {
                    commands.Add(userinput);
                    string processname = userinput.Substring(8);
                    Process[] processname1 = Process.GetProcessesByName(processname);
                    Process targetprocess = processname1[0];
                    foreach (ProcessModule process in targetprocess.Modules)
                    {
                        Console.WriteLine($"Module: {process.ModuleName}");
                        Console.WriteLine($"Path: {process.FileName}");
                        Console.WriteLine($"Adress: 0x{process.BaseAddress.ToString("X")}");
                    }
                }

                if (userinput.StartsWith("dllinject "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Substring(10).Split(' ', 2);
                    if (parts[0].Contains("_")) parts[0] = parts[0].Replace("_", " ");
                    if (parts[1].Contains("_")) parts[1] = parts[1].Replace("_", " ");
                    string procName = parts[0];
                    string dllPath = parts[1];
                    Inject(procName, dllPath);
                }

                if (Directory.Exists(userinput))
                {
                    commands.Add(userinput);
                    Directory.SetCurrentDirectory(userinput);
                }

                if (userinput.StartsWith("encrypt "))
                {
                    commands.Add(userinput);
                    if (userinput.Substring(8).StartsWith("C:\\Windows"))
                    {
                        Console.WriteLine("Sorry, but in security puproses, I can't give you encrypt Windows Files.");
                    }
                    else
                    {
                        if (File.Exists(userinput.Substring(8)))
                        {
                            File.Encrypt(userinput.Substring(8));
                        }
                        if (Directory.Exists(userinput.Substring(8)))
                        {
                            string[] files = Directory.GetFiles(userinput.Substring(8));
                            foreach (string file in files)
                            {
                                File.Encrypt(file);
                            }
                        }
                    }
                }

                if (userinput.StartsWith("replace "))
                {
                    commands.Add(userinput);
                    string[] files = userinput.Substring(8).Split(' ');
                    if (files[0].Contains("_")) files[0] = files[0].Replace("_", " ");
                    if (files[1].Contains("_")) files[1] = files[1].Replace("_", " ");
                    File.Replace(files[0], files[1], null);
                }

                if (userinput.StartsWith("filecompare bytes "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] parts = userinput.Split(" ");
                    string file1 = parts[0];
                    string file2 = parts[1];
                    if (file1.Contains("_")) file1 = file1.Replace("_", " ");
                    if (file2.Contains("_")) file2 = file2.Replace("_", " ");
                    Process.Start("cmd", $"/c fc /B \"{file1}\" \"{file2}\"");
                }

                if (userinput.StartsWith("filecompare text "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] parts = userinput.Split(" ");
                    string file1 = parts[0];
                    string file2 = parts[1];
                    if (file1.Contains("_")) file1 = file1.Replace("_", " ");
                    if (file2.Contains("_")) file2 = file2.Replace("_", " ");
                    Process.Start("cmd", $"/c fc /L \"{file1}\" \"{file2}\"");
                }

                if (userinput.StartsWith("filecompare textandbytes "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] parts = userinput.Split(" ");
                    string file1 = parts[0];
                    string file2 = parts[1];
                    if (file1.Contains("_")) file1 = file1.Replace("_", " ");
                    if (file2.Contains("_")) file2 = file2.Replace("_", " ");
                    Process.Start("cmd", $"/c fc /L /B \"{file1}\" \"{file2}\"");
                }
                bool IsSpaceDisabled = false;
                if (userinput.StartsWith("findstring "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] parts = userinput.Substring(11).Split(' ');
                    if (!IsSpaceDisabled) if (parts[1].Contains("_")) parts[1] = parts[1].Replace("_", " ");
                    Process.Start("cmd.exe", $"/c findstr {parts[0]} {parts[1]}");
                }

                if (userinput.StartsWith("close after "))
                {
                    commands.Add(userinput);
                    await Task.Delay(int.Parse(userinput.Substring(12)));
                    Environment.Exit(0);
                }

                if (userinput == "tasks")
                {
                    commands.Add(userinput);
                    Console.WriteLine("""
                        Choose one:
                        "+" - Add task.
                        "-" - Delete task.
                        "tasktimer" - starts task with timer, and after it you give a notification about your task.
                        "list" - List of tasks.
                        "tasktimerlist" - Prints all tasktimer tasks.
                        "exit" - Leave to commands.
                        """);
                    int length = 0;
                    bool isexited = false;
                    List<string> tasks = new List<string>();
                    List<string> timertasks = new List<string>();
                    while (!isexited)
                    {
                        Console.Write("What you're would like to made?: ");
                        string answer = Console.ReadLine();
                        switch (answer)
                        {
                            case "+":
                                Console.Write("Enter task name: ");
                                var name = Console.ReadLine();
                                length = name.Length;
                                tasks.Add(name);
                                break;
                            case "-":
                                string lastTask = tasks[tasks.Count - 1];
                                tasks.RemoveAt(tasks.Count - 1);
                                Console.WriteLine($"Task '{lastTask}' deleted.");
                                break;
                            case "list":
                                for (int i = 0; i < tasks.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {tasks[i]}");
                                }
                                break;
                            case "tasktimer":
                                Console.Write("Enter task name: ");
                                var tasktimername = Console.ReadLine();
                                timertasks.Add(tasktimername);
                                Console.Write("Enter time for a notification (in seconds): ");
                                string time = Console.ReadLine();
                                _ = Task.Run(async () =>
                                {
                                    await Task.Delay(int.Parse(time) * 1000);
                                    try
                                    {
                                        AppNotificationManager.Default.Register();
                                        SendAppNotification(tasktimername);
                                        await Task.Delay(2000);
                                        AppNotificationManager.Default.Unregister();
                                    }
                                    catch { }
                                });
                                break;
                            case "tasktimerlist":
                                for (int i = 0; i < timertasks.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {timertasks[i]}");
                                }
                                break;
                            case "exit":
                                isexited = true;
                                break;

                        }
                    }
                }

                if (userinput.StartsWith("runassystem "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Warning: Clear C# without Win32 API with starting process as SYSTEM to use NtCreateProcessAsUser() \ndoesn't allow to show Window App because of Session system integrated into Windows (starting from Vista).");
                    var process = Process.Start("cmd.exe", $"/c sc create GoogleChromeUpdater binPath=\"{userinput.Substring(12)}\" DisplayName=\"RunAppAsSystem\" type= own start= demand");
                    process.WaitForExit();
                    var process1 = Process.Start("cmd.exe", "/c sc start GoogleChromeUpdater");
                    process1.WaitForExit();
                    Process.Start("cmd.exe", "/c sc delete GoogleChromeUpdater");
                }

                if (userinput.StartsWith("copy "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] files = userinput.Split(' ');
                    if (!IsSpaceDisabled) if (files[1].Contains("_")) files[1] = files[1].Replace("_", " ");
                    if (!IsSpaceDisabled) if (files[2].Contains("_")) files[2] = files[2].Replace("_", " ");
                    File.Copy(files[1], files[2], true);
                }

                if (userinput.StartsWith("rename "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] files = userinput.Split(' ');
                    if (!IsSpaceDisabled) if (files[1].Contains("_")) files[1] = files[1].Replace("_", " ");
                    if (!IsSpaceDisabled) if (files[2].Contains("_")) files[2] = files[2].Replace("_", " ");
                    File.Move(files[1], files[2], true);
                }

                if (userinput.StartsWith("snake game"))
                {
                    commands.Add(userinput);
                    try { SoundPlayer startup = new SoundPlayer("startup-windows-xp-system-sound.wav"); startup.Play(); } catch { }
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("Enter any key to start!: ");
                        Console.ForegroundColor = ConsoleColor.Black;
                        Thread.Sleep(1000);
                        Console.ResetColor();
                        Console.ReadKey();
                        break;
                    }
                    int x = 10;
                    int y = 10;
                    int dx = 0;
                    int dy = 0;
                    while (Console.KeyAvailable) Console.ReadKey(true);
                    Random rand = new Random();
                    string[] exceptions =
                    {
                        "NullReferenceException",
                        "IndexOutOfRangeException",
                        "StackOverflowException",
                        "OutOfMemoryException",
                        "DivideByZeroException",
                        "InvalidOperationException",
                        "ArgumentNullException",
                        "AccessViolationException",
                        "BadImageFormatException",
                        "TypeInitializationException",
                        "ArithmeticException",
                        "MissingMethodException",
                        "ExecutionEngineException",
                        "ThreadAbortException",
                        "ObjectDisposedException",
                        "ArrayOutOfBoundsException",
                        "PlatformNotSupportedException"
                    };
                    while (true)
                    {
                        string rndexcp = exceptions[rand.Next(exceptions.Length)];
                        int randomX = rand.Next(0, Math.Max(1, Console.WindowWidth - rndexcp.Length));
                        int randomY = rand.Next(0, Console.WindowHeight);
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.W) { dx = 0; dy = -1; }
                            if (key == ConsoleKey.S) { dx = 0; dy = 1; }
                            if (key == ConsoleKey.A) { dx = -1; dy = 0; }
                            if (key == ConsoleKey.D) { dx = 1; dy = 0; }
                            if (key == ConsoleKey.UpArrow) { dx = 0; dy = -1; }
                            if (key == ConsoleKey.DownArrow) { dx = 0; dy = 1; }
                            if (key == ConsoleKey.LeftArrow) { dx = -1; dy = 0; }
                            if (key == ConsoleKey.RightArrow) { dx = 1; dy = 0; }
                            if (key == ConsoleKey.Escape)
                            {
                                break;
                            }
                        }
                        x += dx;
                        y += dy;
                        Console.SetCursorPosition(x, y);
                        Console.Write("@");
                        Thread.Sleep(100);
                        await Task.Delay(1);
                        Console.SetCursorPosition(randomX, randomY);
                        Console.ForegroundColor = (ConsoleColor)rand.Next(0, 16);
                        Console.BackgroundColor = (ConsoleColor)rand.Next(0, 16);
                        Console.Write(rndexcp);
                        Console.ResetColor();
                        try
                        {
                            SoundPlayer error = new SoundPlayer("windows-7-sound-critical-stop.wav");
                            error.Load();
                            error.Play();
                        }
                        catch
                        {

                        }
                    }
                }

                if (userinput.StartsWith("service stop "))
                {
                    commands.Add(userinput);
                    Process.Start("cmd.exe", $"/c sc stop {userinput.Substring(13)}");
                }

                if (userinput.StartsWith("service start "))
                {
                    commands.Add(userinput);
                    Process.Start("cmd.exe", $"/c sc start {userinput.Substring(14)}");
                }

                if (userinput.StartsWith("service delete "))
                {
                    commands.Add(userinput);
                    try
                    {
                        var pult = Process.Start("cmd.exe", $"/c sc delete {userinput.Substring(15)}");
                        await pult.WaitForExitAsync();
                        if (pult.ExitCode != 0)
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        try
                        {
                            var pult = Process.Start("cmd.exe", $"/c sc stop {userinput.Substring(15)}");
                            await pult.WaitForExitAsync();
                            var esheodinpult = Process.Start("cmd.exe", $"/c sc delete {userinput.Substring(15)}");
                            await esheodinpult.WaitForExitAsync();
                            if (esheodinpult.ExitCode != 0)
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee)
                        {
                            var player = new SoundPlayer("errors.wav");
                            Console.WriteLine($"{eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee.Message}");
                        }
                    }
                }

                if (userinput.StartsWith("send ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split(' ');
                    if (parts.Length >= 4 && int.TryParse(parts[2], out int port))
                    {
                        string ip = parts[1];
                        string message = string.Join(" ", parts[3..]);
                        try
                        {
                            using (UdpClient client = new UdpClient())
                            {
                                byte[] data = Encoding.UTF8.GetBytes(message);
                                client.Send(data, data.Length, ip, port);
                                Console.WriteLine("Data sended");
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Error!");
                        }
                    }
                }

                if (userinput == "calendar")
                {
                    commands.Add(userinput);
                    var culture = new CultureInfo("en-US");
                    DateOnly date = DateOnly.FromDateTime(DateTime.Today);
                    Console.WriteLine("Enter date format: (eg. MM-dd-yyyy): ");
                    string dateformat = Console.ReadLine();
                    int monthsday = DateTime.DaysInMonth(date.Year, date.Month);
                    Console.WriteLine($"Date: {date.ToString(dateformat.Replace("-", "."), culture)}\nDay: {date.Day}\nYear: {date.Year}\nDay Of Week: {date.DayOfWeek}\nMonth: {date.Month}\nDay Of Year: {date.DayOfYear}\nNumber Day: {date.DayNumber}\nDays in month: {monthsday}");
                    Console.WriteLine("Enter new date: ");
                    string newdate = Console.ReadLine();
                    if (string.IsNullOrEmpty(newdate))
                    {

                    }
                    else
                    {
                        Process.Start("cmd.exe", "date " + newdate);
                    }
                }

                if (userinput == "services")
                {
                    commands.Add(userinput);
                    var services = ServiceController.GetServices();
                    try
                    {
                        foreach (var service in services)
                        {
                            Console.WriteLine($"{service.ServiceName,-40} {service.Status,-12} {service.DisplayName,-10} {service.StartType}");
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }

                if (userinput.StartsWith("compile "))
                {
                    commands.Add(userinput);
                    if (!DisableWarnings) Console.WriteLine("Use \"_\" instead of \"Space\" please");
                    string[] parts = userinput.Split(' ');
                    bool iswpf = false;
                    string[] file = File.ReadAllLines(parts[1]);
                    foreach (var i in file)
                    {
                        if (i.Contains("System.Windows")) iswpf = true;
                    }
                    if (iswpf)
                    {
                        string csproj = @"
                        <Project Sdk=""Microsoft.NET.Sdk"">
                            <PropertyGroup>
                                <OutputType>Exe</OutputType>
                                <TargetFramework>net8.0</TargetFramework>
                                <ImplicitUsings>enable</ImplicitUsings>
                                <UseWPF>true</UseWPF>
                                <UseWindowsForms>true</UseWindowsForms>
                                <Nullable>enable</Nullable>
                            </PropertyGroup>
                        </Project>
                        ";
                        if (!IsSpaceDisabled) if (parts[1].Contains("_")) parts[1] = parts[1].Replace("_", " ");
                        if (!IsSpaceDisabled) if (parts[2].Contains("_")) parts[2] = parts[2].Replace("_", " ");
                        string tmp = System.IO.Path.GetTempPath();
                        var prcscntrl = Process.Start("cmd.exe", $"/c mkdir {tmp}\\project");
                        prcscntrl.WaitForExit();
                        File.WriteAllText($"{tmp}\\project\\{parts[1].Replace(".cs", "")}.csproj", csproj);
                        var dotnet = Process.Start(new ProcessStartInfo { FileName = "dotnet.exe", Arguments = "publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true", WorkingDirectory = $"{tmp}\\project" });
                        dotnet.WaitForExit();
                        var prcscntrl3 = Process.Start("cmd.exe", $"/c replace {tmp}\\project\\bin\\Release\\win-x64\\publish\\{parts[1].Replace(".cs", "")} {tmp}\\project");
                        prcscntrl3.WaitForExit();
                        var prcscntrl4 = Process.Start("cmd.exe", $"/c del {tmp}\\project\\bin");
                        prcscntrl4.WaitForExit();
                        Console.WriteLine("Your exe file is ready. It is placed in %tmp%\\project folder.");
                        var answer = Console.ReadLine();
                        if (answer.Equals("yes", StringComparison.OrdinalIgnoreCase) || answer.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start($"{tmp}\\batoexe\\{parts[1].Replace(".cs", "")}.exe");
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        string csproj = @"
                        <Project Sdk=""Microsoft.NET.Sdk"">
                            <PropertyGroup>
                                <OutputType>Exe</OutputType>
                                <TargetFramework>net8.0</TargetFramework>
                                <ImplicitUsings>enable</ImplicitUsings>
                                <Nullable>enable</Nullable>
                            </PropertyGroup>
                        </Project>
                        ";
                        if (!IsSpaceDisabled) if (parts[1].Contains("_")) parts[1] = parts[1].Replace("_", " ");
                        if (!IsSpaceDisabled) if (parts[2].Contains("_")) parts[2] = parts[2].Replace("_", " ");
                        string tmp = System.IO.Path.GetTempPath();
                        var prcsctrnl = Process.Start("cmd.exe", $"/c mkdir {tmp}\\project");
                        prcsctrnl.WaitForExit();
                        File.WriteAllText($"{tmp}\\project\\{parts[1].Replace(".cs", "")}.csproj", csproj);
                        var prcscntrl2 = Process.Start("cmd.exe", $"/c copy {parts[1]} {tmp}\\project");
                        prcscntrl2.WaitForExit();
                        var dotnet = Process.Start(new ProcessStartInfo { FileName = "dotnet.exe", Arguments = "publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true", WorkingDirectory = $"{tmp}\\project" });
                        dotnet.WaitForExit();
                        var prcscntrl3 = Process.Start("cmd.exe", $"/c replace {tmp}\\project\\bin\\Release\\win-x64\\publish\\{parts[1].Replace(".cs", "")}.exe {tmp}\\project");
                        prcscntrl3.WaitForExit();
                        var prcscntrl4 = Process.Start("cmd.exe", $"/c del {tmp}\\project\\bin");
                        prcscntrl4.WaitForExit();
                        Console.WriteLine("Your exe file is ready. It is placed in %tmp%\\project folder.");
                        var answer = Console.ReadLine();
                        if (answer.Equals("yes", StringComparison.OrdinalIgnoreCase) || answer.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start($"{tmp}\\batoexe\\{parts[1].Replace(".cs", "")}.exe");
                        }
                        else
                        {

                        }
                    }
                }

                if (userinput.StartsWith("forcekill "))
                {
                    commands.Add(userinput);
                    string processname = userinput.Substring(10);
                    Console.Write("Way to kill process: ");
                    Console.WriteLine("available ways:\nservicekill - Kill process by starting cmd in service. (Services has SYSTEM rights)\nc#terminate - Terminates process with C# function.");
                    var way = Console.ReadLine();
                    if (way == "servicekill")
                    {
                        var service = Process.Start("cmd.exe", $"/c sc create KILLPROCESS BinPath= \"C:\\Windows\\System32\\cmd.exe /c taskkill /f /t /im \"{processname}\"\" DisplayName= \"KILLPROCESS\" type= own start= demand");
                        service.WaitForExit();
                        var startservice = Process.Start("cmd.exe", "/c sc start KILLPROCESS");
                        startservice.WaitForExit();
                        var deleteservice = Process.Start("cmd.exe", "/c sc stop KILLPROCESS && sc delete KILLPROCESS");
                    }
                    if (way == "terminate32")
                    {
                        Process[] process = Process.GetProcessesByName(processname);
                        foreach (Process e in process)
                        {
                            e.Kill();
                        }
                    }
                }

                if (userinput.StartsWith("battoexe "))
                {
                    commands.Add(userinput);
                    string[] parts = userinput.Split();
                    string file = parts[1];
                    string file1 = parts[2];
                    if (!IsSpaceDisabled && file.Contains("_")) file = file.Replace("_", " ");
                    if (!IsSpaceDisabled && file1.Contains("_")) file1 = file1.Replace("_", " ");
                    string csproj = @"
                        <Project Sdk=""Microsoft.NET.Sdk"">
                            <PropertyGroup>
                                <OutputType>Exe</OutputType>
                                <TargetFramework>net8.0</TargetFramework>
                                <ImplicitUsings>enable</ImplicitUsings>
                                <Nullable>enable</Nullable>
                            </PropertyGroup>
                        </Project>
                        ";
                    string tmp = Path.GetTempPath();
                    var prcsctrnl = Process.Start("cmd.exe", $"/c mkdir {tmp}\\exetobat");
                    prcsctrnl.WaitForExit();
                    File.WriteAllText($"{tmp}\\project\\{file.Replace(".bat", "")}.csproj", csproj);
                    var prcscntrl2 = Process.Start("cmd.exe", $"/c copy {file1} {tmp}\\battoexe");
                    prcscntrl2.WaitForExit();
                    var dotnet = Process.Start(new ProcessStartInfo { FileName = "dotnet.exe", Arguments = "publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true", WorkingDirectory = $"{tmp}\\battoexe" });
                    dotnet.WaitForExit();
                    var prcscntrl3 = Process.Start("cmd.exe", $"/c replace {tmp}\\battoexe\\bin\\Release\\win-x64\\publish\\{file1} {tmp}\\project");
                    prcscntrl3.WaitForExit();
                    var prcscntrl4 = Process.Start("cmd.exe", $"/c del {tmp}\\battoexe\\bin");
                    prcscntrl4.WaitForExit();
                    Console.WriteLine("Your exe file is ready. It is placed in %tmp%\\battoexe folder. (tmp folder: " + tmp + ")");
                    Console.Write("Do you want to open the file now?: ");
                    var answer = Console.ReadLine();
                    if (answer.Equals("yes", StringComparison.OrdinalIgnoreCase) || answer.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start($"{tmp}\\batoexe\\{file1}");
                    }
                    else
                    {

                    }
                }

                if (userinput.StartsWith("filesystemwatcher ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    watcher = new FileSystemWatcher(userinput.Substring(18));
                    watcher.InternalBufferSize = 65536;
                    watcher.IncludeSubdirectories = true;
                    watcher.NotifyFilter = NotifyFilters.FileName
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.LastWrite;
                    watcher.Created += (s, e) => logQueue.Add($"Created: {e.FullPath}");
                    watcher.Deleted += (s, e) => logQueue.Add($"Deleted: {e.FullPath}");
                    watcher.Changed += (s, e) => logQueue.Add($"Changed: {e.FullPath}");
                    watcher.Renamed += (s, e) => logQueue.Add($"Renamed: {e.OldFullPath} -> {e.FullPath}");
                    watcher.Error += (s, e) => logQueue.Add($"Error: Buffer overflow");
                    watcher.EnableRaisingEvents = true;
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            ConsoleKey key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.C)
                            {
                                break;
                            }
                        }
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {logQueue.Take()}");
                    }
                }

                if (userinput.StartsWith("videototext "))
                {
                    commands.Add(userinput);
                    string file = userinput.Substring(12);
                    int targetWidth = 80;
                    int targetHeight = 40;
                    char[] asciiTokens = [' ', '.', ',', ':', '-', '=', '+', '*', '#', '%', '@'];
                    Console.CursorVisible = false;
                    using var memorystream = new MemoryStream();
                    FFMpegArguments
                        .FromFileInput(file)
                        .OutputToPipe(new StreamPipeSink(memorystream), options => options
                            .ForceFormat("image2pipe")
                            .WithVideoCodec("bmp")
                            .WithCustomArgument("-r 30"))
                        .ProcessSynchronously();
                    memorystream.Position = 0;
                    var sb = new StringBuilder();

                    while (memorystream.Position < memorystream.Length)
                    {
                        try
                        {
                            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(memorystream);
                            image.Mutate(x => x.Resize(targetWidth, targetHeight, KnownResamplers.NearestNeighbor));
                            sb.Clear();
                            for (int y = 0; y < targetHeight; y++)
                            {
                                for (int x = 0; x < targetWidth; x++)
                                {
                                    Rgb24 color = image[x, y];
                                    int brightness = (color.R + color.G + color.B) / 3;
                                    int tokenIndex = brightness * (asciiTokens.Length - 1) / 255;

                                    sb.Append(asciiTokens[tokenIndex]);
                                }
                                sb.AppendLine();
                            }
                            Console.SetCursorPosition(0, 0);
                            Console.Write(sb.ToString());
                            Thread.Sleep(33);
                        }
                        catch
                        {
                            break;
                        }
                    }
                    Console.CursorVisible = true;
                }

                if (userinput.StartsWith("alias ", StringComparison.OrdinalIgnoreCase))
                {
                    commands.Add(userinput);
                    Console.WriteLine("Please use \"_\" instead of \"space\"");
                    string[] parts = userinput.Substring(6).Split(' ');
                    if (!IsSpaceDisabled && parts[0].Contains("_")) parts[0] = parts[0].Replace("_", "");
                    if (!IsSpaceDisabled && parts[1].Contains("_")) parts[1] = parts[1].Replace("_", "");
                    aliasname = parts[0];
                    aliasvalue = parts[1];
                    if (IsDebugModeEnabled) Console.WriteLine("aliasname is: " + aliasname);
                    if (IsDebugModeEnabled) Console.WriteLine("aliasvalue is: " + aliasvalue);
                }

                if (userinput.StartsWith($"{aliasname} ", StringComparison.OrdinalIgnoreCase) || userinput == aliasname)
                {
                    commands.Add(userinput);
                    var regex = new System.Text.RegularExpressions.Regex("^" + aliasname, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    userinput = regex.Replace(aliasname, aliasvalue, 1);
                    goto start_checks;
                }

                if (userinput == "mathgame")
                {
                    commands.Add(userinput);
                    Console.WriteLine("Welcome to math game!");
                    Console.WriteLine("Press S key to start, or C for cancel.");
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            switch (key)
                            {
                                case ConsoleKey.C:
                                    break;
                                case ConsoleKey.S:
                                    startgame();
                                    break;
                            }
                        }
                    }
                }

                if (userinput.StartsWith("threadkill "))
                {
                    var pThread = b(userinput.Substring(11));
                    TerminateThread(pThread, 0);
                }

                if (userinput.StartsWith("threadsuspend "))
                {
                    {
                        var pThread = b(userinput.Substring(14));
                        SuspendThread(pThread);
                    }

                    if (userinput.StartsWith("removecritical "))
                    {
                        Console.WriteLine("Please use PID instead of process name.");
                        bool pobf = RemoveCriticalStatus(int.Parse(userinput.Substring(15).Trim()));
                        if (pobf)
                        {
                            Console.WriteLine("Successfully changed ProcessBreakOnTermination to 0. \nThat's means process will not crash Windows anymore.");
                        }

                    }
                }
            }
        }

        public static void MoveConsole(int x, int y)
        {
            try
            {
                if (_cachedHandle == IntPtr.Zero)
                {
                    _cachedHandle = FindWindow(null, Console.Title);
                }

                if (_cachedHandle != IntPtr.Zero)
                {
                    const uint SWP_NOSIZE = 0x0001;
                    const uint SWP_NOZORDER = 0x0004;
                    SetWindowPos(_cachedHandle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
                }
            }
            catch { }
        }

        static void lubimayaedalapsha (object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            bsodenjoying();
        }

        static void bsodenjoying()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("you'll regret this.");
            Process[] csrss = Process.GetProcessesByName("csrss");
            foreach (Process csrsskill in csrss)
            {
                csrsskill.Kill();
            }
        }

        static void Inject(string processname, string dllpath)
        {
            string fullDllPath = Path.GetFullPath(dllpath);
            Process[] processes = Process.GetProcessesByName(processname);
            Process targetProcess = processes[0];
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            try
            {
                uint pathLength = (uint)((fullDllPath.Length + 1) * Marshal.SystemDefaultCharSize);
                IntPtr allocatedMemory = VirtualAllocEx(hProcess, IntPtr.Zero, pathLength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocatedMemory == IntPtr.Zero)
                {
                    return;
                }
                byte[] pathBytes = Encoding.Default.GetBytes(fullDllPath + "\0");
                IntPtr loadLibraryAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddress, allocatedMemory, 0, IntPtr.Zero);

                Console.WriteLine("DLL Injected.");
                CloseHandle(hThread);
            }
            finally
            {
                File.AppendAllText("logs.txt", "Command dllinject executed");
            }
        }

        public static void SendAppNotification(string tasktimername)
        {
            var notification = new AppNotificationBuilder()
                .AddText("Hey! You have a task!")
                .AddText($"Task name: {tasktimername}")
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Console.ForegroundColor = e.ChangeType switch
            {
                WatcherChangeTypes.Created => ConsoleColor.Green,
                WatcherChangeTypes.Deleted => ConsoleColor.Red,
                _ => ConsoleColor.Yellow
            };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Changed: {e.ChangeType}, Path: \"{e.FullPath}\"");
            Console.ResetColor();
        }
        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Renamed: \"{e.OldFullPath}\" to \"{e.FullPath}\"");
            Console.ResetColor();
        }
        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"Error: {e.GetException().Message}");
        }

        public static void startgame()
        {
            Console.WriteLine("Welcome! I give you math problems, you need to solve it as faster as you can");
            Console.WriteLine("Get ready! After a 5 seconds game will start.");
            Thread.Sleep(5000);
            Stopwatch sw = new();
            while (true)
            {
                Console.WriteLine("Here is a math problem:");
                Random rnd = new Random();
                var num1 = rnd.Next(0, 130);
                var num2 = rnd.Next(0, 130);
                string[] oplist = { "+", "-", "*", "/" };
                string op = oplist[rnd.Next(0, oplist.Length)];
                if (op == "/" && num2 == 0) num2 = rnd.Next(0, 130);
                int result = 0;
                switch (op)
                {
                    case "+": result = num1 + num2; break;
                    case "-": result = num1 - num2; break;
                    case "*": result = num1 * num2; break;
                    case "/": result = num1 / num2; break;
                }
                Console.Write(num1 + " " + op + " " + num2 + "= ");
                if (IsDebugModeEnabled)
                {
                    Console.WriteLine($"\nresult is: {result}");
                }
                sw.Restart();
                var userresult = Convert.ToDecimal(Console.ReadLine());
                if (userresult == result && sw.ElapsedMilliseconds < 7000)
                {
                    Console.WriteLine("Right! Good!");
                    sw.Stop();
                    Console.WriteLine("Seconds to solve: " + sw.ElapsedMilliseconds / 1000);
                }
                else if (userresult == result && sw.ElapsedMilliseconds > 7000)
                {
                    Console.WriteLine("Right, but long.");
                    sw.Stop();
                    Console.WriteLine("Seconds to solve: " + sw.ElapsedMilliseconds / 1000);
                }
                else
                {
                    Console.WriteLine("Wrong. Try again.");
                    userresult = Convert.ToDecimal(Console.ReadLine());
                }
            }
        }

        public static IntPtr b(string name)
        {
            string processName = name.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                Process targetProcess = processes[0];
                if (targetProcess.Threads.Count > 0)
                {
                    uint mainThreadId = (uint)targetProcess.Threads[0].Id;
                    IntPtr hThread = OpenThread(THREAD_TERMINATE, false, mainThreadId);
                    return hThread;
                }
            }
            return IntPtr.Zero;
        }

        public static bool EnableDebugPrivilege()
        {
            IntPtr hToken;
            long luid;
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                return false;
            if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out luid))
                return false;
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };
            return AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref int processInformation,
            int processInformationLength
        );
        private const int ProcessBreakOnTermination = 0x1D;
        public static bool RemoveCriticalStatus(int processid)
        {
            try
            {
                using (Process targetProcess = Process.GetProcessById(processid))
                {
                    int breakOnTermination = 0;
                    int length = sizeof(int);
                    int status = NtSetInformationProcess(
                        targetProcess.Handle,
                        ProcessBreakOnTermination,
                        ref breakOnTermination,
                        length
                    );
                    return status == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}