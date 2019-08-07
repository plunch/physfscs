using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	unsafe class ManagedPhysFSStream
	{
		readonly IPhysicsFSStream stream;
		GCHandle me;

		PHYSFS_Io io;
		static PHYSFS_Io.readwrite_ptr read = Read;
		static PHYSFS_Io.readwrite_ptr write = Write;
		static PHYSFS_Io.seek_ptr seek = Seek;
		static PHYSFS_Io.tell_ptr tell = Tell;
		static PHYSFS_Io.length_ptr length = Length;
		static PHYSFS_Io.duplicate_ptr duplicate = Duplicate;
		static PHYSFS_Io.flush_ptr flush = Flush;
		static PHYSFS_Io.destroy_ptr destroy = Destroy;
		static Dictionary<IntPtr, ManagedPhysFSStream> allocated = new Dictionary<IntPtr, ManagedPhysFSStream>();

		public IPhysicsFSStream Stream => stream;

		public static bool TryGetAsManagedIo(PHYSFS_Io* io, out ManagedPhysFSStream wrapper)
		{
			return allocated.TryGetValue(io->opaque, out wrapper);
		}

		public ManagedPhysFSStream(IPhysicsFSStream stream)
		{
			/*
			if (stream is UnmanagedPhysFSStream)
				throw new ArgumentException("Provided implementation is not managed.");
				*/

			this.stream = stream;
			me = GCHandle.Alloc(this);

			io.version = 0;
			io.opaque = (IntPtr)me;
			allocated.Add(io.opaque, this);

			io.read = read;
			io.write = write;
			io.seek = seek;
			io.tell = tell;
			io.length = length;
			io.duplicate = duplicate;
			io.flush = flush;
			io.destroy = destroy;
		}

		public static explicit operator PHYSFS_Io*(ManagedPhysFSStream wrapper)
		{
			if (wrapper == null)
				return null;

			// Since wrappers are always fixed, then this should be safe ?
			fixed(PHYSFS_Io* io = &wrapper.io)
				return io;
		}


		static long Read(PHYSFS_Io* io, byte[] buf, ulong len)
		{
			try {
				var stream = allocated[io->opaque].stream;
				if (stream == null)
					return 0;
				return stream.Read(buf);
			} catch(Exception ex) {
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return -1;
			}
		}

		static long Write(PHYSFS_Io* io, byte[] buf, ulong len)
		{
			try {
				var stream = allocated[io->opaque].stream;
				if (stream == null)
					return (long)len;
				return stream.Write(buf);
			} catch(Exception ex) {
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return -1;
			}
		}

		static int Seek(PHYSFS_Io* io, ulong offset)
		{
			return Safely(() => {
				var stream = allocated[io->opaque].stream;
				if (stream != null)
					stream.Position = (long)offset;
				return 1;
			});
		}

		static long Tell(PHYSFS_Io* io)
		{
			try {
				var stream = allocated[io->opaque].stream;
				if (stream == null) return 0;
				return stream.Position;
			} catch (Exception ex)
			{
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return -1;
			}
		}

		static long Length(PHYSFS_Io* io)
		{
			try {
				var stream = allocated[io->opaque].stream;
				if (stream == null) return 0;
				return stream.Length;
			} catch (Exception ex) {
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return -1;
			}
		}

		static PHYSFS_Io* Duplicate(PHYSFS_Io* io)
		{
			try {
				var stream = allocated[io->opaque].stream;

				if (stream == null) {
					return (PHYSFS_Io*)new ManagedPhysFSStream(null);
				}

				var stream2 = stream.Duplicate();

				var other = new ManagedPhysFSStream(stream2);

				return (PHYSFS_Io*)other;
			} catch (Exception ex) {
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return null;
			}
		}

		static int Flush(PHYSFS_Io* io)
		{
			return Safely(() => {
				var stream = allocated[io->opaque].stream;
				if (stream != null)
					stream.Flush();
				return 1;
			});
		}

		static void Destroy(PHYSFS_Io* io)
		{
			// Unfortunately, this call may not fail.
			// We cannot let ANY exception whatsoever
			// escape this method, so we'll just have
			// to swallow it.
			try {
				var stream = allocated[io->opaque].stream;
				if (stream != null)
					stream.Dispose();
			} catch {
			} finally {
				try {
					allocated[io->opaque].me.Free();
				} catch { }
			}
		}
	}
}
