using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using Huddle.Engine.Data;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;
using Point = System.Windows.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Basics (Gray/Float)", "Basics")]
    public class BasicsGrayFloat : BaseImageProcessor<Gray, float>
    {
        #region private fields

        private bool _mouseDown;
        private Point _mousePoint;

        #endregion

        #region commands

        public RelayCommand<SenderAwareEventArgs> MouseDownCommand { get; private set; }
        public RelayCommand<SenderAwareEventArgs> MouseMoveCommand { get; private set; }
        public RelayCommand<SenderAwareEventArgs> MouseUpCommand { get; private set; }

        #endregion

        #region public properties

        #region IsInitialized

        // IsInitialized is used to set ROI if filter is used the first time.
        public bool IsInitialized { get; set; }

        #endregion

        #region IsUseROI

        /// <summary>
        /// The <see cref="IsUseROI" /> property's name.
        /// </summary>
        public const string IsUseROIPropertyName = "IsUseROI";

        private bool _isUseROI = true;

        /// <summary>
        /// Sets and gets the IsUseROI property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsUseROI
        {
            get
            {
                return _isUseROI;
            }

            set
            {
                if (_isUseROI == value)
                {
                    return;
                }

                RaisePropertyChanging(IsUseROIPropertyName);
                _isUseROI = value;
                RaisePropertyChanged(IsUseROIPropertyName);
            }
        }

        #endregion

        #region ROI

        /// <summary>
        /// The <see cref="ROI" /> property's name.
        /// </summary>
        public const string ROIPropertyName = "ROI";

        private Rectangle _roi = new Rectangle(0, 0, 1, 1);

        /// <summary>
        /// Sets and gets the ROI property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rectangle ROI
        {
            get
            {
                return _roi;
            }

            set
            {
                if (_roi == value)
                {
                    return;
                }

                RaisePropertyChanging(ROIPropertyName);
                _roi = value;
                RaisePropertyChanged(ROIPropertyName);
            }
        }

        #endregion

        #region ROITemp

        /// <summary>
        /// The <see cref="ROITemp" /> property's name.
        /// </summary>
        public const string ROITempPropertyName = "ROITemp";

        private Rectangle _roiTemp = Rectangle.Empty;

        /// <summary>
        /// Sets and gets the ROITemp property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rectangle ROITemp
        {
            get
            {
                return _roiTemp;
            }

            set
            {
                if (_roiTemp == value)
                {
                    return;
                }

                RaisePropertyChanging(ROITempPropertyName);
                _roiTemp = value;
                RaisePropertyChanged(ROITempPropertyName);
            }
        }

        #endregion

        #region Scale

        /// <summary>
        /// The <see cref="Scale" /> property's name.
        /// </summary>
        public const string ScalePropertyName = "Scale";

        private double _scale = 1.0;

        /// <summary>
        /// Sets and gets the Scale property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Scale
        {
            get
            {
                return _scale;
            }

            set
            {
                if (_scale == value)
                {
                    return;
                }

                RaisePropertyChanging(ScalePropertyName);
                _scale = value;
                RaisePropertyChanged(ScalePropertyName);
            }
        }

        #endregion

        #region FlipVertical

        /// <summary>
        /// The <see cref="FlipVertical" /> property's name.
        /// </summary>
        public const string FlipVerticalPropertyName = "FlipVertical";

        private bool _flipVertical = false;

        /// <summary>
        /// Sets and gets the FlipVertical property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool FlipVertical
        {
            get
            {
                return _flipVertical;
            }

            set
            {
                if (_flipVertical == value)
                {
                    return;
                }

                RaisePropertyChanging(FlipVerticalPropertyName);
                _flipVertical = value;
                RaisePropertyChanged(FlipVerticalPropertyName);
            }
        }

        #endregion

        #region FlipHorizontal

        /// <summary>
        /// The <see cref="FlipHorizontal" /> property's name.
        /// </summary>
        public const string FlipHorizontalPropertyName = "FlipHorizontal";

        private bool _flipHorizontal = false;

        /// <summary>
        /// Sets and gets the FlipHorizontal property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool FlipHorizontal
        {
            get
            {
                return _flipHorizontal;
            }

            set
            {
                if (_flipHorizontal == value)
                {
                    return;
                }

                RaisePropertyChanging(FlipHorizontalPropertyName);
                _flipHorizontal = value;
                RaisePropertyChanged(FlipHorizontalPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public BasicsGrayFloat()
            : base(false)
        {
            IsInitialized = false;
            MouseDownCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as IInputElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (sender == null || e == null) return;

                _mouseDown = true;

                sender.CaptureMouse();

                _mousePoint = e.GetPosition(sender);

                e.Handled = true;
            });

            MouseMoveCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as FrameworkElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (sender == null || e == null || !_mouseDown) return;

                var position = e.GetPosition(sender);
                var diff = position - _mousePoint;

                var x = Math.Min(_mousePoint.X, position.X);
                var y = Math.Min(_mousePoint.Y, position.Y);
                var width = Math.Abs(diff.X);
                var height = Math.Abs(diff.Y);

                ROITemp = new Rectangle((int)x, (int)y, (int)width, (int)height);

                e.Handled = true;
            });

            MouseUpCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as IInputElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (sender == null || e == null || !_mouseDown) return;

                ROI = ROITemp;
                ROITemp = Rectangle.Empty;

                sender.ReleaseMouseCapture();

                _mouseDown = false;
                e.Handled = true;
            });
        }

        #endregion

        public override IData Process(IData data)
        {
            var roi = data as ROI;
            if (roi != null)
                ROI = roi.RoiRectangle;

            return base.Process(data);
        }

        public override Image<Gray, float> PreProcess(Image<Gray, float> image0)
        {
            if (!IsInitialized)
            {
                ROI = new Rectangle(0, 0, image0.Width, image0.Height);

                IsInitialized = true;
            }

            var image = base.PreProcess(image0);

            image.Draw(ROI, new Gray(255.0), 1);

            return image;
        }

        public override Image<Gray, float> ProcessAndView(Image<Gray, float> image)
        {
            // mirror image
            try
            {
                var imageCopy = IsUseROI ? image.Copy(ROI) : image.Copy();

                // TODO Revise code.
                if (Scale != 1.0)
                {
                    var imageCopy2 = new Image<Gray, float>((int)(imageCopy.Width * Scale), (int)(imageCopy.Height * Scale));
                    CvInvoke.cvResize(imageCopy.Ptr, imageCopy2.Ptr, INTER.CV_INTER_CUBIC);

                    imageCopy.Dispose();
                    imageCopy = imageCopy2;
                }


                var flipCode = FLIP.NONE;

                if (FlipHorizontal)
                    flipCode |= FLIP.HORIZONTAL;
                if (FlipVertical)
                    flipCode |= FLIP.VERTICAL;

                if (flipCode != FLIP.NONE)
                    CvInvoke.cvFlip(imageCopy.Ptr, imageCopy.Ptr, flipCode);

                return imageCopy;
            }
            catch (Exception e)
            {
                Log("{0}", e.StackTrace);
                return null;
            }
        }
    }
}
