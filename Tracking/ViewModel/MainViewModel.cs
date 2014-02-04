using System.Windows.Input;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ProcessorViewModel
    {
        #region private fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);

        #endregion

        #region commands

        public RelayCommand<MouseButtonEventArgs> DragInitiateCommand { get; private set; }
        
        #endregion

        #region public properties

        #region Pipeline

        /// <summary>
        /// The <see cref="Pipeline" /> property's name.
        /// </summary>
        public const string PipelinePropertyName = "Pipeline";

        private PipelineViewModel _pipeline = new PipelineViewModel();

        /// <summary>
        /// Sets and gets the Pipeline property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public PipelineViewModel Pipeline
        {
            get
            {
                return _pipeline;
            }

            set
            {
                if (_pipeline == value)
                {
                    return;
                }

                RaisePropertyChanging(PipelinePropertyName);
                _pipeline = value;
                RaisePropertyChanged(PipelinePropertyName);

                RaisePropertyChanged(IsInputSourceSetPropertyName);
            }
        }

        #endregion

        #region IsInputSourceSet

        /// <summary>
        /// The <see cref="IsInputSourceSet" /> property's name.
        /// </summary>
        public const string IsInputSourceSetPropertyName = "IsInputSourceSet";

        /// <summary>
        /// Sets and gets the IsInputSourceSet property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsInputSourceSet
        {
            get { return Pipeline != null; }
        }

        #endregion

        #endregion

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
            }
            else
            {
            }
        }
    }
}