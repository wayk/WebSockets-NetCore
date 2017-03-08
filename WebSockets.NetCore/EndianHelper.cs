using System;
using System.Linq;

namespace WebSockets.NetCore
{
	internal static class EndianHelper
    {
		public static short Swap(short value)
		{
			return BitConverter.ToInt16(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static ushort Swap(ushort value)
		{
			return BitConverter.ToUInt16(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static int Swap(int value)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static uint Swap(uint value)
		{
			return BitConverter.ToUInt32(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static long Swap(long value)
		{
			return BitConverter.ToInt64(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}

		public static ulong Swap(ulong value)
		{
			return BitConverter.ToUInt64(BitConverter.GetBytes(value).Reverse().ToArray(), 0);
		}
	}
}
