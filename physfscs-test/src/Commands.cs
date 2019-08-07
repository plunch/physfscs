using System;
using System.Linq;
using PhysicsFSCS;

namespace PhysicsFSCS.Test
{
	static class Commands {
    		public static CommandInfo[] All = new [] {
			new CommandInfo( "quit",           Quit,           0, null                         ),
			new CommandInfo( "q",              Quit,           0, null                         ),
			new CommandInfo( "help",           Help,           0, null                         ),
			new CommandInfo( "init",           Init,           1, "<argv0>"                    ),
			new CommandInfo( "deinit",         DeInit,         0, null                         ),
			new CommandInfo( "addarchive",     AddArchive,     2, "<archiveLocation> <append>" ),
			new CommandInfo( "mount",          Mount,          3, "<archiveLocation> <mntpoint> <append>" ),
			new CommandInfo( "mountmem",       MountMem,       3, "<archiveLocation> <mntpoint> <append>" ),
			new CommandInfo( "mounthandle",    MountHandle,    3, "<archiveLocation> <mntpoint> <append>" ),
			new CommandInfo( "removearchive",  RemoveArchive,  1, "<archiveLocation>"          ),
			new CommandInfo( "unmount",        RemoveArchive,  1, "<archiveLocation>"          ),
			new CommandInfo( "enumerate",      Enumerate,      1, "<dirToEnumerate>"           ),
			new CommandInfo( "ls",             Enumerate,      1, "<dirToEnumerate>"           ),
			new CommandInfo( "getlasterror",   GetLastError,   0, null                         ),
			new CommandInfo( "getarchivers",   GetArchivers,   0, null                         ),
			new CommandInfo( "getdirsep",      GetDirSep,      0, null                         ),
			new CommandInfo( "getcdromdirs",   GetCdRomDirs,   0, null                         ),
			new CommandInfo( "getsearchpath",  GetSearchPath,  0, null                         ),
			new CommandInfo( "getbasedir",     GetBaseDir,     0, null                         ),
			new CommandInfo( "getuserdir",     GetUserDir,     0, null                         ),
			new CommandInfo( "getprefdir",     GetPrefDir,     2, "<org> <app>"                ),
			new CommandInfo( "getwritedir",    GetWriteDir,    0, null                         ),
			new CommandInfo( "setwritedir",    SetWriteDir,    1, "<newWriteDir>"              ),
			new CommandInfo( "permitsymlinks", PermitSyms,     1, "<1or0>"                     ),
			new CommandInfo( "setsaneconfig",  SetSaneConfig,  5, "<org> <appName> <arcExt> <includeCdRoms> <archivesFirst>" ),
			new CommandInfo( "mkdir",          MkDir,          1, "<dirToMk>"                  ),
			new CommandInfo( "delete",         Delete,         1, "<dirToDelete>"              ),
			new CommandInfo( "getrealdir",     GetRealDir,     1, "<fileToFind>"               ),
			new CommandInfo( "exists",         Exists,         1, "<fileToCheck>"              ),
			new CommandInfo( "isdir",          IsDir,          1, "<fileToCheck>"              ),
			new CommandInfo( "issymlink",      IsSymlink,      1, "<fileToCheck>"              ),
			new CommandInfo( "cat",            Cat,            1, "<fileToCat>"                ),
			new CommandInfo( "cat2",           Cat2,           2, "<fileToCat1> <fileToCat2>"  ),
			new CommandInfo( "filelength",     FileLength,     1, "<fileToCheck>"              ),
			new CommandInfo( "stat",           Stat,           1, "<fileToStat>"               ),
			new CommandInfo( "append",         Append,         1, "<fileToAppend>"             ),
			new CommandInfo( "write",          Write,          1, "<fileToCreateOrTrash>"      ),
			new CommandInfo( "getlastmodtime", GetLastModTime, 1, "<fileToExamine>"            ),
			new CommandInfo( "setbuffer",      SetBuffer,      1, "<bufferSize>"               ),
			new CommandInfo( "stressbuffer",   Stressbuffer,   1, "<bufferSize>"               ),
			new CommandInfo( "crc32",          Crc32,          1, "<fileToHash>"               ),
			new CommandInfo( "getmountpoint",  GetMountPoint,  1, "<dir>"                      )
	    	    };

		static void Quit(string[] args)
		{
			throw new ExitException();
		}

