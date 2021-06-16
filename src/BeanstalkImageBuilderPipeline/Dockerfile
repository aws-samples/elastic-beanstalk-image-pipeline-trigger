# This sample, non-production-ready project that demonstrates how to detect when an Amazon Elastic Beanstalk 
# platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
# © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
# This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
# http://aws.amazon.com/agreement or other written agreement between Customer and either
# Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
# SPDX-License-Identifier: MIT-0

FROM public.ecr.aws/lambda/dotnet:5.0

WORKDIR /var/task

# This COPY command copies the .NET Lambda project's build artifacts from the host machine into the image. 
# The source of the COPY should match where the .NET Lambda project publishes its build artifacts. If the Lambda function is being built 
# with the AWS .NET Lambda Tooling, the `--docker-host-build-output-dir` switch controls where the .NET Lambda project
# will be built. The .NET Lambda project templates default to having `--docker-host-build-output-dir`
# set in the aws-lambda-tools-defaults.json file to "bin/Release/net5.0/linux-x64/publish".
#
# Alternatively Docker multi-stage build could be used to build the .NET Lambda project inside the image.
# For more information on this approach checkout the project's README.md file.
COPY "bin/Release/net5.0/linux-x64/publish"  .
