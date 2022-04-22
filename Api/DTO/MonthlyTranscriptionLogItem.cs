using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beey.Api.DTO;

public class MonthlyTranscriptionLogItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Minutes { get; set; }
}
