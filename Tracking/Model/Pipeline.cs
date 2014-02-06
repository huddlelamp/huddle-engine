using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Model
{
    [XmlRoot]
    public class Pipeline : BaseProcessor
    {
        #region ctor

        public Pipeline()
        {

        }

        #endregion

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
