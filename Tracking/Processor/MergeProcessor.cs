using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Domain;
using Tools.FlockingDevice.Tracking.Extensions;
using Tools.FlockingDevice.Tracking.Util;
using Point = System.Windows.Point;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("Merge", "Merge")]
    public class MergeProcessor : BaseProcessor
    {
        #region private fields

        private readonly List<BlobData> _blobs = new List<BlobData>();

        private DateTime _lastUpdate;

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

        private ObservableCollection<DrawModel> _drawModels = new ObservableCollection<DrawModel>();

        /// <summary>
        /// Sets and gets the DrawModels property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public ObservableCollection<DrawModel> DrawModels
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

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (dataContainer.Any(d => d is BlobData))
            {
                _lastUpdate = DateTime.Now;

                _blobs.Clear();
                DispatcherHelper.RunAsync(() => DrawModels.RemoveAll(m => m.Type == 1));

                var blobs = dataContainer.OfType<BlobData>().ToList();
                DispatcherHelper.RunAsync(() =>
                {
                    var removed = Devices.RemoveAll(device => blobs.All(b => b.Id != device.Id));
                    Console.WriteLine("Removed {0}", removed);
                });
            }
            else if (dataContainer.Any(d => d is LocationData))
            {
                _lastUpdate = DateTime.Now;

                DispatcherHelper.RunAsync(() => DrawModels.RemoveAll(m => m.Type == 2));
            }
            else
            {
                if (_lastUpdate != null)
                {
                    var diff = (DateTime.Now - _lastUpdate).TotalMilliseconds;

                    if (diff > 1000)
                    {
                        DispatcherHelper.RunAsync(() =>
                        {
                            Devices.Clear();
                            DrawModels.Clear();
                        });
                    }
                }
            }

            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            if (data is BlobData)
            {
                var blob = data as BlobData;

                _blobs.Add(blob.Copy() as BlobData);

                AddDrawModel(blob.X * Width, blob.Y * Height, Brushes.DeepPink, 1);

                if (Devices.All(d => d.Id != blob.Id)) return null;

                var device = Devices.Single(d => d.Id == blob.Id);
                device.X = blob.X * Width;
                device.Y = blob.Y * Height;
            }
            else if (data is LocationData)
            {
                var loc = data as LocationData;

                var locPoint = new Point(loc.X, loc.Y);

                AddDrawModel(loc.X * Width, loc.Y * Height, Brushes.DeepSkyBlue, 2);

                foreach (var blob in _blobs)
                {
                    // debug hook to check if update of devices works with blob only
                    if (Devices.Any(d => d.Id == blob.Id))
                    {
                        var device = Devices.Single(d => d.Id == blob.Id);
                        device.Angle = loc.Angle;
                        continue;
                    }
                        

                    //var contains = blob.Area.Contains(new Point(loc.X, loc.Y));

                    var blobPoint = new Point(blob.X, blob.Y);

                    var length = (locPoint - blobPoint).Length;

                    if (length < Distance)
                    {
                        var blob1 = blob;
                        DispatcherHelper.RunAsync(() => Devices.Add(new Device
                        {
                            Id = blob1.Id,
                            Key = loc.Key,
                            X = blob1.X * Width,
                            Y = blob1.Y * Height,
                            Angle = loc.Angle
                        }));
                    }
                }
            }
            return null;
        }

        private void AddDrawModel(double x, double y, Brush color, int type)
        {
            DispatcherHelper.RunAsync(() =>
            {
                var model = new DrawModel
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

    public class DrawModel : ObservableObject
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
