using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public class Marker: LocationData
    {
        public Marker(IProcessor source, string key) : base(source, key)
        {
        }

        public override IData Copy()
        {
            return new Marker(Source, Key)
            {
                X = X,
                Y = Y,
                Angle = Angle
            };
        }

        public override void Dispose()
        {

        }
    }
}
