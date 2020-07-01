using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;

namespace Deploy
{
    public class ColdStartSimulatorStack : Stack
    {
        internal ColdStartSimulatorStack(Construct scope, string id, StackProps props = null) : base(scope, id, props)
        {
            var coldStartSimulatorLambdaPolicy = new Amazon.CDK.AWS.IAM.PolicyStatement
            {
                Effect = Amazon.CDK.AWS.IAM.Effect.ALLOW
            };
            coldStartSimulatorLambdaPolicy.AddResources("*");
            coldStartSimulatorLambdaPolicy.AddActions("lambda:*");
            coldStartSimulatorLambdaPolicy.AddActions("xray:*");

            var coldStartSimulatorSetupFunction = new Function(this, "coldstartsimulator-setup", new FunctionProps
            {
                FunctionName = "coldstartsimulator-setup",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/coldstartsimulator/bin/Release/netcoreapp3.1/publish"),
                Handler = "ColdStartSimulator::ColdStartSimulator.StepFunctionTasks::Setup",
                Timeout = Duration.Seconds(31),
                MemorySize = 512
            });

            var coldStartSimulatorInvokeLambdaFunction = new Function(this, "coldstartsimulator-invokelambda", new FunctionProps
            {
                FunctionName = "coldstartsimulator-invokelambda",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/coldstartsimulator/bin/Release/netcoreapp3.1/publish"),
                Handler = "ColdStartSimulator::ColdStartSimulator.StepFunctionTasks::InvokeLambda",
                Timeout = Duration.Seconds(31),
                MemorySize = 512
            });
            coldStartSimulatorInvokeLambdaFunction.AddToRolePolicy(coldStartSimulatorLambdaPolicy);

            var coldStartSimulatorTouchLambdaFunction = new Function(this, "coldstartsimulator-touchlambda", new FunctionProps
            {
                FunctionName = "coldstartsimulator-touchlambda",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/coldstartsimulator/bin/Release/netcoreapp3.1/publish"),
                Handler = "ColdStartSimulator::ColdStartSimulator.StepFunctionTasks::TouchLambda",
                Timeout = Duration.Seconds(31),
                MemorySize = 512
            });
            coldStartSimulatorTouchLambdaFunction.AddToRolePolicy(coldStartSimulatorLambdaPolicy);

            var metricS3Bucket = new Bucket(this, "metric-bucket", new BucketProps
            {
                BucketName = "coldstart-metric-bucket",
            });

            var coldStartSimulatorCollectMetricsFunction = new Function(this, "coldstartsimulator-collectmetrics", new FunctionProps
            {
                FunctionName = "coldstartsimulator-collectmetrics",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/coldstartsimulator/bin/Release/netcoreapp3.1/publish"),
                Handler = "ColdStartSimulator::ColdStartSimulator.StepFunctionTasks::CollectMetrics",
                Timeout = Duration.Seconds(31),
                MemorySize = 512
            });
            metricS3Bucket.GrantPut(coldStartSimulatorCollectMetricsFunction);
            coldStartSimulatorCollectMetricsFunction.AddEnvironment("MetricS3BucketName", metricS3Bucket.BucketName);
            coldStartSimulatorCollectMetricsFunction.AddToRolePolicy(coldStartSimulatorLambdaPolicy);

            var setup = new LambdaInvoke(this, "Setup", new LambdaInvokeProps
            {
                LambdaFunction = coldStartSimulatorSetupFunction,
                OutputPath = "$.Payload"
            });

            var touch = new LambdaInvoke(this, "Touch", new LambdaInvokeProps
            {
                LambdaFunction = coldStartSimulatorTouchLambdaFunction,
                OutputPath = "$.Payload"
            });

            var invoke = new LambdaInvoke(this, "Invoke", new LambdaInvokeProps
            {
                LambdaFunction = coldStartSimulatorInvokeLambdaFunction,
                OutputPath = "$.Payload"
            });

            var collectMetrics = new LambdaInvoke(this, "Collect Metrics", new LambdaInvokeProps
            {
                LambdaFunction = coldStartSimulatorCollectMetricsFunction,
                OutputPath = "$.Payload"
            });

            var wait3Seconds = new Wait(this, "Wait 3 seconds", new WaitProps
            {
                Time = WaitTime.Duration(Duration.Seconds(3))
            });

            var wait30Seconds = new Wait(this, "Wait 30 seconds", new WaitProps
            {
                Time = WaitTime.Duration(Duration.Seconds(30))
            });

            var invokeAgainChoice = new Choice(this, "Invoke again?");
            invokeAgainChoice.When(Condition.BooleanEquals("$.Continue", true), touch);
            invokeAgainChoice.Otherwise(wait30Seconds);

            wait30Seconds
                .Next(collectMetrics);

            var definition = setup
                .Next(touch)
                .Next(wait3Seconds)
                .Next(invoke)
                .Next(invokeAgainChoice);

            new StateMachine(this, "ColdStartSimulatorStateMachine", new StateMachineProps
            {
                Definition = definition,
                Timeout = Duration.Minutes(10)
            });
        }
    }
}