using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Proximity Processor", "ProximityProcessorTemplate")]
    public class ProximityProcessor : BaseProcessor
    {
        #region member fields

        private const string FakeDevicePrefix = "FakeDevice";

        private long _fakeDeviceId;

        private readonly object _deviceLock = new object();
        private readonly object _drawModelsLock = new object();

        #endregion

        #region commands

        public RelayCommand<SenderAwareEventArgs> AddFakeDeviceCommand { get; private set; }
        public RelayCommand<Device> RemoveFakeDeviceCommand { get; private set; }

        #endregion

        #region properties

        #region Distance

        /// <summary>
        /// The <see cref="Distance" /> property's name.
        /// </summary>
        public const string DistancePropertyName = "Distance";

        private double _distance = 0.005;

        /// <summary>
        /// Sets and gets the Distance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double Distance
        {
            get
            {
                return _distance;
            }

            set
            {
                if (_distance == value)
                {
                    return;
                }

                RaisePropertyChanging(DistancePropertyName);
                _distance = value;
                RaisePropertyChanged(DistancePropertyName);
            }
        }

        #endregion

        #region Devices

        /// <summary>
        /// The <see cref="Devices" /> property's name.
        /// </summary>
        public const string DevicesPropertyName = "Devices";

        private ObservableCollection<Device> _devices = new ObservableCollection<Device>();

        /// <summary>
        /// Sets and gets the Devices property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public ObservableCollection<Device> Devices
        {
            get
            {
                return _devices;
            }

            set
            {
                if (_devices == value)
                {
                    return;
                }

                RaisePropertyChanging(DevicesPropertyName);
                _devices = value;
                RaisePropertyChanged(DevicesPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public ProximityProcessor()
        {
            // Make collection accessible in current processor thread => UI Thread
            DispatcherHelper.CheckBeginInvokeOnUI(() => BindingOperations.EnableCollectionSynchronization(Devices, _deviceLock));
        }

        #endregion

        private bool _thresholdThreadRunning = false;
        private DateTime _lastUpdateTime;

        public override void Start()
        {
            #region Timeout Handling

            _thresholdThreadRunning = true;
            new Thread(() =>
            {
                while (_thresholdThreadRunning)
                {
                    var timeDiff = (DateTime.Now - _lastUpdateTime).TotalMilliseconds;
                    if (timeDiff > 200)
                    {
                        Devices.Clear();
                        Thread.Sleep(200);
                    }
                    else
                    {
                        Thread.Sleep((int)(1000 - timeDiff));
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            #endregion

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();

            // cleanup data
            Devices.Clear();

            #region Timeout Handling

            _thresholdThreadRunning = false;

            #endregion
        }

        #region Data Processing

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var disconnected = dataContainer.OfType<Disconnected>().ToList();
            if (disconnected.Count > 0)
            {
                // Remove all devices that are disconnected.
                Devices.RemoveAll(device => disconnected.All(d => d.Value == device.DeviceId));
                return null;
            }

            // Update last update timer that will be used by timeout handling
            _lastUpdateTime = DateTime.Now;

            var blobs = dataContainer.OfType<BlobData>().ToList();
            var markers = dataContainer.OfType<Marker>().ToList();
            var hands = dataContainer.OfType<Hand>().ToList();

            // Remove all devices that are not present by a blob anymore
            Devices.RemoveAll(device => blobs.All(b => b.Id != device.BlobId));

            // TODO optimize for each loop -> use parallel for each loop?
            Parallel.ForEach(blobs, blob =>
            {
                if (!DeviceExists(blob))
                    CreateDevice(blob);

                // Find matching QrCode for current blob
                double distance;
                var marker = GetClosestMarker(markers, blob, out distance);

                if (marker != null && distance < Distance)
                {
                    UpdateDevice(blob, marker);
                }
                else
                {
                    UpdateDevice(blob);
                }
            });

            return dataContainer;
        }

        public override IData Process(IData data)
        {
            return data;
        }

        public override IDataContainer PostProcess(IDataContainer dataContainer)
        {
            var devices = Devices.ToArray();

            var identifiedDevices = devices.Where(d => d.IsIdentified).ToArray();
            foreach (var device1 in identifiedDevices)
            {
                var p1 = new Point(device1.SmoothedCenter.X, device1.SmoothedCenter.Y);

                var location = new Point3D(p1.X, p1.Y, 0);
                var orientation = device1.SmoothedAngle;
                var proximity = CreateProximity("Display", device1, location, orientation);

                #region Calculate Proximities

                // TODO optimize for each loop -> parallel for each loop?
                foreach (var device2 in identifiedDevices)
                {
                    if (Equals(device1, device2)) continue;

                    if (device1.Key.StartsWith(FakeDevicePrefix)) continue;

                    var x = device2.SmoothedCenter.X - device1.SmoothedCenter.X;
                    var y = device2.SmoothedCenter.Y - device1.SmoothedCenter.Y;

                    var distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

                    // device2 to device1 angle
                    var globalAngle = Math.Atan(y / x).RadiansToDegree();

                    if (x >= 0 && y < 0)
                        globalAngle = 90 + globalAngle;
                    else if (x >= 0 && y >= 0)
                        globalAngle = 90 + globalAngle;
                    else if (x < 0 && y >= 0)
                        globalAngle = 270 + globalAngle;
                    else if (x < 0 && y < 0)
                        globalAngle = 270 + globalAngle;

                    // subtract own angle
                    var localAngle = globalAngle + (360 - device1.SmoothedAngle); // angle -= (device1.Angle % 180);
                    localAngle %= 360;

                    // Log device locations only if processor view is visible
                    if (IsRenderContent)
                    {
                        var log = new StringBuilder();

                        if (localAngle >= 225 && localAngle < 315)
                            log.AppendFormat("Device {0} is right of device {1}", device1.Key, device2.Key);
                        else if (localAngle >= 45 && localAngle < 135)
                            log.AppendFormat("Device {0} is left of device {1}", device1.Key, device2.Key);
                        else if (localAngle >= 135 && localAngle < 225)
                            log.AppendFormat("Device {0} is top of device {1}", device1.Key, device2.Key);
                        else //
                            log.AppendFormat("Device {0} is bottom of device {1}", device1.Key, device2.Key);

                        log.AppendFormat(" in a distance of {0}", distance);

                        log.AppendFormat(" and its local angle is {0} (Global Angle {1})", localAngle, globalAngle);

                        Log(log.ToString());
                    }

                    var p2 = new Point(device2.SmoothedCenter.X, device2.SmoothedCenter.Y);

                    var location2 = new Point3D(p2.X, p2.Y, 0);
                    var distance2 = (p2 - p1).Length;
                    proximity.Presences.Add(CreateProximity("Display", device2, location2, localAngle, distance2));
                }

                #endregion

                // TODO optimize -> the hand calculation below uses absolute values
                foreach (var hand in dataContainer.OfType<Hand>().ToArray())
                {
                    var x = hand.SmoothedCenter.X * 320 - device1.SmoothedCenter.X;
                    var y = hand.SmoothedCenter.Y * 240 - device1.SmoothedCenter.Y;

                    var distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

                    // Log only if processor view is visible
                    if (IsRenderContent)
                    {
                        if (distance < 30 && hand.Depth < 80)
                        {
                            Log("Hand {0} close to {1}", hand.Id, device1.DeviceId);
                        }
                    }

                    proximity.Presences.Add(new Proximity(this, "Hand", hand.Key)
                    {
                        Identity = "" + hand.Id,
                        Distance = distance,
                        Location = new Point3D(hand.SmoothedCenter.X, hand.SmoothedCenter.Y, hand.SlidingDepth),
                    });
                }

                Stage(proximity);
            }

            Stage(devices.ToArray<IData>());

            Push();

            return null;
        }

        #endregion

        private Proximity CreateProximity(string type, Device device, Point3D location, double orientation, double distance = 0.0)
        {
            return new Proximity(this, type, device.Key)
                   {
                       State = device.State,
                       Identity = device.DeviceId,
                       Location = location,
                       Distance = distance,
                       Orientation = orientation,
                       RgbImageToDisplayRatio = device.RgbImageToDisplayRatio
                   };
        }

        private void CreateDevice(BlobData blob)
        {
            var center = new Point(blob.Center.X, blob.Center.Y);
            var device = new Device(this, "unknown")
            {
                BlobId = blob.Id,
                IsIdentified = false,
                Center = center,
                State = blob.State,
                LastBlobAngle = blob.Angle,
                Shape = blob.Polygon,
                Area = blob.Area,
            };
            AddDevice(device);
        }

        private void UpdateDevice(BlobData blob, Marker marker = null)
        {
            var device = Devices.Single(d => d.BlobId == blob.Id);
            var center = new Point(blob.Center.X, blob.Center.Y);
            device.Key = "identified";
            device.BlobId = blob.Id;
            device.Center = center;
            device.State = blob.State;
            device.Shape = blob.Polygon;
            device.Area = blob.Area;

            if (marker != null)
            {
                device.DeviceId = marker.Id;
                device.IsIdentified = true;
                device.Angle = marker.Angle;
                device.RgbImageToDisplayRatio = marker.RgbImageToDisplayRatio;
            }
            else
            {
                var deltaAngle = blob.Angle - device.LastBlobAngle;

                // this is a hack but it works pretty good
                //if (deltaAngle > 45)
                //{
                //    deltaAngle -= 90;
                //    //return;
                //}
                //else if (deltaAngle < -45)
                //{
                //    deltaAngle += 90;
                //}

                //Console.WriteLine("Delta Angle {0}", deltaAngle);

                device.LastBlobAngle = blob.Angle;
                device.Angle += deltaAngle;
            }
        }

        private bool DeviceExists(BlobData blob, Func<Device, bool> whereFunc = null)
        {
            if (whereFunc != null)
                return Devices.Where(whereFunc).Any(d => d.BlobId == blob.Id);

            return Devices.Any(d => d.BlobId == blob.Id);
        }

        private void AddDevice(Device device)
        {
            Devices.Add(device);
        }

        private static Marker GetClosestMarker(IEnumerable<Marker> markers, BlobData blob, out double retDistance)
        {
            Marker candidate = null;
            var leastDistance = double.MaxValue;
            foreach (var marker in markers)
            {
                var distance = (marker.Center - blob.Center).Length;

                if (leastDistance < distance) continue;

                candidate = marker;
                leastDistance = distance;
            }

            retDistance = leastDistance;
            return candidate;
        }
    }
}
