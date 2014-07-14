using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Huddle.Engine.Converter
{
    public class Point3DToJsonArrayConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var point = (Point3D)value;
            writer.WriteStartArray();
            writer.WriteValue(point.X);
            writer.WriteValue(point.Y);
            writer.WriteValue(point.Z);
            writer.WriteEndArray();

            writer.Flush();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Point3D);
        }
    }
}
