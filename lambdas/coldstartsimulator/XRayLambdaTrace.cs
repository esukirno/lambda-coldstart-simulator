using System.Collections.Generic;

namespace ColdStartSimulator
{
    public class XRayLambdaTrace
    {
        public class Aws
        {
            public string account_id { get; set; }
            public string function_arn { get; set; }
            public List<string> resource_names { get; set; }
        }

        public class Aws2
        {
            public string function_arn { get; set; }
        }

        public class Subsegment
        {
            public string id { get; set; }
            public string name { get; set; }
            public double start_time { get; set; }
            public double end_time { get; set; }
            public Aws2 aws { get; set; }
        }

        public class Document
        {
            public string id { get; set; }
            public string name { get; set; }
            public double start_time { get; set; }
            public string trace_id { get; set; }
            public double end_time { get; set; }
            public string parent_id { get; set; }
            public Aws aws { get; set; }
            public string origin { get; set; }
            public List<Subsegment> subsegments { get; set; }
        }
    }
}