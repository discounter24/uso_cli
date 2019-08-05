using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uso_cli
{
    public class Command
    {
        public string RootCommand;
        public Argument[] Args; 

        public Command(string main, params Argument[] args)
        {
            this.RootCommand = main;
            this.Args = args;
        }



        public void execute()
        {
            CommandCollection.CommandDelegation delegation;
            if (CommandCollection.commands.TryGetValue(RootCommand.ToLower(), out delegation))
            {
                delegation(Args);
            }
            else
            {
                Console.WriteLine(string.Format("Unknown command: {0}",RootCommand));
            }
        }

        public struct Argument
        {
            public string id;
            public string value;
        }


    }
}
