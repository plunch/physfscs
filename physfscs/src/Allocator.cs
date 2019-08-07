using System;
using System.Runtime.InteropServices;
using static PhysicsFSCS.NativeMethods;

namespace PhysicsFSCS
{
	public interface IAllocator : IDisposable
	{
		void Initialize();
		IntPtr Allocate(ulong size);
		IntPtr Reallocate(IntPtr ptr, ulong size);
		void Release(IntPtr ptr);
	}

	public interface IAllocatorFactory
	{
		IAllocator GetAllocator();
	}

	unsafe class UnmanagedAllocator : IAllocator
	{
		readonly PHYSFS_Allocator* alloc;

		public UnmanagedAllocator(PHYSFS_Allocator* alloc)
		{
			this.alloc = alloc;
		}

		// This method should not be called by managed code.
		void IAllocator.Initialize() { }
		void IDisposable.Dispose() { }

		public IntPtr Allocate(ulong size)
		{
			if (alloc->Malloc == null)
				return IntPtr.Zero;
			return (IntPtr)alloc->Malloc(size);
		}

		public IntPtr Reallocate(IntPtr ptr, ulong size)
		{
			if (alloc->Realloc == null)
				return IntPtr.Zero;
			return (IntPtr)alloc->Realloc((void*)ptr, size);
		}

		public void Release(IntPtr ptr)
		{
			if (alloc->Free != null)
				alloc->Free((void*)ptr);
		}
	}

	unsafe static class ManagedAllocator
	{
		static IAllocator allocator;
		static IAllocatorFactory factory;
		static PHYSFS_Allocator* ptr;

		static PHYSFS_Allocator.Init_fptr _init = Init;
		static PHYSFS_Allocator.Deinit_fptr _deinit = Deinit;
		static PHYSFS_Allocator.Malloc_fptr _malloc = Malloc;
		static PHYSFS_Allocator.Realloc_fptr _realloc = Realloc;
		static PHYSFS_Allocator.Free_fptr _free = Free;

		public static bool IsDefaultAllocator => ptr == null;

		public static IAllocator Allocator {
			get
			{
				if (ptr != null) {
					return allocator;
				} else {
					PHYSFS_Allocator* a = PHYSFS_getAllocator();
					if (a == null)
						return null;
					return new UnmanagedAllocator(a);
				}
			}
		}

		public static IAllocatorFactory AllocatorFactory {
			get { return factory; }
			set
			{
				bool reset = value == null;
				bool allocated = false;

				if (ptr == null && !reset) {
					ptr = (PHYSFS_Allocator*)Marshal.AllocHGlobal(Marshal.SizeOf<PHYSFS_Allocator>());
					allocated = true;

					ptr->Init = Init;
					ptr->Deinit = Deinit;
					ptr->Malloc = Malloc;
					ptr->Realloc = Realloc;
					ptr->Free = Free;
				}

				if (PHYSFS_setAllocator(reset ? null : ptr) == 0) {
					// OK
					if (reset && ptr != null)
						Marshal.FreeHGlobal((IntPtr)ptr);
					factory = value;
				} else {
					if (allocated) {
						Marshal.FreeHGlobal((IntPtr)ptr);
						ptr = null;
					}
					throw Exception();
				}
			}
		}

		static int Init()
		{
			return Safely(() => {
				if (factory != null)
				{
					if (allocator != null)
						throw new InvalidOperationException("Double-init of allocator");

					allocator = factory.GetAllocator();
				} else if (allocator == null) {
					throw new InvalidOperationException("Managed callback registered, but no allocator");
				}
				return 1;
			});
		}

		static void Deinit()
		{
			try {
				allocator.Dispose();
			} catch {
				// Unfortunately no way to report errors ?
			} finally {
				allocator = null;
			}
		}

		static unsafe void* Malloc(ulong size)
		{
			try {
				return (void*)allocator.Allocate(size);
			} catch {
				return null;
			}
		}

		static unsafe void* Realloc(void* ptr, ulong size)
		{
			try {
				return (void*)allocator.Reallocate((IntPtr)ptr, size);
			} catch {
				return null;
			}
		}

		static unsafe void Free(void* ptr)
		{
			try {
				allocator.Release((IntPtr)ptr);
			} catch {
				// Unfortunately no way to report errors ?
			}
		}
	}
}
