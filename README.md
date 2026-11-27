**MyLanguage (or fully MyLanguageNETVersion)** is a Windows command-line interpreter and system utility written in C# (.NET 8.0). Built for working with the OS.

- **What can it do?**
Working with Registry Editor, shutdown or restart your PC, load or unload drivers, inject or unject dll from process and much more.
To see available commands, type help.

## All comamnds splited by categories

### Files & Disks
* `all files` - Show all files in current directory.
* `all drivers` - Print all drives.
* `cd [directoryname]` - Direct you to your directory.
* `C:\...` - Open a directory that starts with C:\.
* `D:\...` - Open a directory that starts with D:\.
* `create [filename.extension]` - Create file.
* `delete [filename.extension]` - Delete file.
* `copy [file1] [file2]` - Copy file to file.
* `copydrive [DiskLetter:\] [DiskLetter:\]` - Copy all source files to target device.
* `rename [file1] [file2]` - Renames file name to your file name.
* `read [filename.extension]` - Read file.
* `writeatfirstline [filename] [text]` - Write your text at first line in your requested file.
* `writeatlastline [filename] [text]` - Write your text at last line in your requested file.
* `findstr [string] [file]` - Finds a string in a file.
* `filecompare text [file1] [file2]` - Compares two files by text.
* `filecompare bytes [file1] [file2]` - Compares two files by bytes.
* `filecompare textandbytes [file1] [file2]` - Compares two files by text and bytes.
* `hash [filename]` - Types your file hash.
* `hex [filename]` - Read file by HEX way.
* `properties [filename]` - Show all available information available in C# Environment method.
* `attribute/attr [attributes] [file]` - Gives your attributes to your files.
* `access/grant/icacls [filename] [groupnameorusername]` - Give full rights for group/user.
* `whouses [pathtofile]` - Show you which processes are using file.
* `cleartemp/cleantempfolder/tempdel` - Clean %systemroot%\Temp folder and %TEMP% folder.
* `bootdata [index - optional]` - Types first 512 bytes of \\.\PhysicalDrive0 in console or types first 512 bytes of disk with other index.

### Processes, Threads & System
* `task manager` - Integrated task manager.
* `tasks` - Give you a task menu.
* `open process [processname]` - Create a process by its name.
* `[name].exe` - Start executable file by its name.
* `[name].txt` - Open text document file with Windows Notepad.
* `where [pidorprocessnamewithoutextension]` - Show you the process directory.
* `currentprocessinfo` - Prints all current process info available in Environment method.
* `kill process [processname]` - Kill a process by its name.
* `kill process-tree [processname]` - Kill original process and its children.
* `forcekill [processname]` - Force kills process by two different ways.
* `processsuspend [processname.exe]` - Suspends process.
* `processresume [processname.exe]` - Resumes process.
* `threadkill [processname.exe]` - Kills process thread.
* `threadsuspend [pid]` - Suspends process thread.
* `removecritical [process]` - Removes critical status from process.
* `administrator permission [set/remove] [appname]` - Set/Remove administrator rights for/from process.
* `runassystem [filepath]` - Run a process without window as NT AUTHORITY\SYSTEM user.
* `systeminfo` (or `pcinfo`) - Give you information about your PC.
* `cmd` - Open command prompt.
* `powershell` - Open powershell.
* `user add [username]` - Create a user in your OS.
* `user remove [username]` - Remove a user in your OS.
* `shutdown pc` - Shutdown your OS.
* `shutdown pc timer [timer]` - Shutdown your OS by the timer (seconds).
* `restart pc` - Restart your OS.
* `restart pc timer [timer]` - Restart your OS by the timer (seconds).

### Services, Registry & Drivers
* `service add [name] [path] [displayname] [service_type] [start_type]` - Create a service with your arguments.
* `service stop [servicename]` - Stop service by its name.
* `service remove [servicename]` - Removes service by its name.
* `service delete [servicename]` - Delete service by its name.
* `regedit add [path] [regszordwordname]` - Add a regedit value in whatever directory.
* `regedit edit [path] [regszdwordname]` - Edit a regedit value in whatever directory.
* `regedit remove [path] [whatevertype]` - Remove a regedit value in whatever directory.
* `driverload [path]` - Loads driver in Windows session.
* `driverunload [path]` - Unloads driver from Windows session.

