using System;
using System.Runtime.InteropServices;
using System.IO;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public unsafe class PhysicsFSFileStream : Stream, IPhysicsFSStream
	{
		SafePhysicsFSFileHandle handle;
		string filename;
		readonly bool write;
		ulong bufsize;

		public ulong BufferSize {
			get { return bufsize; }
			set {
				int ret = PHYSFS_setBuffer(handle, value);
				if (ret == 0)
					throw Exception();
				bufsize = value;
			}
		}

		public override bool CanRead => true;
		public override bool CanWrite => write;
		public override bool CanSeek => true;

		public override long Length {
			get { return PHYSFS_fileLength(handle); }
		}
		public override long Position {
			get {
				long ret = PHYSFS_tell(handle);
				if (ret < 0)
					throw Exception();
				return ret;
			}
			set {
				int ret;

				//Console.WriteLine($"Seek ({value})");
				if (value < 0)
					ret = PHYSFS_seek(handle, 0);
				else if (value > Length)
					ret = PHYSFS_seek(handle, (ulong)Length);
				else
					ret = PHYSFS_seek(handle, (ulong)value);

				if (ret == 0)
					throw Exception();
			}
		}



		public PhysicsFSFileStream(SafePhysicsFSFileHandle handle, string filename, bool write)
		{
			this.handle = handle;
			this.filename = filename;
			this.write = write;
			this.bufsize = 0;
		}



		public override long Seek(long offset, SeekOrigin origin)
		{
			switch(origin) {
				case SeekOrigin.Begin:
					return Position = offset;
				case SeekOrigin.Current:
					return Position = Position + offset;
				case SeekOrigin.End:
					return Position = Length + offset;
				default:
					throw new ArgumentException("origin");
			}
		}

		public override void Flush()
		{
			int ret = PHYSFS_flush(handle);
			if (ret == 0)
				throw Exception();
		}

		public long Read(byte[] buffer)
		{
			return (long)Read(buffer, offset: 0, count: buffer.Length);
		}
		public override unsafe int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if (offset + count > buffer.Length)
				throw new ArgumentException();

			//Console.WriteLine($"Read buffer={buffer} offset={offset} count={count}");
			long ret;
			fixed(byte* buf = &buffer[offset]) {
				ret = PHYSFS_readBytes(handle, buf, (ulong)count);
			}
			/*
			if (count == 4) {
				Console.Write("\t");
				Console.WriteLine(BitConverter.ToInt32(buffer, offset).ToString("x"));
			} else if (count == 256 || count == 252) {
				Console.Write("\t");
				for(int i = 0; i < count; ++i) {
					Console.Write(" ");
					Console.Write(buffer[offset + i].ToString("x2"));
					if (i != 0 && i % 16 == 0)
						Console.WriteLine();
				}
				Console.WriteLine();
			}*/
			if (ret == -1)
				throw Exception();
			if (ret < count && PHYSFS_eof(handle) == 0)
				throw Exception();
			return (int)ret;
		}

		public long Write(byte[] buffer)
		{
			Write(buffer, offset: 0, count: buffer.Length);
			return buffer.Length;
		}
		public override unsafe void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if (offset + count > buffer.Length)
				throw new ArgumentException();

			long ret;
			fixed(byte* buf = &buffer[offset]) {
				ret = PHYSFS_writeBytes(handle, buf, (ulong)count);
			}
			if (ret < count)
				throw Exception();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				// Since PHYSFS_close can fail if flushing fails,
				// and we won't be able to handle it, we'll flush
				// now and hope for the best.
				Flush();
				handle.Dispose();
			}
		}

		IPhysicsFSStream IPhysicsFSStream.Duplicate()
		{
			PhysicsFSFileStream stream;
			if (write)
				stream = FileSystem.OpenAppend(filename);
			else
				stream = FileSystem.OpenRead(filename);

			if (bufsize > 0)
				stream.BufferSize = bufsize;

			return stream;
		}
	}

	public class SafePhysicsFSFileHandle : SafeHandle
	{
		public SafePhysicsFSFileHandle() : base(IntPtr.Zero, ownsHandle: true) { }

		public override bool IsInvalid => handle == IntPtr.Zero;

		override protected bool ReleaseHandle()
		{
			int ret = NativeMethods.PHYSFS_close(handle);
			return ret != 0;
		}
	}
}
