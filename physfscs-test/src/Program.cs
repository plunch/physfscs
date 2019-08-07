using System;
using PhysicsFSCS;
using System.Linq;
using System.Collections.Generic;

namespace PhysicsFSCS.Test
{
	class Program
	{
		internal static IDisposable disp = null;
		internal static ulong doBufferSize = 0;
		internal static Dictionary<string, IDisposable> mounted = new Dictionary<string, IDisposable>();

		static void Main(string[] args)
		{
			try {
				disp = PhysicsFS.Initialize();

				OutputVersions();

				Console.WriteLine("Enter commands. Enter \"help\" for instructions");
				while (true)
				{
					Console.Write("> ");
					string line = Console.ReadLine();
					if (line == null)
						break;
					try {
						ProcessCommand(line);
					} catch (PrintNothingException) {
					} catch (ExitException) {
						break;
					} catch (Exception ex) {
						Console.WriteLine(ex.ToString());
					}
				}
			} finally {
				disp?.Dispose();
			}
		}

		static void ProcessCommand(string text)
		{
			var args = text.Split(" ", StringSplitOptions.RemoveEmptyEntries);

			if (args.Length > 0) {
				var command = Commands.All.FirstOrDefault(x => x.Command == args[0]);

				if (command == null)
					Console.WriteLine("Unknown command. Enter \"help\" for instructions");
				else if (command.ArgumentCount >= 0 && args.Length-1 != command.ArgumentCount)
					OutputUsage("usage: ", command);
				else
					command.Func(args);
			}
		}

		internal static void OutputUsage(string intro, CommandInfo cmd)
		{
			if (cmd.ArgumentCount == 0)
				Console.WriteLine($"{intro} \"{cmd.Command}\" (no arguments)");
			else
				Console.WriteLine($"{intro} \"{cmd.Command}\" {cmd.Usage}");
		}

		static void OutputVersions() 
		{
			var ver = PhysicsFS.LinkedVersion;
			Console.WriteLine($"Linked against {ver.Major}.{ver.Minor}.{ver.Build}\n");
		}
	}
}
