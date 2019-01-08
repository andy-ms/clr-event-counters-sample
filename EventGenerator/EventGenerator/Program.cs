using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Session;

namespace EventGenerator
{
	sealed class MinimalEventSource : EventSource
	{
		public static readonly MinimalEventSource Log = new MinimalEventSource();

		private readonly EventCounter counter;

		private MinimalEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
		{
			counter = new EventCounter("request", this);
		}

		public void EventName(float y)
		{
			// WriteEvent(1, y); // not needed, can be done in addition to event counter
			counter.WriteMetric(y);
		}
	}

	class Program
	{
		static void Main(string[] args) => DoMain().Wait();

		static async Task DoMain()
		{
			Random r = new Random();
			const string sessionName = "MinimalEventSource";
			using (TraceEventSession session = new TraceEventSession(sessionName))
			{
				session.EnableProvider(MinimalEventSource.Log.Guid);

				for (int i = 0; ; i++)
				{
					await Task.Delay(TimeSpan.FromSeconds(0.05));
					MinimalEventSource.Log.EventName((float)Math.Sin(i / 100.0));
				}
			}
		}
	}
}