### Reverse Engineering & Memory
* `csdecompiler [csbinary.exe] [outputfile.* - optional]` - Decompiles C# binary and types file text in console or writes result to target.
* `decompiler [binary.exe] [outputfile.* - optional]` - Decompiles binary and types Intel Asm instructions in console or writes result to target.
* `readprocessmemory [pid] [0xmemadr] [dwSize]` - Read process memory.
* `writeprocessmemory [procname] [hex_addr] [int]` - Write process memory.
* `findmemory [procname] [imagebaseadrr] [target] [bytes - optional]` - Finds target in process memory.
* `pebinfo [procname]` - Show you PEB info about process.
* `memory [procname]` - Show you how many bytes process allocate for itself.
* `dllinject [processname] [dlllibrary]` - Injects dll file to exe file.
* `dllunject [pid] [dllname]` - Unjects dll from process.
* `createdump [pid] [directory]` - Creates dump (.dmp) file about process to your directory.
* `dumpread [directory]` - Reads dump (.dmp) file.
* `processwatcher [program - optional]` - Watch all things made by all processes or watch all things made by your process.

### Network
* `myip` - Show your IP Address.
* `ping [dns.adress or domain]` - Pings domain or DNS Server.
* `network connections` - Show all network connections.
* `httpwatcher [program - optional]` - Watch all network requests or watch network requests by your process.
* `download [url] [filename]` - Download your file from requested URL.
* `send [ip] [port] [text]` - Connects to device with your IP Address and port, converts text into bytes, and sends it to your device.
* `firewall add [ip] [address]` - Adds a string in hosts file to redirect to your IP.
* `firewall delete [ip] [address]` - Removes a string in hosts file.

### Code & Scripting
* `redactor` - Give you C# redactor, where you can type C# code.
* `compile [name.cs] [namewithoutexeextension]` - Compiles .exe file using DOTNET and program file.
* `dotnet [arguments]` - Run a dotnet task with your arguments.
* `battoexe [batfile.bat] [exefile]` - Converts bat file to exe file.
* `execute [filename.mylangscript]` - Executes your script.
* `alias [aliasname] [commandname]` - Make an alias for any command.
* `converttoansi [stringorfile]` - Converts string or file to ANSI.
* `generate random password` - Generate random password.
* `clear garbage` - Call C# GC (Garbage Collector) Method and clear all garbage.

### Console & Utilities
* `print [text]` - Print text.
* `clear` - Clear console.
* `color [foreground or background] [colorname]` - Color your console.
* `theme [themename]` - Allow you to change console theme (matrix, rainbow, verity/veritymatrix).
* `size [int] [int]` - Size your console by coordinates.
* `MoveConsole [int] [int]` - Move your console by coordinates.
* `windowscreen [pname] [directory]` - Make a window screenshot and saves to your directory.
* `allow logs` - Allow logging executed commands.
* `deny logs` - Deny logging executed commands.
* `clear logs` - Clean logs file.
* `disable warnings` - Disable all program warnings.
* `disable warnings [flag, true or false or y or n]` - Disable/Enable all program warnings.
* `enable warnings` - Enable all warnings.
* `enable warnings [flag]` - Enable/Disable all program warnings.
* `info` - Prints info about this app.
* `license` - Show program license.
* `close after [timer]` - Close program after typed seconds.
* `calc` - Open calculator mode (also available when you start this program with -calcmode argument).
* `beep [int] [int]` - Play console beep sound.
* `speaker [text]` - Speak your text by standard Windows Voice.
* `msgbox [errortype] [title] | [text]` - Show MessageBox window with arguments.
* `videototext [file.mp4]` - Converts mp4 file to text and plays it in console.
* `snake game` - Console snake game.
* `game` - A C# redactor where empty input, invalid code, or closing triggers a BSOD.
##
- **Usage examples**
```
print hi
pid chrome
pebinfo chrome
readprocessmemory 1234 0x12341234 900
```
Shared with **License Apache 2.0**.
