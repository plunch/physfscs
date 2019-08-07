using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	unsafe class UnmanagedPhysFSStream : IPhysicsFSStream
	{
		PHYSFS_Io* io;

		public long Position { 
			get
			{
				CheckDisposed();
				long l = io->tell(io);
				if (l == -1) {
					var error = (ErrorCode)PHYSFS_getLastErrorCode();
					if (error > ErrorCode.Ok)
						throw Exception(error);
				}
				return l;
			}
			set
			{
				CheckDisposed();
				if (value < 0)
					throw new ArgumentException("Position cannot be negative", "value");
				int ret = io->seek(io, (ulong)value);
				if (ret == 0)
					throw Exception();
			}
		}
		public long Length {
			get {
				CheckDisposed();

				long l = io->length(io);
				if (l == -1) {
					var error = (ErrorCode)PHYSFS_getLastErrorCode();
					if (error > ErrorCode.Ok)
						throw Exception(error);
				}
				return l;
			}
		}

		public UnmanagedPhysFSStream(PHYSFS_Io* io)
		{
			if (ManagedPhysFSStream.TryGetAsManagedIo(io, out var _))
				throw new ArgumentException("Provided implementation is managed.");
			this.io = io;
		}

		public static explicit operator PHYSFS_Io*(UnmanagedPhysFSStream stream)
		{
			if (stream == null)
				return null;
			else
				return stream.io;
		}


		void CheckDisposed()
		{
			if (io == null)
				throw new ObjectDisposedException(nameof(UnmanagedPhysFSStream));
		}


		public long Read(byte[] buffer)
		{
			CheckDisposed();

			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) return 0;

			var ret = io->read(io, buffer, (ulong)buffer.Length);
			if (ret == -1) {
				throw Exception();
			} else if (ret < buffer.Length) {
				var error = (ErrorCode)PHYSFS_getLastErrorCode();
				if (error > ErrorCode.Ok)
					throw Exception(error);
			}
			return ret;
		}

		public long Write(byte[] buffer)
		{
			CheckDisposed();

			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) return 0;

			var ret = io->write(io, buffer, (ulong)buffer.Length);
			if (ret == -1) {
				throw Exception();
			} else if (ret < buffer.Length) {
				var error = (ErrorCode)PHYSFS_getLastErrorCode();
				if (error > ErrorCode.Ok)
					throw Exception(error);
			}
			return ret;
		}

		public void Flush()
		{
			CheckDisposed();

			int ret = io->flush(io);
			if (ret == 0)
				throw Exception();
		}

		public IPhysicsFSStream Duplicate()
		{
			CheckDisposed();

			PHYSFS_Io* dup = io->duplicate(io);

			if (dup == null) {
				var error = (ErrorCode)PHYSFS_getLastErrorCode();
				if (error > ErrorCode.Ok)
					throw Exception(error);
				else
					return null;
			}
			return new UnmanagedPhysFSStream(dup);
		}

		public void Dispose()
		{
			PHYSFS_Io* i = io;
			io = null;

			if (i != null)
				i->destroy(i);
		}
	}
}
