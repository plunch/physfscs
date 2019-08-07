using System;
using System.IO;
using System.Collections.Generic;
using static PhysicsFSCS.NativeMethods;
using static PhysicsFSCS.FileSystem;
using static PhysicsFSCS.PhysicsFS;
using System.Text;

namespace PhysicsFSCS
{
	public static unsafe class FileSystem
	{
		public static string BaseDir => UTF8ToString(PHYSFS_getBaseDir());
		public static string UserDir => UTF8ToString(PHYSFS_getUserDir());

		public static string WriteDir
		{
			get { 
				return UTF8ToString(PHYSFS_getWriteDir());
			}
			set {
				int ret;
				if (value == null) {
					ret = PHYSFS_setWriteDir(null);
				} else {
					fixed(byte* b = StringToUTF8(value)) {
						ret = PHYSFS_setWriteDir(b);
					}
				}

				if (ret == 0)
					throw Exception();
			}
		}

		public static string GetPrefDir(string org, string app)
		{
			fixed(byte* o = StringToUTF8(org))
			fixed(byte* a = StringToUTF8(app)) {
				return UTF8ToString(PHYSFS_getPrefDir(o, a));
			}
		}

		public static SearchPathEnumerator SearchPath {
			get { return new SearchPathEnumerator(); }
		}

		public static CdRomDirsEnumerator CdRomDirs {
			get { return new CdRomDirsEnumerator(); }
		}



		public static void CreateDirectory(string directory)
		{
			int ret;
			fixed(byte* dir = StringToUTF8(directory)) {
				ret = PHYSFS_mkdir(dir);
			}
			if (ret == 0)
				throw Exception();
		}

		public static void Delete(string path)
		{
			int ret;
			fixed(byte* p = StringToUTF8(path)) {
				ret = PHYSFS_delete(p);
			}
			if (ret == 0)
				throw Exception();
		}

		public static string GetRealDir(string filename)
		{
			fixed(byte* fn = StringToUTF8(filename)) {
				return UTF8ToString(PHYSFS_getRealDir(fn));
			}
		}

		public static PhysicsFSFileInfo Stat(string filename)
		{
			PHYSFS_Stat stat;
			int ret;
			fixed(byte* fn = StringToUTF8(filename))
				ret = PHYSFS_stat(fn, &stat);

			if (ret == 0) {
				var errorCode = (ErrorCode)PHYSFS_getLastErrorCode();
				if (errorCode != ErrorCode.Ok)
					throw Exception(errorCode);
				else
					return null;
			} else {
				return new PhysicsFSFileInfo(filename, stat);
			}
		}

		public static FileNameEnumerator EnumerateFiles(string directory)
		{
			return new FileNameEnumerator(directory);
		}

		public static bool Exists(string filename)
		{
			fixed(byte* fn = StringToUTF8(filename)) {
				return PHYSFS_exists(fn) != 0;
			}
		}

		public static PhysicsFSFileStream OpenRead(string filename)
		{
			SafePhysicsFSFileHandle handle;
			fixed(byte* fn = StringToUTF8(filename)) {
				handle = PHYSFS_openRead(fn);
			}

			if (handle.IsInvalid)
				throw Exception();

			return new PhysicsFSFileStream(handle, filename, write: false);
		}

		public static PhysicsFSFileStream OpenWrite(string filename)
		{
			SafePhysicsFSFileHandle handle;
			fixed(byte* fn = StringToUTF8(filename)) {
				handle = PHYSFS_openWrite(fn);
			}

			if (handle.IsInvalid)
				throw Exception();

			return new PhysicsFSFileStream(handle, filename, write: true);
		}

		public static PhysicsFSFileStream OpenAppend(string filename)
		{
			SafePhysicsFSFileHandle handle;
			fixed(byte* fn = StringToUTF8(filename)) {
				handle = PHYSFS_openAppend(fn);
			}

			if (handle.IsInvalid)
				throw Exception();

			return new PhysicsFSFileStream(handle, filename, write: true);
		}

		public static IDisposable Mount(string newDir, string mountPoint,
		                                       bool appendToPath)
		{
			int ret;
			fixed(byte* nd = StringToUTF8(newDir))
			fixed(byte* mp = StringToUTF8(mountPoint)) {
				ret = PHYSFS_mount(nd, mp, appendToPath ? 1 : 0);
			}
			if (ret == 0)
				throw Exception();
			return new UnmountDisposable(newDir);
		}

		public static IDisposable Mount(IPhysicsFSStream stream, string fname,
				                       string mountPoint,
				                       bool appendToPath)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			PHYSFS_Io* io;
			switch(stream) {
				case UnmanagedPhysFSStream unmanaged:
					io = (PHYSFS_Io*)unmanaged;
					break;
				case ManagedPhysFSStream managed:
					io = (PHYSFS_Io*)managed;
					break;
				default:
					io = (PHYSFS_Io*)new ManagedPhysFSStream(stream);
					break;
			}

			int ret;
			fixed(byte* fn = StringToUTF8(fname))
			fixed(byte* mp = StringToUTF8(mountPoint)) {
				ret = PHYSFS_mountIo(io, fn, mp, appendToPath ? 1 : 0);
			}
			if (ret == 0)
				throw Exception();
			return new UnmountDisposable(fname);
		}

		internal static void Unmount(string dir)
		{
			int ret;
			fixed(byte* d = StringToUTF8(dir)) {
				ret = PHYSFS_unmount(d);
			}
			if (ret == 0)
				throw Exception();
		}

		public static string GetMountPoint(string directory)
		{
			fixed(byte* dir = StringToUTF8(directory)) {
				return UTF8ToString(PHYSFS_getMountPoint(dir));
			}
		}

		public static void GetCdRomDirs(Action<string> forEach)
		{
			PHYSFS_StringCallback callback = (void* data, byte* str) => {
								string dir = UTF8ToString(str);
								forEach(dir);
							};
			PHYSFS_getCdRomDirsCallback(callback, null);
		}

		public static void GetSearchPath(Action<string> forEach)
		{
			Exception callbackException = null;
			PHYSFS_StringCallback callback = (void* data, byte* str) => {
								string dir = UTF8ToString(str);
								try {
									forEach(dir);
								} catch (Exception e) {
									callbackException = e;
								}
							};
			PHYSFS_getCdRomDirsCallback(callback, null);
			if (callbackException != null)
				throw callbackException;
		}

		public static void Enumerate(string directory, Func<string, string, bool> forEach)
		{
			Exception callbackException = null;
			PHYSFS_EnumerateCallback callback = (void* data, byte* origdir, byte* fname) => {
								string dir = UTF8ToString(origdir);
								string file = UTF8ToString(fname);
								try {
									bool cont = forEach(dir, file);
									if (cont)
										return PHYSFS_ENUM_OK;
									else
										return PHYSFS_ENUM_STOP;
								} catch (Exception e) {
									callbackException = e;
									return PHYSFS_ENUM_ERROR;
								}
							};
			int ret;
			fixed(byte* dir = StringToUTF8(directory)) {
				ret = PHYSFS_enumerate(dir, callback, null);
			}
			if (callbackException != null)
				throw callbackException;
			if (ret != 0)
				throw Exception();
		}
	}
}
