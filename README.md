# Cold Start Lambda simulator

## Build and Deploy

### 1. Build Amazon Linux 2 docker image

```
cd build\amazonlinux2-dotnetsdk
docker build -t aws/codebuild/amazonlinux2-x86_64-standard:3.0 .
```

### 2. Build Lambda projects
```
cd build
docker-compose up
```

### 3. Build CDK project
```
dotnet build .\deploy\src
```

### 4. Deploy to AWS
```
cd deploy
cdk deploy *
```

For more information visit 

## References

* [Understanding Lambda csharp tracing](https://docs.aws.amazon.com/lambda/latest/dg/csharp-tracing.html)
* [AWS docker images github repo](https://github.com/aws/aws-codebuild-docker-images)
* [Quicksight manifest file format](https://docs.aws.amazon.com/quicksight/latest/user/supported-manifest-file-format.html)