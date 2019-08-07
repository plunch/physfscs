using System;

namespace PhysicsFSCS.Test
{
    class CommandInfo {
    	    public string Command { get; }
	    public Action<string[]> Func {get;}
	    public int ArgumentCount { get; }
	    public string Usage { get; }

	    public CommandInfo(string command, Action<string[]> func, int argumentCount, string usage) {
	    	    this.Command = command;
		    this.Func = func;
		    this.ArgumentCount = argumentCount;
		    this.Usage = usage;
	    }
    }

    class ExitException : Exception {}
    class PrintNothingException : Exception {}
}
