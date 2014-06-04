using System;
using System.Text;

namespace FalconSoft.Data.Console
{
    public class CommandLineParser
    {
        public class GetParams
        {
           
            public string DataSourceUrn;
            public string FilterRules;
            public string FileName;
            public string Separator = "\t";
        }

        public class SubmitParams
        {
            public string DataSourceUrn;
            public string UpdateFileName;
            public string DeleteFileName;
            public string Separator = "\t";
            public string Comment;
        }

        public class SubscribeParams
        {
            public string DataSourceUrn;
            public string FilterRules;
            public string FileName;
            public string Separator = "\t";
        }

        public enum CommandType
        {
            get,
            subscribe,
            submit,
            exit,
            help
        };

        private GetParams _getArgument = new GetParams();
        private SubmitParams _submitArgument = new SubmitParams();
        private SubscribeParams _subscribeArgument = new SubscribeParams();

        public CommandType Command { get; set; }

        public bool Parse(string[] commandlineArgs)
        {
            switch (commandlineArgs[0])
            {
                case "get": 
                    Command = CommandType.get;
                    if(!ReadGetPameters(commandlineArgs))
                        return false;
                    break;
                case "subscribe": Command = CommandType.subscribe;
                    if(!ReadSubscribePameters(commandlineArgs))
                        return false;
                    break;
                case "submit":
                    Command = CommandType.submit;
                    if(!ReadSubmitPameters(commandlineArgs))
                        return false;
                    break;
                case "exit":
                    Command = CommandType.exit;
                    break;
                case "help": Command = CommandType.help;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool ReadSubscribePameters(string[] commandlineArgs)
        {
            try
            {
                _subscribeArgument.DataSourceUrn = commandlineArgs[1];
                _subscribeArgument.FileName = commandlineArgs[2];
                _subscribeArgument.FilterRules = (commandlineArgs.Length >= 4)? commandlineArgs[3] : string.Empty;
                _subscribeArgument.Separator = (commandlineArgs.Length >= 5)? commandlineArgs[4] : "\t";
            }
            catch (Exception)
            {
                ErrorMessage = "Incorrect subscribe parameters";
                return false;
            }

            return true;
        }

        private bool ReadSubmitPameters(string[] commandlineArgs)
        {
            try
            {
                _submitArgument.UpdateFileName = commandlineArgs[1];
                _submitArgument.DeleteFileName = (commandlineArgs.Length >= 3)? commandlineArgs[2] : string.Empty;
                _submitArgument.DataSourceUrn = (commandlineArgs.Length >= 4) ? commandlineArgs[3] : string.Empty;
                _submitArgument.Comment = (commandlineArgs.Length >= 5) ? commandlineArgs[4] : string.Empty;
                _submitArgument.Separator = (commandlineArgs.Length >= 6)? commandlineArgs[5] : "\t";
            }
            catch (Exception)
            {
                ErrorMessage = "Incorrect submit parameters";
                return false;
            }

            return true;
        }

        private bool ReadGetPameters(string[] commandlineArgs)
        {
            try
            {
                _getArgument.DataSourceUrn = commandlineArgs[1];
                _getArgument.FileName = commandlineArgs[2];
                _getArgument.FilterRules = (commandlineArgs.Length >= 4) ? commandlineArgs[3] : string.Empty;
                _getArgument.Separator = (commandlineArgs.Length >= 5) ? commandlineArgs[4] : "\t";
            }
            catch (Exception)
            {
                ErrorMessage = "Incorrect get parameters";
                return false;
            }
            

            return true;
        }

        public string ErrorMessage { get; private set; }


        public GetParams GetArguments { get { return _getArgument; } }
        public SubmitParams SubmitArguments { get { return _submitArgument; } }
        public SubscribeParams SubscribeArguments { get { return _subscribeArgument; } }

        public CommandLineParser()
        {
            ErrorMessage = "Incorrect command!!! use <help> command for more info";
        }
        public string Help()
        {
            var helpInfo = new StringBuilder();
            helpInfo.AppendLine("--get <DataSource Name> <Output File Name> [FilterRules] [Separator(optional default to TAB)]");
            helpInfo.AppendLine("--submit <update filename (file what has to be uploaded)> <delete filename (file with RecordKeys to be deleted)> <DataSource name> [comment (optional)] [separator (optional) default to TAB]");
            helpInfo.AppendLine("--subscribe <DataSource name> <filename (file name where output should be dumped)> [FilterRules] [separator (optional default to TAB)]");
            helpInfo.AppendLine("--exit");
            helpInfo.AppendLine("--help");

            return helpInfo.ToString();
        }
    }
}