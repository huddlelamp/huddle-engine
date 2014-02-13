using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor.Network
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
                _webSocketServer.Stop();
        }

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            var dataSerial = JsonConvert.SerializeObject(data);
            
            // inject the data type
            var serial = string.Format("{{\"DataType\":\"{0}\",\"Data\":{1}}}", data.GetType().Name, dataSerial);

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

                client.Send(serial);
            }
                
            
            return null;
        }
    }
    public class Client
    {
        public UserContext Context { get; set; }

        public Client(UserContext context)
        {
            Context = context;
        }

        public void Send(string data)
        {
            try
            {
                Context.Send(data);
            }
            catch (Exception e)
            {
                Context.Send(e.Message);
            }
        }
    }
}
