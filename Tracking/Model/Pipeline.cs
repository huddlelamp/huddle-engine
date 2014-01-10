using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Processor;
using Tools.FlockingDevice.Tracking.Source;
using Tools.FlockingDevice.Tracking.Source.Senz3D;

namespace Tools.FlockingDevice.Tracking.Model
{
    [XmlRoot]
    public class Pipeline : ObservableObject
    {
        #region InputSource

        /// <summary>
        /// The <see cref="InputSource" /> property's name.
        /// </summary>
        public const string InputSourcePropertyName = "InputSource";

        private InputSource _inputSource;

        /// <summary>
        /// Sets and gets the InputSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlElement]
        public InputSource InputSource
        {
            get
            {
                return _inputSource;
            }

            set
            {
                if (_inputSource == value)
                {
                    return;
                }

                RaisePropertyChanging(InputSourcePropertyName);
                _inputSource = value;
                RaisePropertyChanged(InputSourcePropertyName);
            }
        }

        #endregion

        #region ColorImageProcessors

        /// <summary>
        /// The <see cref="ColorImageProcessors" /> property's name.
        /// </summary>
        public const string ColorImageProcessorsPropertyName = "ColorImageProcessors";

        private ObservableCollection<RgbProcessor> _colorImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the ColorImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("ColorImageProcessors")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<RgbProcessor> ColorImageProcessors
        {
            get
            {
                return _colorImageProcessors;
            }

            set
            {
                if (_colorImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageProcessorsPropertyName);
                _colorImageProcessors = value;
                RaisePropertyChanged(ColorImageProcessorsPropertyName);
            }
        }

        #endregion

        #region DepthImageProcessors

        /// <summary>
        /// The <see cref="DepthImageProcessors" /> property's name.
        /// </summary>
        public const string DepthImageProcessorsPropertyName = "DepthImageProcessors";

        private ObservableCollection<RgbProcessor> _depthImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the DepthImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("DepthImageProcessors")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<RgbProcessor> DepthImageProcessors
        {
            get
            {
                return _depthImageProcessors;
            }

            set
            {
                if (_depthImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImageProcessorsPropertyName);
                _depthImageProcessors = value;
                RaisePropertyChanged(DepthImageProcessorsPropertyName);
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
