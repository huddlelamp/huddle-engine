using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace Tools.FlockingDevice.Tracking.Model
{
    [XmlType]
    public class Test
    {
        [IgnoreDataMember]
        public string A { get; set; }
    }
}
