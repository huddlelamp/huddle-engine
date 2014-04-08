using System;
using System.Collections.Concurrent;
using System.Linq;
using Alchemy.Classes;
using Huddle.Engine.Data;
using Huddle.Engine.Domain;
using Huddle.Engine.Util;
using Newtonsoft.Json;

namespace Huddle.Engine.Processor.Network
{
    [ViewTemplate("Web Socket Server", "WebSocketServerTemplate")]
    public class WebSocketServer : BaseProcessor
    {
        #region member fields

        private Alchemy.WebSocketServer _webSocketServer;

        //private readonly ConcurrentDictionary<string, string> _clientIdToAddress = new ConcurrentDictionary<string, string>(); 
        private readonly ConcurrentDictionary<string, Client> _connectedClients = new ConcurrentDictionary<string, Client>();

        #endregion

        #region ctor

        public WebSocketServer()
        {

        }

        #endregion

        public override void Start()
        {
            _webSocketServer = new Alchemy.WebSocketServer(4711);
            _webSocketServer.OnConnect += context => Log("Connect {0}", context);

            _webSocketServer.OnConnected += context =>
            {
                Log("Connected {0}", context);
                _connectedClients.TryAdd(context.ClientAddress.ToString(), new Client(context));
            };

            _webSocketServer.OnDisconnect += context =>
            {
                Log("Disconnect {0}", context);

                var address = context.ClientAddress.ToString();

                Client client;
                _connectedClients.TryRemove(address, out client);
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
                            var deviceId = response.DeviceId.Value;
                            var client = _connectedClients[context.ClientAddress.ToString()];
                            client.DeviceId = deviceId;
                            break;
                        case "Alive":
                            return;
                        case "Broadcast":
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

            base.Start();
        }

        public override void Stop()
        {
            if (_webSocketServer != null)
            {
                _webSocketServer.Stop();
                _webSocketServer.Dispose();
                _webSocketServer = null;
            }

            // send disconnect??
            foreach (var client in _connectedClients.Values)
            {
                //client.Send();
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
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.DeviceId))) continue;

                client.Send(digital);
            }

            var digital2 = new Digital(this, "Identify") { Value = false };
            foreach (var client in clients)
            {
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.DeviceId)))
                {
                    client.Send(digital2);
                }
            }

            #endregion

            #region Send proximity information to clients

            var proximities = dataContainer.OfType<Proximity>();

            foreach (var proximity in proximities)
            {
                var proximity1 = proximity;
                foreach (var client in clients.Where(c => Equals(c.DeviceId, proximity1.Identity)))
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

        #region DeviceId

        public string DeviceId { get; set; }

        #endregion

        #endregion

        public UserContext Context { get; private set; }

        public Client(UserContext context)
        {
            DeviceId = null;
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
