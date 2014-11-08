using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Network.Huddle;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;
using Newtonsoft.Json;
using Fleck;

namespace Huddle.Engine.Processor.Network
{
    [ViewTemplate("Huddle Server", "HuddleServerTemplate")]
    public class HuddleServer : BaseProcessor
    {
        #region member fields

        #region Limit Fps

        private readonly Stopwatch _limitFpsStopwatch = new Stopwatch();

        #endregion

        #region Fps Calculation

        private Stopwatch _stopwatch;
        private readonly double[] _fpsSmoothing = new double[60];
        private int _fpsSmoothingIndex;

        #endregion

        private Fleck.WebSocketServer _webSocketServer;

        private readonly ConcurrentQueue<string> _deviceIdQueue = new ConcurrentQueue<string>();

        private readonly Dictionary<string, string> _deviceIdToGlyph = new Dictionary<string, string>();

        private readonly ConcurrentDictionary<Guid, Client> _connectedClients = new ConcurrentDictionary<Guid, Client>();

        #endregion

        #region properties

        #region Port

        /// <summary>
        /// The <see cref="Port" /> property's name.
        /// </summary>
        public const string PortPropertyName = "Port";

        private int _port = 1948;

        /// <summary>
        /// Sets and gets the Port property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                if (_port == value)
                {
                    return;
                }

