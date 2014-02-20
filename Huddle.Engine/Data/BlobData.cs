using System.Windows;

namespace Huddle.Engine.Data
{
    public class BlobData : LocationData
    {
        #region properties

        #region Id

        /// <summary>
        /// The <see cref="Id" /> property's name.
        /// </summary>
        public const string IdPropertyName = "Id";

        private long _id;

        /// <summary>
        /// Sets and gets the Id property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (_id == value)
                {
                    return;
                }

                RaisePropertyChanging(IdPropertyName);
                _id = value;
                RaisePropertyChanged(IdPropertyName);
            }
        }

        #endregion

        #region Area

        /// <summary>
        /// The <see cref="Area" /> property's name.
        /// </summary>
        public const string AreaPropertyName = "Area";

        private Rect _area = Rect.Empty;

        /// <summary>
        /// Sets and gets the Area property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rect Area
        {
            get
            {
                return _area;
            }

            set
            {
                if (_area == value)
                {
                    return;
                }

                RaisePropertyChanging(AreaPropertyName);
                _area = value;
                RaisePropertyChanged(AreaPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public BlobData(string key)
            : base(key)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new BlobData(Key)
            {
                Id = Id,
                X = X,
                Y = Y,
                Angle = Angle,
                Area = Area
            };
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
