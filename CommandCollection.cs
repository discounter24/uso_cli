using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalOrganiserControls.Unturned3.Installer;
using System.IO;
using UniversalOrganiserControls.Unturned3;
using UniversalOrganiserControls.Unturned3.UCB;
using UniversalOrganiserControls.Unturned3.RocketMod;
using UniversalOrganiserControls.Unturned3.Workshop;
using System.Net;

namespace uso_cli
{
    public static partial class CommandCollection
    {
        public static Dictionary<string, CommandDelegation> commands = new Dictionary<string, CommandDelegation>();
        public static Dictionary<string, string> commandDescriptions = new Dictionary<string, string>();
        public static Dictionary<string, string> commandUsages = new Dictionary<string, string>();

        public delegate void CommandDelegation(Command.Argument[] args);


        public static void RegisterCommands()
        {
            RegisterCommand("help", Help, "A help command.", "", "?");
            RegisterCommand("close", Close, "Closes the CLI tool.", "", "exit");
            RegisterCommand("ping", Ping, "Playing ping pong.");

            RegisterCommand("varlist", ListVars, "Shows a list of current registred variables.", "", "listvar");
            RegisterCommand("varset", SetVar, "Sets a variable used by the CLI.", "varset(id=value)", "setvar");
            RegisterCommand("updateengine", UpdateEngine, "Updates the Unturned server files", "updateEngine(validate)", "updateserver");
            RegisterCommand("runserver", RunServer, "Launches an Unturned server", "runserver(serverid)", "startserver");
            RegisterCommand("updaterocketmod", InstallRocketMod, "Installs or updates RocketMod.", "updaterocketmod(validate)", "installrocketmod");

            RegisterCommand("updatecli", UpdateCLI, "Updates this tool", "");

            RegisterCommand("addmod", AddWorkshopMod,"Installs a steam workshop mod","addmod(serverid,modid OR modlink)");
            RegisterCommand("removemod", RemoveWorkshopMod, "Removes a steam workshop mod", "removemod(serverid, modid)");
            RegisterCommand("listmods", ListWorkshopMods, "Lists all installed steam workshop mods", "listmods(serverid)", "modlist");
            RegisterCommand("script", executeScript, "Runs all commands defined in a script file", "script(file)");
        }

        private static string GetArgument(string id, Command.Argument[] args)
        {
            foreach(Command.Argument arg in args)
            {
               
                if (arg.id.ToLower() == id.ToLower())
                {
                    Program.VARS[arg.id] = arg.value;
                    return arg.value;
                }
            }


            if (Program.VARS.ContainsKey(id)) return Program.VARS[id];

            return null;
        }


        public static void RegisterCommand(string command, CommandDelegation del, string description = "", string usage = "", params string[] aliases)
        {
            commands.Add(command, del);
            commandDescriptions.Add(command, description);
            commandUsages.Add(command, usage);

            foreach (string alias in aliases)
            {
                commands.Add(alias, del);
                commandUsages.Add(alias, usage);
                commandDescriptions.Add(alias, description);
            }
        }


        public static void Close(Command.Argument[] args)
        {
            Program.RequestExit = true;
        }

        public static void Ping(Command.Argument[] args)
        {
            uint count = 1;
            foreach(Command.Argument arg in args)
            {
                if (arg.id.Equals("count"))
                {
                    try
                    {
                        count = Convert.ToUInt32(arg.value);
                    }
                    catch (Exception) { }
                }
            }
            for(int i = 0; i < count; i++)
            {
                Console.WriteLine("Pong!");
            }
        }


        public static void UpdateEngine(Command.Argument[] args)
        {

            DirectoryInfo enginePath;
            enginePath = new DirectoryInfo(GetArgument("enginePath", args));


            U3OnlineInstaller installer = new U3OnlineInstaller(enginePath);
            foreach(Command.Argument arg in args)
            {
                if (arg.id.Equals("validate"))
                {
                    installer.Validate = Convert.ToBoolean(arg.value);
                }
                else if (arg.id.Equals("fresh"))
                {
                    installer.FreshInstall = Convert.ToBoolean(arg.value);
                }
                else if (arg.id.Equals("keepServers"))
                {
                   installer.KeepServersOnFreshInstall = Convert.ToBoolean(arg.value);
                }
            }
            installer.InstallationProgressChanged += Installer_InstallationProgressChanged;
            installer.UpdateInterval = 1000;

            installer.Update().Wait();
        }