                RaisePropertyChanging(PortPropertyName);
                _port = value;
                RaisePropertyChanged(PortPropertyName);
            }
        }

        #endregion

        #region ClientCount

        /// <summary>
        /// The <see cref="ClientCount" /> property's name.
        /// </summary>
        public const string ClientCountPropertyName = "ClientCount";

        private int _clientCount;

        /// <summary>
        /// Sets and gets the ClientCount property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int ClientCount
        {
            get
            {
                return _clientCount;
            }

            set
            {
                if (_clientCount == value)
                {
                    return;
                }

                RaisePropertyChanging(ClientCountPropertyName);
                _clientCount = value;
                RaisePropertyChanged(ClientCountPropertyName);
            }
        }

        #endregion

        #region LimitFps

        /// <summary>
        /// The <see cref="LimitFps" /> property's name.
        /// </summary>
        public const string LimitFpsPropertyName = "LimitFps";

        private int _limitFps = 30;

        /// <summary>
        /// Sets and gets the LimitFps property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int LimitFps
        {
            get
            {
                return _limitFps;
            }

            set
            {
                if (_limitFps == value)
                {
                    return;
                }

                RaisePropertyChanging(LimitFpsPropertyName);
                _limitFps = value;
                RaisePropertyChanged(LimitFpsPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public HuddleServer()
        {
            using (var reader = new StreamReader("Resources/TagDefinitions.txt"))
            {
                String line;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        var tokens = line.Split(' ');
                        var deviceId = tokens[0];
                        var code = tokens[1];

                        _deviceIdToGlyph.Add(deviceId, code);
                        _deviceIdQueue.Enqueue(deviceId);
                    }
                } while (line != null);
            }
        }

        #endregion

        public override void Start()
        {
            PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case PortPropertyName:
                        RestartWebSocketServer();
                        break;
                }
            };

            // start web socket server.
            StartWebSocketServer();

            base.Start();
        }

        public override void Stop()
        {
            // stop web socket server
            StopWebSocketServer();

            base.Stop();
        }

        /// <summary>
        /// Start web socket server.
        /// </summary>
        private void StartWebSocketServer()
        {
            // stop web socket server in case a server is already running.
            StopWebSocketServer();

            _limitFpsStopwatch.Start();

            _webSocketServer = new Fleck.WebSocketServer(string.Format("ws://0.0.0.0:{0}", Port));
            _webSocketServer.Start(socket =>
            {
                socket.OnOpen = () => OnClientConnect(socket);
                socket.OnClose = () => OnClientDisconnect(socket);
                socket.OnMessage = message => OnClientMessage(socket, message);
            });
        }

        /// <summary>
        /// Stop web socket server.
        /// </summary>
        private void StopWebSocketServer()
        {
            _limitFpsStopwatch.Stop();

            // send disconnect to clients
            foreach (var client in _connectedClients.Values)
                client.Close();

            // reset client count
            ClientCount = 0;

            if (_webSocketServer != null)
                _webSocketServer.Dispose();
        }

        /// <summary>
        /// Restart web socket server.
        /// </summary>
        private void RestartWebSocketServer()
        {
            StopWebSocketServer();
            StartWebSocketServer();
        }

        public override IData Process(IData data)
        {
            return data;
        }

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var devices = dataContainer.OfType<Device>().ToArray();
            var identifiedDevices = devices.Where(d => d.IsIdentified).ToArray();

            var clients = _connectedClients.Values.ToArray();

            #region Reveal QrCode on unidentified clients

            var digital = new Digital(this, "Identify") { Value = true };
            foreach (var client in clients.Where(c => c.State != ClientState.AwaitIdentification))
            {
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.Id)) || client.Id == null) continue;

                client.Send(digital);
                client.State = ClientState.AwaitIdentification;
            }

            var digital2 = new Digital(this, "Identify") { Value = false };
            foreach (var client in clients.Where(c => c.State == ClientState.AwaitIdentification))
            {
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.Id)))
                {
                    client.Send(digital2);
                    client.State = ClientState.Identified;
                }
            }

            #endregion

            #region Send proximity information to clients

            if (_limitFpsStopwatch.ElapsedMilliseconds > (1000 / LimitFps))
            {

                var proximities = dataContainer.OfType<Proximity>().ToArray();

                #region Fps Calculation

                // Calculate frames per second -> this speed defines the outgoing fps
                if (proximities.Any())
                {
                    if (_stopwatch == null)
                    {
                        _stopwatch = new Stopwatch();
                        _stopwatch.Start();
                    }
                    else
                    {
                        _fpsSmoothingIndex = ++_fpsSmoothingIndex % _fpsSmoothing.Length;
                        _fpsSmoothing[_fpsSmoothingIndex] = 1000.0 / _stopwatch.ElapsedMilliseconds;
                        Pipeline.Fps = _fpsSmoothing.Average();
                        _stopwatch.Restart();
                    }
                }

                #endregion

                foreach (var proximity in proximities)
                {
                    var proximity1 = proximity;
                    foreach (var client in clients.Where(c => Equals(c.Id, proximity1.Identity)))
                    {
                        client.Send(proximity);
                    }
                }

                _limitFpsStopwatch.Restart();
            }

            #endregion

            return null;
        }

        #region WebSocket Handling

        /// <summary>
        /// Handle client connection.
        /// </summary>
        /// <param name="socket">Client socket.</param>
        private void OnClientConnect(IWebSocketConnection socket)
        {
            var clientKey = socket.ConnectionInfo.Id;

            var client = new Client(socket);

            _connectedClients.TryAdd(clientKey, client);
            ClientCount = _connectedClients.Count;

            // Log client connected message.
            var info = socket.ConnectionInfo;
            LogFormat("Client {0}:{1} connected", info.ClientIpAddress, info.ClientPort);
        }

        /// <summary>
        /// Handle client disconnection.
        /// </summary>
        /// <param name="socket">Client socket.</param>
        private void OnClientDisconnect(IWebSocketConnection socket)
        {
            var clientKey = socket.ConnectionInfo.Id;

            Client client;
            _connectedClients.TryRemove(clientKey, out client);
            ClientCount = _connectedClients.Count;

            if (client == null)
            {
                var info = socket.ConnectionInfo;
                LogFormat("Client does exists for socket connection: {0}:{1}", info.ClientIpAddress, info.ClientPort);
                return;
            }

            // Log client disconnected message.
            LogFormat("Client {0} [id={1}, deviceType={2}] disconnected", client.Name, client.Id, client.DeviceType);

            // Put unused device id back to queue.
            _deviceIdQueue.Enqueue(client.Id);

            // Notify tracking that client disconnected.
            Stage(new Disconnected(this, "Disconnect") { Value = client.Id });
            Push();
        }

        /// <summary>
        /// Handle incoming client messages.
        /// </summary>
        /// <param name="socket">Client socket.</param>
        /// <param name="message">Client message.</param>
        private void OnClientMessage(IWebSocketConnection socket, string message)
        {
            var clientKey = socket.ConnectionInfo.Id;

            Client client;
            _connectedClients.TryGetValue(clientKey, out client);

            // check if client exists for the socket connection
            if (client == null)
            {
                var info = socket.ConnectionInfo;
                LogFormat("Client does exists for socket connection: {0}:{1}", info.ClientIpAddress, info.ClientPort);
                return;
            }

            try
            {
                dynamic response = JsonConvert.DeserializeObject(message);

                var type = response.Type.Value;

                switch (type as string)
                {
                    case "Handshake":
                        OnHandshake(client, response.Data);
                        break;
                    case "Alive":
                        OnAlive(client);
                        return;
                    case "Message":
                        OnMessage(client, message);
                        break;
                }
            }
            catch (Exception e)
            {
                client.Error(300, "Could not deserialize message. Not a valid JSON format.");
                LogFormat("Could not deserialize message. Not a valid JSON format: {0}", e.Message);
            }
        }

        #endregion

        #region Handling Client Messages

        /// <summary>
        /// Handles the handshake message. The server will assign an unused glyph if glyph id is not set
        /// by client.
        /// </summary>
        /// <param name="client">The sender of the handshake.</param>
        /// <param name="handshake">Handshake message from the client.</param>
        private void OnHandshake(Client client, dynamic handshake)
        {
            string name = null;
            if (handshake.Name != null)
                name = handshake.Name.Value;

            string glyphId = null;
            if (handshake.GlyphId != null)
                glyphId = handshake.GlyphId.Value;

            string deviceType = null;
            if (handshake.DeviceType != null)
                deviceType = handshake.DeviceType.Value;

            // TODO is this console log necessary???
            if (handshake.Options != null)
                Log(handshake.Options.ToString());

            // if glyph id is not set by the client then assign a random id.
            if (glyphId == null || !_deviceIdToGlyph.ContainsKey(glyphId))
            {
                if (_deviceIdQueue.Count > 0)
                {
                    // Get an unsed device id.
                    if (!_deviceIdQueue.TryDequeue(out glyphId))
                        throw new Exception("Could not dequeue device id");
                }
                else
                {
                    // TODO possible improvement of API could be client.Error("ERR300", "Too many connected devices. Try again later."));
                    client.Send("Too many connected devices. Try again later.");
                    return;
                }
            }

            // store client information
            client.Id = glyphId;
            client.Name = name;
            client.DeviceType = deviceType;

            // Log client disconnected message.
            LogFormat("Client {0} [id={1}, deviceType={2}] identified", client.Name, client.Id, client.DeviceType);

            // Get glyph data for device id.
            var glyphData = _deviceIdToGlyph[glyphId];

            // inject the data type
            var serial = string.Format("{{\"Type\":\"{0}\",\"Id\":\"{1}\",\"GlyphData\":\"{2}\"}}", "Glyph", glyphId, glyphData);

            // Send glyph data to device in order to identify device in huddle.
            client.Send(serial);
        }

        /// <summary>
        /// Called each time a client sends an alive message.
        /// </summary>
        /// <param name="sender">The sender of the alive message.</param>
        private void OnAlive(Client sender)
        {
            // do nothing yet
        }

        /// <summary>
        /// Sends the received message to connected clients except for the sender.
        /// </summary>
        /// <param name="sender">The sender of the message, which does not receive its message.</param>
        /// <param name="message">Message sent to connected clients.</param>
        private void OnMessage(Client sender, string message)
        {
            foreach (var c in _connectedClients.Values.Where(c => !c.Equals(sender)))
            {
                c.Send(message);
            }
        }

        #endregion
    }
}
