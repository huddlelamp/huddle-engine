using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Sync Fusion", "SyncFusion")]
    public class SyncFusion : BaseProcessor
    {
        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var push = false;
            Parallel.ForEach(StagedData, staged =>
            {
                if (dataContainer.Any(d => d.Source.Equals(staged.Source)))
                push = true;
            });

            if (push)
                Push();

            //foreach (var data in dataContainer.OfType<BlobData>())
            //    Stage(data);

            Stage(dataContainer.ToArray());

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
