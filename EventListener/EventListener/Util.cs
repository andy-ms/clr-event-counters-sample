using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventListener
{
	static class Util
	{
		public static ulong LongToUlong(long l)
		{
			if (l < 0) throw new ArgumentException();
			return (ulong)l;
		}

		public static ulong DoubleToUlong(double d)
		{
			if (d < 0.0) throw new ArgumentException();
			return (ulong)d;
		}

		// Should mimic `Date.now()` in JavaScript
		public static ulong MsSinceUnixEpoch() =>
			MsSinceUnixEpoch(DateTime.Now);

		private static readonly DateTime unixEpoch = new DateTime(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);

		public static ulong MsSinceUnixEpoch(DateTime time) =>
			LongToUlong((time.ToUniversalTime() - unixEpoch).Ticks) / 10_000;

		/// Calls 'p' on elements one at a time (in reverse), removing all where it returns false.
		public static async Task FilterMutate<T>(List<T> list, Func<T, Task<bool>> p)
		{
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (!await p(list[i]))
				{
					list.RemoveAt(i);
				}
			}
		}
	}
}
