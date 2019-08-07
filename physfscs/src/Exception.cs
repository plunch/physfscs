using System;
using System.Runtime.Serialization;


namespace PhysicsFSCS
{
	[Serializable]
	public class PhysicsFSException : Exception
	{
		readonly ErrorCode errorCode;

		public ErrorCode ErrorCode { get { return errorCode; } }

		public PhysicsFSException(ErrorCode errorCode, string message) : base(message)
		{
			this.errorCode = errorCode;
		}

		protected PhysicsFSException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.errorCode = (ErrorCode)info.GetInt32("PhysFS_errorCode");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("PhysFS_errorCode", (int)errorCode);
		}
	}

	public enum ErrorCode {
		Ok = 0,
		OtherError = 1,
		OutOfMemory = 2,
		NotInitialized = 3,
		IsInitialized = 4,
		Argv0IsNull = 5,
		Unsupported = 6,
		PastEOF = 7,
		FilesStillOpen = 8,
		InvalidArgument = 9,
		NotMounted = 10,
		NotFound = 11,
		SymlinkForbidden = 12,
		NoWriteDirectory = 13,
		OpenForReading = 14,
		OpenForWriting = 15,
		NotAFile = 16,
		ReadOnly = 17,
		Corrupt = 18,
		SymlinkLoop = 19,
		IO = 20,
		Permission = 21,
		NoSpace = 22,
		BadFilename = 23,
		Busy = 24,
		DirectoryNotEmpty = 25,
		OSError = 26,
		Duplicate = 27,
		BadPassword = 28,
		AppCallback = 29
	}
}
