using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Alchemy.Classes;
using Huddle.Engine.Data;
using Huddle.Engine.Util;
using Newtonsoft.Json;

namespace Huddle.Engine.Processor.Network
{
    [ViewTemplate("Web Socket Server", "WebSocketServerTemplate")]
    public class WebSocketServer : BaseProcessor
    {
        #region member fields

        private bool _isRunning = false;

        private Stopwatch _stopwatch;

        private Alchemy.WebSocketServer _webSocketServer;

        private readonly ConcurrentQueue<string> _deviceIdQueue = new ConcurrentQueue<string>();

        private readonly Dictionary<string, string> _deviceIdToGlyph = new Dictionary<string, string>();

        //private readonly ConcurrentDictionary<string, string> _clientIdToAddress = new ConcurrentDictionary<string, string>(); 
        private readonly ConcurrentDictionary<string, Client> _connectedClients = new ConcurrentDictionary<string, Client>();

        #endregion

        #region ctor

        public WebSocketServer()
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
            _webSocketServer = new Alchemy.WebSocketServer(4711, IPAddress.Any);
            _webSocketServer.OnConnect += context => Log("Connect {0}", context);

            _webSocketServer.OnConnected += context =>
            {
                Log("Connected {0}", context);

                var client = new Client(context);

                _connectedClients.TryAdd(context.ClientAddress.ToString(), client);
            };

            _webSocketServer.OnDisconnect += context =>
            {
                Log("Disconnect {0}", context);

                var address = context.ClientAddress.ToString();

                Client client;
                _connectedClients.TryRemove(address, out client);

                if (client == null)
                {
                    Console.WriteLine("WARNING - Client is null");
                    return;
                }

                Console.WriteLine("Client {0} disconnected", client.Id);

                // Put unused device id back to queue.
                _deviceIdQueue.Enqueue(client.Id);

                Stage(new Disconnected(this, "Disconnect") { Value = client.Id });
                Push();
            };

            _webSocketServer.OnReceive += context =>
            {
                var data = context.DataFrame.ToString();

                try
                {
                    dynamic response = JsonConvert.DeserializeObject(data);

                    var type = response.Type.Value;

                    switch (type as string)
                    {
                        case "Handshake":
                            var handshake = response.Data;

                            string name = null;
                            if (handshake.Name != null)
                                name = handshake.Name.Value;

                            string glyphId = null;
                            if (handshake.GlyphId != null)
                                glyphId = handshake.GlyphId.Value;

                            string deviceType = null;
                            if (handshake.DeviceType != null)
                                deviceType = handshake.DeviceType.Value;

                            if (handshake.Options != null)
                                Console.WriteLine(handshake.Options);

                            var client = _connectedClients[context.ClientAddress.ToString()];

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
                                    client.Send("No connection possible at this time because no glyph is available.");
                                }
                            }

                            client.Id = glyphId;
                            client.Name = name;

                            // Get glyph data for device id.
                            var glyphData = _deviceIdToGlyph[glyphId];

                            // inject the data type
                            var serial = string.Format("{{\"Type\":\"{0}\",\"Id\":\"{1}\",\"GlyphData\":\"{2}\"}}", "Glyph", glyphId, glyphData);

                            // Send glyph data to device in order to identify device in huddle.
                            client.Send(serial);

                            break;
                        case "Alive":
                            return;
                        case "Message":
                            foreach (var c in _connectedClients.Values)
                            {
                                if (Equals(context, c.Context))
                                {
                                    Console.WriteLine();
                                }
                                else
                                {
                                    c.Send(data);
                                }
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not deserialize Json response {0}", e.Message);
                }
            };
            //_webSocketServer.OnSend += context => Log("Send {0}", context);

            _webSocketServer.Start();

            // TODO Debug thread to check how many clients are connected.
            _isRunning = true;
            new Thread(() =>
                       {
                           while (_isRunning)
                           {
                               Console.WriteLine("{0} clients are connected.", _connectedClients.Count);
                               Thread.Sleep(1000);
                           }
                       })
            {
                IsBackground = true
            }.Start();

            base.Start();
        }

        public override void Stop()
        {
            _isRunning = false;

            // Alchemy WebSocketServer requires to manually disconnect client in
            // order to close the server.
            foreach (var client in _connectedClients.Values)
            {
                client.Context.OnDisconnect();
            }

            // Remove all connected clients.
            _connectedClients.Clear();

            if (_webSocketServer != null)
            {
                _webSocketServer.Stop();
                _webSocketServer.Dispose();
                _webSocketServer = null;
            }

            base.Stop();
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
            foreach (var client in clients)
            {
                // do not send identify message to clients already identified and clients that do not yet
                // have a glyph id assigned (assignment of glyph happens through handshake message).
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.Id)) ||
                    client.Id == null) continue;

                client.Send(digital);
            }

            var digital2 = new Digital(this, "Identify") { Value = false };
            foreach (var client in clients)
            {
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.Id)))
                {
                    client.Send(digital2);
                }
            }

            #endregion

            #region Send proximity information to clients

            var proximities = dataContainer.OfType<Proximity>().ToArray();

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
                    Pipeline.Fps = 1000.0 / _stopwatch.ElapsedMilliseconds;
                    _stopwatch.Restart();
                }
            }

            foreach (var proximity in proximities)
            {
                var proximity1 = proximity;
                foreach (var client in clients.Where(c => Equals(c.Id, proximity1.Identity)))
                {
                    client.Send(proximity);
                }
            }

            #endregion

            return null;
        }
    }
    public class Client
    {
        #region properties

        #region Id

        public string Id { get; set; }

        public string Name { get; set; }

        #endregion

        #endregion

        public UserContext Context { get; private set; }

        public Client(UserContext context)
        {
            Id = null;
            Name = null;
            Context = context;
        }

        public void Send(string message)
        {
            try
            {
                Context.Send(message);
            }
            catch (Exception e)
            {
                Context.Send(e.Message);
            }
        }

        public void Send(IData data)
        {
            try
            {
                var dataSerial = JsonConvert.SerializeObject(data);

                // inject the data type
                var serial = string.Format("{{\"Type\":\"{0}\",\"Data\":{1}}}", data.GetType().Name, dataSerial);

                Context.Send(serial);
            }
            catch (Exception e)
            {
                Context.Send(e.Message);
            }
        }
    }
}
