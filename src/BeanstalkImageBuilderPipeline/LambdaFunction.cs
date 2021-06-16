// This sample, non-production-ready project that provides provides the ability to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;

    public abstract class LambdaFunction {

        protected LambdaFunction(IServiceProvider serviceProvider = null)
        {
            ServiceProvider = serviceProvider ?? GetServiceCollection();

            ServiceProvider.GetService<ILogger<LambdaFunction>>()
                           ?.LogDebug( "Environment Variables {@EnvironmentVariables}", Environment.GetEnvironmentVariables());
        }

        protected static IServiceProvider ServiceProvider { get; private set; }

        [ExcludeFromCodeCoverage]
        private IServiceProvider GetServiceCollection() {
            if (ServiceProvider != null)
                return ServiceProvider;

            var host = Host.CreateDefaultBuilder()
                           .UseSerilog((hostingContext, _, loggerConfiguration) => loggerConfiguration.ReadFrom
                                                                                                      .Configuration(hostingContext.Configuration))
                           .ConfigureServices(ConfigureServices)
                           .Build();

            return host.Services;
        }

        protected abstract void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services);
    }
}
