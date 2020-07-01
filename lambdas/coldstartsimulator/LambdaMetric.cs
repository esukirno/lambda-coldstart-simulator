using Amazon.Lambda.Core;

namespace ColdStartSimulator
{
    public class LambdaMetric
    {
        public string FunctionName { get; set; }
        public string TraceId { get; set; }
        public string TraceType { get; set; }
        public string MetricName { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration { get; set; }
    }
}