using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconSoft.Data.Console.FileCrawler
{
    public class CommandLineParser
    {
        public enum CommandType
        {
            Start,
            Test,
            Exit,
            Help
        };

        public class StartParams
        {
            public string Path { get; set; }
            public string Filter { get; set; }
        }

        public StartParams StartParameters { get; set; }
        public CommandType Command { get; set; }

        public bool Parse(string[] commandlineArgs)
        {
            switch (commandlineArgs[0])
            {
                case "start":
                    if (ReadStartParams(commandlineArgs))
                        return false;
                    break;
                case "test":
                    Command = CommandType.Test;
                    break;
                case "exit":
                    break;
                case "help": 
                    Help();
                    break;
                default:
                    return false;
            }
            return true;
        }
        private bool ReadStartParams(string[] commandlineArgs)
        {
            StartParameters = new StartParams();
            Command = CommandType.Start;
            try
            {
                StartParameters.Path = commandlineArgs[1];
                StartParameters.Filter = commandlineArgs[2];
            }
            catch (Exception)
            {
                StartParameters = null;
                return false;
            }
           return true;
        }

        public void Help()
        {
            System.Console.WriteLine("start -> <data path(like c:\\data\\)>  <filter(like *690.txt)>");
        }
    }
}
