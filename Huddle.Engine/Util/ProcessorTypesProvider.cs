using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Util
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
