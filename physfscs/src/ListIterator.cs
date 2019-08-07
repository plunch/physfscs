using System;
using System.Collections.Generic;
using System.Collections;
using static PhysicsFSCS.NativeMethods;
using static PhysicsFSCS.PhysicsFS;

namespace PhysicsFSCS
{
	public unsafe struct SearchPathEnumerator : IEnumerable<string>
	{
		public StringListEnumerator GetEnumerator()
		{
			return new StringListEnumerator(PHYSFS_getSearchPath());
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public unsafe struct CdRomDirsEnumerator : IEnumerable<string>
	{
		public StringListEnumerator GetEnumerator()
		{
			return new StringListEnumerator(PHYSFS_getCdRomDirs());
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public unsafe struct FileNameEnumerator : IEnumerable<string>
	{
		string directory;

		internal FileNameEnumerator(string directory)
		{
			this.directory = directory;
		}

		public StringListEnumerator GetEnumerator()
		{
			byte** ptr;
			fixed(byte* dir = StringToUTF8(directory))
				ptr = PHYSFS_enumerateFiles(dir);
			if (ptr == null) {
				var error = (ErrorCode)PHYSFS_getLastErrorCode();
				if (error > 0)
					throw Exception(error);
			}
			return new StringListEnumerator(ptr);
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public unsafe struct StringListEnumerator : IEnumerator<string>
	{
		byte** ptr;
		byte** current;

		public string Current { get; private set; }
		object IEnumerator.Current => Current;

		internal StringListEnumerator(byte** list)
		{
			this.ptr = list;
			this.current = null;
			this.Current = null;
		}

		public bool MoveNext() 
		{
			if (current == null) {
				current = ptr;
				if (*current == null)
					return false;
				Current = UTF8ToString(*current);
				return true;
			}

			current++;

			if (*current == null)
				return false;

			Current = UTF8ToString(*current);
			return true;
		}

		void IEnumerator.Reset()
		{
			current = null;
		}

		public void Dispose()
		{
			var p = ptr;
			ptr = null;
			if (p != null)
				PHYSFS_freeList(p);
		}
	}

	public unsafe struct SupportedArchiveTypesEnumerable : IEnumerable<ArchiverInfo>
	{
		public ArchiveInfoListEnumerator GetEnumerator()
		{
			return new ArchiveInfoListEnumerator(PHYSFS_supportedArchiveTypes());
		}

		IEnumerator<ArchiverInfo> IEnumerable<ArchiverInfo>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public unsafe struct ArchiveInfoListEnumerator : IEnumerator<ArchiverInfo>
	{
		PHYSFS_ArchiveInfo** ptr;
		PHYSFS_ArchiveInfo** current;

		public ArchiverInfo Current { get; private set; }
		object IEnumerator.Current => Current;

		internal ArchiveInfoListEnumerator(PHYSFS_ArchiveInfo** list)
		{
			this.ptr = list;
			this.current = null;
			this.Current = null;
		}

		ArchiverInfo Build(PHYSFS_ArchiveInfo* v)
		{
			string ext = UTF8ToString(v->extension);
			string desc = UTF8ToString(v->description);
			string author = UTF8ToString(v->author);
			string url = UTF8ToString(v->url);
			bool symlinks = v->supportsSymlinks != 0;

			return new ArchiverInfo(ext, desc, author, url, symlinks);
		}

		public bool MoveNext() 
		{
			if (current == null) {
				current = ptr;
				if (*current == null)
					return false;
				Current = Build(*current);
				return true;
			}

			current++;

			if (*current == null)
				return false;

			Current = Build(*current);
			return true;
		}

		void IEnumerator.Reset()
		{
			current = null;
		}

		public void Dispose()
		{
			// return values from PHYSFS_supportedArchiveTypes should not be freed
			
			/*
			var p = ptr;
			ptr = null;
			if (p != null)
				PHYSFS_freeList(p);
				*/
		}
	}
}
