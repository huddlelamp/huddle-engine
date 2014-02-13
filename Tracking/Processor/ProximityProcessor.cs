using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using FirstFloor.ModernUI.Presentation;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Domain;
using Tools.FlockingDevice.Tracking.Extensions;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("Proximity Processor", "ProximityProcessorTemplate")]
    public class ProximityProcessor : BaseProcessor
    {
        #region member fields

        private const string FakeDevicePrefix = "FakeDevice";

        private long _fakeDeviceId;

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
            AddFakeDeviceCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as IInputElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (sender == null || e == null) return;

                var position = e.GetPosition(sender);

                e.Handled = true;

                Devices.Add(new Device
                {
                    Id = 999,
                    Key = string.Format("{0}{1}", FakeDevicePrefix, ++_fakeDeviceId),
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

        private DateTime _lastUpdateTime;

        public override void Start()
        {
            new Thread(() =>
            {
                while (true)
                {
                    var timeDiff = (DateTime.Now - _lastUpdateTime).TotalMilliseconds;
                    if (timeDiff > 1000)
                    {
                        DispatcherHelper.RunAsync(() => Devices.Clear());
                        ClearDrawModels();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep((int) (1000 - timeDiff));   
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            base.Start();
        }

        #region Data Processing

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            _lastUpdateTime = DateTime.Now;

            var blobs = dataContainer.OfType<BlobData>().ToList();
            var qrCodes = dataContainer.OfType<LocationData>().ToList();

            foreach (var blob in blobs)
                qrCodes.Remove(blob);

            #region Update DrawModels

            ClearDrawModels();

            foreach (var blob in blobs)
                AddDrawModel(blob.X * Width, blob.Y * Height, Brushes.DeepPink, 1);

            foreach (var code in qrCodes)
                AddDrawModel(code.X * Width, code.Y * Height, Brushes.DeepSkyBlue, 2);

            #endregion

            // Remove all devices that are not present by a blob anymore
            DispatcherHelper.RunAsync(() => Devices.RemoveAll(device => blobs.All(b => b.Id != device.Id)));

            var notForDevices = Devices.Select(d => d.DeviceId).ToList();

            foreach (var blob in blobs)
            {
                // debug hook to check if update of devices works with blob only
                if (Devices.Any(d => d.Id == blob.Id))
                {
                    var device = Devices.Single(d => d.Id == blob.Id);
                    device.X = blob.X * Width;
                    device.Y = blob.Y * Height;
                    continue;
                }

                var blobPoint = new Point(blob.X, blob.Y);

                var codes = qrCodes.Where(c => (new Point(c.X, c.Y) - blobPoint).Length < Distance);

                if (!codes.Any())
                {
                    Stage(new Digital("ShowQrCode")
                    {
                        NotFor = notForDevices,
                        Value = true
                    });
                    Push();
                    continue;
                }

                var code = codes.First();

                Stage(new Digital("ShowQrCode")
                {
                    Id = code.Id,
                    Value = false
                });
                Push();

                AddDevice(blob, code);
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
            foreach (var device1 in devices)
            {
                foreach (var device2 in devices)
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

                    //Stage(new Proximity(device1.Key)
                    //{
                    //    Identity = device1.Key,
                    //    Location = new Point(device1.X, device1.Y),
                    //    Orientation = globalAngle
                    //});
                }

                Stage(new Proximity(device1.Key)
                {
                    Identity = device1.DeviceId,
                    Location = new Point(device1.X / Width, device1.Y / Height),
                    Orientation = device1.Angle
                });
            }

            Push();

            return null;
        }

        #endregion

        private void AddDevice(BlobData blob, LocationData code)
        {
            Stage(new LocationData(string.Format("{0}{1}", code.Key, blob.Id))
            {
                X = blob.X,
                Y = blob.Y,
                Angle = code.Angle
            });

            DispatcherHelper.RunAsync(() => Devices.Add(new Device
            {
                Id = blob.Id,
                DeviceId = code.Id,
                Key = code.Key,
                X = blob.X * Width,
                Y = blob.Y * Height,
                Angle = code.Angle
            }));
        }

        private void ClearDrawModels()
        {
            DispatcherHelper.RunAsync(() => DrawModels.Clear());
        }

        private void AddDrawModel(double x, double y, Brush color, int type)
        {
            DispatcherHelper.RunAsync(() =>
            {
                var model = new DrawModel2
                {
                    X = x,
                    Y = y,
                    Color = color,
                    Type = type
                };
                DrawModels.Add(model);
            });
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
