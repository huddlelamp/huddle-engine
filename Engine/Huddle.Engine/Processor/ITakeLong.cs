﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("I Take Long", "ITakeLong")]
    public class ITakeLong : BaseProcessor
    {
        private readonly Random _random = new Random();

        public override IData Process(IData data)
        {
            Thread.Sleep(_random.Next(10, 1000));
            return data;
        }
    }
}
