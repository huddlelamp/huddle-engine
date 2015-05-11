using Emgu.CV;
using Huddle.Engine.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huddle.Engine.Data
{
    public class RotationMatrixData : BaseData
    {
        #region properties

        #region RotationMatrix

        /// <summary>
        /// The <see cref="RotationMatrix" /> property's name.
        /// </summary>
        public const string RotationMatrixPropertyName = "RotationMatrix";

        private Matrix<double> _rotationMatrix;

        /// <summary>
        /// Sets and gets the RotationMatrix property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Matrix<double> RotationMatrix
        {
            get
            {
                return _rotationMatrix;
            }

            set
            {
                if (_rotationMatrix == value)
                {
                    return;
                }

                RaisePropertyChanging(RotationMatrixPropertyName);
                _rotationMatrix = value;
                RaisePropertyChanged(RotationMatrixPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public RotationMatrixData(IProcessor source, string key, Matrix<double> rotationMatrix)
            : base(source, key)
        {
            RotationMatrix = rotationMatrix;
        }

        #endregion

        public override IData Copy()
        {
            return new RotationMatrixData(Source, Key, RotationMatrix);
        }

        public override void Dispose()
        {
            
        }
    }
}
