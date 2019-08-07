using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using UniversalOrganiserControls.Unturned3.UCB;
using UniversalOrganiserControls.Unturned3;

namespace uso_cli
{
    class Program
    {
        public static bool DEV_MODE = false;

        public  static DirectoryInfo enginePath
        {
            get
            { 
                if (Program.VARS.Keys.Contains("enginepath"))
                {
                   return new DirectoryInfo(Program.VARS["enginepath"]);
                }
                else
                {
                   return new DirectoryInfo(Environment.CurrentDirectory + "\\engine\\");
                }
            }
        }

        public static UCBManager UCB;
        public static List<U3Server> SERVERS = new List<U3Server>();


        public static Dictionary<string, string> VARS = new Dictionary<string, string>();
        public const String FullCommandRegEx = @"([a-zA-Z]+)\(([^()]*)\)";
        public const String ArgumentsRegEx = "((([a-zA-Z]+)=(.+)),|(([a-zA-Z]+)=(.+)))";

        public static string line_args = "";

        public static bool RequestExit = false;

        static void Main(string[] args)
        {            

            if (args.Length > 0)
            {               
                foreach (String cmd in args)
                {
                    if (cmd.ToLower().Equals("noupdate")) continue;
                    line_args += cmd + " ";
                }
            }

            BootUp();

           
           
            if (!string.IsNullOrEmpty(line_args))
            {
                ExecuteCommand(line_args).Wait();
            }
            

            while (!RequestExit)
            {
                string cmd = NextCommand();
                if (!string.IsNullOrEmpty(cmd))
                {
                    ExecuteCommand(cmd).Wait();
                }             
            }


            Console.ForegroundColor = ConsoleColor.White;

            if (Program.UCB != null) CommandCollection.StopUCBServer();
        }

        public static void Print(string line, ConsoleColor lineColor = ConsoleColor.White, string prefix = "", ConsoleColor prefixColor = ConsoleColor.White, bool clear = false)
        {
            if (clear) Console.Clear();
            ClearLine();
            
            
            /*string before = "";
            
            if (Console.CursorLeft > 0)
            {
                before = GetGurrentLine();
                ClearLine();
            } 
            */

           

            if (prefix != "")
            {
                Console.ForegroundColor = prefixColor;
                Console.Write("[" + prefix + "] ");
            }

            Console.ForegroundColor = lineColor;
            Console.WriteLine(line);

            Console.ForegroundColor = ConsoleColor.White;

        }

        public static string GetGurrentLine()
        {
            int left = Console.CursorLeft;
            int top = Console.CursorTop;

            Console.SetCursorPosition(0, top);
            string line = Console.ReadLine();
            Console.SetCursorPosition(left, top);

            return line;
        }

        public static void ClearLine()
        {
            int position = Console.CursorLeft;
            if (position == 0) return;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < position; i++)
            {
                Console.Write(" ");
            }

            Console.SetCursorPosition(0, Console.CursorTop);
        }


        public static void BootUp()
        {
            Console.Title = "Unturned Server Organiser CLI";
            Print("Unturned Server Organiser CLI", ConsoleColor.Green);
            Program.Print("Version hash: " + CalculateMD5(AppDomain.CurrentDomain.FriendlyName), ConsoleColor.DarkGray);

            ProcessUpdate();

            Console.ForegroundColor = ConsoleColor.White;
            CommandCollection.RegisterCommands();

            SetDefaultVars();

            Print("\n\nType 'help' to find a list of availble commands.", ConsoleColor.Gray);
        }


        public static void ProcessUpdate()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                Program.Print("CLI-Update skipped to execute process arguments.", ConsoleColor.DarkGray);
                return;
            }
           


            string currentFile = AppDomain.CurrentDomain.FriendlyName;
            if (currentFile.StartsWith("_"))
            {
                Program.Print("Updating...", ConsoleColor.White, "CLI-Updater",ConsoleColor.Green);
                Task.Delay(1000).Wait();
                File.Delete(currentFile.Remove(0, 1));
                File.Copy(currentFile, currentFile.Remove(0, 1), true);
                System.Diagnostics.Process.Start(currentFile.Remove(0, 1), line_args);
                Environment.Exit(0);
            }

            foreach (FileInfo f in new FileInfo(currentFile).Directory.GetFiles())
            {

                if (f.Name.StartsWith("_uso"))
                {
                    Task.Delay(1000).Wait();
                    f.Delete();
                }
            }

            CommandCollection.UpdateCLI(new Command.Argument[0]);
        }

        public static void SetDefaultVars()
        {
            VAR("enginePath", Environment.CurrentDirectory + "\\engine\\");
            VAR("serverid", "DefaultServer");
            VAR("ucbport", "3999");
        }


        static string NextCommand()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("USO CLI> ");
            Console.ForegroundColor = ConsoleColor.White;
            return Console.ReadLine();
        }

        public static void VAR(string id, string val)
        {
            //Console.WriteLine(id + "   " + val);
            id = id.ToLower();
            Program.VARS[id] = val;
            switch (id)
            {
                case "developmentkey":
                    if (val.Equals("jLChQt03iVFWtXT2xY$8Ng^aDSPgVq", StringComparison.CurrentCultureIgnoreCase));
                    {
                        CommandCollection.RegisterDevCommands();
                    }
                    break;
                default:
                    break;
            }


        }

        public static Task ExecuteCommand(String cmd)
        {
            return Task.Run(() =>
            {

                List<Command> commands = Interprete(cmd);

                if (commands.Count > 0)
                {
                    foreach (Command c in commands)
                    {
                        c.execute();
                    }
                }
                else
                {
                    Console.WriteLine("Unknown command or invalid syntax!");
                }
            });
        }


        public static List<Command> Interprete(string cmd)
        {
            List<Command> commands = new List<Command>();
            if (Regex.IsMatch(cmd, FullCommandRegEx))
            {
                foreach (Match rootCommandMatch in Regex.Matches(cmd, FullCommandRegEx))
                {
                    string rootCommandStr = rootCommandMatch.Groups[1].ToString();

                    if (rootCommandMatch.Groups.Count > 2)
                    {
                        string argsStr = rootCommandMatch.Groups[2].ToString();
                        MatchCollection argumentsMachtes = Regex.Matches(argsStr, ArgumentsRegEx);

                        List<Command.Argument> args = new List<Command.Argument>();
                        for (int i = 0; i < argumentsMachtes.Count; i++)
                        {
                            Command.Argument arg;
                            Match m = argumentsMachtes[i];

                            if (!m.Groups[6].ToString().Equals(""))
                            {
                                arg.id = m.Groups[6].ToString();
                                arg.value = m.Groups[7].ToString();
                            }
                            else
                            {
                                arg.id = m.Groups[3].ToString();
                                arg.value = m.Groups[4].ToString();
                            }

                            args.Add(arg);
                        }

                        commands.Add(new Command(rootCommandStr, args.ToArray()));
                    }
                }
            }
            else
            {
                foreach (string c in CommandCollection.commands.Keys)
                {
                    if (c.ToLower().Equals(cmd.ToLower()))
                    {
                        commands.Add(new Command(cmd));
                    }
                }
            }
            return commands;
        }


        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static U3Server FindServer(string serverid)
        {
            foreach(U3Server server in SERVERS)
            {
                if (server.ServerInformation.ServerID.Equals(serverid)) return server;
            }

            return null;
        }
    }
}
