using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Newtonsoft.Json;
using Ninja.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventListener
{
	using static Util;

	struct EventCounterData
	{
#pragma warning disable CS0649 // These fields are assigned to by JsonConvert.DeserializeObject
		[JsonProperty]
		public readonly string Name;
		[JsonProperty]
		public readonly double Mean;
		[JsonProperty]
		public readonly double StandardDeviation;
		[JsonProperty]
		public readonly uint Count;
		[JsonProperty]
		public readonly double Min;
		[JsonProperty]
		public readonly double Max;
		[JsonProperty]
		/// This should be approximately Program.EventCounterIntervalSec.
		public readonly double IntervalSec;
#pragma warning restore CS0649

		public override string ToString() => JsonConvert.SerializeObject(this);
	}

	struct DataPoint
	{
		[JsonProperty]
		public readonly double timeMs;
		[JsonProperty]
		public readonly double y;

		public DataPoint(double _timeMs, double _y) { timeMs = _timeMs; y = _y; }
	}

	internal class EventListener : IDisposable
	{
		private readonly TraceEventSession session;
		private readonly ETWTraceEventSource source;

		private EventListener(TraceEventSession _session, ETWTraceEventSource _source) { session = _session; source = _source;  }

		void IDisposable.Dispose()
		{
			session.Dispose();
			source.Dispose();
		}

		public delegate void EventCounterHandler(ulong timestamp, EventCounterData data);

		public event EventCounterHandler EventCounterEvent;

		//TODO:MOVE
		private static T Parse<T>(string json)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(json);
			}
			catch (JsonReaderException)
			{
				throw new Exception($"Invalid json: {json}");
			}
		}

		public static EventListener Create(Guid eventSourceGuid, string sessionName, double eventCounterIntervalSeconds)
		{
			var session = new TraceEventSession(sessionName);
			TraceEventProviderOptions options = new TraceEventProviderOptions("EventCounterIntervalSec", eventCounterIntervalSeconds.ToString()); //I'm just guessing what the args should be...
			session.EnableProvider(eventSourceGuid, providerLevel: TraceEventLevel.Verbose, matchAnyKeywords: ulong.MaxValue, options: options);
			var source = new ETWTraceEventSource(sessionName, TraceEventSourceType.Session);
			var e = new EventListener(session, source);
			source.Dynamic.All += delegate (TraceEvent data)
			{
				if (data.ProviderGuid != eventSourceGuid) throw new Exception();

				if (data.EventName == "EventCounters")
				{
					string s = data.PayloadString(0).Replace("∞", "0");
					EventCounterData ed = Parse<EventCounterData>(s);
					e.EventCounterEvent?.Invoke(MsSinceUnixEpoch(data.TimeStamp), ed);
				}
			};
			return e;
		}

		/// never terminates!
		public void Process() => source.Process();
	}

	struct ClientAndSocket : IDisposable
	{
		public readonly TcpClient tcpClient;
		public readonly WebSocket webSocket;
		public ClientAndSocket(TcpClient _tcpClient, WebSocket _webSocket) { tcpClient = _tcpClient; webSocket = _webSocket; }

		public void Dispose()
		{
			tcpClient.Dispose();
			webSocket.Dispose();
		}
	}

	class Server : IDisposable
	{
		private readonly TcpListener listener;
		private readonly List<ClientAndSocket> clients = new List<ClientAndSocket>();
		private Server(TcpListener _listener) { listener = _listener; }

		void IDisposable.Dispose()
		{
			foreach (var x in clients) x.Dispose();
		}

		// TODO: Cancellation token -- upon Dispose(), stop this
		private async Task AcceptConnections()
		{
			while (true)
			{
				var tcpClient = await listener.AcceptTcpClientAsync();
				Stream stream = tcpClient.GetStream();
				WebSocketHttpContext context = await new WebSocketServerFactory().ReadHttpHeaderFromStreamAsync(stream);
				if (!context.IsWebSocketRequest) throw new Exception("TODO");
				WebSocket webSocket = await new WebSocketServerFactory().AcceptWebSocketAsync(context);
				clients.Add(new ClientAndSocket(tcpClient, webSocket));
				Console.WriteLine("connected");
			}
		}

		public static Server Create(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			var server = new Server(listener);
			// Do *not* await -- this will run forever until this is closed.
			var _ = server.AcceptConnections();
			return server;
		}

		public async Task Send(DataPoint data) => await FilterMutate(clients, async client =>
		{
			ReadOnlyMemory<byte> buff = Encoding.Default.GetBytes(JsonConvert.SerializeObject(data)).AsMemory();
			try
			{
				await client.webSocket.SendAsync(buff, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: new CancellationTokenSource().Token);
			}
			catch (IOException) // TODO: distinguish different kinds of errors. It seems like I get an IOException when the socket was closed.
			{
				Console.WriteLine($"Disconnecting from {client}");
				return false;
			}
			return true;
		});
	}

	class Program
	{
		static readonly Guid minimaleventsource_log_guid = new Guid("7370a335-9b9a-5ba0-63c2-8ce045c35503"); // TODO: MinimalEventSource.Log.Guid (make that a shared library)

		const int port = 8002;

		static void Main()
		{
			using (var events = EventListener.Create(minimaleventsource_log_guid, "my session name", eventCounterIntervalSeconds: 0.2))
			using (var server = Server.Create(8002))
			{
				events.EventCounterEvent += async (timestamp, data) =>
					await server.Send(new DataPoint(timestamp, data.Mean));
				events.Process();
			}
		}
	}
}
