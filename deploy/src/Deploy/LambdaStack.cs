using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.APIGateway;

namespace Deploy
{
    public class LambdaStack : Stack
    {
        public Function DotNetCore31HelloLambda { get; set; }
        public Function DotNetCore31SCHelloLambda { get; set; }
        public Function DotNetCore31RTRHelloLambda { get; set; }
        public Function DotNetCore21HelloLambda { get; set; }

        internal LambdaStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here
            DotNetCore31HelloLambda = new Function(this, "DotNetCore31HelloLambda", new FunctionProps
            {
                FunctionName = "hello-dotnetcore31",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/hello-dotnetcore31/bin/Release/netcoreapp3.1/publish"),
                Handler = "Hello::Hello.Functions::Get",
                Timeout = Duration.Seconds(10),
                MemorySize = 128,
                Tracing = Tracing.ACTIVE
            });

            DotNetCore31SCHelloLambda = new Function(this, "DotNetCore31SCHelloLambda", new FunctionProps
            {
                FunctionName = "hello-dotnetcore31-sc",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/hello-dotnetcore31-sc/bin/Release/netcoreapp3.1/linux-x64/publish"),
                Handler = "Hello::Hello.Functions::Get",
                Timeout = Duration.Seconds(30),
                MemorySize = 128,
                Tracing = Tracing.ACTIVE
            });

            DotNetCore31RTRHelloLambda = new Function(this, "DotNetCore31RTRHelloLambda", new FunctionProps
            {
                FunctionName = "hello-dotnetcore31-rtr",
                Runtime = Runtime.DOTNET_CORE_3_1,
                Code = Code.FromAsset("../lambdas/hello-dotnetcore31-rtr/bin/Release/netcoreapp3.1/linux-x64/publish"),
                Handler = "Hello::Hello.Functions::Get",
                Timeout = Duration.Seconds(30),
                MemorySize = 128,
                Tracing = Tracing.ACTIVE
            });

            DotNetCore21HelloLambda = new Function(this, "DotNetCore21HelloLambda", new FunctionProps
            {
                FunctionName = "hello-dotnetcore21",
                Runtime = Runtime.DOTNET_CORE_2_1,
                Code = Code.FromAsset("../lambdas/hello-dotnetcore21/bin/Release/netcoreapp2.1/publish"),
                Handler = "Hello::Hello.Functions::Get",
                Timeout = Duration.Seconds(30),
                MemorySize = 128,
                Tracing = Tracing.ACTIVE
            });

            var api = new RestApi(this, "DotNetCoreHelloApi", new LambdaRestApiProps
            {
                RestApiName = "Hello"
            });

            var dotnetcore31 = api.Root.AddResource("31");
            dotnetcore31.AddMethod("GET", new LambdaIntegration(DotNetCore31HelloLambda));

            var dotnetcore31SC = api.Root.AddResource("31-sc");
            dotnetcore31SC.AddMethod("GET", new LambdaIntegration(DotNetCore31SCHelloLambda));

            var dotnetcore31RTR = api.Root.AddResource("31-rtr");
            dotnetcore31RTR.AddMethod("GET", new LambdaIntegration(DotNetCore31RTRHelloLambda));

            var dotnetcore21 = api.Root.AddResource("21");
            dotnetcore21.AddMethod("GET", new LambdaIntegration(DotNetCore21HelloLambda));
        }
    }
}