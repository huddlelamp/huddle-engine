using System.Drawing;

namespace Huddle.Engine.Processor
{
    public interface ISnapshoter
    {
        Bitmap[] TakeSnapshots();
    }
}
