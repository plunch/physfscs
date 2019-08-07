using System;

namespace PhysicsFSCS
{
	public class ArchiverInfo
	{
		readonly string extension;
		readonly string description;
		readonly string author;
		readonly string url;
		readonly bool supportsSymlinks;

		public string Extension => extension;
		public string Description => description;
		public string Author => author;
		public string Url => url;
		public bool SupportsSymlinks => supportsSymlinks;

		public ArchiverInfo(string extension,
		                   string description,
		                   string author,
		                   string url,
		                   bool supportsSymlinks)
		{
			this.extension = extension;
			this.description = description;
			this.author = author;
			this.url = url;
			this.supportsSymlinks = supportsSymlinks;
		}
	}
}