		static void Help(string[] args)
		{
			Console.WriteLine("Commands:");
			foreach(var cmd in All)
				Program.OutputUsage("  -", cmd);
		}

		static void Init(string[] args)
		{
			Program.disp = PhysicsFS.Initialize();
		}

		static void DeInit(string[] args)
		{
			Program.disp?.Dispose();
		}

		static void AddArchive(string[] args)
		{
			bool appending = int.TryParse(args[2], out int i) && i != 0;

			var disp = FileSystem.Mount(args[1], null, appending);
		}

		static void Mount(string[] args)
		{
			var archiveLocation = args[1];
			var mntPoint = args[2];
			bool append = int.TryParse(args[3], out int i) && i != 0;

			var disp = FileSystem.Mount(archiveLocation, mntPoint, append);
			try {
				Program.mounted.Add(archiveLocation, disp);
			} catch {
				disp.Dispose();
				throw;
			}
		}

		static void MountMem(string[] args)
		{
			var archiveLocation = args[1];
			var mntPoint = args[2];
			bool append = int.TryParse(args[3], out int i) && i != 0;

			var memory = new System.IO.MemoryStream();
			using(var file = System.IO.File.OpenRead(args[1]))
				file.CopyTo(memory);

			var fsm = new PhysFSMs(memory);
			try {
				var disp = FileSystem.Mount(fsm, archiveLocation, mntPoint, append);
				try {
					Program.mounted.Add(archiveLocation, disp);
				} catch {
					disp.Dispose();
					throw;
				}
			} catch {
				fsm.Dispose();
				throw;
			}
		}

		class PhysFSMs : IPhysicsFSStream {
			readonly System.IO.MemoryStream ms;

			public PhysFSMs(System.IO.MemoryStream ms)
			{
				this.ms = ms;
			}

			public long Position { 
				get { return ms.Position; }
				set { ms.Position = value; }
			}
			public long Length {
				get { return ms.Length; }
			}

			public long Read(byte[] buffer)
			{
				return ms.Read(buffer, 0, buffer.Length);
			}

			public long Write(byte[] buffer)
			{
				ms.Write(buffer, 0, buffer.Length);
				return buffer.Length;
			}

			public void Flush()
			{
				ms.Flush();
			}

			public IPhysicsFSStream Duplicate()
			{
				return new PhysFSMs(new System.IO.MemoryStream(ms.GetBuffer()));
			}

			public void Dispose()
			{
				ms.Dispose();
			}
		}

		static void MountHandle(string[] args)
		{
			var archiveLocation = args[1];
			var mntPoint = args[2];
			bool append = int.TryParse(args[3], out int i) && i != 0;

			var handle = FileSystem.OpenRead(archiveLocation);
			try {
				var disp = FileSystem.Mount(handle, archiveLocation, mntPoint, append);
				try {
					Program.mounted.Add(archiveLocation, disp);
				} catch {
					disp.Dispose();
					throw;
				}
			} catch {
				handle.Dispose();
				throw;
			}
		}

		static void GetMountPoint(string[] args)
		{
			var dir = args[1]; 
			var mountedAt = FileSystem.GetMountPoint(dir);
			Console.WriteLine($"Dir [{dir}] is mounted at [{mountedAt}].");
		}

		static void RemoveArchive(string[] args)
		{
			var dir = args[1];
			if (Program.mounted.TryGetValue(dir, out var disp)) {
				Program.mounted.Remove(dir);
				disp.Dispose();
				Console.WriteLine("Successful.");
			} else {
				Console.WriteLine("Failure. Not mounted?");
			}
		}

		static void Enumerate(string[] args)
		{
			int count = 0;
			foreach(var filename in FileSystem.EnumerateFiles(args[1]))
			{
				count++;
				Console.WriteLine(filename);
			}
			Console.WriteLine($"\n total ({count}) files.");
		}

		static void GetDirSep(string[] args)
		{
			Console.WriteLine($"Directory separator is [{System.IO.Path.DirectorySeparatorChar}].");
		}

		static void GetLastError(string[] args)
		{
			Console.WriteLine("Hah! I don't do errors!");
		}