        private static void Installer_InstallationProgressChanged(object sender, UniversalOrganiserControls.Unturned3.U3OnlineInstallationProgressArgs e)
        {
            switch (e.state)
            {
                case UniversalOrganiserControls.Unturned3.U3InstallationState.SearchingUpdates:
                    Program.Print(string.Format("Searching for updates.."),ConsoleColor.White,"U3Installer",ConsoleColor.Cyan,true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.DeletingOldFiles:
                    Program.Print(string.Format("Deleting old files.."), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.CalculatingFileDifferences:
                    Program.Print(string.Format("Calculating differences.."), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.Downloading:
                    Program.Print(string.Format("Downloading {0}% ..", e.percentage), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.Ok:
                    Program.Print(string.Format("OK"), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.FailedSome:
                    Program.Print(string.Format("Failed: Not able to update all files"), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.FailedInternet:
                    Program.Print(string.Format("Failed: internet"), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.FailedUnknown:
                    Program.Print(string.Format("Failed unknown"), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.FailedInvalidResponse:
                    Program.Print(string.Format("Failed: invalid response"), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.PausedServerBusy:
                    Program.Print(string.Format("Paused while server is busy.."), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                case UniversalOrganiserControls.Unturned3.U3InstallationState.AbortedByCall:
                    Program.Print(string.Format("Aborted by call."), ConsoleColor.White, "U3Installer", ConsoleColor.Cyan, true);
                    break;
                default:
                    break;
            }
        }

        public static void RunServer(Command.Argument[] args)
        {
            string sid = GetArgument("serverid", args);
            
            U3ServerEngineSettings settings = new U3ServerEngineSettings(new FileInfo(Program.enginePath.FullName + "\\Unturned.exe"),sid);
            U3Server server = new U3Server(settings);

            Program.Print(string.Format("Starting {0} ..", sid), ConsoleColor.Green);
            U3ServerStartResult result = server.Start();
            Console.WriteLine("ServerStartResponse: " + result.ToString());
        }


        public static void SetVar(Command.Argument[] args)
        {
            foreach(Command.Argument arg in args)
            {
                Program.VARS[arg.id.ToLower()] = arg.value;
                Console.WriteLine("set variable " + arg.id + " to " + arg.value);
            }
        }

        public static void ListVars(Command.Argument[] args)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            foreach (string id in Program.VARS.Keys)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("ID: " + id + "    ");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Value: " + Program.VARS[id]);
                Console.WriteLine("");

            }
            Console.ForegroundColor = ConsoleColor.White;
        }



        public static void Help(Command.Argument[] args)
        {
            Program.Print("Commands have the following syntax: cmd(argumentId=argumentValue)", ConsoleColor.Yellow);

            Program.Print("Here is a list of available commands:",ConsoleColor.Yellow);
            foreach (string command in commands.Keys)
            {
                if (commandDescriptions.ContainsKey(command))
                {
                    Program.Print(string.Format("Description: {0} Usage: {1}", commandDescriptions[command], commandUsages[command] == "" ? command : commandUsages[command]), ConsoleColor.White, command, ConsoleColor.Cyan);
                }
                else
                {
                    Program.Print("(no description available)", ConsoleColor.White, command, ConsoleColor.Cyan);
                }
            }
        }


        public static void InstallRocketMod(Command.Argument[] args)
        {
            bool validate = false;
            foreach (Command.Argument arg in args)
            {
                if (arg.id.ToLower().Equals("validate")) validate = Convert.ToBoolean(arg.value.ToLower());
            }

            RocketModInstaller installer = new RocketModInstaller(new DirectoryInfo(Program.VARS["enginePath"]));

            Program.Print("Searching for RocketMod Updates..", ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);
            string serverVersion = installer.GetServerVersion().Result;
            bool updateAvail = installer.IsUpdateAvailable().Result;

            Program.Print("Current RocketMod Version: " + installer.LocalVersion, ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);
            Program.Print("Newest RocketMod Version: " + serverVersion, ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);

            if (updateAvail | validate)
            {
                Program.Print("Updating RocketMod.." + installer.LocalVersion, ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);
                RocketModInstallationCompletedType r = installer.Install(validate).Result;
                Program.Print("Update finished: " + r.ToString(), ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);

            }
            else
            {
                Program.Print("No updates found.", ConsoleColor.White, "RocketMod", ConsoleColor.Cyan);
            }          
        }


        public static void UpdateCLI(Command.Argument[] args)
        {
            string downloadUrl = "http://update.unturned-server-organiser.com/usocli.exe";
            string versionUrl = "http://update.unturned-server-organiser.com/CLIVersion.php";

            WebClient client = new WebClient();

            Program.Print("Searching for updates..", ConsoleColor.White, "CLI-Updater", ConsoleColor.Green);
            string newest_hash = client.DownloadString(new Uri(versionUrl));
            string current_hash = Program.CalculateMD5(AppDomain.CurrentDomain.FriendlyName);


            if (!current_hash.Equals(newest_hash))
            {
                Program.Print("Downloading update...", ConsoleColor.White, "CLI-Updater", ConsoleColor.Green);
                client.DownloadFile(new Uri(downloadUrl), Environment.CurrentDirectory + "\\_usocli.exe");
                Program.Print("Download complete!", ConsoleColor.White, "CLI-Updater", ConsoleColor.Green);
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\_usocli.exe");
                Environment.Exit(0);
            }
            else
            {
                Program.Print("No updates found.", ConsoleColor.White, "CLI-Updater",ConsoleColor.Green);
            }
        }


        public static void AddWorkshopMod(Command.Argument[] args)
        {
            string sid = GetArgument("serverid", args);
            string modid = null;
            string modlink = null;

            foreach(Command.Argument arg in args)
            {
                if (arg.id == "modid") modid = arg.value;
                if (arg.id == "modlink") modlink = arg.value;
            }

            if (string.IsNullOrEmpty(sid))
            {
                Program.Print("Please pass the command argument \"serverid\".");
                return;
            }

      
            U3Server server = new U3Server(new U3ServerEngineSettings(new FileInfo(Program.enginePath.FullName + "\\Unturned.exe"), sid));


            if (string.IsNullOrEmpty(modid) && string.IsNullOrEmpty(modlink))
            {
                Program.Print("Please pass the command argument \"modid\" or \"modlink\".",ConsoleColor.Yellow,"SteamWorkshop",ConsoleColor.Cyan);
                return;
            }
            else
            {
                List<string> ids = new List<string>();
                if (!string.IsNullOrEmpty(modlink))
                {

                    ids.AddRange(U3WorkshopMod.getIDFromWorkshopSite(modlink));
                }

                if (!string.IsNullOrEmpty(modid))
                {
                    if (!ids.Contains(modid)) ids.Add(modid);
                }

                foreach (string id in ids)
                {
                    server.WorkshopAutoUpdaterConfig.Add(id);
                }
                
            }
            Program.Print("Mod(s) added!", ConsoleColor.Green, "SteamWorkshop", ConsoleColor.Cyan);
        }


        public static void RemoveWorkshopMod(Command.Argument[] args)
        {
            string sid = GetArgument("serverid",args);
            string modid = null;
            string modlink = null;

            foreach (Command.Argument arg in args)
            {
                if (arg.id == "modid") modid = arg.value;
                if (arg.id == "modlink") modlink = arg.value;
            }

            if (string.IsNullOrEmpty(sid))
            {
                Program.Print("Please pass the command argument \"serverid\".");
                return;
            }


            U3Server server = new U3Server(new U3ServerEngineSettings(new FileInfo(Program.enginePath.FullName + "\\Unturned.exe"), sid));


            if (string.IsNullOrEmpty(modid) && string.IsNullOrEmpty(modlink))
            {
                Program.Print("Please pass the command argument \"modid\" or \"modlink\".", ConsoleColor.Yellow, "SteamWorkshop", ConsoleColor.Cyan);
                return;
            }
            else
            {
                List<string> ids = new List<string>();
                if (!string.IsNullOrEmpty(modlink))
                {

                    ids.AddRange(U3WorkshopMod.getIDFromWorkshopSite(modlink));
                }

                if (!string.IsNullOrEmpty(modid))
                {
                    if (!ids.Contains(modid)) ids.Add(modid);
                }

                foreach (string id in ids)
                {
                    server.WorkshopAutoUpdaterConfig.Remove(id,true);
                }

            }
            Program.Print("Mod(s) removed!", ConsoleColor.Green, "SteamWorkshop", ConsoleColor.Cyan);
        }

        public static void ListWorkshopMods(Command.Argument[] args)
        {
            string sid = GetArgument("serverid", args);

            if (string.IsNullOrEmpty(sid))
            {
                Program.Print("Please pass the command argument \"serverid\".");
                return;
            }


            U3Server server = new U3Server(new U3ServerEngineSettings(new FileInfo(Program.enginePath.FullName + "\\Unturned.exe"), sid));

            Program.Print(string.Format("Searching mods for {0} ..",sid), ConsoleColor.White, "SteamWorkshop", ConsoleColor.Cyan);


            string output = string.Format("{0} mod(s) registered in UpdaterConfig.\n", server.WorkshopAutoUpdaterConfig.RegistredMods.Count);
            output += string.Format("{0} mod(s) installed.\n", server.WorkshopAutoUpdaterConfig.InstalledMods.ToArray().Length);

            output += "Registered in UpdaterConfig: {";
            string title;
            foreach(string modid in server.WorkshopAutoUpdaterConfig.RegistredMods)
            {
                title = U3WorkshopMod.getModTitle(modid);
                output += !string.IsNullOrEmpty(title) ? string.Format("{0}({1}),", modid, title) : modid + ",";
            }
            if (output.EndsWith(",")) output = output.Remove(output.Length - 1,1);
            output += "}\n";

            output += "Installed maps: {";
            foreach (U3WorkshopMod mod in server.WorkshopAutoUpdaterConfig.InstalledMapMods)
            {
                output += !string.IsNullOrEmpty(mod.Title) ? string.Format("{0}({1}),", mod.ID, mod.Title) + "," : mod.ID + ",";
            }
            if (output.EndsWith(",")) output = output.Remove(output.Length - 1, 1);
            output += "}\n";

            output += "Installed object mods: {";
            foreach (U3WorkshopMod mod in server.WorkshopAutoUpdaterConfig.InstalledContentMods)
            {
                output += !string.IsNullOrEmpty(mod.Title) ? string.Format("{0}({1}),",mod.ID,mod.Title) : mod.ID + ",";
            }
            if (output.EndsWith(",")) output = output.Remove(output.Length - 1, 1);
            output += "}\n";

            Program.Print(output, ConsoleColor.White);
                 
        }


        public static void executeScript(Command.Argument[] args)
        {
            string scriptFilePath = GetArgument("file",args);
            if (string.IsNullOrEmpty(scriptFilePath))
            {
                Program.Print("Please pass the argument \"file\"", ConsoleColor.Red);
                return;
            }
            FileInfo scriptFile = new FileInfo(scriptFilePath);
            if (scriptFile.Exists)
            {
                Program.Print("Executing script..", ConsoleColor.DarkGray);
                string[] commands = File.ReadAllLines(scriptFilePath);
                foreach(string command in commands)
                {
                    Program.Print(command, ConsoleColor.White, "Script", ConsoleColor.Cyan);
                    Program.ExecuteCommand(command).Wait();
                }
            }
            else
            {
                Program.Print("Script file not found!", ConsoleColor.Red);
            }

        }


    }
}
