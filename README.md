# Development

This section addresses the development a new processor and its integration into the tracking framework. It is split into the three parts *Processor Logic*, *Processor Properties*, and *Processor User Interface*. If each part is implemented as described, a processor can be integrated into a processing pipeline using the drag and drop user interface of the tracking framework.

## Processor Logic - Create a new Processor

A new processor can be integrated into the tracking framework by deriving it from <code>RgbProcessor</code>. The <code>RgbProcessor</code> is an abstract base class that handles basic functions such as rendering pre-process and post-process images into <code>BitmapSource</code> objects. The <code>BitmapSource</code> object can be used later to visualize images apriori and posteriori manipulation. The ProcessAndView method provides access to the image stream of either an <code>InputSource</code> or another processor.

For the UI a <code>ViewTemplateAttribute</code> with a template name is required. Usually, the template name is the class name. The tracking framework uses the template name to search for the corresponding <code>DataTemplate</code> key within the assembly. A data binding between a processor's properties and its <code>DataTemplate</code> is possible. Adding properties to a processor class is explained below.

    /// <summary>
    /// 
    /// </summary>
    [XmlType]
    [ViewTemplate("MyProcessor")]
    public class MyProcessor : RgbProcessor
    {
		public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
        	// manipulation of image goes here

        	return image;
        }
    }

__IMPORTANT:__ In order to allow serialization (e.g., save or load a pipeline):

1.The new processor class needs a <code>XmlTypeAttribute</code>.

2.Include the new processor class in the abstract base class <code>RgbProcessor</code> using the <code>XmlIncludeAttribute</code>.

    [XmlInclude(typeof(Basics))]
    [XmlInclude(typeof(BlobTracker))]
    [XmlInclude(typeof(CannyEdges))]
    [XmlInclude(typeof(MyProcessor))]
    public abstract class RgbProcessor : GenericProcessor<Rgb, byte>
    {
    }

## Processor Properties - Add Properties to a Processor

A property is a standard .NET property. If it needs to be serialized, the property requires a <code>XmlAttributeAttribute</code>. Please consult the [MSDN Xml serialization website](http://msdn.microsoft.com/en-us/library/System.Xml(v=vs.110).aspx) for further information. To enable data binding, a property needs to raise property change events (see <code>INotifyPropertyChanged</code>).

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

## Processor User Interface (UI) - Create UI for a Processor

Create a new <code>ResourceDictionary</code> or use an existing dictionary and add a new <code>DataTemplate</code> to the dictionary. A processor has a <code>PreProcessImage</code> and a <code>PostProcessImage</code> property by default. Each can be bound to an <code>Image</code> component's source.

Additional properties like the <code>FlipHorizontal</code> property can be bound in order to make them accessible and manipulable in the UI.

    <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	                    xmlns:util="clr-namespace:Tools.FlockingDevice.Tracking.Util"
	                    xmlns:processor="clr-namespace:Tools.FlockingDevice.Tracking.Processor">
	    <DataTemplate x:Key="MyProcessor" DataType="processor:MyProcessor">
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