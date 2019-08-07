using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PhysicsFSCS.HTTP
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var host = BuildWebHost(new string[0]);

			using(PhysicsFS.Initialize()) {
				if (args.Length > 0)
					FileSystem.WriteDir = args[0];

				foreach(var archive in args.Skip(1))
					FileSystem.Mount(archive, null, appendToPath: true);

				if (args.Length > 0)
					FileSystem.Mount(args[0], null, appendToPath: true);

				host.Run();
			}
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.Build();
	}
}
