﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IGlobalState
    {
        string ConfigPath { get; init; }
        bool DryRun { get; init; }
        string JobName { get; init; }
        bool Once { get; init; }
    }

    public class GlobalState : IGlobalState
    {
        public string ConfigPath { get; init; } = string.Empty;
        public string JobName { get; init; } = string.Empty;
        public bool DryRun { get; init; }
        public bool Once { get; init; }
    }
}