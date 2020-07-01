using Amazon.Lambda.Core;
using CsvHelper.Configuration;

namespace ColdStartSimulator
{
    public class LambdaMetricMap : ClassMap<LambdaMetric>
    {
        public LambdaMetricMap()
        {
            Map(m => m.FunctionName).Index(0).Name("FunctionName");
            Map(m => m.TraceId).Index(0).Name("TraceId");
            Map(m => m.TraceType).Index(0).Name("TraceType");
            Map(m => m.MetricName).Index(0).Name("MetricName");
            Map(m => m.StartTime).Index(0).Name("StartTime");
            Map(m => m.EndTime).Index(0).Name("EndTime");
            Map(m => m.Duration).Index(0).Name("Duration");
        }
    }
}