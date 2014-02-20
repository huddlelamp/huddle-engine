using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Huddle.Engine.Model
{
    [XmlType]
    public class Test
    {
        [IgnoreDataMember]
        public string A { get; set; }
    }
}
