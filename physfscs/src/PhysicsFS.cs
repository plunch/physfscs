using System;
using System.IO;
using System.Collections.Generic;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public static class PhysicsFS
	{
		public static bool IsInitialized => PHYSFS_isInit() != 0;

		public static unsafe DeinitDisposable Initialize()
		{
			var argv = Environment.GetCommandLineArgs();
			int ret;
			fixed(byte* argv0 = StringToUTF8(argv[0]))
				ret = PHYSFS_init(argv0);
			if (ret == 0)
				throw Exception();
			return new DeinitDisposable();
		}

		public static IAllocator Allocator { get { return ManagedAllocator.Allocator; } }

		public static IAllocatorFactory AllocatorFactory {
			get { return ManagedAllocator.AllocatorFactory; }
			set { ManagedAllocator.AllocatorFactory = value; }
		}

		public static Version LinkedVersion
		{
			get {
				PHYSFS_Version ver;

				unsafe {
					PHYSFS_getLinkedVersion(&ver);
				}

				return new Version(ver.major, ver.minor, ver.patch);
			}
		}

		public static SupportedArchiveTypesEnumerable SupportedArchiveTypes
		{
			get { return new SupportedArchiveTypesEnumerable(); }
		}

		public static bool PermitSymbolicLinks
		{
			get { return PHYSFS_symbolicLinksPermitted() != 0; }
			set { PHYSFS_permitSymbolicLinks(value ? 1 : 0); }
		}

		public static unsafe void RegisterArchiver(ArchiverInfo info, Func<ArchiveArguments, IArchive> open)
		{
			var managed = new ManagedArchiveType(open);
			var native = (PHYSFS_Archiver*)managed;

			int ret;
			fixed(byte* x = StringToUTF8(info.Extension))
			fixed(byte* d = StringToUTF8(info.Description))
			fixed(byte* a = StringToUTF8(info.Author))
			fixed(byte* u = StringToUTF8(info.Url)) {
				native->info.extension = x;
				native->info.description = d;
				native->info.author = a;
				native->info.url = u;
				native->info.supportsSymlinks = info.SupportsSymlinks ? 1 : 0;

				ret = PHYSFS_registerArchiver(native);
			}
			if (ret == 0)
				throw Exception();
		}

		public static unsafe void DeregisterArchiver(string ext)
		{
			int ret;
			fixed(byte* b = StringToUTF8(ext))
				ret = PHYSFS_deregisterArchiver(b);
			if (ret == 0)
				throw Exception();
		}

		public static unsafe void SetSaneConfig(string organization, string appName,
				                        string archiveExt, bool includeCdRoms,
							bool archivesFirst)
		{
			int ret;
			fixed(byte* org = StringToUTF8(organization))
			fixed(byte* app = StringToUTF8(appName))
			fixed(byte* ar = StringToUTF8(archiveExt)) {
				ret = PHYSFS_setSaneConfig(org, app, ar,
				                           includeCdRoms?1:0, archivesFirst?1:0);
			}
			if (ret == 0)
				throw Exception();
		}
	}
}
