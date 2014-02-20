using System.ComponentModel;

namespace Huddle.Engine.Model
{
    public interface ILocator : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region properties

        double X { get; set; }
        double Y { get; set; }
        double Angle { get; set; }

        #endregion
    }
}
