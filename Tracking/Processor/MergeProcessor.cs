using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        #endregion

        #region properties

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

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (dataContainer.Any(d => d is BlobData))
            {
                _blobs.Clear();

                var blobs = dataContainer.OfType<BlobData>().ToList();
                DispatcherHelper.RunAsync(() =>
                {
                    var removed = Devices.RemoveAll(device => blobs.All(b => b.Id != device.Id));
                    Console.WriteLine("Removed {0}", removed);
                });
            }

            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            if (data is BlobData)
            {
                var blob = data as BlobData;
                _blobs.Add(blob.Copy() as BlobData);

                if (Devices.All(d => d.Id != blob.Id)) return null;

                var device = Devices.Single(d => d.Id == blob.Id);
                device.X = blob.X;
                device.Y = blob.Y;
            }
            else if (data is LocationData)
            {
                var loc = data as LocationData;

                foreach (var blob in _blobs)
                {
                    // debug hook to check if update of devices works with blob only
                    if (Devices.Any(d => d.Id == blob.Id))
                        continue;

                    var contains = blob.Area.Contains(new Point(loc.X, loc.Y));

                    if (contains)
                    {
                        var blob1 = blob;
                        DispatcherHelper.RunAsync(() => Devices.Add(new Device
                        {
                            Id = blob1.Id,
                            Key = loc.Key,
                            X = blob1.X,
                            Y = blob1.Y,
                            Angle = loc.Angle
                        }));
                    }
                }
            }
            return null;
        }
    }
}
