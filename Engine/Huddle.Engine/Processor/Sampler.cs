using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Sampler", "Sampler")]
    public class Sampler : BaseProcessor
    {
        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (dataContainer.FrameId % 10 == 0)
            {
                return dataContainer;
            }
            return null;
        }

        public override IData Process(IData data)
        {
            return data;
        }
    }
}
