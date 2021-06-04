// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
