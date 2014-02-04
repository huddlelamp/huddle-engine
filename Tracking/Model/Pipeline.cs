using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Model
{
    [XmlRoot]
    public class Pipeline : ObservableObject
    {
        #region Processors

        /// <summary>
        /// The <see cref="Processors" /> property's name.
        /// </summary>
        public const string ProcessorsPropertyName = "Processors";

        private List<BaseProcessor> _processors = new List<BaseProcessor>();

        /// <summary>
        /// Sets and gets the Processors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("Processors")]
        [XmlArrayItem("Processor")]
        public List<BaseProcessor> Processors
        {
            get
            {
                return _processors;
            }

            set
            {
                if (_processors == value)
                {
                    return;
                }

                RaisePropertyChanging(ProcessorsPropertyName);
                _processors = value;
                RaisePropertyChanged(ProcessorsPropertyName);
            }
        }

        #endregion

        #region ctor

        public Pipeline()
        {

        }

        #endregion

        //#region IXmlSerializable

        //public XmlSchema GetSchema()
        //{
        //    return null;
        //}

        //public void ReadXml(XmlReader reader)
        //{
        //    reader.IsStartElement("InputSource");
        //    var type = Type.GetType(reader.GetAttribute("AssemblyQualifiedName"));
        //    var serial = new XmlSerializer(type);

        //    reader.ReadStartElement("IAnimal");
        //    InputSource = ((IInputSource)serial.Deserialize(reader));
        //    reader.ReadEndElement(); //InputSource
        //}

        //public void WriteXml(XmlWriter writer)
        //{
        //    writer.WriteStartElement("InputSource");
        //    writer.WriteAttributeString("AssemblyQualifiedName", InputSource.GetType().AssemblyQualifiedName);
        //    var xmlSerializer = new XmlSerializer(InputSource.GetType());
        //    xmlSerializer.Serialize(writer, InputSource);
        //    writer.WriteEndElement();
        //}
        //#endregion
    }
}
