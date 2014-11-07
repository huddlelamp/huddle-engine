using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Merge RgbImage And Device", "MergeRgbImageAndDevice")]
    public class MergeRgbImageAndDevice : BaseProcessor
    {
        #region private members

        private RgbImageData _rgbImageData;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <returns></returns>
        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var rgbImages = dataContainer.OfType<RgbImageData>().ToArray();
            if (rgbImages.Any())
            {
                _rgbImageData = rgbImages.First().Copy() as RgbImageData;
                return null;
            }

            if (_rgbImageData != null)
            {
                dataContainer.Add(_rgbImageData);
                _rgbImageData = null;
            }

            return dataContainer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override IData Process(IData data)
        {
            return data;
        }
    }
}
