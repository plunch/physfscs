using System;
using System.Threading;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public struct UnmountDisposable : IDisposable
	{
		string directory;

		public UnmountDisposable(string directory)
		{
			this.directory = directory;
		}

		public void Dispose()
		{
			var d = Interlocked.Exchange(ref directory, null);
			if (d != null)
				FileSystem.Unmount(d);
		}
	}

	public struct DeinitDisposable : IDisposable
	{
		public void Dispose()
		{
			if (PhysicsFS.IsInitialized) {
				int ret = PHYSFS_deinit();
				if (ret == 0)
					throw Exception();
			}
		}
	}
}
