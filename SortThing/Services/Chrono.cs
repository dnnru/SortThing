﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IChrono
    {
        DateTimeOffset Now { get; }
    }

    public class Chrono : IChrono
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
