using System;
using System.Net.WebSockets;
using Akka.Actor;
using PureWebSockets;
using Serilog;
using Newtonsoft.Json;

namespace CreateAR.Snap
{
    public class ConnectionActor : ReceiveActor
    {
        public class Connect
        {
            public string Url;

            public string Token;

            public string InstanceId;

            public string UserId;

            public IActorRef Subscriber;
        }

        public class Ready
        {
            public string Url;
        }

        public class Heartbeat
        {
            //
        }

        private string _token;

        private string _instanceId;

        private string _userId;

        private IActorRef _subscriber;

        private PureWebSocket _socket;

        private ICancelable _heartbeat;

        public ConnectionActor()
        {
            Become(Waiting);
        }

        private void Waiting()
        {
            Receive<Connect>(msg => OnConnect(msg));
        }

        private void Connecting()
        {
            if (null != _heartbeat)
            {
                _heartbeat.Cancel();
                _heartbeat = null;
            }
        }

        private void Subscribed()
        {
            // listen for a heartbeat
            Receive<Heartbeat>(msg => _socket.Send("40"));

            // start the heartbeat
            _heartbeat = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(3),
                Self,
                new Heartbeat(),
                null);

            // tell subscriber we're ready!
            _subscriber.Tell(new Ready());
        }

        private void Send(WebSocketRequest request)
        {
            if (null == request.Headers)
            {
                request.Headers = new WebSocketRequest.HeaderData();
            }
            request.Headers.Authorization = $"Bearer {_token}";

            var json = $"42[\"post\", {JsonConvert.SerializeObject(request)}]";
            Log.Information($"Sending {json}.");
            if (!_socket.Send(json))
            {
                Log.Warning("Could not send!");
            }
        }

        private void OnConnect(Connect msg)
        {
            _token = msg.Token;
            _instanceId = msg.InstanceId;
            _userId = msg.UserId;
            _subscriber = msg.Subscriber;

            Become(Connecting);

            Log.Information("Starting connection to {0}.", msg.Url);

            // start connecting
            _socket = new PureWebSocket(
                msg.Url,
                new PureWebSocketOptions
                {
                    DebugMode = true,
                    SendDelay = 100
                });

            _socket.OnStateChanged += Socket_OnStateChanged;
            _socket.OnMessage += Socket_OnMessage;
            _socket.OnClosed += Socket_OnClosed;
            _socket.OnSendFailed += Socket_OnSendFailed;
            _socket.Connect();
        }

        private void Socket_OnSendFailed(string data, Exception ex)
        {
            Log.Warning("Could not send message: {0} -> {1}.", data, ex);

            // TODO: tell parent
        }

        private void Socket_OnClosed(WebSocketCloseStatus reason)
        {
            Log.Information("Socket closed : {0}.", reason);

            // TODO: reconnect
        }

        private void Socket_OnMessage(string message)
        {
            Log.Information("Received message : {0}.", message);
        }

        private void Socket_OnStateChanged(WebSocketState newState, WebSocketState prevState)
        {
            Log.Information("Socket stage change: {0} -> {1}.",
                prevState,
                newState);
            
            if (newState == WebSocketState.Open)
            {
                // subscribe to trellis events
                Send(new WebSocketRequest(
                    $"/v1/snap/{_instanceId}/{_userId}/subscribe",
                    "post"
                ));
            }
        }
    }
}