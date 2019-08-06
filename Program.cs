using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;

namespace uso_cli
{
    class Program
    {
        
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
                ExecuteCommand(NextCommand()).Wait();
            }


            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Print(string line, ConsoleColor lineColor = ConsoleColor.White, string prefix = "", ConsoleColor prefixColor = ConsoleColor.White, bool clear = false)
        {
            if (clear) Console.Clear();

            if (prefix != "")
            {
                Console.ForegroundColor = prefixColor;
                Console.Write("[" + prefix + "] ");
            }

            Console.ForegroundColor = lineColor;
            Console.WriteLine(line);

        }

        public static void BootUp()
        {
            Console.Title = "Unturned Server Organiser CLI";
            Print("Unturned Server Organiser CLI", ConsoleColor.Green);
            Program.Print("Version hash: " + CalculateMD5(AppDomain.CurrentDomain.FriendlyName), ConsoleColor.DarkGray);

            if (!Environment.GetCommandLineArgs().Contains("noupdate"))
            {
                ProcessUpdate();
            }

            Console.ForegroundColor = ConsoleColor.White;
            CommandCollection.RegisterCommands();

            SetDefaultVars();

            Print("\n\nType 'help' to find a list of availble commands.", ConsoleColor.Gray);
        }


        public static void ProcessUpdate()
        {
            string currentFile = AppDomain.CurrentDomain.FriendlyName;
            if (currentFile.StartsWith("_"))
            {
                Program.Print("Updating...", ConsoleColor.DarkGray);
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
            VARS["enginePath"] = Environment.CurrentDirectory + "\\engine\\";
            VARS["serverid"] = "DefaultServer";
        }


        static string NextCommand()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("USO CLI> ");
            Console.ForegroundColor = ConsoleColor.White;
            return Console.ReadLine();
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
    }
}