		static void GetArchivers(string[] args)
		{
			Console.WriteLine("Supported archive types:\n");
			foreach(var type in PhysicsFS.SupportedArchiveTypes) {
				Console.WriteLine($" * {type.Extension}: {type.Description}");
				Console.WriteLine($"	Written by {type.Author}.");
				Console.WriteLine($"	{type.Url}");
				Console.WriteLine($"	{(type.SupportsSymlinks ? "Supports" : "Does not support")} symbolic links.");
			}
			Console.WriteLine();
		}

		static void GetCdRomDirs(string[] args)
		{
			int count = 0;
			foreach(var dir in FileSystem.CdRomDirs) {
				count++;
				Console.WriteLine(dir);
			}
			Console.WriteLine($"\n total ({count}) drives.");
		}

		static void GetSearchPath(string[] args)
		{
			int count = 0;
			foreach(var dir in FileSystem.SearchPath) {
				count++;
				Console.WriteLine(dir);
			}
			Console.WriteLine($"\n total ({count}) directories.");
		}

		static void GetBaseDir(string[] args)
		{
			Console.WriteLine($"Base dir is [{FileSystem.BaseDir}].");
		}

		static void GetUserDir(string[] args)
		{
			Console.WriteLine($"User dir is [{FileSystem.UserDir}].");
		}

		static void GetPrefDir(string[] args)
		{
			var org = args[1];
			var app = args[2];
			var prefDir = FileSystem.GetPrefDir(org, app);
			Console.WriteLine($"Pref dir is [{prefDir}].");
		}

		static void GetWriteDir(string[] args)
		{
			Console.WriteLine($"Write dir is [{FileSystem.WriteDir}].");
		}

		static void SetWriteDir(string[] args)
		{
			FileSystem.WriteDir = args[1];
		}

		static void PermitSyms(string[] args)
		{
			bool permit = int.TryParse(args[1], out int i) && i != 0;
			PhysicsFS.PermitSymbolicLinks = permit;
			Console.WriteLine("Symlinks are now" + (permit ? "permitted" : "forbidden"));
		}

		static void SetBuffer(string[] args)
		{
			ulong newSize = ulong.TryParse(args[1], out ulong i) ? i : 0;
			Program.doBufferSize = newSize;

			if (newSize > 0)
				Console.WriteLine($"Further tests will set a ({newSize}) size buffer.");
			else
				Console.WriteLine($"Further tests will NOT use a buffer.");
		}

		static void SetSaneConfig(string[] args) {
			var org = args[1];
			var app = args[2];
			var arcExt = args[3];
			var includeCdRoms = int.TryParse(args[4], out int i) && i != 0;
			var archFirst = int.TryParse(args[5], out int j) && j != 0;

			PhysicsFS.SetSaneConfig(org, app, arcExt, includeCdRoms, archFirst);
			Console.WriteLine("Successful.");
		}

		static void MkDir(string[] args)
		{
			FileSystem.CreateDirectory(args[1]);
			Console.WriteLine("Successful.");
		}

		static void Delete(string[] args)
		{
			FileSystem.Delete(args[1]);
			Console.WriteLine("Successful.");
		}

		static void GetRealDir(string[] args)
		{
			var dir = FileSystem.GetRealDir(args[1]);
			if (dir != null)
				Console.WriteLine($"Found at [{dir}].");
			else
				Console.WriteLine("Not found.");
		}
		
		static void Exists(string[] args)
		{
			if (FileSystem.Exists(args[1]))
				Console.WriteLine("File exists.");
			else
				Console.WriteLine("File does not exist.");
		}

		static void IsDir(string[] args)
		{
			var fi = FileSystem.Stat(args[1]);

			if (fi.Type == FileType.Directory)
				Console.WriteLine($"File {fi.Name} is a directory.");
			else
				Console.WriteLine($"File {fi.Name} is NOT a directory.");
		}

		static void IsSymlink(string[] args)
		{
			var fi = FileSystem.Stat(args[1]);

			if (fi.Type == FileType.Symlink)
				Console.WriteLine($"File {fi.Name} is a symlink.");
			else
				Console.WriteLine($"File {fi.Name} is NOT a symlink.");
		}

		static void Cat(string[] args)
		{
			using(var stream = FileSystem.OpenRead(args[1]))
			using(var o = Console.OpenStandardOutput())
			{
				if (Program.doBufferSize != 0)
					stream.BufferSize = Program.doBufferSize;

				var buffer = new byte[128];
				while(true) {
					int read = stream.Read(buffer, 0, buffer.Length);
					if (read == 0)
						break;
					o.Write(buffer, 0, read);
				}
			}
			Console.WriteLine("\n");
		}

