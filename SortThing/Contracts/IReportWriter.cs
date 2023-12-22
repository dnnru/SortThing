#region

using System.Collections.Generic;
using System.Threading.Tasks;
using SortThing.Models;

#endregion

namespace SortThing.Contracts;

public interface IReportWriter
{
    Task<string> WriteReport(JobReport report);

    Task<string> WriteReports(IEnumerable<JobReport> reports);
}