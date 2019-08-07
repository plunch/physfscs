using System;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.IO;
using System.Text;

namespace PhysicsFSCS
{
	unsafe static class NativeMethods
	{
		const string LN = "physfs";

		public const ErrorCode EXCEPTION_ERROR = (ErrorCode)short.MaxValue;
		static ThreadLocal<ExceptionDispatchInfo> threadException = new ThreadLocal<ExceptionDispatchInfo>(trackAllValues: false);

		internal static int Safely(Func<int> action)
		{
			try {
				return action();
			} catch (Exception ex) {
				SetException(ex);
				PHYSFS_setErrorCode((int)EXCEPTION_ERROR);
				return 0;
			}
		}

		internal static void SetException(Exception exception)
		{
			threadException.Value = ExceptionDispatchInfo.Capture(exception);
		}

		internal static Exception Exception()
		{
			return Exception((ErrorCode)PHYSFS_getLastErrorCode());
		}
		internal static unsafe Exception Exception(ErrorCode errorCode)
		{
			if (errorCode == EXCEPTION_ERROR) {
				var ex = threadException.Value;
				threadException.Value = null;
				if (ex == null)
					throw new Exception("Internal error");
				ex.Throw();
			}

			string message = UTF8ToString(PHYSFS_getErrorByCode((int)errorCode));

			if (message == null)
				message = "Unknown error"; // TODO ?

			return Wrap(new PhysicsFSException(errorCode, message));
		}

		static Exception Wrap(PhysicsFSException exception)
		{
			switch(exception.ErrorCode) {
				default:
					return exception;
				case ErrorCode.NotInitialized:
				case ErrorCode.IsInitialized:
				case ErrorCode.NoWriteDirectory:
				case ErrorCode.OpenForReading:
				case ErrorCode.OpenForWriting:
				case ErrorCode.SymlinkForbidden:
					return new InvalidOperationException(exception.Message, exception);
				case ErrorCode.BadFilename:
				case ErrorCode.InvalidArgument:
				case ErrorCode.NotAFile:
					return new ArgumentException(exception.Message, exception);

				case ErrorCode.Unsupported:
					return new NotSupportedException(exception.Message, exception);

				case ErrorCode.NotFound:
					return new DirectoryNotFoundException(exception.Message, exception);

				case ErrorCode.PastEOF:
					return new EndOfStreamException(exception.Message, exception);
				case ErrorCode.IO:
				case ErrorCode.DirectoryNotEmpty:
				case ErrorCode.Busy:
				case ErrorCode.NoSpace:
				case ErrorCode.Permission:
					return new IOException(exception.Message, exception);
				case ErrorCode.Corrupt:
					return new InvalidDataException(exception.Message, exception);
			}
		}

		internal unsafe static int strlen(byte* b)
		{
			byte* ptr = b;

			while(*ptr != 0)
				ptr++;

			return (int)(ptr - b);
		}

		internal static byte[] StringToUTF8(string str)
		{
			return Encoding.UTF8.GetBytes(str + '\0');
		}

		internal unsafe static string UTF8ToString(byte* b)
		{
			if (b == null)
				return null;
			return Encoding.UTF8.GetString(b, strlen(b));
		}





