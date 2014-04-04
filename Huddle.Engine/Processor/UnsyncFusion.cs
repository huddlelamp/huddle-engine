using System.Linq;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Unsync Fusion", "UnsyncFusionTemplate")]
    public class UnsyncFusion : BaseProcessor
    {
        #region Data Processing

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (dataContainer.OfType<BlobData>().Any())
            {
                if (StagedData.OfType<BlobData>().Any())
                    Push();

                foreach (var data in dataContainer)
                    Stage(data);
            }
            else if (dataContainer.OfType<LocationData>().Any())
            {
                if (StagedData.OfType<LocationData>().Any())
                    Push();

                foreach (var data in dataContainer)
                    Stage(data);
            }
            else if (dataContainer.OfType<Hand>().Any())
            {
                if (StagedData.OfType<Hand>().Any())
                    Push();

                foreach (var data in dataContainer)
                    Stage(data);
            }

            return null;
        }

        public override IData Process(IData data)
        {
            return data;
        }

        #endregion
    }
}
