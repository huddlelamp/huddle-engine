using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Domain;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;

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

        #region Width

        /// <summary>
        /// The <see cref="Width" /> property's name.
        /// </summary>
        public const string WidthPropertyName = "Width";

        private double _width = 320.0;

        /// <summary>
        /// Sets and gets the Width property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Width
        {
            get
            {
                return _width;
            }

            set
            {
                if (_width == value)
                {
                    return;
                }

                RaisePropertyChanging(WidthPropertyName);
                _width = value;
                RaisePropertyChanged(WidthPropertyName);
            }
        }

        #endregion

        #region Height

        /// <summary>
        /// The <see cref="Height" /> property's name.
        /// </summary>
        public const string HeightPropertyName = "Height";

        private double _height = 240.0;

        /// <summary>
        /// Sets and gets the Height property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Height
        {
            get
            {
                return _height;
            }

            set
            {
                if (_height == value)
                {
                    return;
                }

                RaisePropertyChanging(HeightPropertyName);
                _height = value;
                RaisePropertyChanged(HeightPropertyName);
            }
        }

        #endregion

        #region DrawModels

        /// <summary>
        /// The <see cref="DrawModels" /> property's name.
        /// </summary>
        public const string DrawModelsPropertyName = "DrawModels";

        private ObservableCollection<DrawModel2> _drawModels = new ObservableCollection<DrawModel2>();

        /// <summary>
        /// Sets and gets the DrawModels property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public ObservableCollection<DrawModel2> DrawModels
        {
            get
            {
                return _drawModels;
            }

            set
            {
                if (_drawModels == value)
                {
                    return;
                }

                RaisePropertyChanging(DrawModelsPropertyName);
                _drawModels = value;
                RaisePropertyChanged(DrawModelsPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public ProximityProcessor()
        {
            // Make collection accessible in current processor thread => UI Thread
            DispatcherHelper.CheckBeginInvokeOnUI(() => BindingOperations.EnableCollectionSynchronization(Devices, _deviceLock));
            DispatcherHelper.CheckBeginInvokeOnUI(() => BindingOperations.EnableCollectionSynchronization(DrawModels, _drawModelsLock));

            AddFakeDeviceCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as IInputElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (sender == null || e == null) return;

                var position = e.GetPosition(sender);

                e.Handled = true;

                Devices.Add(new Device(string.Format("{0}{1}", FakeDevicePrefix, ++_fakeDeviceId))
                {
                    BlobId = 999,
                    DeviceId = "999",
                    IsIdentified = true,
                    X = position.X,
                    Y = position.Y,
                    Angle = 0//Math.PI
                });
            });

            RemoveFakeDeviceCommand = new RelayCommand<Device>(d =>
            {
                Console.WriteLine();
            });
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
                    if (timeDiff > 1000)
                    {
                        Devices.Clear();
                        ClearDrawModels();
                        Thread.Sleep(1000);
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

            _thresholdThreadRunning = false;
        }

        #region Data Processing

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            // Update last update timer that will be used by timeout handling
            _lastUpdateTime = DateTime.Now;

            var blobs = dataContainer.OfType<BlobData>().ToList();
            var qrCodes = dataContainer.OfType<LocationData>().ToList();

            // Update view
            UpdateView(blobs, qrCodes);

            // Remove all devices that are not present by a blob anymore
            Devices.RemoveAll(device => blobs.All(b => b.Id != device.BlobId));

            foreach (var blob in blobs)
            {
                if (!DeviceExists(blob))
                    CreateDevice(blob);

                var blobPoint = new Point(blob.X, blob.Y);

                // Find matching QrCode for current blob
                var codes = qrCodes.Where(c => (new Point(c.X, c.Y) - blobPoint).Length < Distance).ToArray();

                if (codes.Any())
                {
                    var code = codes.First();

                    //Console.WriteLine("Update device {0} with blob {1}", code.Id, blob.Id);
                    UpdateDevice(blob, code);
                }
                else
                {
                    //Console.WriteLine("Update blob {0}", blob.Id);
                    UpdateDevice(blob);
                }
            }

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
            var identifiedDevices0 = devices.Where(d => d.IsIdentified).ToArray();
            foreach (var device1 in identifiedDevices)
            {
                var p1 = new Point(device1.X / Width, device1.Y / Height);
                var proximity = new Proximity(device1.Key)
                {
                    Identity = device1.DeviceId,
                    Location = p1,
                    Orientation = device1.Angle
                };

                #region Calculate Proximities

                foreach (var device2 in identifiedDevices0)
                {
                    if (Equals(device1, device2)) continue;

                    if (device1.Key.StartsWith(FakeDevicePrefix)) continue;

                    var x = device2.X - device1.X;
                    var y = device2.Y - device1.Y;

                    var distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

                    // device2 to device1 angle
                    var globalAngle = Math.Atan(y / x).RandianToDegree();

                    if (x >= 0 && y < 0)
                        globalAngle = 90 + globalAngle;
                    else if (x >= 0 && y >= 0)
                        globalAngle = 90 + globalAngle;
                    else if (x < 0 && y >= 0)
                        globalAngle = 270 + globalAngle;
                    else if (x < 0 && y < 0)
                        globalAngle = 270 + globalAngle;

                    // subtract own angle
                    var localAngle = globalAngle + (360 - device1.Angle); // angle -= (device1.Angle % 180);
                    localAngle %= 360;

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

                    var p2 = new Point(device2.X / Width, device2.Y / Height);

                    proximity.Presences.Add(new Proximity(device2.Key)
                    {
                        Identity = device2.DeviceId,
                        Location = p2,
                        Distance = (p2 - p1).Length,
                        Orientation = localAngle
                    });
                }

                #endregion

                Stage(proximity);
            }

            Stage(devices.ToArray<IData>());

            Push();

            return null;
        }

        #endregion

        private void CreateDevice(BlobData blob)
        {
            var device = new Device("not identified")
            {
                BlobId = blob.Id,
                IsIdentified = false,
                X = blob.X * Width,
                Y = blob.Y * Height,
                LastBlobAngle = blob.Angle
            };
            AddDevice(device);
        }

        private void UpdateDevice(BlobData blob, LocationData code = null)
        {
            var device = Devices.Single(d => d.BlobId == blob.Id);
            device.Key = "identified";
            device.BlobId = blob.Id;
            device.X = blob.X * Width;
            device.Y = blob.Y * Height;

            if (code != null)
            {
                device.DeviceId = code.Id;
                device.IsIdentified = true;
                device.Angle = code.Angle;
            }
            else
            {
                var deltaAngle = blob.Angle - device.LastBlobAngle;

                // this is a hack but it works pretty good
                if (deltaAngle > 45)
                {
                    deltaAngle -= 90;
                    //return;
                }
                else if (deltaAngle < -45)
                {
                    deltaAngle += 90;
                }

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

        private void ClearDrawModels()
        {
            DrawModels.Clear();
        }

        private void AddDrawModel(double x, double y, Brush color, int type)
        {
            var model = new DrawModel2
            {
                X = x,
                Y = y,
                Color = color,
                Type = type
            };
            DrawModels.Add(model);
        }

        private void UpdateView(IEnumerable<BlobData> blobs, IEnumerable<LocationData> qrCodes)
        {
            #region Update DrawModels

            ClearDrawModels();

            foreach (var blob in blobs)
                AddDrawModel(blob.X * Width, blob.Y * Height, Brushes.DeepPink, 1);

            foreach (var code in qrCodes)
                AddDrawModel(code.X * Width, code.Y * Height, Brushes.DeepSkyBlue, 2);

            #endregion
        }
    }

    public class DrawModel2 : ObservableObject
    {
        #region properties

        #region X

        /// <summary>
        /// The <see cref="X" /> property's name.
        /// </summary>
        public const string XPropertyName = "X";

        private double _x = 0.0;

        /// <summary>
        /// Sets and gets the X property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                if (_x == value)
                {
                    return;
                }

                RaisePropertyChanging(XPropertyName);
                _x = value;
                RaisePropertyChanged(XPropertyName);
            }
        }

        #endregion

        #region Y

        /// <summary>
        /// The <see cref="Y" /> property's name.
        /// </summary>
        public const string YPropertyName = "Y";

        private double _y = 0.0;

        /// <summary>
        /// Sets and gets the Y property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                if (_y == value)
                {
                    return;
                }

                RaisePropertyChanging(YPropertyName);
                _y = value;
                RaisePropertyChanged(YPropertyName);
            }
        }

        #endregion

        #region Color

        /// <summary>
        /// The <see cref="Color" /> property's name.
        /// </summary>
        public const string ColorPropertyName = "Color";

        private Brush _color = Brushes.Yellow;

        /// <summary>
        /// Sets and gets the Color property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Brush Color
        {
            get
            {
                return _color;
            }

            set
            {
                if (_color == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorPropertyName);
                _color = value;
                RaisePropertyChanged(ColorPropertyName);
            }
        }

        #endregion

        #region Type

        /// <summary>
        /// The <see cref="Type" /> property's name.
        /// </summary>
        public const string TypePropertyName = "Type";

        private int _type = -1;

        /// <summary>
        /// Sets and gets the Type property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Type
        {
            get
            {
                return _type;
            }

            set
            {
                if (_type == value)
                {
                    return;
                }

                RaisePropertyChanging(TypePropertyName);
                _type = value;
                RaisePropertyChanged(TypePropertyName);
            }
        }

        #endregion

        #endregion
    }
}
