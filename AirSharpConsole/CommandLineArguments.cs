namespace AirSharp {
    using System;
    using System.Collections.Generic;

    public class CommandLineArguments {

        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();

        public CommandLineArguments() {
        }

        private CommandLineArguments(Dictionary<string, string> args) {
            _arguments = args;
        }

        public bool OptionExists(string option) {
            return _arguments.ContainsKey(option.ToUpper());
        }

        public string GetOptionValue(string option) {
            if (!OptionExists(option)) {
                throw new ArgumentException(string.Format("{0} not found", option));
            }

            string o = _arguments[option.ToUpper()];
            if (o == null) {
                return String.Empty;
            }

            return o;
        }

        public void AddOption(string option) {
            _arguments.Add(option.ToUpper(), null);
        }

        public void AddOption(string option, string val) {
            _arguments.Add(option.ToUpper(), val);
        }

        public static CommandLineArguments Parse(string[] args) {
            Dictionary<string, string> argParams = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (arg[0] != '/') {
                    argParams.Add(arg.ToUpper(), null);
                }
                else {
                    arg = arg.Substring(1);
                    string[] argArr = arg.Split("=".ToCharArray(), 2);
                    argParams.Add(argArr[0].ToUpper(), ((argArr.Length > 1) && (argArr[1].Length > 0)) ? argArr[1] : null);
                }
            }

            CommandLineArguments a = new CommandLineArguments(argParams);
            return a;
        }
    }
}
