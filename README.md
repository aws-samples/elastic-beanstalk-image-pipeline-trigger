# Elastic Beanstalk EC2 Image Builder Pipeline for Windows Automation

Repository for project whose aim is to provide the ability for customers to detect when an Elastic Beanstalk platform's AMI is updated and kickoff an EC2 Image Builder Pipeline to automate the creation of a golden image.

## License
This library is licensed under the MIT-0 License. See the [LICENSE](LICENSE.TXT) file.

Additionally, this project installs the following software for the purposes of deploying and running the labs into the lab environment:

* [Microsoft .NET](https://github.com/dotnet/runtime) is an open source developer platform, created by Microsoft is provided under the MIT License.
* [Serilog](https://github.com/serilog/serilog) is an open source software diagnostic logging library for .NET applications provided under the Apache 2 License.
* [Moq](https://github.com/moq/moq4) is an open source software mocking library for .NET provided under the BSD 3-Clause License.

## Table of Contents
- [Elastic Beanstalk EC2 Image Builder Pipeline for Windows Automation](#elastic-beanstalk-ec2-image-builder-pipeline-for-windows-automation)
  - [License](#license)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Project Structure](#project-structure)
  - [Running the Code](#running-the-code)
    - [Environment Variables](#environment-variables)
      - [AmiMonitor](#amimonitor)
      - [ImageBuilderTrigger](#imagebuildertrigger)
  - [Design](#design)
    - [Static Relationships](#static-relationships)
    - [Dynamic Behavior](#dynamic-behavior)
      - [Lambda Function Initialization](#lambda-function-initialization)
        - [Logging](#logging)
        - [Dependency Injection](#dependency-injection)
      - [AMI Version Change Monitoring](#ami-version-change-monitoring)
      - [EC2 Image Builder Pipeline Execution](#ec2-image-builder-pipeline-execution)
  - [Security](#security)

## Overview

This project provides a sample solution to automate the execution of an EC2 Image Builder Pipeline that uses an Amazon Elastic Beanstalk managed AMI as the source AMI. There are currently two parts to the solution, each a Lambda function:

* **AmiMonitor** - Determine when the Amazon Elastic Beanstalk managed AMI has been updated for a platform
* **ImageBuilderTrigger** - Start the execution of an Amazon EC2 Image Builder pipeline when Elastic Beanstalk managed AMI is updated

Both these Lambda functions are contained in the same .NET assembly.

## Project Structure

This project follows the [src project structure](https://docs.microsoft.com/en-us/dotnet/core/porting/project-structure). In other words, this:
```
├─ src
│  └─ Project
│     └─ ...
├─ tests
│  ├─ Project.UnitTests
│  └─ ...
```
## Running the Code

### Environment Variables

#### AmiMonitor
* **PLATFORM_ARN** - The ARN of the Elastic Beanstalk platform version to monitor for changes.
* **SSM_PARAMETER_NAME** - Name of the Systems Manager Parameter Store parameter used to store the latest AMI ID for the Beanstalk Platform version being monitored.

#### ImageBuilderTrigger
* **IMAGE_PIPELINE_ARN** - The ARN of the EC2 Image Builder Pipeline that needs to be executed when a new version of the Elastic Beanstalk platform AMI is detected.
## Design
![AMI ID Version Update Conceptual Design](./docs/ami-version-change-monitoring-conceptual.png)

The solution has been split into to components. The first is one that monitors the selected Elastic Beanstalk platform for changes to the AMI associated with it and ensure that a Systems Manager Parameter Store parameter is kept up to date.

![Image Builder Pipeline Conceptual Design](./docs/start-new-image-builder-pipeline-execution-conceptual.png)

The next component picks up where that Systems Manager Parameter Store parameter left off and will update the associated EC2 Image Builder Pipeline's recipe with the updated AMI ID, and will trigger the execution of the pipeline so that a new AMI is created.

### Static Relationships
![Image Builder Pipeline Conceptual Design](./docs/class-diagram.png)

The base class for all Lambda functions is named LambdaFunction and provides functionality such as lazy initialization of an IServiceProvider that can be used for Dependency Injection, as well as the ConfigureServices template method used to allow subclasses to register their own dependencies.

The two methods depicted in the class diagram are key to how the Lambda functions are initialized. The following section covers how these types interact with each other.

### Dynamic Behavior
#### Lambda Function Initialization
![Service Provider Initialization](./docs/service-provider-initialization.png)

At the start of the Lambda execution environment, the LambdaFunction class takes care of initializing the IServiceProvider instance that used to provide Dependency Injection to the Lambda function.

The LambdaFunction class does this by calling the GetServiceProvider method which in turn creates an instance of [.NET's Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host). The Generic Host is used because it configures:

* Logging
* Dependency Injection
* Configuration

##### Logging
[Serilog](https://serilog.net) is used to provide Structured Logging, and include the [AWS Request Id](https://docs.aws.amazon.com/lambda/latest/dg/csharp-context.html) in each log entry.

##### Dependency Injection
Once the Generic Host has initialized the Service Collection, the ConfigureServices [template method](https://en.wikipedia.org/wiki/Template_method_pattern) is called so that LambdaFunction's subclasses may register their dependencies.

#### AMI Version Change Monitoring
![AMI ID Version Update Sequence Diagram](./docs/ami-version-change-monitoring.png)

The AmiMonitor Lambda function is executed on a schedule. AmiMonitor retrieves the latest AMI version for the Beanstalk platform specified in the PLATFORM_ARN [environment variable](#environment-variables) using the [DescribePlatformVersion](https://docs.aws.amazon.com/elasticbeanstalk/latest/api/API_DescribePlatformVersion.html) API call.

If the AMI ID returned is different than the value of the Systems Manager Parameter Store parameter specified in the SSM_PARAMETER_NAME [environment variable](#environment-variables), the parameter is updated.

This parameter update kicks off the second half of the process (i.e. starting the execution of the EC2 Image Builder Pipeline).

#### EC2 Image Builder Pipeline Execution
![Image Builder Pipeline Sequence Diagram](./docs/start-new-image-builder-pipeline-execution.png)

## Security
See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