		static void Cat2(string[] args)
		{
			using(var s1 = FileSystem.OpenRead(args[1]))
			using(var s2 = FileSystem.OpenRead(args[2]))
			using(var ms1 = new System.IO.MemoryStream())
			using(var ms2 = new System.IO.MemoryStream())
			{
				if (Program.doBufferSize != 0) {
					s1.BufferSize = Program.doBufferSize;
					s2.BufferSize = Program.doBufferSize;
				}

				while(true) {
					var buffer = new byte[128];

					int r1 = s1.Read(buffer, 0, buffer.Length);
					if (r1 > 0)
						ms1.Write(buffer, 0, r1);

					int r2 = s2.Read(buffer, 0, buffer.Length);
					if(r2 > 0)
					ms2.Write(buffer, 0, r2);

					if (r1 == 0 && r2 == 0)
						break;
				}

				Console.WriteLine($"file '{args[1]}' ...\n");
				using(var o = Console.OpenStandardOutput()) {
					ms1.Position = 0;
					ms1.CopyTo(o);
				}

				Console.WriteLine($"file '{args[2]}' ...\n");
				using(var o = Console.OpenStandardOutput()) {
					ms2.Position = 0;
					ms2.CopyTo(o);
				}
				Console.WriteLine("\n");
			}
		}
		
		static void Crc32(string[] args)
		{
			using(var stream = FileSystem.OpenRead(args[1]))
			{
				var buffer = new byte[512];
				uint crc = uint.MaxValue;
				long bytesRead;

				while ((bytesRead = stream.Read(buffer)) > 0) 
				{
					for(int i = 0; i < bytesRead; ++i)
					{
						unchecked {
							for(byte bit = 0; bit < 8; bit++, buffer[i] >>= 1)
								crc = (crc >> 1) ^ (((crc ^ buffer[i]) & 1) != 0 ? 0xEDB88320 : 0);
						}
					}
				}

				
				crc ^= uint.MaxValue;
				Console.WriteLine($"CRC32 for {args[1]} is 0x{crc:X8}");
			}
		}

		static void FileLength(string[] args)
		{
			var fi = FileSystem.Stat(args[1]);

			Console.WriteLine($" (cast to int) {(int)fi.Length} bytes.");
		}

		static void Append(string[] args)
		{
			using(var stream = FileSystem.OpenAppend(args[1]))
			{
				if (Program.doBufferSize != 0)
					stream.BufferSize = Program.doBufferSize;

				var bytes = System.Text.Encoding.UTF8.GetBytes("The cat sat on the mat.\n\n");
				stream.Write(bytes);
			}

			Console.WriteLine("Successful.");
		}

		static void Write(string[] args)
		{
			using(var stream = FileSystem.OpenWrite(args[1]))
			{
				if (Program.doBufferSize != 0)
					stream.BufferSize = Program.doBufferSize;

				var bytes = System.Text.Encoding.UTF8.GetBytes("The cat sat on the mat.\n\n");
				stream.Write(bytes);
			}

			Console.WriteLine("Successful.");
		}

		static void GetLastModTime(string[] args)
		{
			var fi = FileSystem.Stat(args[1]);

			if (fi.ModTime is DateTime m)
				Console.WriteLine($"Last modified {m}.");
			else
				Console.WriteLine("Failed to determine.");
		}

		static void Stat(string[] args)
		{
			var fi = FileSystem.Stat(args[1]);

			switch(fi.Type) {
				case FileType.Regular:
					Console.WriteLine("Type: File");
					break;
				case FileType.Directory:
					Console.WriteLine("Type: Directory");
					break;
				case FileType.Symlink:
					Console.WriteLine("Type: Symlink");
					break;
				default:
					Console.WriteLine("Type: Unknown");
					break;
			}

			Console.WriteLine($"Created at: {fi.CreateTime}");
			Console.WriteLine($"Last modified at: {fi.ModTime}");
			Console.WriteLine($"Last accessed at: {fi.AccessTime}");
			Console.WriteLine($"Readonly: {fi.IsReadOnly}");
		}

		static void Stressbuffer(string[] args)
		{
			throw new NotImplementedException("???");
		}
	}
}
