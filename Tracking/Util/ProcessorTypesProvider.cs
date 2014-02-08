using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Util
{
    static class ProcessorTypesProvider
    {
        public static IEnumerable<Type> GetProcessorTypes<T>()
            where T : BaseProcessor
        {
            return from t in Assembly.GetExecutingAssembly().GetTypes()
                   where typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                   select t;
        }
    }
}
