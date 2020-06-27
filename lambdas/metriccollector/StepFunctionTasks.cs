using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.XRay;
using Amazon.XRay.Model;
using Amazon.S3;
using Amazon.S3.Model;
using CsvHelper;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using CsvHelper.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MetricCollector
{
    public class StepFunctionTasks
    {
        private const int DefaultCount = 2;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public StepFunctionTasks()
        {
        }

        public State Setup(State state, ILambdaContext context)
        {
            if (state.Count <= 0)
            {
                state.Count = DefaultCount;
            }

            state.Index = 0;
            state.Step = 1;
            state.Continue = true;
            state.StartTimeInEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return state;
        }

        public async Task<State> TouchLambda(State state, ILambdaContext context)
        {
            var client = new AmazonLambdaClient(Amazon.RegionEndpoint.APSoutheast2);
            var variables = new Dictionary<string, string>();
            variables["LastTouched"] = DateTime.UtcNow.ToString();

            var updatedEnvironment = new Amazon.Lambda.Model.Environment
            {
                Variables = variables,
                IsVariablesSet = true
            };

            var updateFunctionConfigurationRequest = new UpdateFunctionConfigurationRequest
            {
                FunctionName = state.FunctionName,
                Environment = updatedEnvironment
            };

            var response = await client.UpdateFunctionConfigurationAsync(updateFunctionConfigurationRequest);
            return state;
        }

        public async Task<State> InvokeLambda(State state, ILambdaContext context)
        {
            var client = new AmazonLambdaClient(Amazon.RegionEndpoint.APSoutheast2);
            var invokeRequest = new Amazon.Lambda.Model.InvokeRequest
            {
                FunctionName = state.FunctionName
            };

            var response = await client.InvokeAsync(invokeRequest);

            state.Index += state.Step;
            state.Continue = state.Index < state.Count;

            return state;
        }

        public async Task<State> CollectStats(State state, ILambdaContext context)
        {
            state.EndTimeInEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var client = new AmazonXRayClient(RegionEndpoint.APSoutheast2);
            var getTraceSummariesRequest = new GetTraceSummariesRequest
            {
                StartTime = DateTimeOffset.FromUnixTimeSeconds(state.StartTimeInEpoch).UtcDateTime,
                EndTime = DateTimeOffset.FromUnixTimeSeconds(state.EndTimeInEpoch).UtcDateTime,
                TimeRangeType = TimeRangeType.Event
            };

            var getTraceSummariesResponse = await client.GetTraceSummariesAsync(getTraceSummariesRequest);
            var traceIds = getTraceSummariesResponse.TraceSummaries.Select(x => x.Id);

            var request = new BatchGetTracesRequest
            {
                TraceIds = traceIds.ToList()
            };
            var response = await client.BatchGetTracesAsync(request);

            var metrics = new List<LambdaMetric>();

            foreach (var trace in response.Traces)
            {
                foreach (var segment in trace.Segments)
                {
                    var document = JsonConvert.DeserializeObject<Document>(segment.Document);
                    Console.WriteLine(segment.Document);
                    if (document.name == state.FunctionName)
                    {
                        if (document.origin == "AWS::Lambda::Function")
                        {
                            metrics.Add(CreateLambdaMetric1(document, document.trace_id, document.name, document.origin, "Total"));
                            foreach (var subSegment in document.subsegments)
                            {
                                metrics.Add(CreateLambdaMetric2(subSegment, document.trace_id, document.name, document.origin, null));
                            }
                        }
                        else if (document.origin == "AWS::Lambda")
                        {
                            metrics.Add(CreateLambdaMetric1(document, document.trace_id, document.name, document.origin, "Total"));
                        }
                    }
                }
            }

            var contentBody = string.Empty;
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, true))
            {
                csv.WriteRecords(metrics);
                csv.Flush();
                contentBody = writer.ToString();
            }

            var bucketName = System.Environment.GetEnvironmentVariable("MetricS3BucketName");
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = DateTime.Today.ToString("yyyy-MM-dd") + "/" + DateTime.Now.ToString("hhmmss") + ".csv",
                ContentType = "text/csv",
                ContentBody = contentBody
            };

            var s3Client = new AmazonS3Client(RegionEndpoint.APSoutheast2);
            await s3Client.PutObjectAsync(putObjectRequest);

            return state;
        }

        private LambdaMetric CreateLambdaMetric1(Document document, string traceId, string functionName, string origin, string name)
        {
            if (name == null)
            {
                name = document.name;
            }

            var duration = document.end_time - document.start_time;
            var metric = new LambdaMetric
            {
                FunctionName = functionName,
                TraceType = origin,
                TraceId = traceId,
                MetricName = name,
                StartTime = document.start_time,
                EndTime = document.end_time,
                Duration = duration
            };
            return metric;
        }

        private LambdaMetric CreateLambdaMetric2(Subsegment document, string traceId, string functionName, string origin, string name)
        {
            if (name == null)
            {
                name = document.name;
            }

            var duration = document.end_time - document.start_time;
            var metric = new LambdaMetric
            {
                FunctionName = functionName,
                TraceType = origin,
                TraceId = traceId,
                MetricName = name,
                StartTime = document.start_time,
                EndTime = document.end_time,
                Duration = duration
            };
            return metric;
        }
    }

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