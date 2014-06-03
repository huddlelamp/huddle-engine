# Get Started in Minutes (Huddle Web Applications - Meteor)

1. Install Meteor (https://www.meteor.com)
2. Install npm (node package manager, part of NodeJS - http://nodejs.org)
3. Install Meteorite (https://atmospherejs.com)

# Development of Huddle Processors (C#/WPF Development)

1. Install Emgu CV (http://www.emgu.com/ or direct from SourceForge.net http://sourceforge.net/projects/emgucv/)
1.1 Huddle.Engine was implemented with Emgu CV version 2.4.9-alpha

This section addresses the development a new processor and its integration into the tracking framework. It is split into the three parts *Processor Logic*, *Processor Properties*, and *Processor User Interface*. If each part is implemented as described, a processor can be integrated into a processing pipeline using the drag and drop user interface of the tracking framework.

## Processor Logic - Create a new Processor

A new processor can be integrated into the tracking framework by deriving it from <code>BaseProcessor</code> or <code>RgbProcessor</code>. The <code>RgbProcessor</code> is an abstract base class for image processing that handles basic functions such as rendering pre-process and post-process images into <code>BitmapSource</code> objects. The <code>BitmapSource</code> object can be used later to visualize images apriori and posteriori manipulation. The ProcessAndView method provides access to the image stream of either an <code>InputSource</code> or another processor. For more information on the usage of the <code>BaseProcessor</code> see <code>MergeProcessor</code>.

For the UI a <code>ViewTemplateAttribute</code> with a processor name and a view template name is required. The tracking framework uses the template name to search for the corresponding <code>DataTemplate</code> key within the assembly. A data binding between a processor's properties and its <code>DataTemplate</code> is possible. Adding properties to a processor class is explained below.

    /// <summary>
    /// 
    /// </summary>
    [ViewTemplate("MyProcessor", "MyViewTemplateName")]
    public class MyProcessor : RgbProcessor
    {
		public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
        	// manipulation of image goes here

        	return image;
        }
    }

## Processor Properties - Add Properties to a Processor

A property is a standard .NET property and works with any MVVM framework such as [MVVM Light](https://mvvmlight.codeplex.com/). To enable data binding, a property needs to raise property change events (see <code>INotifyPropertyChanged</code>).

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

__IMPORTANT:__ The current <code>DataContractSerializer</code> uses an opt-in method to serialize properties. Please consult the [MSDN Xml serialization website](http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datacontractserializer(v=vs.110).aspx) for further information. Therefore, the following is important in order to avoid exception during serialization (e.g., save or load a pipeline):

Add a <code>IgnoreDataMemberAttribute</code> to each property that should not be serialized. 

    #region PreProcessImage

    /// <summary>
    /// The <see cref="PreProcessImage" /> property's name.
    /// </summary>
    public const string PreProcessImagePropertyName = "PreProcessImage";

    private BitmapSource _preProcessImage;

    /// <summary>
    /// Sets and gets the PreProcessImage property.
    /// Changes to that property's value raise the PropertyChanged event. 
    /// </summary>
    [IgnoreDataMember]
    public BitmapSource PreProcessImage
    {
        get
        {
            return _preProcessImage;
        }

        set
        {
            if (_preProcessImage == value)
            {
                return;
            }

            RaisePropertyChanging(PreProcessImagePropertyName);
            _preProcessImage = value;
            RaisePropertyChanged(PreProcessImagePropertyName);
        }
    }

    #endregion

## Processor User Interface (UI) - Create UI for a Processor

Create a new <code>ResourceDictionary</code> or use an existing dictionary and add a new <code>DataTemplate</code> to the dictionary. A processor has a <code>PreProcessImage</code> and a <code>PostProcessImage</code> property by default. Each can be bound to an <code>Image</code> component's source.

Additional properties like the <code>FlipHorizontal</code> property can be bound in order to make them accessible and manipulable in the UI.

    <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	                    xmlns:util="clr-namespace:Tools.FlockingDevice.Tracking.Util"
	                    xmlns:processor="clr-namespace:Tools.FlockingDevice.Tracking.Processor">
	    <DataTemplate x:Key="MyViewTemplateName" DataType="processor:MyProcessor">
	        <StackPanel>
	            <StackPanel Orientation="Horizontal" Margin="5">
	                <GroupBox Header="Pre Processed Image">
	                    <Image Source="{Binding Path=PreProcessImage}" />
	                </GroupBox>
	                <GroupBox Header="Post Processed Image">
	                    <Image Source="{Binding Path=PostProcessImage}" />
	                </GroupBox>
	            </StackPanel>

	            <CheckBox Content="Flip Horizontal" VerticalAlignment="Center" IsChecked="{Binding Path=FlipHorizontal}" />
	        </StackPanel>
	    </DataTemplate>
	</ResourceDictionary>

# Notes about the different available notes #

* When started, the "Senz3D" node automatically determines the region that the RGB image has within the larger depth image using the UV map. The result is then sent a single time further down the pipeline and can be picked up by the "Basics" node as data of type ROI. The "Basics" node then uses just the region for which RGB and depth information is available.
* The "Basics" node tries to process every input, so that's why "Basics" needs some data filtering before the node.