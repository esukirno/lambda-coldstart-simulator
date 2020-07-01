# Welcome to your CDK C# project!

This is a blank project for C# development with CDK.

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `dotnet build src` compile this app
* `cdk deploy`       deploy this stack to your default AWS account/region
* `cdk diff`         compare deployed stack with current state
* `cdk synth`        emits the synthesized CloudFormation template


yum update
yum install -y wget tar gzip


wget -O dotnet-sdk-3.1.301-linux-x64.tar.gz https://download.visualstudio.microsoft.com/download/pr/8db2b522-7fa2-4903-97ec-d6d04d297a01/f467006b9098c2de256e40d2e2f36fea/dotnet-sdk-3.1.301-linux-x64.tar.gz
mkdir -p "$HOME/dotnet" && tar zxf dotnet-sdk-3.1.301-linux-x64.tar.gz -C "$HOME/dotnet"
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true



dotnet publish -r linux-x64 --self-contained

references

https://docs.microsoft.com/en-us/dotnet/core/install/linux-centos#manual-install

https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0
https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-1

https://github.com/dotnet/core/blob/master/Documentation/build-and-install-rhel6-prerequisites.md
https://docs.microsoft.com/en-us/dotnet/core/rid-catalog