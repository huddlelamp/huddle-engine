using System;
using System.Windows.Media.Media3D;
using Fleck;
using Huddle.Engine.Data;
using Huddle.Engine.Properties;
using Newtonsoft.Json;

namespace Huddle.Engine.Processor.Network.Huddle
{
    /// <summary>
    /// This class wraps around a fleck socket connection and provides high-level methods to communicate
    /// with the client.
    /// </summary>
    internal class Client
    {
        #region member fields

        // connection to the client
        private readonly IWebSocketConnection _socket;

        private Proximity _previousProximity = null;

        #endregion

        #region properties

        #region State

        public ClientState State { get; set; }

        #endregion

        #region Id

        public string Id { get; set; }

        #endregion

        #region Name

        public string Name { get; set; }

        #endregion

        #region DeviceType

        public string DeviceType { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// A fleck client, which provides high-level methods to send data to the client (socket).
        /// </summary>
        /// <param name="socket">Client socket connection</param>
        public Client(IWebSocketConnection socket)
        {
            State = ClientState.Unknown;
            Id = null;
            Name = null;
            _socket = socket;
        }

        /// <summary>
        /// Sends a message to the client.
        /// </summary>
        /// <param name="message">Message</param>
        public void Send(string message)
        {
            try
            {
                _socket.Send(message);
            }
            catch (Exception e)
            {
                _socket.Send(e.Message);
            }
        }

        /// <summary>
        /// Sends data to the client. The data is serialized with JsonConvert and wrapped into a proper
        /// Huddle message format.
        /// 
        /// {"Type":"[DataType]","Data":[Data]}
        /// </summary>
        /// <param name="data">The data object (it must be serializable with Newtonsoft JsonConvert)</param>
        public void Send(IData data)
        {
            if (data is Proximity)
            {
                var proximity = data as Proximity;
                var x = proximity.Location.X;
                var y = proximity.Location.Y;
                var z = proximity.Location.Z;
                var angle = proximity.Orientation;

                //var rx = Math.Round(x, 2);
                //var ry = Math.Round(y, 2);
                //var rz = Math.Round(z, 2);
                //var rangle = Math.Round(angle, 2);

                //proximity.Location = new Point3D(rx, ry, rz);
                //proximity.Orientation = rangle;

                data = proximity;

                // do not send if proximity did not change.
                if (Equals(_previousProximity, proximity)) return;

                _previousProximity = proximity.Copy() as Proximity;
            }

            var dataSerial = JsonConvert.SerializeObject(data);

            // inject the data type into the message
            var serial = string.Format(Resources.TemplateDataMessage, data.GetType().Name, dataSerial);

            Send(serial);
        }

        /// <summary>
        /// Sends an error to the client.
        /// </summary>
        /// <param name="code">Error code to decode message on client.</param>
        /// <param name="reason">Reason for the error.</param>
        public void Error(int code, string reason)
        {
            var errorMessage = string.Format(Resources.TemplateErrorMessage, code, reason);

            Send(errorMessage);
        }

        /// <summary>
        /// Sends a bye bye message to the client and closes the connection.
        /// </summary>
        public void Close()
        {
            Send(Resources.TemplateByeByeMessage);
            _socket.Close();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherClient = obj as Client;
            if (otherClient == null)
                return false;

            return Equals(_socket.ConnectionInfo.Id, otherClient._socket.ConnectionInfo.Id);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return _socket.ConnectionInfo.Id.GetHashCode();
        }
    }
}