	[StructLayout(LayoutKind.Sequential)]
	public struct PHYSFS_Version
	{
		public byte major, minor, patch;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PHYSFS_Stat
	{
		public long filesize, modtime, createtime, accesstime;
		public int filetype;
		public int @readonly;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct PHYSFS_Allocator
	{
		IntPtr init;
		IntPtr deinit;
		IntPtr malloc;
		IntPtr realloc;
		IntPtr free;

		public Init_fptr Init {
			get => Marshal.GetDelegateForFunctionPointer<Init_fptr>(init);
			set => init = Marshal.GetFunctionPointerForDelegate(value);
		}
		public Deinit_fptr Deinit {
			get => Marshal.GetDelegateForFunctionPointer<Deinit_fptr>(deinit);
			set => deinit = Marshal.GetFunctionPointerForDelegate(value);
		}
		public Malloc_fptr Malloc {
			get => Marshal.GetDelegateForFunctionPointer<Malloc_fptr>(malloc);
			set => malloc = Marshal.GetFunctionPointerForDelegate(value);
		}
		public Realloc_fptr Realloc {
			get => Marshal.GetDelegateForFunctionPointer<Realloc_fptr>(realloc);
			set => realloc = Marshal.GetFunctionPointerForDelegate(value);
		}
		public Free_fptr Free {
			get => Marshal.GetDelegateForFunctionPointer<Free_fptr>(free);
			set => free = Marshal.GetFunctionPointerForDelegate(value);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int Init_fptr();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void Deinit_fptr();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void* Malloc_fptr(ulong size);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void* Realloc_fptr(void* ptr, ulong size);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void Free_fptr(void* ptr);
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct PHYSFS_Io
	{
		public static readonly GCHandle MANAGED = GCHandle.Alloc(new object());

		public int version;
		public IntPtr opaque;

		IntPtr _read, _write, _seek, _tell, _length, _duplicate, _flush, _destroy;

		public readwrite_ptr read {
			get => Marshal.GetDelegateForFunctionPointer<readwrite_ptr>(_read);
			set => _read = Marshal.GetFunctionPointerForDelegate<readwrite_ptr>(value);
		}
		public readwrite_ptr write {
			get => Marshal.GetDelegateForFunctionPointer<readwrite_ptr>(_write);
			set => _write = Marshal.GetFunctionPointerForDelegate<readwrite_ptr>(value);
		}
		public seek_ptr seek {
			get => Marshal.GetDelegateForFunctionPointer<seek_ptr>(_seek);
			set => _seek = Marshal.GetFunctionPointerForDelegate(value);
		}
		public tell_ptr tell {
			get => Marshal.GetDelegateForFunctionPointer<tell_ptr>(_tell);
			set => _tell = Marshal.GetFunctionPointerForDelegate(value);
		}
		public length_ptr length {
			get => Marshal.GetDelegateForFunctionPointer<length_ptr>(_length);
			set => _length = Marshal.GetFunctionPointerForDelegate(value);
		}
		public duplicate_ptr duplicate {
			get => Marshal.GetDelegateForFunctionPointer<duplicate_ptr>(_duplicate);
			set => _duplicate = Marshal.GetFunctionPointerForDelegate(value);
		}
		public flush_ptr flush {
			get => Marshal.GetDelegateForFunctionPointer<flush_ptr>(_flush);
			set => _flush = Marshal.GetFunctionPointerForDelegate(value);
		}
		public destroy_ptr destroy {
			get => Marshal.GetDelegateForFunctionPointer<destroy_ptr>(_destroy);
			set => _destroy = Marshal.GetFunctionPointerForDelegate(value);
		}


		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long readwrite_ptr(PHYSFS_Io* io, 
				          	  [MarshalAs(UnmanagedType.LPArray,
				                     	     ArraySubType = UnmanagedType.U1,
					             	     SizeParamIndex = 2)]
				          	  byte[] buf,
					  	  ulong len);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int seek_ptr(PHYSFS_Io* io, ulong offset);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long tell_ptr(PHYSFS_Io* io);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long length_ptr(PHYSFS_Io* io);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate PHYSFS_Io* duplicate_ptr(PHYSFS_Io* io);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int flush_ptr(PHYSFS_Io* io);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void destroy_ptr(PHYSFS_Io* io);
	}


	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct PHYSFS_ArchiveInfo
	{
		public byte* extension;
		public byte* description;
		public byte* author;
		public byte* url;
		public int supportsSymlinks;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct PHYSFS_Archiver
	{
		public uint version; // must be 1
		public PHYSFS_ArchiveInfo info;

		IntPtr openArchive;
		IntPtr enumerate;
		IntPtr openRead, openWrite, openAppend;
		IntPtr remove;
		IntPtr mkdir;
		IntPtr stat;
		IntPtr closeArchive;


		public openArchive_ptr OpenArchive {
			get => Marshal.GetDelegateForFunctionPointer<openArchive_ptr>(openArchive);
			set => openArchive = Marshal.GetFunctionPointerForDelegate(value);
		}
		public enumerate_ptr Enumerate {
			get => Marshal.GetDelegateForFunctionPointer<enumerate_ptr>(enumerate);
			set => enumerate = Marshal.GetFunctionPointerForDelegate(value);
		}
		public open_ptr OpenRead {
			get => Marshal.GetDelegateForFunctionPointer<open_ptr>(openRead);
			set => openRead = Marshal.GetFunctionPointerForDelegate(value);
		}
		public open_ptr OpenWrite {
			get => Marshal.GetDelegateForFunctionPointer<open_ptr>(openWrite);
			set => openWrite = Marshal.GetFunctionPointerForDelegate(value);
		}
		public open_ptr OpenAppend {
			get => Marshal.GetDelegateForFunctionPointer<open_ptr>(openAppend);
			set => openAppend = Marshal.GetFunctionPointerForDelegate(value);
		}
		public remove_ptr Remove {
			get => Marshal.GetDelegateForFunctionPointer<remove_ptr>(remove);
			set => remove = Marshal.GetFunctionPointerForDelegate(value);
		}
		public mkdir_ptr CreateDirectory {
			get => Marshal.GetDelegateForFunctionPointer<mkdir_ptr>(mkdir);
			set => mkdir = Marshal.GetFunctionPointerForDelegate(value);
		}
		public stat_ptr Stat {
			get => Marshal.GetDelegateForFunctionPointer<stat_ptr>(stat);
			set => stat = Marshal.GetFunctionPointerForDelegate(value);
		}
		public closeArchive_ptr CloseArchive {
			get => Marshal.GetDelegateForFunctionPointer<closeArchive_ptr>(closeArchive);
			set => closeArchive = Marshal.GetFunctionPointerForDelegate(value);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void* openArchive_ptr(PHYSFS_Io* io, byte* name,
				                      int forWrite, int* claimed);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int enumerate_ptr(void* opaque, byte* dirname,
				                  PHYSFS_EnumerateCallback cb,
					          byte* origdir, void* callbackdata);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate PHYSFS_Io* open_ptr(void* opaque, byte* fnm);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int remove_ptr(void* opaque, byte* fnm);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int mkdir_ptr(void* opaque, byte* fnm);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int stat_ptr(void* opaque, byte* fnm, PHYSFS_Stat* stat);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void closeArchive_ptr(void* opaque);
	}


/**
 * \fn void PHYSFS_getLinkedVersion(PHYSFS_Version *ver)
 * \brief Get the version of PhysicsFS that is linked against your program.
 *
 * If you are using a shared library (DLL) version of PhysFS, then it is
 *  possible that it will be different than the version you compiled against.
 *
 * This is a real function; the macro PHYSFS_VERSION tells you what version
 *  of PhysFS you compiled against:
 *
 * \code
 * PHYSFS_Version compiled;
 * PHYSFS_Version linked;
 *
 * PHYSFS_VERSION(&compiled);
 * PHYSFS_getLinkedVersion(&linked);
 * printf("We compiled against PhysFS version %d.%d.%d ...\n",
 *           compiled.major, compiled.minor, compiled.patch);
 * printf("But we linked against PhysFS version %d.%d.%d.\n",
 *           linked.major, linked.minor, linked.patch);
 * \endcode
 *
 * This function may be called safely at any time, even before PHYSFS_init().
 *
 * \sa PHYSFS_VERSION
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern void PHYSFS_getLinkedVersion(PHYSFS_Version *ver);


/**
 * \fn int PHYSFS_init(const char *argv0)
 * \brief Initialize the PhysicsFS library.
 *
 * This must be called before any other PhysicsFS function.
 *
 * This should be called prior to any attempts to change your process's
 *  current working directory.
 *
 *   \param argv0 the argv[0] string passed to your program's mainline.
 *          This may be NULL on most platforms (such as ones without a
 *          standard main() function), but you should always try to pass
 *          something in here. Unix-like systems such as Linux _need_ to
 *          pass argv[0] from main() in here.
 *  \return nonzero on success, zero on error. Specifics of the error can be
 *          gleaned from PHYSFS_getLastError().
 *
 * \sa PHYSFS_deinit
 * \sa PHYSFS_isInit
 */
//public static extern int PHYSFS_init(const char *argv0);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_init(byte* argv0);


/**
 * \fn int PHYSFS_deinit(void)
 * \brief Deinitialize the PhysicsFS library.
 *
 * This closes any files opened via PhysicsFS, blanks the search/write paths,
 *  frees memory, and invalidates all of your file handles.
 *
 * Note that this call can FAIL if there's a file open for writing that
 *  refuses to close (for example, the underlying operating system was
 *  buffering writes to network filesystem, and the fileserver has crashed,
 *  or a hard drive has failed, etc). It is usually best to close all write
 *  handles yourself before calling this function, so that you can gracefully
 *  handle a specific failure.
 *
 * Once successfully deinitialized, PHYSFS_init() can be called again to
 *  restart the subsystem. All default API states are restored at this
 *  point, with the exception of any custom allocator you might have
 *  specified, which survives between initializations.
 *
 *  \return nonzero on success, zero on error. Specifics of the error can be
 *          gleaned from PHYSFS_getLastError(). If failure, state of PhysFS is
 *          undefined, and probably badly screwed up.
 *
 * \sa PHYSFS_init
 * \sa PHYSFS_isInit
 */
//public static extern int PHYSFS_deinit(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_deinit();


/**
 * \fn const PHYSFS_ArchiveInfo **PHYSFS_supportedArchiveTypes(void)
 * \brief Get a list of supported archive types.
 *
 * Get a list of archive types supported by this implementation of PhysicFS.
 *  These are the file formats usable for search path entries. This is for
 *  informational purposes only. Note that the extension listed is merely
 *  convention: if we list "ZIP", you can open a PkZip-compatible archive
 *  with an extension of "XYZ", if you like.
 *
 * The returned value is an array of pointers to PHYSFS_ArchiveInfo structures,
 *  with a NULL entry to signify the end of the list:
 *
 * \code
 * PHYSFS_ArchiveInfo **i;
 *
 * for (i = PHYSFS_supportedArchiveTypes(); *i != NULL; i++)
 * {
 *     printf("Supported archive: [%s], which is [%s].\n",
 *              (*i)->extension, (*i)->description);
 * }
 * \endcode
 *
 * The return values are pointers to internal memory, and should
 *  be considered READ ONLY, and never freed. The returned values are
 *  valid until the next call to PHYSFS_deinit(), PHYSFS_registerArchiver(),
 *  or PHYSFS_deregisterArchiver().
 *
 *   \return READ ONLY Null-terminated array of READ ONLY structures.
 *
 * \sa PHYSFS_registerArchiver
 * \sa PHYSFS_deregisterArchiver
 */
//public static extern const PHYSFS_ArchiveInfo **PHYSFS_supportedArchiveTypes(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern PHYSFS_ArchiveInfo** PHYSFS_supportedArchiveTypes();


/**
 * \fn void PHYSFS_freeList(void *listVar)
 * \brief Deallocate resources of lists returned by PhysicsFS.
 *
 * Certain PhysicsFS functions return lists of information that are
 *  dynamically allocated. Use this function to free those resources.
 *
 * It is safe to pass a NULL here, but doing so will cause a crash in versions
 *  before PhysicsFS 2.1.0.
 *
 *   \param listVar List of information specified as freeable by this function.
 *                  Passing NULL is safe; it is a valid no-op.
 *
 * \sa PHYSFS_getCdRomDirs
 * \sa PHYSFS_enumerateFiles
 * \sa PHYSFS_getSearchPath
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
//public static extern void PHYSFS_freeList(void *listVar);
public static extern void PHYSFS_freeList(void* listVar);


/**
 * \fn void PHYSFS_permitSymbolicLinks(int allow)
 * \brief Enable or disable following of symbolic links.
 *
 * Some physical filesystems and archives contain files that are just pointers
 *  to other files. On the physical filesystem, opening such a link will
 *  (transparently) open the file that is pointed to.
 *
 * By default, PhysicsFS will check if a file is really a symlink during open
 *  calls and fail if it is. Otherwise, the link could take you outside the
 *  write and search paths, and compromise security.
 *
 * If you want to take that risk, call this function with a non-zero parameter.
 *  Note that this is more for sandboxing a program's scripting language, in
 *  case untrusted scripts try to compromise the system. Generally speaking,
 *  a user could very well have a legitimate reason to set up a symlink, so
 *  unless you feel there's a specific danger in allowing them, you should
 *  permit them.
 *
 * Symlinks are only explicitly checked when dealing with filenames
 *  in platform-independent notation. That is, when setting up your
 *  search and write paths, etc, symlinks are never checked for.
 *
 * Please note that PHYSFS_stat() will always check the path specified; if
 *  that path is a symlink, it will not be followed in any case. If symlinks
 *  aren't permitted through this function, PHYSFS_stat() ignores them, and
 *  would treat the query as if the path didn't exist at all.
 *
 * Symbolic link permission can be enabled or disabled at any time after
 *  you've called PHYSFS_init(), and is disabled by default.
 *
 *   \param allow nonzero to permit symlinks, zero to deny linking.
 *
 * \sa PHYSFS_symbolicLinksPermitted
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern void PHYSFS_permitSymbolicLinks(int allow);


/**
 * \fn char **PHYSFS_getCdRomDirs(void)
 * \brief Get an array of paths to available CD-ROM drives.
 *
 * The dirs returned are platform-dependent ("D:\" on Win32, "/cdrom" or
 *  whatnot on Unix). Dirs are only returned if there is a disc ready and
 *  accessible in the drive. So if you've got two drives (D: and E:), and only
 *  E: has a disc in it, then that's all you get. If the user inserts a disc
 *  in D: and you call this function again, you get both drives. If, on a
 *  Unix box, the user unmounts a disc and remounts it elsewhere, the next
 *  call to this function will reflect that change.
 *
 * This function refers to "CD-ROM" media, but it really means "inserted disc
 *  media," such as DVD-ROM, HD-DVD, CDRW, and Blu-Ray discs. It looks for
 *  filesystems, and as such won't report an audio CD, unless there's a
 *  mounted filesystem track on it.
 *
 * The returned value is an array of strings, with a NULL entry to signify the
 *  end of the list:
 *
 * \code
 * char **cds = PHYSFS_getCdRomDirs();
 * char **i;
 *
 * for (i = cds; *i != NULL; i++)
 *     printf("cdrom dir [%s] is available.\n", *i);
 *
 * PHYSFS_freeList(cds);
 * \endcode
 *
 * This call may block while drives spin up. Be forewarned.
 *
 * When you are done with the returned information, you may dispose of the
 *  resources by calling PHYSFS_freeList() with the returned pointer.
 *
 *   \return Null-terminated array of null-terminated strings.
 *
 * \sa PHYSFS_getCdRomDirsCallback
 */
//public static extern char **PHYSFS_getCdRomDirs(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte** PHYSFS_getCdRomDirs();


/**
 * \fn const char *PHYSFS_getBaseDir(void)
 * \brief Get the path where the application resides.
 *
 * Helper function.
 *
 * Get the "base dir". This is the directory where the application was run
 *  from, which is probably the installation directory, and may or may not
 *  be the process's current working directory.
 *
 * You should probably use the base dir in your search path.
 *
 *  \return READ ONLY string of base dir in platform-dependent notation.
 *
 * \sa PHYSFS_getPrefDir
 */
//public static extern const char *PHYSFS_getBaseDir(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getBaseDir();


/**
 * \fn const char *PHYSFS_getUserDir(void)
 * \brief Get the path where user's home directory resides.
 *
 * \deprecated As of PhysicsFS 2.1, you probably want PHYSFS_getPrefDir().
 *
 * Helper function.
 *
 * Get the "user dir". This is meant to be a suggestion of where a specific
 *  user of the system can store files. On Unix, this is her home directory.
 *  On systems with no concept of multiple home directories (MacOS, win95),
 *  this will default to something like "C:\mybasedir\users\username"
 *  where "username" will either be the login name, or "default" if the
 *  platform doesn't support multiple users, either.
 *
 *  \return READ ONLY string of user dir in platform-dependent notation.
 *
 * \sa PHYSFS_getBaseDir
 * \sa PHYSFS_getPrefDir
 */
//PHYSFS_DECL const char *PHYSFS_getUserDir(void) PHYSFS_DEPRECATED;
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getUserDir();


/**
 * \fn const char *PHYSFS_getWriteDir(void)
 * \brief Get path where PhysicsFS will allow file writing.
 *
 * Get the current write dir. The default write dir is NULL.
 *
 *  \return READ ONLY string of write dir in platform-dependent notation,
 *           OR NULL IF NO WRITE PATH IS CURRENTLY SET.
 *
 * \sa PHYSFS_setWriteDir
 */
//public static extern const char *PHYSFS_getWriteDir(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getWriteDir();


/**
 * \fn int PHYSFS_setWriteDir(const char *newDir)
 * \brief Tell PhysicsFS where it may write files.
 *
 * Set a new write dir. This will override the previous setting.
 *
 * This call will fail (and fail to change the write dir) if the current
 *  write dir still has files open in it.
 *
 *   \param newDir The new directory to be the root of the write dir,
 *                   specified in platform-dependent notation. Setting to NULL
 *                   disables the write dir, so no files can be opened for
 *                   writing via PhysicsFS.
 *  \return non-zero on success, zero on failure. All attempts to open a file
 *           for writing via PhysicsFS will fail until this call succeeds.
 *           Use PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_getWriteDir
 */
//public static extern int PHYSFS_setWriteDir(const char *newDir);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_setWriteDir(byte* newDir);


/**
 * \fn char **PHYSFS_getSearchPath(void)
 * \brief Get the current search path.
 *
 * The default search path is an empty list.
 *
 * The returned value is an array of strings, with a NULL entry to signify the
 *  end of the list:
 *
 * \code
 * char **i;
 *
 * for (i = PHYSFS_getSearchPath(); *i != NULL; i++)
 *     printf("[%s] is in the search path.\n", *i);
 * \endcode
 *
 * When you are done with the returned information, you may dispose of the
 *  resources by calling PHYSFS_freeList() with the returned pointer.
 *
 *   \return Null-terminated array of null-terminated strings. NULL if there
 *            was a problem (read: OUT OF MEMORY).
 *
 * \sa PHYSFS_getSearchPathCallback
 * \sa PHYSFS_addToSearchPath
 * \sa PHYSFS_removeFromSearchPath
 */
//public static extern char **PHYSFS_getSearchPath(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte** PHYSFS_getSearchPath();


/**
 * \fn int PHYSFS_setSaneConfig(const char *organization, const char *appName, const char *archiveExt, int includeCdRoms, int archivesFirst)
 * \brief Set up sane, default paths.
 *
 * Helper function.
 *
 * The write dir will be set to the pref dir returned by
 *  \code PHYSFS_getPrefDir(organization, appName) \endcode, which is
 *  created if it doesn't exist.
 *
 * The above is sufficient to make sure your program's configuration directory
 *  is separated from other clutter, and platform-independent.
 *
 *  The search path will be:
 *
 *    - The Write Dir (created if it doesn't exist)
 *    - The Base Dir (PHYSFS_getBaseDir())
 *    - All found CD-ROM dirs (optionally)
 *
 * These directories are then searched for files ending with the extension
 *  (archiveExt), which, if they are valid and supported archives, will also
 *  be added to the search path. If you specified "PKG" for (archiveExt), and
 *  there's a file named data.PKG in the base dir, it'll be checked. Archives
 *  can either be appended or prepended to the search path in alphabetical
 *  order, regardless of which directories they were found in. All archives
 *  are mounted in the root of the virtual file system ("/").
 *
 * All of this can be accomplished from the application, but this just does it
 *  all for you. Feel free to add more to the search path manually, too.
 *
 *    \param organization Name of your company/group/etc to be used as a
 *                         dirname, so keep it small, and no-frills.
 *
 *    \param appName Program-specific name of your program, to separate it
 *                   from other programs using PhysicsFS.
 *
 *    \param archiveExt File extension used by your program to specify an
 *                      archive. For example, Quake 3 uses "pk3", even though
 *                      they are just zipfiles. Specify NULL to not dig out
 *                      archives automatically. Do not specify the '.' char;
 *                      If you want to look for ZIP files, specify "ZIP" and
 *                      not ".ZIP" ... the archive search is case-insensitive.
 *
 *    \param includeCdRoms Non-zero to include CD-ROMs in the search path, and
 *                         (if (archiveExt) != NULL) search them for archives.
 *                         This may cause a significant amount of blocking
 *                         while discs are accessed, and if there are no discs
 *                         in the drive (or even not mounted on Unix systems),
 *                         then they may not be made available anyhow. You may
 *                         want to specify zero and handle the disc setup
 *                         yourself.
 *
 *    \param archivesFirst Non-zero to prepend the archives to the search path.
 *                         Zero to append them. Ignored if !(archiveExt).
 *
 *  \return nonzero on success, zero on error. Use PHYSFS_getLastErrorCode()
 *          to obtain the specific error.
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_setSaneConfig(const char *organization,
                                     const char *appName,
                                     const char *archiveExt,
                                     int includeCdRoms,
                                     int archivesFirst);
				     */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_setSaneConfig(byte* organization,
                                       byte* appName,
                                       byte* archiveExt,
                                       int includeCdRoms,
                                       int archivesFirst);



/* Directory management stuff ... */

/**
 * \fn int PHYSFS_mkdir(const char *dirName)
 * \brief Create a directory.
 *
 * This is specified in platform-independent notation in relation to the
 *  write dir. All missing parent directories are also created if they
 *  don't exist.
 *
 * So if you've got the write dir set to "C:\mygame\writedir" and call
 *  PHYSFS_mkdir("downloads/maps") then the directories
 *  "C:\mygame\writedir\downloads" and "C:\mygame\writedir\downloads\maps"
 *  will be created if possible. If the creation of "maps" fails after we
 *  have successfully created "downloads", then the function leaves the
 *  created directory behind and reports failure.
 *
 *   \param dirName New dir to create.
 *  \return nonzero on success, zero on error. Use
 *          PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_delete
 */
//public static extern int PHYSFS_mkdir(const char *dirName);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mkdir(byte* dirName);


/**
 * \fn int PHYSFS_delete(const char *filename)
 * \brief Delete a file or directory.
 *
 * (filename) is specified in platform-independent notation in relation to the
 *  write dir.
 *
 * A directory must be empty before this call can delete it.
 *
 * Deleting a symlink will remove the link, not what it points to, regardless
 *  of whether you "permitSymLinks" or not.
 *
 * So if you've got the write dir set to "C:\mygame\writedir" and call
 *  PHYSFS_delete("downloads/maps/level1.map") then the file
 *  "C:\mygame\writedir\downloads\maps\level1.map" is removed from the
 *  physical filesystem, if it exists and the operating system permits the
 *  deletion.
 *
 * Note that on Unix systems, deleting a file may be successful, but the
 *  actual file won't be removed until all processes that have an open
 *  filehandle to it (including your program) close their handles.
 *
 * Chances are, the bits that make up the file still exist, they are just
 *  made available to be written over at a later point. Don't consider this
 *  a security method or anything.  :)
 *
 *   \param filename Filename to delete.
 *  \return nonzero on success, zero on error. Use PHYSFS_getLastErrorCode()
 *          to obtain the specific error.
 */
//public static extern int PHYSFS_delete(const char *filename);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_delete(byte* filename);


/**
 * \fn const char *PHYSFS_getRealDir(const char *filename)
 * \brief Figure out where in the search path a file resides.
 *
 * The file is specified in platform-independent notation. The returned
 *  filename will be the element of the search path where the file was found,
 *  which may be a directory, or an archive. Even if there are multiple
 *  matches in different parts of the search path, only the first one found
 *  is used, just like when opening a file.
 *
 * So, if you look for "maps/level1.map", and C:\\mygame is in your search
 *  path and C:\\mygame\\maps\\level1.map exists, then "C:\mygame" is returned.
 *
 * If a any part of a match is a symbolic link, and you've not explicitly
 *  permitted symlinks, then it will be ignored, and the search for a match
 *  will continue.
 *
 * If you specify a fake directory that only exists as a mount point, it'll
 *  be associated with the first archive mounted there, even though that
 *  directory isn't necessarily contained in a real archive.
 *
 * \warning This will return NULL if there is no real directory associated
 *          with (filename). Specifically, PHYSFS_mountIo(),
 *          PHYSFS_mountMemory(), and PHYSFS_mountHandle() will return NULL
 *          even if the filename is found in the search path. Plan accordingly.
 *
 *     \param filename file to look for.
 *    \return READ ONLY string of element of search path containing the
 *             the file in question. NULL if not found.
 */
//public static extern const char *PHYSFS_getRealDir(const char *filename);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getRealDir(byte* filename);


/**
 * \fn char **PHYSFS_enumerateFiles(const char *dir)
 * \brief Get a file listing of a search path's directory.
 *
 * \warning In PhysicsFS versions prior to 2.1, this function would return
 *          as many items as it could in the face of a failure condition
 *          (out of memory, disk i/o error, etc). Since this meant apps
 *          couldn't distinguish between complete success and partial failure,
 *          and since the function could always return NULL to report
 *          catastrophic failures anyway, in PhysicsFS 2.1 this function's
 *          policy changed: it will either return a list of complete results
 *          or it will return NULL for any failure of any kind, so we can
 *          guarantee that the enumeration ran to completion and has no gaps
 *          in its results.
 *
 * Matching directories are interpolated. That is, if "C:\mydir" is in the
 *  search path and contains a directory "savegames" that contains "x.sav",
 *  "y.sav", and "z.sav", and there is also a "C:\userdir" in the search path
 *  that has a "savegames" subdirectory with "w.sav", then the following code:
 *
 * \code
 * char **rc = PHYSFS_enumerateFiles("savegames");
 * char **i;
 *
 * for (i = rc; *i != NULL; i++)
 *     printf(" * We've got [%s].\n", *i);
 *
 * PHYSFS_freeList(rc);
 * \endcode
 *
 *  \...will print:
 *
 * \verbatim
 * We've got [x.sav].
 * We've got [y.sav].
 * We've got [z.sav].
 * We've got [w.sav].\endverbatim
 *
 * Feel free to sort the list however you like. However, the returned data
 *  will always contain no duplicates, and will be always sorted in alphabetic
 *  (rather: case-sensitive Unicode) order for you.
 *
 * Don't forget to call PHYSFS_freeList() with the return value from this
 *  function when you are done with it.
 *
 *    \param dir directory in platform-independent notation to enumerate.
 *   \return Null-terminated array of null-terminated strings, or NULL for
 *           failure cases.
 *
 * \sa PHYSFS_enumerate
 */
//public static extern char **PHYSFS_enumerateFiles(const char *dir);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte** PHYSFS_enumerateFiles(byte* dir);


/**
 * \fn int PHYSFS_exists(const char *fname)
 * \brief Determine if a file exists in the search path.
 *
 * Reports true if there is an entry anywhere in the search path by the
 *  name of (fname).
 *
 * Note that entries that are symlinks are ignored if
 *  PHYSFS_permitSymbolicLinks(1) hasn't been called, so you
 *  might end up further down in the search path than expected.
 *
 *    \param fname filename in platform-independent notation.
 *   \return non-zero if filename exists. zero otherwise.
 */
//public static extern int PHYSFS_exists(const char *fname);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_exists(byte* fname);


/* i/o stuff... */

/**
 * \fn PHYSFS_File *PHYSFS_openWrite(const char *filename)
 * \brief Open a file for writing.
 *
 * Open a file for writing, in platform-independent notation and in relation
 *  to the write dir as the root of the writable filesystem. The specified
 *  file is created if it doesn't exist. If it does exist, it is truncated to
 *  zero bytes, and the writing offset is set to the start.
 *
 * Note that entries that are symlinks are ignored if
 *  PHYSFS_permitSymbolicLinks(1) hasn't been called, and opening a
 *  symlink with this function will fail in such a case.
 *
 *   \param filename File to open.
 *  \return A valid PhysicsFS filehandle on success, NULL on error. Use
 *          PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_openRead
 * \sa PHYSFS_openAppend
 * \sa PHYSFS_write
 * \sa PHYSFS_close
 */
//public static extern PHYSFS_File *PHYSFS_openWrite(const char *filename);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern SafePhysicsFSFileHandle PHYSFS_openWrite(byte* filename);


/**
 * \fn PHYSFS_File *PHYSFS_openAppend(const char *filename)
 * \brief Open a file for appending.
 *
 * Open a file for writing, in platform-independent notation and in relation
 *  to the write dir as the root of the writable filesystem. The specified
 *  file is created if it doesn't exist. If it does exist, the writing offset
 *  is set to the end of the file, so the first write will be the byte after
 *  the end.
 *
 * Note that entries that are symlinks are ignored if
 *  PHYSFS_permitSymbolicLinks(1) hasn't been called, and opening a
 *  symlink with this function will fail in such a case.
 *
 *   \param filename File to open.
 *  \return A valid PhysicsFS filehandle on success, NULL on error. Use
 *          PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_openRead
 * \sa PHYSFS_openWrite
 * \sa PHYSFS_write
 * \sa PHYSFS_close
 */
//public static extern PHYSFS_File *PHYSFS_openAppend(const char* filename);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern SafePhysicsFSFileHandle PHYSFS_openAppend(byte* filename);


/**
 * \fn PHYSFS_File *PHYSFS_openRead(const char *filename)
 * \brief Open a file for reading.
 *
 * Open a file for reading, in platform-independent notation. The search path
 *  is checked one at a time until a matching file is found, in which case an
 *  abstract filehandle is associated with it, and reading may be done.
 *  The reading offset is set to the first byte of the file.
 *
 * Note that entries that are symlinks are ignored if
 *  PHYSFS_permitSymbolicLinks(1) hasn't been called, and opening a
 *  symlink with this function will fail in such a case.
 *
 *   \param filename File to open.
 *  \return A valid PhysicsFS filehandle on success, NULL on error.
 *          Use PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_openWrite
 * \sa PHYSFS_openAppend
 * \sa PHYSFS_read
 * \sa PHYSFS_close
 */
//public static extern PHYSFS_File *PHYSFS_openRead(const char *filename);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern SafePhysicsFSFileHandle PHYSFS_openRead(byte* filename);


/**
 * \fn int PHYSFS_close(PHYSFS_File *handle)
 * \brief Close a PhysicsFS filehandle.
 *
 * This call is capable of failing if the operating system was buffering
 *  writes to the physical media, and, now forced to write those changes to
 *  physical media, can not store the data for some reason. In such a case,
 *  the filehandle stays open. A well-written program should ALWAYS check the
 *  return value from the close call in addition to every writing call!
 *
 *   \param handle handle returned from PHYSFS_open*().
 *  \return nonzero on success, zero on error. Use PHYSFS_getLastErrorCode()
 *          to obtain the specific error.
 *
 * \sa PHYSFS_openRead
 * \sa PHYSFS_openWrite
 * \sa PHYSFS_openAppend
 */
//public static extern int PHYSFS_close(PHYSFS_File *handle);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_close(IntPtr handle);


/* File position stuff... */

/**
 * \fn int PHYSFS_eof(PHYSFS_File *handle)
 * \brief Check for end-of-file state on a PhysicsFS filehandle.
 *
 * Determine if the end of file has been reached in a PhysicsFS filehandle.
 *
 *   \param handle handle returned from PHYSFS_openRead().
 *  \return nonzero if EOF, zero if not.
 *
 * \sa PHYSFS_read
 * \sa PHYSFS_tell
 */
//public static extern int PHYSFS_eof(PHYSFS_File *handle);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_eof(SafePhysicsFSFileHandle handle);


/**
 * \fn PHYSFS_sint64 PHYSFS_tell(PHYSFS_File *handle)
 * \brief Determine current position within a PhysicsFS filehandle.
 *
 *   \param handle handle returned from PHYSFS_open*().
 *  \return offset in bytes from start of file. -1 if error occurred.
 *           Use PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_seek
 */
//public static extern PHYSFS_sint64 PHYSFS_tell(PHYSFS_File *handle);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern long PHYSFS_tell(SafePhysicsFSFileHandle handle);


/**
 * \fn int PHYSFS_seek(PHYSFS_File *handle, PHYSFS_uint64 pos)
 * \brief Seek to a new position within a PhysicsFS filehandle.
 *
 * The next read or write will occur at that place. Seeking past the
 *  beginning or end of the file is not allowed, and causes an error.
 *
 *   \param handle handle returned from PHYSFS_open*().
 *   \param pos number of bytes from start of file to seek to.
 *  \return nonzero on success, zero on error. Use PHYSFS_getLastErrorCode()
 *          to obtain the specific error.
 *
 * \sa PHYSFS_tell
 */
//public static extern int PHYSFS_seek(PHYSFS_File *handle, PHYSFS_uint64 pos);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_seek(SafePhysicsFSFileHandle handle, ulong pos);


/**
 * \fn PHYSFS_sint64 PHYSFS_fileLength(PHYSFS_File *handle)
 * \brief Get total length of a file in bytes.
 *
 * Note that if another process/thread is writing to this file at the same
 *  time, then the information this function supplies could be incorrect
 *  before you get it. Use with caution, or better yet, don't use at all.
 *
 *   \param handle handle returned from PHYSFS_open*().
 *  \return size in bytes of the file. -1 if can't be determined.
 *
 * \sa PHYSFS_tell
 * \sa PHYSFS_seek
 */
//public static extern PHYSFS_sint64 PHYSFS_fileLength(PHYSFS_File *handle);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern long PHYSFS_fileLength(SafePhysicsFSFileHandle handle);


/* Buffering stuff... */

/**
 * \fn int PHYSFS_setBuffer(PHYSFS_File *handle, PHYSFS_uint64 bufsize)
 * \brief Set up buffering for a PhysicsFS file handle.
 *
 * Define an i/o buffer for a file handle. A memory block of (bufsize) bytes
 *  will be allocated and associated with (handle).
 *
 * For files opened for reading, up to (bufsize) bytes are read from (handle)
 *  and stored in the internal buffer. Calls to PHYSFS_read() will pull
 *  from this buffer until it is empty, and then refill it for more reading.
 *  Note that compressed files, like ZIP archives, will decompress while
 *  buffering, so this can be handy for offsetting CPU-intensive operations.
 *  The buffer isn't filled until you do your next read.
 *
 * For files opened for writing, data will be buffered to memory until the
 *  buffer is full or the buffer is flushed. Closing a handle implicitly
 *  causes a flush...check your return values!
 *
 * Seeking, etc transparently accounts for buffering.
 *
 * You can resize an existing buffer by calling this function more than once
 *  on the same file. Setting the buffer size to zero will free an existing
 *  buffer.
 *
 * PhysicsFS file handles are unbuffered by default.
 *
 * Please check the return value of this function! Failures can include
 *  not being able to seek backwards in a read-only file when removing the
 *  buffer, not being able to allocate the buffer, and not being able to
 *  flush the buffer to disk, among other unexpected problems.
 *
 *   \param handle handle returned from PHYSFS_open*().
 *   \param bufsize size, in bytes, of buffer to allocate.
 *  \return nonzero if successful, zero on error.
 *
 * \sa PHYSFS_flush
 * \sa PHYSFS_read
 * \sa PHYSFS_write
 * \sa PHYSFS_close
 */
//public static extern int PHYSFS_setBuffer(PHYSFS_File *handle, PHYSFS_uint64 bufsize);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_setBuffer(SafePhysicsFSFileHandle handle, ulong bufsize);


/**
 * \fn int PHYSFS_flush(PHYSFS_File *handle)
 * \brief Flush a buffered PhysicsFS file handle.
 *
 * For buffered files opened for writing, this will put the current contents
 *  of the buffer to disk and flag the buffer as empty if possible.
 *
 * For buffered files opened for reading or unbuffered files, this is a safe
 *  no-op, and will report success.
 *
 *   \param handle handle returned from PHYSFS_open*().
 *  \return nonzero if successful, zero on error.
 *
 * \sa PHYSFS_setBuffer
 * \sa PHYSFS_close
 */
//public static extern int PHYSFS_flush(PHYSFS_File *handle);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_flush(SafePhysicsFSFileHandle handle);



/* Everything above this line is part of the PhysicsFS 1.0 API. */

/**
 * \fn int PHYSFS_isInit(void)
 * \brief Determine if the PhysicsFS library is initialized.
 *
 * Once PHYSFS_init() returns successfully, this will return non-zero.
 *  Before a successful PHYSFS_init() and after PHYSFS_deinit() returns
 *  successfully, this will return zero. This function is safe to call at
 *  any time.
 *
 *  \return non-zero if library is initialized, zero if library is not.
 *
 * \sa PHYSFS_init
 * \sa PHYSFS_deinit
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_isInit();


/**
 * \fn int PHYSFS_symbolicLinksPermitted(void)
 * \brief Determine if the symbolic links are permitted.
 *
 * This reports the setting from the last call to PHYSFS_permitSymbolicLinks().
 *  If PHYSFS_permitSymbolicLinks() hasn't been called since the library was
 *  last initialized, symbolic links are implicitly disabled.
 *
 *  \return non-zero if symlinks are permitted, zero if not.
 *
 * \sa PHYSFS_permitSymbolicLinks
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_symbolicLinksPermitted();


/**
 * \struct PHYSFS_Allocator
 * \brief PhysicsFS allocation function pointers.
 *
 * (This is for limited, hardcore use. If you don't immediately see a need
 *  for it, you can probably ignore this forever.)
 *
 * You create one of these structures for use with PHYSFS_setAllocator.
 *  Allocators are assumed to be reentrant by the caller; please mutex
 *  accordingly.
 *
 * Allocations are always discussed in 64-bits, for future expansion...we're
 *  on the cusp of a 64-bit transition, and we'll probably be allocating 6
 *  gigabytes like it's nothing sooner or later, and I don't want to change
 *  this again at that point. If you're on a 32-bit platform and have to
 *  downcast, it's okay to return NULL if the allocation is greater than
 *  4 gigabytes, since you'd have to do so anyhow.
 *
 * \sa PHYSFS_setAllocator
 */
#if false
typedef struct PHYSFS_Allocator
{
    int (*Init)(void);   /**< Initialize. Can be NULL. Zero on failure. */
    void (*Deinit)(void);  /**< Deinitialize your allocator. Can be NULL. */
    void *(*Malloc)(PHYSFS_uint64);  /**< Allocate like malloc(). */
    void *(*Realloc)(void *, PHYSFS_uint64); /**< Reallocate like realloc(). */
    void (*Free)(void *); /**< Free memory from Malloc or Realloc. */
} PHYSFS_Allocator;
#endif


/**
 * \fn int PHYSFS_setAllocator(const PHYSFS_Allocator *allocator)
 * \brief Hook your own allocation routines into PhysicsFS.
 *
 * (This is for limited, hardcore use. If you don't immediately see a need
 *  for it, you can probably ignore this forever.)
 *
 * By default, PhysicsFS will use whatever is reasonable for a platform
 *  to manage dynamic memory (usually ANSI C malloc/realloc/free, but
 *  some platforms might use something else), but in some uncommon cases, the
 *  app might want more control over the library's memory management. This
 *  lets you redirect PhysicsFS to use your own allocation routines instead.
 *  You can only call this function before PHYSFS_init(); if the library is
 *  initialized, it'll reject your efforts to change the allocator mid-stream.
 *  You may call this function after PHYSFS_deinit() if you are willing to
 *  shut down the library and restart it with a new allocator; this is a safe
 *  and supported operation. The allocator remains intact between deinit/init
 *  calls. If you want to return to the platform's default allocator, pass a
 *  NULL in here.
 *
 * If you aren't immediately sure what to do with this function, you can
 *  safely ignore it altogether.
 *
 *    \param allocator Structure containing your allocator's entry points.
 *   \return zero on failure, non-zero on success. This call only fails
 *           when used between PHYSFS_init() and PHYSFS_deinit() calls.
 */
//public static extern int PHYSFS_setAllocator(const PHYSFS_Allocator *allocator);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_setAllocator(PHYSFS_Allocator* allocator);


/**
 * \fn int PHYSFS_mount(const char *newDir, const char *mountPoint, int appendToPath)
 * \brief Add an archive or directory to the search path.
 *
 * If this is a duplicate, the entry is not added again, even though the
 *  function succeeds. You may not add the same archive to two different
 *  mountpoints: duplicate checking is done against the archive and not the
 *  mountpoint.
 *
 * When you mount an archive, it is added to a virtual file system...all files
 *  in all of the archives are interpolated into a single hierachical file
 *  tree. Two archives mounted at the same place (or an archive with files
 *  overlapping another mountpoint) may have overlapping files: in such a case,
 *  the file earliest in the search path is selected, and the other files are
 *  inaccessible to the application. This allows archives to be used to
 *  override previous revisions; you can use the mounting mechanism to place
 *  archives at a specific point in the file tree and prevent overlap; this
 *  is useful for downloadable mods that might trample over application data
 *  or each other, for example.
 *
 * The mountpoint does not need to exist prior to mounting, which is different
 *  than those familiar with the Unix concept of "mounting" may not expect.
 *  As well, more than one archive can be mounted to the same mountpoint, or
 *  mountpoints and archive contents can overlap...the interpolation mechanism
 *  still functions as usual.
 *
 *   \param newDir directory or archive to add to the path, in
 *                   platform-dependent notation.
 *   \param mountPoint Location in the interpolated tree that this archive
 *                     will be "mounted", in platform-independent notation.
 *                     NULL or "" is equivalent to "/".
 *   \param appendToPath nonzero to append to search path, zero to prepend.
 *  \return nonzero if added to path, zero on failure (bogus archive, dir
 *          missing, etc). Use PHYSFS_getLastErrorCode() to obtain
 *          the specific error.
 *
 * \sa PHYSFS_removeFromSearchPath
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_getMountPoint
 * \sa PHYSFS_mountIo
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mount(const char *newDir,
                             const char *mountPoint,
                             int appendToPath);
			     */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mount(byte* newDir,
                               byte* mountPoint,
                               int appendToPath);

/**
 * \fn int PHYSFS_getMountPoint(const char *dir)
 * \brief Determine a mounted archive's mountpoint.
 *
 * You give this function the name of an archive or dir you successfully
 *  added to the search path, and it reports the location in the interpolated
 *  tree where it is mounted. Files mounted with a NULL mountpoint or through
 *  PHYSFS_addToSearchPath() will report "/". The return value is READ ONLY
 *  and valid until the archive is removed from the search path.
 *
 *   \param dir directory or archive previously added to the path, in
 *              platform-dependent notation. This must match the string
 *              used when adding, even if your string would also reference
 *              the same file with a different string of characters.
 *  \return READ-ONLY string of mount point if added to path, NULL on failure
 *          (bogus archive, etc). Use PHYSFS_getLastErrorCode() to obtain the
 *          specific error.
 *
 * \sa PHYSFS_removeFromSearchPath
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_getMountPoint
 */
//public static extern const char *PHYSFS_getMountPoint(const char *dir);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getMountPoint(byte* dir);


/**
 * \typedef PHYSFS_StringCallback
 * \brief Function signature for callbacks that report strings.
 *
 * These are used to report a list of strings to an original caller, one
 *  string per callback. All strings are UTF-8 encoded. Functions should not
 *  try to modify or free the string's memory.
 *
 * These callbacks are used, starting in PhysicsFS 1.1, as an alternative to
 *  functions that would return lists that need to be cleaned up with
 *  PHYSFS_freeList(). The callback means that the library doesn't need to
 *  allocate an entire list and all the strings up front.
 *
 * Be aware that promises data ordering in the list versions are not
 *  necessarily so in the callback versions. Check the documentation on
 *  specific APIs, but strings may not be sorted as you expect.
 *
 *    \param data User-defined data pointer, passed through from the API
 *                that eventually called the callback.
 *    \param str The string data about which the callback is meant to inform.
 *
 * \sa PHYSFS_getCdRomDirsCallback
 * \sa PHYSFS_getSearchPathCallback
 */
//typedef void (*PHYSFS_StringCallback)(void *data, const char *str);
public delegate void PHYSFS_StringCallback(void* data, byte* str);


/**
 * \typedef PHYSFS_EnumFilesCallback
 * \brief Function signature for callbacks that enumerate files.
 *
 * \warning As of PhysicsFS 2.1, Use PHYSFS_EnumerateCallback with
 *  PHYSFS_enumerate() instead; it gives you more control over the process.
 *
 * These are used to report a list of directory entries to an original caller,
 *  one file/dir/symlink per callback. All strings are UTF-8 encoded.
 *  Functions should not try to modify or free any string's memory.
 *
 * These callbacks are used, starting in PhysicsFS 1.1, as an alternative to
 *  functions that would return lists that need to be cleaned up with
 *  PHYSFS_freeList(). The callback means that the library doesn't need to
 *  allocate an entire list and all the strings up front.
 *
 * Be aware that promised data ordering in the list versions are not
 *  necessarily so in the callback versions. Check the documentation on
 *  specific APIs, but strings may not be sorted as you expect and you might
 *  get duplicate strings.
 *
 *    \param data User-defined data pointer, passed through from the API
 *                that eventually called the callback.
 *    \param origdir A string containing the full path, in platform-independent
 *                   notation, of the directory containing this file. In most
 *                   cases, this is the directory on which you requested
 *                   enumeration, passed in the callback for your convenience.
 *    \param fname The filename that is being enumerated. It may not be in
 *                 alphabetical order compared to other callbacks that have
 *                 fired, and it will not contain the full path. You can
 *                 recreate the fullpath with $origdir/$fname ... The file
 *                 can be a subdirectory, a file, a symlink, etc.
 *
 * \sa PHYSFS_enumerateFilesCallback
 */
/*
typedef void (*PHYSFS_EnumFilesCallback)(void *data, const char *origdir,
                                         const char *fname);
					 */
public delegate void PHYSFS_EnumFilesCallback(void* data, byte* origdir, byte* fname);


/**
 * \fn void PHYSFS_getCdRomDirsCallback(PHYSFS_StringCallback c, void *d)
 * \brief Enumerate CD-ROM directories, using an application-defined callback.
 *
 * Internally, PHYSFS_getCdRomDirs() just calls this function and then builds
 *  a list before returning to the application, so functionality is identical
 *  except for how the information is represented to the application.
 *
 * Unlike PHYSFS_getCdRomDirs(), this function does not return an array.
 *  Rather, it calls a function specified by the application once per
 *  detected disc:
 *
 * \code
 *
 * static void foundDisc(void *data, const char *cddir)
 * {
 *     printf("cdrom dir [%s] is available.\n", cddir);
 * }
 *
 * // ...
 * PHYSFS_getCdRomDirsCallback(foundDisc, NULL);
 * \endcode
 *
 * This call may block while drives spin up. Be forewarned.
 *
 *    \param c Callback function to notify about detected drives.
 *    \param d Application-defined data passed to callback. Can be NULL.
 *
 * \sa PHYSFS_StringCallback
 * \sa PHYSFS_getCdRomDirs
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern void PHYSFS_getCdRomDirsCallback(PHYSFS_StringCallback c, void* d);


/**
 * \fn void PHYSFS_getSearchPathCallback(PHYSFS_StringCallback c, void *d)
 * \brief Enumerate the search path, using an application-defined callback.
 *
 * Internally, PHYSFS_getSearchPath() just calls this function and then builds
 *  a list before returning to the application, so functionality is identical
 *  except for how the information is represented to the application.
 *
 * Unlike PHYSFS_getSearchPath(), this function does not return an array.
 *  Rather, it calls a function specified by the application once per
 *  element of the search path:
 *
 * \code
 *
 * static void printSearchPath(void *data, const char *pathItem)
 * {
 *     printf("[%s] is in the search path.\n", pathItem);
 * }
 *
 * // ...
 * PHYSFS_getSearchPathCallback(printSearchPath, NULL);
 * \endcode
 *
 * Elements of the search path are reported in order search priority, so the
 *  first archive/dir that would be examined when looking for a file is the
 *  first element passed through the callback.
 *
 *    \param c Callback function to notify about search path elements.
 *    \param d Application-defined data passed to callback. Can be NULL.
 *
 * \sa PHYSFS_StringCallback
 * \sa PHYSFS_getSearchPath
 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern void PHYSFS_getSearchPathCallback(PHYSFS_StringCallback c, void* d);


/* Everything above this line is part of the PhysicsFS 2.0 API. */

/**
 * \typedef PHYSFS_EnumerateCallback
 * \brief Possible return values from PHYSFS_EnumerateCallback.
 *
 * These values dictate if an enumeration callback should continue to fire,
 *  or stop (and why it is stopping).
 *
 * \sa PHYSFS_EnumerateCallback
 * \sa PHYSFS_enumerate
 */
#if false
typedef enum PHYSFS_EnumerateCallbackResult
{
    PHYSFS_ENUM_ERROR = -1,   /**< Stop enumerating, report error to app. */
    PHYSFS_ENUM_STOP = 0,     /**< Stop enumerating, report success to app. */
    PHYSFS_ENUM_OK = 1        /**< Keep enumerating, no problems */
} PHYSFS_EnumerateCallbackResult;
#endif
public const int PHYSFS_ENUM_ERROR = -1;
public const int PHYSFS_ENUM_STOP  = 0;
public const int PHYSFS_ENUM_OK    = 1;

/**
 * \typedef PHYSFS_EnumerateCallback
 * \brief Function signature for callbacks that enumerate and return results.
 *
 * This is the same thing as PHYSFS_EnumFilesCallback from PhysicsFS 2.0,
 *  except it can return a result from the callback: namely: if you're looking
 *  for something specific, once you find it, you can tell PhysicsFS to stop
 *  enumerating further. This is used with PHYSFS_enumerate(), which we
 *  hopefully got right this time.  :)
 *
 *    \param data User-defined data pointer, passed through from the API
 *                that eventually called the callback.
 *    \param origdir A string containing the full path, in platform-independent
 *                   notation, of the directory containing this file. In most
 *                   cases, this is the directory on which you requested
 *                   enumeration, passed in the callback for your convenience.
 *    \param fname The filename that is being enumerated. It may not be in
 *                 alphabetical order compared to other callbacks that have
 *                 fired, and it will not contain the full path. You can
 *                 recreate the fullpath with $origdir/$fname ... The file
 *                 can be a subdirectory, a file, a symlink, etc.
 *   \return A value from PHYSFS_EnumerateCallbackResult.
 *           All other values are (currently) undefined; don't use them.
 *
 * \sa PHYSFS_enumerate
 * \sa PHYSFS_EnumerateCallbackResult
 */
/*
typedef PHYSFS_EnumerateCallbackResult (*PHYSFS_EnumerateCallback)(void *data,
                                       const char *origdir, const char *fname);
				       */
public delegate int PHYSFS_EnumerateCallback(void* data, byte* origdir, byte* fname);

/**
 * \fn int PHYSFS_enumerate(const char *dir, PHYSFS_EnumerateCallback c, void *d)
 * \brief Get a file listing of a search path's directory, using an application-defined callback, with errors reported.
 *
 * Internally, PHYSFS_enumerateFiles() just calls this function and then builds
 *  a list before returning to the application, so functionality is identical
 *  except for how the information is represented to the application.
 *
 * Unlike PHYSFS_enumerateFiles(), this function does not return an array.
 *  Rather, it calls a function specified by the application once per
 *  element of the search path:
 *
 * \code
 *
 * static int printDir(void *data, const char *origdir, const char *fname)
 * {
 *     printf(" * We've got [%s] in [%s].\n", fname, origdir);
 *     return 1;  // give me more data, please.
 * }
 *
 * // ...
 * PHYSFS_enumerate("/some/path", printDir, NULL);
 * \endcode
 *
 * Items sent to the callback are not guaranteed to be in any order whatsoever.
 *  There is no sorting done at this level, and if you need that, you should
 *  probably use PHYSFS_enumerateFiles() instead, which guarantees
 *  alphabetical sorting. This form reports whatever is discovered in each
 *  archive before moving on to the next. Even within one archive, we can't
 *  guarantee what order it will discover data. <em>Any sorting you find in
 *  these callbacks is just pure luck. Do not rely on it.</em> As this walks
 *  the entire list of archives, you may receive duplicate filenames.
 *
 * This API and the callbacks themselves are capable of reporting errors.
 *  Prior to this API, callbacks had to accept every enumerated item, even if
 *  they were only looking for a specific thing and wanted to stop after that,
 *  or had a serious error and couldn't alert anyone. Furthermore, if
 *  PhysicsFS itself had a problem (disk error or whatnot), it couldn't report
 *  it to the calling app, it would just have to skip items or stop
 *  enumerating outright, and the caller wouldn't know it had lost some data
 *  along the way.
 *
 * Now the caller can be sure it got a complete data set, and its callback has
 *  control if it wants enumeration to stop early. See the documentation for
 *  PHYSFS_EnumerateCallback for details on how your callback should behave.
 *
 *    \param dir Directory, in platform-independent notation, to enumerate.
 *    \param c Callback function to notify about search path elements.
 *    \param d Application-defined data passed to callback. Can be NULL.
 *   \return non-zero on success, zero on failure. Use
 *           PHYSFS_getLastErrorCode() to obtain the specific error. If the
 *           callback returns PHYSFS_ENUM_STOP to stop early, this will be
 *           considered success. Callbacks returning PHYSFS_ENUM_ERROR will
 *           make this function return zero and set the error code to
 *           PHYSFS_ERR_APP_CALLBACK.
 *
 * \sa PHYSFS_EnumerateCallback
 * \sa PHYSFS_enumerateFiles
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_enumerate(const char *dir, PHYSFS_EnumerateCallback c,
                                 void *d);
				 */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_enumerate(byte* dir, PHYSFS_EnumerateCallback c, void* d);


/**
 * \fn int PHYSFS_unmount(const char *oldDir)
 * \brief Remove a directory or archive from the search path.
 *
 * This is functionally equivalent to PHYSFS_removeFromSearchPath(), but that
 *  function is deprecated to keep the vocabulary paired with PHYSFS_mount().
 *
 * This must be a (case-sensitive) match to a dir or archive already in the
 *  search path, specified in platform-dependent notation.
 *
 * This call will fail (and fail to remove from the path) if the element still
 *  has files open in it.
 *
 *    \param oldDir dir/archive to remove.
 *   \return nonzero on success, zero on failure. Use
 *           PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_mount
 */
//public static extern int PHYSFS_unmount(const char *oldDir);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_unmount(byte* oldDir);


/**
 * \fn const PHYSFS_Allocator *PHYSFS_getAllocator(void)
 * \brief Discover the current allocator.
 *
 * (This is for limited, hardcore use. If you don't immediately see a need
 *  for it, you can probably ignore this forever.)
 *
 * This function exposes the function pointers that make up the currently used
 *  allocator. This can be useful for apps that want to access PhysicsFS's
 * [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
 *  internal, default allocation routines, as well as for external code that
 *  wants to share the same allocator, even if the application specified their
 *  own.
 *
 * This call is only valid between PHYSFS_init() and PHYSFS_deinit() calls;
 *  it will return NULL if the library isn't initialized. As we can't
 *  guarantee the state of the internal allocators unless the library is
 *  initialized, you shouldn't use any allocator returned here after a call
 *  to PHYSFS_deinit().
 *
 * Do not call the returned allocator's Init() or Deinit() methods under any
 *  circumstances.
 *
 * If you aren't immediately sure what to do with this function, you can
 *  safely ignore it altogether.
 *
 *  \return Current allocator, as set by PHYSFS_setAllocator(), or PhysicsFS's
 *          internal, default allocator if no application defined allocator
 *          is currently set. Will return NULL if the library is not
 *          initialized.
 *
 * \sa PHYSFS_Allocator
 * \sa PHYSFS_setAllocator
 */
//public static extern const PHYSFS_Allocator *PHYSFS_getAllocator(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern PHYSFS_Allocator* PHYSFS_getAllocator();


/**
 * \enum PHYSFS_FileType
 * \brief Type of a File
 *
 * Possible types of a file.
 *
 * \sa PHYSFS_stat
 */
#if false
typedef enum PHYSFS_FileType
{
	PHYSFS_FILETYPE_REGULAR, /**< a normal file */
	PHYSFS_FILETYPE_DIRECTORY, /**< a directory */
	PHYSFS_FILETYPE_SYMLINK, /**< a symlink */
	PHYSFS_FILETYPE_OTHER /**< something completely different like a device */
} PHYSFS_FileType;
#endif

public const int PHYSFS_FILETYPE_REGULAR   = 0;
public const int PHYSFS_FILETYPE_DIRECTORY = 1;
public const int PHYSFS_FILETYPE_SYMLINK   = 2;
public const int PHYSFS_FILETYPE_OTHER     = 3;


/**
 * \struct PHYSFS_Stat
 * \brief Meta data for a file or directory
 *
 * Container for various meta data about a file in the virtual file system.
 *  PHYSFS_stat() uses this structure for returning the information. The time
 *  data will be either the number of seconds since the Unix epoch (midnight,
 *  Jan 1, 1970), or -1 if the information isn't available or applicable.
 *  The (filesize) field is measured in bytes.
 *  The (readonly) field tells you whether the archive thinks a file is
 *  not writable, but tends to be only an estimate (for example, your write
 *  dir might overlap with a .zip file, meaning you _can_ successfully open
 *  that path for writing, as it gets created elsewhere.
 *
 * \sa PHYSFS_stat
 * \sa PHYSFS_FileType
 */
#if false
typedef struct PHYSFS_Stat
{
	PHYSFS_sint64 filesize; /**< size in bytes, -1 for non-files and unknown */
	PHYSFS_sint64 modtime;  /**< last modification time */
	PHYSFS_sint64 createtime; /**< like modtime, but for file creation time */
	PHYSFS_sint64 accesstime; /**< like modtime, but for file access time */
	PHYSFS_FileType filetype; /**< File? Directory? Symlink? */
	int readonly; /**< non-zero if read only, zero if writable. */
} PHYSFS_Stat;
#endif


/**
 * \fn int PHYSFS_stat(const char *fname, PHYSFS_Stat *stat)
 * \brief Get various information about a directory or a file.
 *
 * Obtain various information about a file or directory from the meta data.
 *
 * This function will never follow symbolic links. If you haven't enabled
 *  symlinks with PHYSFS_permitSymbolicLinks(), stat'ing a symlink will be
 *  treated like stat'ing a non-existant file. If symlinks are enabled,
 *  stat'ing a symlink will give you information on the link itself and not
 *  what it points to.
 *
 *    \param fname filename to check, in platform-indepedent notation.
 *    \param stat pointer to structure to fill in with data about (fname).
 *   \return non-zero on success, zero on failure. On failure, (stat)'s
 *           contents are undefined.
 *
 * \sa PHYSFS_Stat
 */
//public static extern int PHYSFS_stat(const char *fname, PHYSFS_Stat *stat);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_stat(byte* fname, PHYSFS_Stat* stat);


/**
 * \fn PHYSFS_sint64 PHYSFS_readBytes(PHYSFS_File *handle, void *buffer, PHYSFS_uint64 len)
 * \brief Read bytes from a PhysicsFS filehandle
 *
 * The file must be opened for reading.
 *
 *   \param handle handle returned from PHYSFS_openRead().
 *   \param buffer buffer of at least (len) bytes to store read data into.
 *   \param len number of bytes being read from (handle).
 *  \return number of bytes read. This may be less than (len); this does not
 *          signify an error, necessarily (a short read may mean EOF).
 *          PHYSFS_getLastErrorCode() can shed light on the reason this might
 *          be < (len), as can PHYSFS_eof(). -1 if complete failure.
 *
 * \sa PHYSFS_eof
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern PHYSFS_sint64 PHYSFS_readBytes(PHYSFS_File *handle, void *buffer,
                                           PHYSFS_uint64 len);
					   */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern long PHYSFS_readBytes(SafePhysicsFSFileHandle handle, void* buffer, ulong len);

/**
 * \fn PHYSFS_sint64 PHYSFS_writeBytes(PHYSFS_File *handle, const void *buffer, PHYSFS_uint64 len)
 * \brief Write data to a PhysicsFS filehandle
 *
 * The file must be opened for writing.
 *
 * Please note that while (len) is an unsigned 64-bit integer, you are limited
 *  to 63 bits (9223372036854775807 bytes), so we can return a negative value
 *  on error. If length is greater than 0x7FFFFFFFFFFFFFFF, this function will
 *  immediately fail. For systems without a 64-bit datatype, you are limited
 *  to 31 bits (0x7FFFFFFF, or 2147483647 bytes). We trust most things won't
 *  need to do multiple gigabytes of i/o in one call anyhow, but why limit
 *  things?
 *
 *   \param handle retval from PHYSFS_openWrite() or PHYSFS_openAppend().
 *   \param buffer buffer of (len) bytes to write to (handle).
 *   \param len number of bytes being written to (handle).
 *  \return number of bytes written. This may be less than (len); in the case
 *          of an error, the system may try to write as many bytes as possible,
 *          so an incomplete write might occur. PHYSFS_getLastErrorCode() can
 *          shed light on the reason this might be < (len). -1 if complete
 *          failure.
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern PHYSFS_sint64 PHYSFS_writeBytes(PHYSFS_File *handle,
                                            const void *buffer,
                                            PHYSFS_uint64 len);
					    */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern long PHYSFS_writeBytes(SafePhysicsFSFileHandle handle, void* buffer, ulong len);



/**
 * \struct PHYSFS_Io
 * \brief An abstract i/o interface.
 *
 * \warning This is advanced, hardcore stuff. You don't need this unless you
 *          really know what you're doing. Most apps will not need this.
 *
 * Historically, PhysicsFS provided access to the physical filesystem and
 *  archives within that filesystem. However, sometimes you need more power
 *  than this. Perhaps you need to provide an archive that is entirely
 *  contained in RAM, or you need to bridge some other file i/o API to
 *  PhysicsFS, or you need to translate the bits (perhaps you have a
 *  a standard .zip file that's encrypted, and you need to decrypt on the fly
 *  for the unsuspecting zip archiver).
 *
 * A PHYSFS_Io is the interface that Archivers use to get archive data.
 *  Historically, this has mapped to file i/o to the physical filesystem, but
 *  as of PhysicsFS 2.1, applications can provide their own i/o implementations
 *  at runtime.
 *
 * This interface isn't necessarily a good universal fit for i/o. There are a
 *  few requirements of note:
 *
 *  - They only do blocking i/o (at least, for now).
 *  - They need to be able to duplicate. If you have a file handle from
 *    fopen(), you need to be able to create a unique clone of it (so we
 *    have two handles to the same file that can both seek/read/etc without
 *    stepping on each other).
 *  - They need to know the size of their entire data set.
 *  - They need to be able to seek and rewind on demand.
 *
 * ...in short, you're probably not going to write an HTTP implementation.
 *
 * Thread safety: PHYSFS_Io implementations are not guaranteed to be thread
 *  safe in themselves. Under the hood where PhysicsFS uses them, the library
 *  provides its own locks. If you plan to use them directly from separate
 *  threads, you should either use mutexes to protect them, or don't use the
 *  same PHYSFS_Io from two threads at the same time.
 *
 * \sa PHYSFS_mountIo
 */
#if false
typedef struct PHYSFS_Io
{
    /**
     * \brief Binary compatibility information.
     *
     * This must be set to zero at this time. Future versions of this
     *  struct will increment this field, so we know what a given
     *  implementation supports. We'll presumably keep supporting older
     *  versions as we offer new features, though.
     */
    PHYSFS_uint32 version;

    /**
     * \brief Instance data for this struct.
     *
     * Each instance has a pointer associated with it that can be used to
     *  store anything it likes. This pointer is per-instance of the stream,
     *  so presumably it will change when calling duplicate(). This can be
     *  deallocated during the destroy() method.
     */
    void *opaque;

    /**
     * \brief Read more data.
     *
     * Read (len) bytes from the interface, at the current i/o position, and
     *  store them in (buffer). The current i/o position should move ahead
     *  by the number of bytes successfully read.
     *
     * You don't have to implement this; set it to NULL if not implemented.
     *  This will only be used if the file is opened for reading. If set to
     *  NULL, a default implementation that immediately reports failure will
     *  be used.
     *
     *   \param io The i/o instance to read from.
     *   \param buf The buffer to store data into. It must be at least
     *                 (len) bytes long and can't be NULL.
     *   \param len The number of bytes to read from the interface.
     *  \return number of bytes read from file, 0 on EOF, -1 if complete
     *          failure.
     */
    PHYSFS_sint64 (*read)(struct PHYSFS_Io *io, void *buf, PHYSFS_uint64 len);

    /**
     * \brief Write more data.
     *
     * Write (len) bytes from (buffer) to the interface at the current i/o
     *  position. The current i/o position should move ahead by the number of
     *  bytes successfully written.
     *
     * You don't have to implement this; set it to NULL if not implemented.
     *  This will only be used if the file is opened for writing. If set to
     *  NULL, a default implementation that immediately reports failure will
     *  be used.
     *
     * You are allowed to buffer; a write can succeed here and then later
     *  fail when flushing. Note that PHYSFS_setBuffer() may be operating a
     *  level above your i/o, so you should usually not implement your
     *  own buffering routines.
     *
     *   \param io The i/o instance to write to.
     *   \param buffer The buffer to read data from. It must be at least
     *                 (len) bytes long and can't be NULL.
     *   \param len The number of bytes to read from (buffer).
     *  \return number of bytes written to file, -1 if complete failure.
     */
    PHYSFS_sint64 (*write)(struct PHYSFS_Io *io, const void *buffer,
                           PHYSFS_uint64 len);

    /**
     * \brief Move i/o position to a given byte offset from start.
     *
     * This method moves the i/o position, so the next read/write will
     *  be of the byte at (offset) offset. Seeks past the end of file should
     *  be treated as an error condition.
     *
     *   \param io The i/o instance to seek.
     *   \param offset The new byte offset for the i/o position.
     *  \return non-zero on success, zero on error.
     */
    int (*seek)(struct PHYSFS_Io *io, PHYSFS_uint64 offset);

    /**
     * \brief Report current i/o position.
     *
     * Return bytes offset, or -1 if you aren't able to determine. A failure
     *  will almost certainly be fatal to further use of this stream, so you
     *  may not leave this unimplemented.
     *
     *   \param io The i/o instance to query.
     *  \return The current byte offset for the i/o position, -1 if unknown.
     */
    PHYSFS_sint64 (*tell)(struct PHYSFS_Io *io);

    /**
     * \brief Determine size of the i/o instance's dataset.
     *
     * Return number of bytes available in the file, or -1 if you
     *  aren't able to determine. A failure will almost certainly be fatal
     *  to further use of this stream, so you may not leave this unimplemented.
     *
     *   \param io The i/o instance to query.
     *  \return Total size, in bytes, of the dataset.
     */
    PHYSFS_sint64 (*length)(struct PHYSFS_Io *io);

    /**
     * \brief Duplicate this i/o instance.
     *
     * This needs to result in a full copy of this PHYSFS_Io, that can live
     *  completely independently. The copy needs to be able to perform all
     *  its operations without altering the original, including either object
     *  being destroyed separately (so, for example: they can't share a file
     *  handle; they each need their own).
     *
     * If you can't duplicate a handle, it's legal to return NULL, but you
     *  almost certainly need this functionality if you want to use this to
     *  PHYSFS_Io to back an archive.
     *
     *   \param io The i/o instance to duplicate.
     *  \return A new value for a stream's (opaque) field, or NULL on error.
     */
    struct PHYSFS_Io *(*duplicate)(struct PHYSFS_Io *io);

    /**
     * \brief Flush resources to media, or wherever.
     *
     * This is the chance to report failure for writes that had claimed
     *  success earlier, but still had a chance to actually fail. This method
     *  can be NULL if flushing isn't necessary.
     *
     * This function may be called before destroy(), as it can report failure
     *  and destroy() can not. It may be called at other times, too.
     *
     *   \param io The i/o instance to flush.
     *  \return Zero on error, non-zero on success.
     */
    int (*flush)(struct PHYSFS_Io *io);

    /**
     * \brief Cleanup and deallocate i/o instance.
     *
     * Free associated resources, including (opaque) if applicable.
     *
     * This function must always succeed: as such, it returns void. The
     *  system may call your flush() method before this. You may report
     *  failure there if necessary. This method may still be called if
     *  flush() fails, in which case you'll have to abandon unflushed data
     *  and other failing conditions and clean up.
     *
     * Once this method is called for a given instance, the system will assume
     *  it is unsafe to touch that instance again and will discard any
     *  references to it.
     *
     *   \param s The i/o instance to destroy.
     */
    void (*destroy)(struct PHYSFS_Io *io);
} PHYSFS_Io;
#endif


/**
 * \fn int PHYSFS_mountIo(PHYSFS_Io *io, const char *fname, const char *mountPoint, int appendToPath)
 * \brief Add an archive, built on a PHYSFS_Io, to the search path.
 *
 * \warning Unless you have some special, low-level need, you should be using
 *          PHYSFS_mount() instead of this.
 *
 * This function operates just like PHYSFS_mount(), but takes a PHYSFS_Io
 *  instead of a pathname. Behind the scenes, PHYSFS_mount() calls this
 *  function with a physical-filesystem-based PHYSFS_Io.
 *
 * (filename) is only used here to optimize archiver selection (if you name it
 *  XXXXX.zip, we might try the ZIP archiver first, for example). It doesn't
 *  need to refer to a real file at all, and can even be NULL. If the filename
 *  isn't helpful, the system will try every archiver until one works or none
 *  of them do.
 *
 * (io) must remain until the archive is unmounted. When the archive is
 *  unmounted, the system will call (io)->destroy(io), which will give you
 *  a chance to free your resources.
 *
 * If this function fails, (io)->destroy(io) is not called.
 *
 *   \param io i/o instance for archive to add to the path.
 *   \param fname Filename that can represent this stream. Can be NULL.
 *   \param mountPoint Location in the interpolated tree that this archive
 *                     will be "mounted", in platform-independent notation.
 *                     NULL or "" is equivalent to "/".
 *   \param appendToPath nonzero to append to search path, zero to prepend.
 *  \return nonzero if added to path, zero on failure (bogus archive, stream
 *                   i/o issue, etc). Use PHYSFS_getLastErrorCode() to obtain
 *                   the specific error.
 *
 * \sa PHYSFS_unmount
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_getMountPoint
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mountIo(PHYSFS_Io *io, const char *fname,
                               const char *mountPoint, int appendToPath);
			       */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mountIo(PHYSFS_Io* io, byte* fname, byte* mountPoint, int appendToPath);


/**
 * \fn int PHYSFS_mountMemory(const void *buf, PHYSFS_uint64 len, void (*del)(void *), const char *fname, const char *mountPoint, int appendToPath)
 * \brief Add an archive, contained in a memory buffer, to the search path.
 *
 * \warning Unless you have some special, low-level need, you should be using
 *          PHYSFS_mount() instead of this.
 *
 * This function operates just like PHYSFS_mount(), but takes a memory buffer
 *  instead of a pathname. This buffer contains all the data of the archive,
 *  and is used instead of a real file in the physical filesystem.
 *
 * (filename) is only used here to optimize archiver selection (if you name it
 *  XXXXX.zip, we might try the ZIP archiver first, for example). It doesn't
 *  need to refer to a real file at all, and can even be NULL. If the filename
 *  isn't helpful, the system will try every archiver until one works or none
 *  of them do.
 *
 * (ptr) must remain until the archive is unmounted. When the archive is
 *  unmounted, the system will call (del)(ptr), which will notify you that
 *  the system is done with the buffer, and give you a chance to free your
 *  resources. (del) can be NULL, in which case the system will make no
 *  attempt to free the buffer.
 *
 * If this function fails, (del) is not called.
 *
 *   \param buf Address of the memory buffer containing the archive data.
 *   \param len Size of memory buffer, in bytes.
 *   \param del A callback that triggers upon unmount. Can be NULL.
 *   \param fname Filename that can represent this stream. Can be NULL.
 *   \param mountPoint Location in the interpolated tree that this archive
 *                     will be "mounted", in platform-independent notation.
 *                     NULL or "" is equivalent to "/".
 *   \param appendToPath nonzero to append to search path, zero to prepend.
 *  \return nonzero if added to path, zero on failure (bogus archive, etc).
 *          Use PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_unmount
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_getMountPoint
 */
/*
  [DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mountMemory(const void *buf, PHYSFS_uint64 len,
                                   void (*del)(void *), const char *fname,
                                   const char *mountPoint, int appendToPath);
				   */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mountMemory(void* buf, ulong len,
                                     mountMemory_delptr del, byte *fname,
                                     byte* mountPoint, int appendToPath);
public delegate void mountMemory_delptr(void* ptr);



/**
 * \fn int PHYSFS_mountHandle(PHYSFS_File *file, const char *fname, const char *mountPoint, int appendToPath)
 * \brief Add an archive, contained in a PHYSFS_File handle, to the search path.
 *
 * \warning Unless you have some special, low-level need, you should be using
 *          PHYSFS_mount() instead of this.
 *
 * \warning Archives-in-archives may be very slow! While a PHYSFS_File can
 *          seek even when the data is compressed, it may do so by rewinding
 *          to the start and decompressing everything before the seek point.
 *          Normal archive usage may do a lot of seeking behind the scenes.
 *          As such, you might find normal archive usage extremely painful
 *          if mounted this way. Plan accordingly: if you, say, have a
 *          self-extracting .zip file, and want to mount something in it,
 *          compress the contents of the inner archive and make sure the outer
 *          .zip file doesn't compress the inner archive too.
 *
 * This function operates just like PHYSFS_mount(), but takes a PHYSFS_File
 *  handle instead of a pathname. This handle contains all the data of the
 *  archive, and is used instead of a real file in the physical filesystem.
 *  The PHYSFS_File may be backed by a real file in the physical filesystem,
 *  but isn't necessarily. The most popular use for this is likely to mount
 *  archives stored inside other archives.
 *
 * (filename) is only used here to optimize archiver selection (if you name it
 *  XXXXX.zip, we might try the ZIP archiver first, for example). It doesn't
 *  need to refer to a real file at all, and can even be NULL. If the filename
 *  isn't helpful, the system will try every archiver until one works or none
 *  of them do.
 *
 * (file) must remain until the archive is unmounted. When the archive is
 *  unmounted, the system will call PHYSFS_close(file). If you need this
 *  handle to survive, you will have to wrap this in a PHYSFS_Io and use
 *  PHYSFS_mountIo() instead.
 *
 * If this function fails, PHYSFS_close(file) is not called.
 *
 *   \param file The PHYSFS_File handle containing archive data.
 *   \param fname Filename that can represent this stream. Can be NULL.
 *   \param mountPoint Location in the interpolated tree that this archive
 *                     will be "mounted", in platform-independent notation.
 *                     NULL or "" is equivalent to "/".
 *   \param appendToPath nonzero to append to search path, zero to prepend.
 *  \return nonzero if added to path, zero on failure (bogus archive, etc).
 *          Use PHYSFS_getLastErrorCode() to obtain the specific error.
 *
 * \sa PHYSFS_unmount
 * \sa PHYSFS_getSearchPath
 * \sa PHYSFS_getMountPoint
 */
/*
public static extern int PHYSFS_mountHandle(PHYSFS_File *file, const char *fname,
                                   const char *mountPoint, int appendToPath);
				   */
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_mountHandle(SafePhysicsFSFileHandle file, byte* fname, byte* mountPoint, int appendToPath);


/**
 * \enum PHYSFS_ErrorCode
 * \brief Values that represent specific causes of failure.
 *
 * Most of the time, you should only concern yourself with whether a given
 *  operation failed or not, but there may be occasions where you plan to
 *  handle a specific failure case gracefully, so we provide specific error
 *  codes.
 *
 * Most of these errors are a little vague, and most aren't things you can
 *  fix...if there's a permission error, for example, all you can really do
 *  is pass that information on to the user and let them figure out how to
 *  handle it. In most these cases, your program should only care that it
 *  failed to accomplish its goals, and not care specifically why.
 *
 * \sa PHYSFS_getLastErrorCode
 * \sa PHYSFS_getErrorByCode
 */
#if false
typedef enum PHYSFS_ErrorCode
{
    PHYSFS_ERR_OK,               /**< Success; no error.                    */
    PHYSFS_ERR_OTHER_ERROR,      /**< Error not otherwise covered here.     */
    PHYSFS_ERR_OUT_OF_MEMORY,    /**< Memory allocation failed.             */
    PHYSFS_ERR_NOT_INITIALIZED,  /**< PhysicsFS is not initialized.         */
    PHYSFS_ERR_IS_INITIALIZED,   /**< PhysicsFS is already initialized.     */
    PHYSFS_ERR_ARGV0_IS_NULL,    /**< Needed argv[0], but it is NULL.       */
    PHYSFS_ERR_UNSUPPORTED,      /**< Operation or feature unsupported.     */
    PHYSFS_ERR_PAST_EOF,         /**< Attempted to access past end of file. */
    PHYSFS_ERR_FILES_STILL_OPEN, /**< Files still open.                     */
    PHYSFS_ERR_INVALID_ARGUMENT, /**< Bad parameter passed to an function.  */
    PHYSFS_ERR_NOT_MOUNTED,      /**< Requested archive/dir not mounted.    */
    PHYSFS_ERR_NOT_FOUND,        /**< File (or whatever) not found.         */
    PHYSFS_ERR_SYMLINK_FORBIDDEN,/**< Symlink seen when not permitted.      */
    PHYSFS_ERR_NO_WRITE_DIR,     /**< No write dir has been specified.      */
    PHYSFS_ERR_OPEN_FOR_READING, /**< Wrote to a file opened for reading.   */
    PHYSFS_ERR_OPEN_FOR_WRITING, /**< Read from a file opened for writing.  */
    PHYSFS_ERR_NOT_A_FILE,       /**< Needed a file, got a directory (etc). */
    PHYSFS_ERR_READ_ONLY,        /**< Wrote to a read-only filesystem.      */
    PHYSFS_ERR_CORRUPT,          /**< Corrupted data encountered.           */
    PHYSFS_ERR_SYMLINK_LOOP,     /**< Infinite symbolic link loop.          */
    PHYSFS_ERR_IO,               /**< i/o error (hardware failure, etc).    */
    PHYSFS_ERR_PERMISSION,       /**< Permission denied.                    */
    PHYSFS_ERR_NO_SPACE,         /**< No space (disk full, over quota, etc) */
    PHYSFS_ERR_BAD_FILENAME,     /**< Filename is bogus/insecure.           */
    PHYSFS_ERR_BUSY,             /**< Tried to modify a file the OS needs.  */
    PHYSFS_ERR_DIR_NOT_EMPTY,    /**< Tried to delete dir with files in it. */
    PHYSFS_ERR_OS_ERROR,         /**< Unspecified OS-level error.           */
    PHYSFS_ERR_DUPLICATE,        /**< Duplicate entry.                      */
    PHYSFS_ERR_BAD_PASSWORD,     /**< Bad password.                         */
    PHYSFS_ERR_APP_CALLBACK      /**< Application callback reported error.  */
} PHYSFS_ErrorCode;
#endif
    public const int PHYSFS_ERR_OK = 1;
    public const int PHYSFS_ERR_OTHER_ERROR = 2;
    public const int PHYSFS_ERR_OUT_OF_MEMORY = 3;
    public const int PHYSFS_ERR_NOT_INITIALIZED=4;
    public const int PHYSFS_ERR_IS_INITIALIZED =5;
    public const int PHYSFS_ERR_ARGV0_IS_NULL = 6;
    public const int PHYSFS_ERR_UNSUPPORTED = 7;
    public const int PHYSFS_ERR_PAST_EOF = 8;
    public const int PHYSFS_ERR_FILES_STILL_OPEN=9;
    public const int PHYSFS_ERR_INVALID_ARGUMENT=10;
    public const int PHYSFS_ERR_NOT_MOUNTED=11;
    public const int PHYSFS_ERR_NOT_FOUND = 12;
    public const int PHYSFS_ERR_SYMLINK_FORBIDDEN = 13;
    public const int PHYSFS_ERR_NO_WRITE_DIR = 14;
    public const int PHYSFS_ERR_OPEN_FOR_READING = 15;
    public const int PHYSFS_ERR_OPEN_FOR_WRITING = 16;
    public const int PHYSFS_ERR_NOT_A_FILE = 17;
    public const int PHYSFS_ERR_READ_ONLY = 18;
    public const int PHYSFS_ERR_CORRUPT = 19;
    public const int PHYSFS_ERR_SYMLINK_LOOP = 20;
    public const int PHYSFS_ERR_IO = 21;
    public const int PHYSFS_ERR_PERMISSION = 22;
    public const int PHYSFS_ERR_NO_SPACE = 23;
    public const int PHYSFS_ERR_BAD_FILENAME = 24;
    public const int PHYSFS_ERR_BUSY = 25;
    public const int PHYSFS_ERR_DIR_NOT_EMPTY = 26;
    public const int PHYSFS_ERR_OS_ERROR = 27;
    public const int PHYSFS_ERR_DUPLICATE = 28;
    public const int PHYSFS_ERR_BAD_PASSWORD = 29;
    public const int PHYSFS_ERR_APP_CALLBACK = 30;


/**
 * \fn PHYSFS_ErrorCode PHYSFS_getLastErrorCode(void)
 * \brief Get machine-readable error information.
 *
 * Get the last PhysicsFS error message as an integer value. This will return
 *  PHYSFS_ERR_OK if there's been no error since the last call to this
 *  function. Each thread has a unique error state associated with it, but
 *  each time a new error message is set, it will overwrite the previous one
 *  associated with that thread. It is safe to call this function at anytime,
 *  even before PHYSFS_init().
 *
 * PHYSFS_getLastError() and PHYSFS_getLastErrorCode() both reset the same
 *  thread-specific error state. Calling one will wipe out the other's
 *  data. If you need both, call PHYSFS_getLastErrorCode(), then pass that
 *  value to PHYSFS_getErrorByCode().
 *
 * Generally, applications should only concern themselves with whether a
 *  given function failed; however, if you require more specifics, you can
 *  try this function to glean information, if there's some specific problem
 *  you're expecting and plan to handle. But with most things that involve
 *  file systems, the best course of action is usually to give up, report the
 *  problem to the user, and let them figure out what should be done about it.
 *  For that, you might prefer PHYSFS_getErrorByCode() instead.
 *
 *   \return Enumeration value that represents last reported error.
 *
 * \sa PHYSFS_getErrorByCode
 */
//public static extern PHYSFS_ErrorCode PHYSFS_getLastErrorCode(void);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_getLastErrorCode();


/**
 * \fn const char *PHYSFS_getErrorByCode(PHYSFS_ErrorCode code)
 * \brief Get human-readable description string for a given error code.
 *
 * Get a static string, in UTF-8 format, that represents an English
 *  description of a given error code.
 *
 * This string is guaranteed to never change (although we may add new strings
 *  for new error codes in later versions of PhysicsFS), so you can use it
 *  for keying a localization dictionary.
 *
 * It is safe to call this function at anytime, even before PHYSFS_init().
 *
 * These strings are meant to be passed on directly to the user.
 *  Generally, applications should only concern themselves with whether a
 *  given function failed, but not care about the specifics much.
 *
 * Do not attempt to free the returned strings; they are read-only and you
 *  don't own their memory pages.
 *
 *   \param code Error code to convert to a string.
 *   \return READ ONLY string of requested error message, NULL if this
 *           is not a valid PhysicsFS error code. Always check for NULL if
 *           you might be looking up an error code that didn't exist in an
 *           earlier version of PhysicsFS.
 *
 * \sa PHYSFS_getLastErrorCode
 */
//public static extern const char *PHYSFS_getErrorByCode(PHYSFS_ErrorCode code);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getErrorByCode(int code);

/**
 * \fn void PHYSFS_setErrorCode(PHYSFS_ErrorCode code)
 * \brief Set the current thread's error code.
 *
 * This lets you set the value that will be returned by the next call to
 *  PHYSFS_getLastErrorCode(). This will replace any existing error code,
 *  whether set by your application or internally by PhysicsFS.
 *
 * Error codes are stored per-thread; what you set here will not be
 *  accessible to another thread.
 *
 * Any call into PhysicsFS may change the current error code, so any code you
 *  set here is somewhat fragile, and thus you shouldn't build any serious
 *  error reporting framework on this function. The primary goal of this
 *  function is to allow PHYSFS_Io implementations to set the error state,
 *  which generally will be passed back to your application when PhysicsFS
 *  makes a PHYSFS_Io call that fails internally.
 *
 * This function doesn't care if the error code is a value known to PhysicsFS
 *  or not (but PHYSFS_getErrorByCode() will return NULL for unknown values).
 *  The value will be reported unmolested by PHYSFS_getLastErrorCode().
 *
 *   \param code Error code to become the current thread's new error state.
 *
 * \sa PHYSFS_getLastErrorCode
 * \sa PHYSFS_getErrorByCode
 */
//public static extern void PHYSFS_setErrorCode(PHYSFS_ErrorCode code);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern void PHYSFS_setErrorCode(int code);


/**
 * \fn const char *PHYSFS_getPrefDir(const char *org, const char *app)
 * \brief Get the user-and-app-specific path where files can be written.
 *
 * Helper function.
 *
 * Get the "pref dir". This is meant to be where users can write personal
 *  files (preferences and save games, etc) that are specific to your
 *  application. This directory is unique per user, per application.
 *
 * This function will decide the appropriate location in the native filesystem,
 *  create the directory if necessary, and return a string in
 *  platform-dependent notation, suitable for passing to PHYSFS_setWriteDir().
 *
 * On Windows, this might look like:
 *  "C:\\Users\\bob\\AppData\\Roaming\\My Company\\My Program Name"
 *
 * On Linux, this might look like:
 *  "/home/bob/.local/share/My Program Name"
 *
 * On Mac OS X, this might look like:
 *  "/Users/bob/Library/Application Support/My Program Name"
 *
 * (etc.)
 *
 * You should probably use the pref dir for your write dir, and also put it
 *  near the beginning of your search path. Older versions of PhysicsFS
 *  offered only PHYSFS_getUserDir() and left you to figure out where the
 *  files should go under that tree. This finds the correct location
 *  for whatever platform, which not only changes between operating systems,
 *  but also versions of the same operating system.
 *
 * You specify the name of your organization (if it's not a real organization,
 *  your name or an Internet domain you own might do) and the name of your
 *  application. These should be proper names.
 *
 * Both the (org) and (app) strings may become part of a directory name, so
 *  please follow these rules:
 *
 *    - Try to use the same org string (including case-sensitivity) for
 *      all your applications that use this function.
 *    - Always use a unique app string for each one, and make sure it never
 *      changes for an app once you've decided on it.
 *    - Unicode characters are legal, as long as it's UTF-8 encoded, but...
 *    - ...only use letters, numbers, and spaces. Avoid punctuation like
 *      "Game Name 2: Bad Guy's Revenge!" ... "Game Name 2" is sufficient.
 *
 * The pointer returned by this function remains valid until you call this
 *  function again, or call PHYSFS_deinit(). This is not necessarily a fast
 *  call, though, so you should call this once at startup and copy the string
 *  if you need it.
 *
 * You should assume the path returned by this function is the only safe
 *  place to write files (and that PHYSFS_getUserDir() and PHYSFS_getBaseDir(),
 *  while they might be writable, or even parents of the returned path, aren't
 *  where you should be writing things).
 *
 *   \param org The name of your organization.
 *   \param app The name of your application.
 *  \return READ ONLY string of user dir in platform-dependent notation. NULL
 *          if there's a problem (creating directory failed, etc).
 *
 * \sa PHYSFS_getBaseDir
 * \sa PHYSFS_getUserDir
 */
//public static extern const char *PHYSFS_getPrefDir(const char *org, const char *app);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern byte* PHYSFS_getPrefDir(byte* org, byte* app);


/**
 * \struct PHYSFS_Archiver
 * \brief Abstract interface to provide support for user-defined archives.
 *
 * \warning This is advanced, hardcore stuff. You don't need this unless you
 *          really know what you're doing. Most apps will not need this.
 *
 * Historically, PhysicsFS provided a means to mount various archive file
 *  formats, and physical directories in the native filesystem. However,
 *  applications have been limited to the file formats provided by the
 *  library. This interface allows an application to provide their own
 *  archive file types.
 *
 * Conceptually, a PHYSFS_Archiver provides directory entries, while
 *  PHYSFS_Io provides data streams for those directory entries. The most
 *  obvious use of PHYSFS_Archiver is to provide support for an archive
 *  file type that isn't provided by PhysicsFS directly: perhaps some
 *  proprietary format that only your application needs to understand.
 *
 * Internally, all the built-in archive support uses this interface, so the
 *  best examples for building a PHYSFS_Archiver is the source code to
 *  PhysicsFS itself.
 *
 * An archiver is added to the system with PHYSFS_registerArchiver(), and then
 *  it will be available for use automatically with PHYSFS_mount(); if a
 *  given archive can be handled with your archiver, it will be given control
 *  as appropriate.
 *
 * These methods deal with dir handles. You have one instance of your
 *  archiver, and it generates a unique, opaque handle for each opened
 *  archive in its openArchive() method. Since the lifetime of an Archiver
 *  (not an archive) is generally the entire lifetime of the process, and it's
 *  assumed to be a singleton, we do not provide any instance data for the
 *  archiver itself; the app can just use some static variables if necessary.
 *
 * Symlinks should always be followed (except in stat()); PhysicsFS will
 *  use the stat() method to check for symlinks and make a judgement on
 *  whether to continue to call other methods based on that.
 *
 * Archivers, when necessary, should set the PhysicsFS error state with
 *  PHYSFS_setErrorCode() before returning. PhysicsFS will pass these errors
 *  back to the application unmolested in most cases.
 *
 * Thread safety: PHYSFS_Archiver implementations are not guaranteed to be
 *  thread safe in themselves. PhysicsFS provides thread safety when it calls
 *  into a given archiver inside the library, but it does not promise that
 *  using the same PHYSFS_File from two threads at once is thread-safe; as
 *  such, your PHYSFS_Archiver can assume that locking is handled for you
 *  so long as the PHYSFS_Io you return from PHYSFS_open* doesn't change any
 *  of your Archiver state, as the PHYSFS_Io won't be as aggressively
 *  protected.
 *
 * \sa PHYSFS_registerArchiver
 * \sa PHYSFS_deregisterArchiver
 * \sa PHYSFS_supportedArchiveTypes
 */
#if false
typedef struct PHYSFS_Archiver
{
    /**
     * \brief Binary compatibility information.
     *
     * This must be set to zero at this time. Future versions of this
     *  struct will increment this field, so we know what a given
     *  implementation supports. We'll presumably keep supporting older
     *  versions as we offer new features, though.
     */
    PHYSFS_uint32 version;

    /**
     * \brief Basic info about this archiver.
     *
     * This is used to identify your archive, and is returned in
     *  PHYSFS_supportedArchiveTypes().
     */
    PHYSFS_ArchiveInfo info;

    /**
     * \brief Open an archive provided by (io).
     *
     * This is where resources are allocated and data is parsed when mounting
     *  an archive.
     * (name) is a filename associated with (io), but doesn't necessarily
     *  map to anything, let alone a real filename. This possibly-
     *  meaningless name is in platform-dependent notation.
     * (forWrite) is non-zero if this is to be used for
     *  the write directory, and zero if this is to be used for an
     *  element of the search path.
     * (claimed) should be set to 1 if this is definitely an archive your
     *  archiver implementation can handle, even if it fails. We use to
     *  decide if we should stop trying other archivers if you fail to open
     *  it. For example: the .zip archiver will set this to 1 for something
     *  that's got a .zip file signature, even if it failed because the file
     *  was also truncated. No sense in trying other archivers here, we
     *  already tried to handle it with the appropriate implementation!.
     * Return NULL on failure and set (claimed) appropriately. If no archiver
     *  opened the archive or set (claimed), PHYSFS_mount() will report
     *  PHYSFS_ERR_UNSUPPORTED. Otherwise, it will report the error from the
     *  archiver that claimed the data through (claimed).
     * Return non-NULL on success. The pointer returned will be
     *  passed as the "opaque" parameter for later calls.
     */
    void *(*openArchive)(PHYSFS_Io *io, const char *name,
                         int forWrite, int *claimed);

    /**
     * \brief List all files in (dirname).
     *
     * Each file is passed to (cb), where a copy is made if appropriate, so
     *  you can dispose of it upon return from the callback. (dirname) is in
     *  platform-independent notation.
     * If you have a failure, call PHYSFS_SetErrorCode() with whatever code
     *  seem appropriate and return PHYSFS_ENUM_ERROR.
     * If the callback returns PHYSFS_ENUM_ERROR, please call
     *  PHYSFS_SetErrorCode(PHYSFS_ERR_APP_CALLBACK) and then return
     *  PHYSFS_ENUM_ERROR as well. Don't call the callback again in any
     *  circumstances.
     * If the callback returns PHYSFS_ENUM_STOP, stop enumerating and return
     *  PHYSFS_ENUM_STOP as well. Don't call the callback again in any
     *  circumstances. Don't set an error code in this case.
     * Callbacks are only supposed to return a value from
     *  PHYSFS_EnumerateCallbackResult. Any other result has undefined
     *  behavior.
     * As long as the callback returned PHYSFS_ENUM_OK and you haven't
     *  experienced any errors of your own, keep enumerating until you're done
     *  and then return PHYSFS_ENUM_OK without setting an error code.
     *
     * \warning PHYSFS_enumerate returns zero or non-zero (success or failure),
     *          so be aware this function pointer returns different values!
     */
    PHYSFS_EnumerateCallbackResult (*enumerate)(void *opaque,
                     const char *dirname, PHYSFS_EnumerateCallback cb,
                     const char *origdir, void *callbackdata);

    /**
     * \brief Open a file in this archive for reading.
     *
     * This filename, (fnm), is in platform-independent notation.
     * Fail if the file does not exist.
     * Returns NULL on failure, and calls PHYSFS_setErrorCode().
     *  Returns non-NULL on success. The pointer returned will be
     *  passed as the "opaque" parameter for later file calls.
     */
    PHYSFS_Io *(*openRead)(void *opaque, const char *fnm);

    /**
     * \brief Open a file in this archive for writing.
     *
     * If the file does not exist, it should be created. If it exists,
     *  it should be truncated to zero bytes. The writing offset should
     *  be the start of the file.
     * If the archive is read-only, this operation should fail.
     * This filename is in platform-independent notation.
     * Returns NULL on failure, and calls PHYSFS_setErrorCode().
     *  Returns non-NULL on success. The pointer returned will be
     *  passed as the "opaque" parameter for later file calls.
     */
    PHYSFS_Io *(*openWrite)(void *opaque, const char *filename);

    /**
     * \brief Open a file in this archive for appending.
     *
     * If the file does not exist, it should be created. The writing
     *  offset should be the end of the file.
     * If the archive is read-only, this operation should fail.
     * This filename is in platform-independent notation.
     * Returns NULL on failure, and calls PHYSFS_setErrorCode().
     *  Returns non-NULL on success. The pointer returned will be
     *  passed as the "opaque" parameter for later file calls.
     */
    PHYSFS_Io *(*openAppend)(void *opaque, const char *filename);

    /**
     * \brief Delete a file or directory in the archive.
     *
     * This same call is used for both files and directories; there is not a
     *  separate rmdir() call. Directories are only meant to be removed if
     *  they are empty.
     * If the archive is read-only, this operation should fail.
     *
     * Return non-zero on success, zero on failure.
     * This filename is in platform-independent notation.
     * On failure, call PHYSFS_setErrorCode().
     */
    int (*remove)(void *opaque, const char *filename);

    /**
     * \brief Create a directory in the archive.
     *
     * If the application is trying to make multiple dirs, PhysicsFS
     *  will split them up into multiple calls before passing them to
     *  your driver.
     * If the archive is read-only, this operation should fail.
     * Return non-zero on success, zero on failure.
     *  This filename is in platform-independent notation.
     * On failure, call PHYSFS_setErrorCode().
     */
    int (*mkdir)(void *opaque, const char *filename);

    /**
     * \brief Obtain basic file metadata.
     *
     * On success, fill in all the fields in (stat), using
     *  reasonable defaults for fields that apply to your archive.
     *
     * Returns non-zero on success, zero on failure.
     * This filename is in platform-independent notation.
     * On failure, call PHYSFS_setErrorCode().
     */
    int (*stat)(void *opaque, const char *fn, PHYSFS_Stat *stat);

    /**
     * \brief Destruct a previously-opened archive.
     *
     * Close this archive, and free any associated memory,
     *  including the original PHYSFS_Io and (opaque) itself, if
     *  applicable. Implementation can assume that it won't be called if
     *  there are still files open from this archive.
     */
    void (*closeArchive)(void *opaque);
} PHYSFS_Archiver;
#endif

/**
 * \fn int PHYSFS_registerArchiver(const PHYSFS_Archiver *archiver)
 * \brief Add a new archiver to the system.
 *
 * \warning This is advanced, hardcore stuff. You don't need this unless you
 *          really know what you're doing. Most apps will not need this.
 *
 * If you want to provide your own archiver (for example, a custom archive
 *  file format, or some virtual thing you want to make look like a filesystem
 *  that you can access through the usual PhysicsFS APIs), this is where you
 *  start. Once an archiver is successfully registered, then you can use
 *  PHYSFS_mount() to add archives that your archiver supports to the
 *  search path, or perhaps use it as the write dir. Internally, PhysicsFS
 *  uses this function to register its own built-in archivers, like .zip
 *  support, etc.
 *
 * You may not have two archivers that handle the same extension. If you are
 *  going to have a clash, you can deregister the other archiver (including
 *  built-in ones) with PHYSFS_deregisterArchiver().
 *
 * The data in (archiver) is copied; you may free this pointer when this
 *  function returns.
 *
 * Once this function returns successfully, PhysicsFS will be able to support
 *  archives of this type until you deregister the archiver again.
 *
 *   \param archiver The archiver to register.
 *  \return Zero on error, non-zero on success.
 *
 * \sa PHYSFS_Archiver
 * \sa PHYSFS_deregisterArchiver
 */
//public static extern int PHYSFS_registerArchiver(const PHYSFS_Archiver *archiver);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_registerArchiver(PHYSFS_Archiver* archiver);

/**
 * \fn int PHYSFS_deregisterArchiver(const char *ext)
 * \brief Remove an archiver from the system.
 *
 * If for some reason, you only need your previously-registered archiver to
 *  live for a portion of your app's lifetime, you can remove it from the
 *  system once you're done with it through this function.
 *
 * This fails if there are any archives still open that use this archiver.
 *
 * This function can also remove internally-supplied archivers, like .zip
 *  support or whatnot. This could be useful in some situations, like
 *  disabling support for them outright or overriding them with your own
 *  implementation. Once an internal archiver is disabled like this,
 *  PhysicsFS provides no mechanism to recover them, short of calling
 *  PHYSFS_deinit() and PHYSFS_init() again.
 *
 * PHYSFS_deinit() will automatically deregister all archivers, so you don't
 *  need to explicitly deregister yours if you otherwise shut down cleanly.
 *
 *   \param ext Filename extension that the archiver handles.
 *  \return Zero on error, non-zero on success.
 *
 * \sa PHYSFS_Archiver
 * \sa PHYSFS_registerArchiver
 */
//public static extern int PHYSFS_deregisterArchiver(const char *ext);
[DllImport(LN, CallingConvention = CallingConvention.Cdecl)]
public static extern int PHYSFS_deregisterArchiver(byte* ext);


/* Everything above this line is part of the PhysicsFS 2.1 API. */

/* end of physfs.h ... */

	}
}
