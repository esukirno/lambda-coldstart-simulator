using Amazon.CDK;

namespace Deploy
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var lambdaStack = new LambdaStack(app, "LambdaStack");
            var coldStartSimulatorStack = new ColdStartSimulatorStack(app, "ColdStartSimulatorStack");

            app.Synth();
        }
    }
}