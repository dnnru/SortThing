#region

using System.Collections.Generic;
using SortThing.Enums;

#endregion

namespace SortThing.Models
{
    public class JobReport
    {
        public string JobName { get; init; } = string.Empty;
        public SortOperation Operation { get; init; }
        public List<OperationResult> Results { get; init; } = new List<OperationResult>();
        public bool DryRun { get; internal set; }
    }
}