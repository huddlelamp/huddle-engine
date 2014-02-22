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
                //var address = context.ClientAddress.ToString();
                //var deviceId = context.DataFrame.ToString();

                //Client client;
                //_connectedClients.TryRemove(address, out client);
                //_identifiedClients.TryAdd(deviceId, client);

                //Log("Identified {0} as {1}", address, deviceId);

                var message = context.DataFrame.ToString();

                if (Equals("alive", message)) return;

                var client = _connectedClients[context.ClientAddress.ToString()];
                client.DeviceId = message;
            };
            //_webSocketServer.OnSend += context => Log("Send {0}", context);

            _webSocketServer.Start();

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();

            // send disconnect??
            foreach (var client in _connectedClients.Values)
            {
                //client.Send();
            }

            if (_webSocketServer != null)
            {
                _webSocketServer.Stop();
                _webSocketServer.Dispose();
            }
        }

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var devices = dataContainer.OfType<Device>().ToArray();
            var identifiedDevices = devices.Where(d => d.IsIdentified).ToArray();

            #region Reveal QrCode on unidentified clients

            var digital = new Digital("Identify") { Value = true };
            foreach (var client in _connectedClients.Values)
            {
                if (identifiedDevices.Any(d => Equals(d.DeviceId, client.DeviceId))) continue;

                client.Send(digital);
            }

            var digital2 = new Digital("Identify") { Value = false };
            foreach (var client in _connectedClients.Values)
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
                foreach (var client in _connectedClients.Values.Where(c => Equals(c.DeviceId, proximity.Identity)))
                {
                    client.Send(proximity);
                }
            }

            #endregion

            return null;
        }

        public override IData Process(IData data)
        {
            var device = data as Device;
            if (device != null)
            {
                if (!device.IsIdentified)
                {
                    // send message to unidentified clients
                    Console.WriteLine();
                }
                else
                {
                    // send update to identified client
                    Console.WriteLine("Identified");
                }
            }

            //var proxemity = data as Proximity;
            //if (proxemity != null)
            //{
            //    if (_identifiedClients.ContainsKey(proxemity.Identity))
            //    {
            //        var client = _identifiedClients[proxemity.Identity];
            //        client.Send(serial);
            //        return null;
            //    }
            //}

            //foreach (var client in _identifiedClients.Values)
            //{
            //    client.Send(serial);
            //}

            foreach (var client in _connectedClients.Values)
            {
                //var digital = data as Digital;
                //if (digital != null)
                //{
                //    if (_clientIdToAddress.ContainsKey(client.Context.ClientAddress.ToString()))
                //    {
                //        if (!digital.NotFor.Any())
                //        {
                //            var deviceId = _clientIdToAddress[client.Context.ClientAddress.ToString()];

                //            if (!string.IsNullOrWhiteSpace(digital.Id) && digital.Id != deviceId)
                //                continue;
                //        }
                //        else if (digital.NotFor.Contains(_clientIdToAddress[client.Context.ClientAddress.ToString()]))
                //            continue;
                //    }
                //}

                //Log("Send serial to {0}: {1}", client.Context.ClientAddress, serial);

                client.Send(data);
            }


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

        public void Send(IData data)
        {
            try
            {
                var dataSerial = JsonConvert.SerializeObject(data);

                // inject the data type
                var serial = string.Format("{{\"DataType\":\"{0}\",\"Data\":{1}}}", data.GetType().Name, dataSerial);

                Context.Send(serial);
            }
            catch (Exception e)
            {
                Context.Send(e.Message);
            }
        }
    }
}
