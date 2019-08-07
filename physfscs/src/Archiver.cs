using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public interface IArchive : IDisposable
	{
		IEnumerable<string> ListFiles(string directory, string originalDirectory);

		IPhysicsFSStream Open(string filename, ArchiveFileMode mode);

		void Remove(string path);
		void CreateDirectory(string path);

		ArchiveFileInfo Stat(string path);
	}

	public enum ArchiveFileMode
	{
		Read,
		Write,
		Append
	}

	public class ArchiveFileInfo
	{
		public ulong? Length { get; set; }
		public FileType Type { get; set; }
		public DateTime? ModTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public DateTime? AccessTime { get; set; }

		public ArchiveFileInfo(FileType type)
		{
			this.Type = type;
		}
	}

	public class ArchiveArguments
	{
		public IPhysicsFSStream Stream { get; }
		public string Name { get; }
		public bool IsForWriting { get; }

		public ArchiveArguments(IPhysicsFSStream stream, string name, bool forWrite)
		{
			this.Stream = stream;
			this.Name = name;
			this.IsForWriting = forWrite;
		}
	}


	unsafe class ManagedArchiveType
	{
		Func<ArchiveArguments, IArchive> open;
		PHYSFS_Archiver native;
		GCHandle me;

		PHYSFS_Archiver.openArchive_ptr _openArchive;
		PHYSFS_Archiver.enumerate_ptr _enumerate;
		PHYSFS_Archiver.open_ptr _openread;
		PHYSFS_Archiver.open_ptr _openwrite;
		PHYSFS_Archiver.open_ptr _openappend;
		PHYSFS_Archiver.remove_ptr _remove;
		PHYSFS_Archiver.mkdir_ptr _mkdir;
		PHYSFS_Archiver.stat_ptr _stat;
		PHYSFS_Archiver.closeArchive_ptr _closeArchive;

		Dictionary<IntPtr, AllocatedArchive> allocated = new Dictionary<IntPtr, AllocatedArchive>();

		class AllocatedArchive
		{
			public IArchive archive;
			public GCHandle handle;

			public AllocatedArchive(IArchive archive)
			{
				this.archive = archive;
				this.handle = GCHandle.Alloc(this);
			}
		}

		public ManagedArchiveType(Func<ArchiveArguments, IArchive> open)
		{
			this.open = open;
			me = GCHandle.Alloc(this);
			_openArchive = OpenArchive;
			_enumerate = Enumerate;
			_openread = OpenRead;
			_openwrite = OpenWrite;
			_openappend = OpenAppend;
			_remove = Remove;
			_mkdir = CreateDirectory;
			_stat = Stat;
			_closeArchive = CloseArchive;

			native.version = 0;
			native.OpenArchive = _openArchive;
			native.Enumerate = _enumerate;
			native.OpenRead = _openread;
			native.OpenWrite = _openwrite;
			native.OpenAppend = _openappend;
			native.Remove = _remove;
			native.CreateDirectory = _mkdir;
			native.Stat = _stat;
			native.CloseArchive = _closeArchive;
		}

		public static explicit operator PHYSFS_Archiver*(ManagedArchiveType m)
		{
			if (m == null)
				return null;
			fixed(PHYSFS_Archiver* a = &m.native)
				return a;
		}


		AllocatedArchive Instance(void* opaque)
		{
			return allocated[(IntPtr)opaque];
		}

		void* OpenArchive(PHYSFS_Io* io, byte* name, int forWrite, int* claimed)
		{
			try {
				IPhysicsFSStream stream;
				if (ManagedPhysFSStream.TryGetAsManagedIo(io, out var managed))
					stream = managed.Stream;
				else
					stream = new UnmanagedPhysFSStream(io);

				var args = new ArchiveArguments(stream, UTF8ToString(name), forWrite != 0);

				var archive = open(args);

				var wrapped = new AllocatedArchive(archive);
				allocated.Add((IntPtr)wrapped.handle, wrapped);

				*claimed = 1;
				return (void*)(IntPtr)wrapped.handle;
			} catch (NotSupportedException) {
				*claimed = 1;
				return null;
			} catch (Exception e) {
				*claimed = 0;
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return null;
			}
		} 

		int Enumerate(void* opaque, byte* dirname, PHYSFS_EnumerateCallback cb,
		              byte* origDir, void* cbData)
		{
			try {
				string dir = UTF8ToString(dirname);
				string orig = UTF8ToString(origDir);

				var result = Instance(opaque).archive.ListFiles(dir, orig);

				foreach(var file in result) {
					int ret;
					fixed(byte* b = StringToUTF8(file))
						ret = cb(cbData, origDir, b);

					switch(ret) {
						case PHYSFS_ENUM_ERROR:
							PHYSFS_setErrorCode((int)ErrorCode.AppCallback);
							return PHYSFS_ENUM_ERROR;
						case PHYSFS_ENUM_STOP:
							return PHYSFS_ENUM_STOP;
						default:
							throw new InvalidOperationException("Badly behaved callback");
					}
				}

				return PHYSFS_ENUM_OK;
			} catch (Exception e) {
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return PHYSFS_ENUM_ERROR;
			}
		}

		PHYSFS_Io* Open(void* opaque, byte* file, ArchiveFileMode mode)
		{
			try {
				string fn = UTF8ToString(file);
				var ret = Instance(opaque).archive.Open(fn, mode);
				return (PHYSFS_Io*)new ManagedPhysFSStream(ret);
			} catch (Exception e) {
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return null;
			}
		}

		PHYSFS_Io* OpenRead(void* opaque, byte* file)
		{
			return Open(opaque, file, ArchiveFileMode.Read);
		}

		PHYSFS_Io* OpenWrite(void* opaque, byte* file)
		{
			return Open(opaque, file, ArchiveFileMode.Write);
		}
		
		PHYSFS_Io* OpenAppend(void* opaque, byte* file)
		{
			return Open(opaque, file, ArchiveFileMode.Append);
		}

		int CreateDirectory(void* opaque, byte* path)
		{
			try {
				Instance(opaque).archive.CreateDirectory(UTF8ToString(path));
				return 1;
			} catch (Exception e) {
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return 0;
			}
		}

		int Remove(void* opaque, byte* path)
		{
			try {
				Instance(opaque).archive.Remove(UTF8ToString(path));
				return 1;
			} catch (Exception e) {
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return 0;
			}
		}

		static readonly DateTime EPOCH = new DateTime(1970, 1, 1);
		static long GetTime(DateTime? time)
		{
			if (time == null || time < EPOCH)
				return -1;

			return (time.Value - EPOCH).Ticks / TimeSpan.TicksPerSecond;
		}

		int Stat(void* opaque, byte* path, PHYSFS_Stat* stat)
		{
			try {
				var details = Instance(opaque).archive.Stat(UTF8ToString(path));

				if (details == null) {
					PHYSFS_setErrorCode((int)ErrorCode.NotFound);
					return 0;
				}

				if (details.Length == null)
					stat->filesize = -1;
				else
					stat->filesize = (long)details.Length;

				stat->filetype = (int)details.Type;

				stat->modtime = GetTime(details.ModTime);
				stat->createtime = GetTime(details.CreateTime);
				stat->accesstime = GetTime(details.AccessTime);

				return 1;
			} catch (Exception e) {
				SetException(e);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return 0;
			}
		}

		void CloseArchive(void* opaque)
		{
			try {
				var a = Instance(opaque);
				a.archive.Dispose();
				a.archive = null;
				a.handle.Free();
			} catch {
				// No way to signal error
			}
		}
	}
}
