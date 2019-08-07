using System;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public class PhysicsFSFileInfo
	{
		static readonly DateTime EPOCH = new DateTime(1970, 1, 1);

		PHYSFS_Stat stat;
		string filename;

		public string Name {
			get { return filename; }
		}
		public long Length {
			get { return stat.filesize; }
		}
		public bool IsReadOnly {
			get { return stat.@readonly != 0; }
		}
		public FileType Type {
			get { return (FileType)stat.filetype; }
		}
		public DateTime? ModTime {
			get { return GetTime(stat.modtime); }
		}
		public DateTime? CreateTime {
			get { return GetTime(stat.createtime); }
		}
		public DateTime? AccessTime {
			get { return GetTime(stat.accesstime); }
		}


		internal PhysicsFSFileInfo(string filename, PHYSFS_Stat stat)
		{
			this.filename = filename;
			this.stat = stat;
		}

		static DateTime? GetTime(long time)
		{
			if (time < 0)
				return null;
			return EPOCH + TimeSpan.FromTicks(TimeSpan.TicksPerSecond * time);
		}


		public PhysicsFSFileStream OpenRead()
		{
			return FileSystem.OpenRead(filename);
		}

		public PhysicsFSFileStream OpenWrite()
		{
			return FileSystem.OpenWrite(filename);
		}

		public System.IO.TextReader OpenText()
		{
			return new System.IO.StreamReader(OpenRead(), System.Text.Encoding.UTF8);
		}
	}

	public enum FileType {
		Regular   = PHYSFS_FILETYPE_REGULAR,
		Directory = PHYSFS_FILETYPE_DIRECTORY,
		Symlink   = PHYSFS_FILETYPE_SYMLINK,
		Other     = PHYSFS_FILETYPE_OTHER,
	}
}
