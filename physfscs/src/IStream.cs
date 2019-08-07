using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public interface IPhysicsFSStream : IDisposable
	{
		long Position { get; set; }
		long Length { get; }

		long Read(byte[] buffer);
		long Write(byte[] buffer);

		void Flush();

		IPhysicsFSStream Duplicate();
	}
}
