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

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ColdStartSimulator
{
    public class StepFunctionTasks
    {
        private const int DefaultCount = 2;
        private const int XRayBatchSize = 5;

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
            var client = new AmazonLambdaClient(RegionEndpoint.APSoutheast2);
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
            var client = new AmazonLambdaClient(RegionEndpoint.APSoutheast2);
            var invokeRequest = new InvokeRequest
            {
                FunctionName = state.FunctionName
            };

            var response = await client.InvokeAsync(invokeRequest);

            state.Index += state.Step;
            state.Continue = state.Index < state.Count;

            return state;
        }

        public async Task<State> CollectMetrics(State state, ILambdaContext context)
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
            var traces = new List<Trace>();

            foreach (var batchIds in Batch(traceIds, XRayBatchSize))
            {
                var request = new BatchGetTracesRequest
                {
                    TraceIds = batchIds.ToList(),
                };
                var response = await client.BatchGetTracesAsync(request);
                traces.AddRange(response.Traces);
            }

            var metrics = new List<LambdaMetric>();
            foreach (var trace in traces)
            {
                foreach (var segment in trace.Segments)
                {
                    var document = JsonConvert.DeserializeObject<XRayLambdaTrace.Document>(segment.Document);
                    Console.WriteLine(segment.Document);
                    if (document.name == state.FunctionName)
                    {
                        if (document.origin == "AWS::Lambda::Function")
                        {
                            metrics.Add(CreateLambdaMetric(document, document.trace_id, document.name, document.origin, "Total"));
                            foreach (var subSegment in document.subsegments)
                            {
                                metrics.Add(CreateLambdaMetric(subSegment, document.trace_id, document.name, document.origin, null));
                            }
                        }
                        else if (document.origin == "AWS::Lambda")
                        {
                            metrics.Add(CreateLambdaMetric(document, document.trace_id, document.name, document.origin, "Total"));
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
            var now = DateTime.Now;
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = $"{now:yyyy-MM-dd}/{state.FunctionName}-{now:HHmmss}.csv",
                ContentType = "text/csv",
                ContentBody = contentBody
            };

            var s3Client = new AmazonS3Client(RegionEndpoint.APSoutheast2);
            await s3Client.PutObjectAsync(putObjectRequest);

            return state;
        }

        private LambdaMetric CreateLambdaMetric(XRayLambdaTrace.Document document, string traceId, string functionName, string origin, string name)
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

        private LambdaMetric CreateLambdaMetric(XRayLambdaTrace.Subsegment subSegment, string traceId, string functionName, string origin, string name)
        {
            if (name == null)
            {
                name = subSegment.name;
            }

            var duration = subSegment.end_time - subSegment.start_time;
            var metric = new LambdaMetric
            {
                FunctionName = functionName,
                TraceType = origin,
                TraceId = traceId,
                MetricName = name,
                StartTime = subSegment.start_time,
                EndTime = subSegment.end_time,
                Duration = duration
            };
            return metric;
        }

        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(IEnumerable<TSource> source, int batchSize)
        {
            var items = new TSource[batchSize];
            var count = 0;
            foreach (var item in source)
            {
                items[count++] = item;
                if (count == batchSize)
                {
                    yield return items;
                    items = new TSource[batchSize];
                    count = 0;
                }
            }
            if (count > 0)
                yield return items.Take(count);
        }
    }
}