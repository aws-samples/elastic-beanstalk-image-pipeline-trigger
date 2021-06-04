// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BeanstalkImageBuilderPipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Amazon.Lambda.Core;
    using System.Threading.Tasks;
    using Amazon.ElasticBeanstalk;
    using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
    using Amazon.SimpleSystemsManagement;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public sealed class AmiMonitor : LambdaFunction {
        /// <summary>
        /// Constructor used by Lambda at runtime.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public AmiMonitor() { }

        public AmiMonitor(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [ExcludeFromCodeCoverage]
        protected override void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services) {
            services.AddScoped<IBeanstalkRepository, BeanstalkRepository>();
            services.AddScoped<ISsmRepository, SsmRepository>();
            services.AddAWSService<IAmazonElasticBeanstalk>();
            services.AddAWSService<IAmazonSimpleSystemsManagement>();
        }

        public async Task Handler(ScheduledEvent request, ILambdaContext context) {
            var logger = ServiceProvider.GetRequiredService<ILogger<AmiMonitor>>();

            using (logger.BeginScope(new Dictionary<string, string> { ["AwsRequestId"] = context.AwsRequestId })) {
                try {
                    var beanstalkRepo = ServiceProvider.GetRequiredService<IBeanstalkRepository>();
                    string beanstalkPlatform = Environment.GetEnvironmentVariable("PLATFORM_ARN");

                    string latestAmiId = await beanstalkRepo.GetLatestAmiVersionAsync(beanstalkPlatform);

                    if (string.IsNullOrEmpty(latestAmiId)) {
                        logger.LogError("Unable to retrieve latest AMI ID for Beanstalk platform {BeanstalkPlatformId}. Ensure PLATFORM_ARN environment variable is valid.", beanstalkPlatform);

                        return;
                    }

                    var ssmRepo = ServiceProvider.GetRequiredService<ISsmRepository>();

                    await ssmRepo.UpdateParameterAsync(Environment.GetEnvironmentVariable("SSM_PARAMETER_NAME"), latestAmiId);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Unhandled Exception During Handler Execution.");

                    throw;
                }
            }
        }
    }
}
