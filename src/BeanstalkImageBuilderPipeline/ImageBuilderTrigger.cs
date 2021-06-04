// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Amazon.Imagebuilder;
    using Amazon.Imagebuilder.Model;
    using Amazon.Lambda.CloudWatchEvents;
    using Amazon.Lambda.Core;
    using Amazon.SimpleSystemsManagement;
    using BeanstalkImageBuilderPipeline.Events;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public sealed class ImageBuilderTrigger : LambdaFunction {
        /// <summary>
        /// Constructor used by Lambda at runtime.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public ImageBuilderTrigger() { }

        public ImageBuilderTrigger(IServiceProvider serviceProvider) : base(serviceProvider) { }

        [ExcludeFromCodeCoverage]
        protected override void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services) {
            services.AddScoped<IImageBuilderRepository, ImageBuilderRepository>();
            services.AddScoped<ISsmRepository, SsmRepository>();
            services.AddAWSService<IAmazonSimpleSystemsManagement>();
            services.AddAWSService<IAmazonImagebuilder>();
        }

        public async Task Handler(CloudWatchEvent<ParameterStoreChangeDetail> cloudWatchEvent, ILambdaContext context) {
            var logger = ServiceProvider.GetRequiredService<ILogger<ImageBuilderTrigger>>();

            using (logger.BeginScope(new Dictionary<string, string> {["AwsRequestId"] = context.AwsRequestId})) {
                try {
                    if (!string.IsNullOrEmpty(cloudWatchEvent.Detail.Exception))
                    {
                        logger.LogInformation("Event was for an exception during SSM Parameter operation {ParameterName}. Skipping Image Builder version bump.", cloudWatchEvent.Detail.Name);
                        return;
                    }

                    if (!string.Equals(cloudWatchEvent.Detail.Operation, "update", StringComparison.InvariantCultureIgnoreCase) &&
                        !string.Equals(cloudWatchEvent.Detail.Operation, "create", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.LogInformation("Event was {EventOperation} and not for an update/create of SSM Parameter operation {ParameterName}. Skipping Image Builder version bump.", cloudWatchEvent.Detail.Operation, cloudWatchEvent.Detail.Name);
                        return;
                    }

                    var ssmRepository = ServiceProvider.GetRequiredService<ISsmRepository>();

                    string newAmiId = await ssmRepository.GetParameterValueAsync(cloudWatchEvent.Detail.Name);

                    if (string.IsNullOrEmpty(newAmiId))
                    {
                        logger.LogWarning("Could not find SSM Parameter named {ParameterName}. Skipping Image Builder version bump.", cloudWatchEvent.Detail.Name);
                        return;
                    }

                    var imagePipelineArn = Environment.GetEnvironmentVariable("IMAGE_PIPELINE_ARN");
                    var imageBuilderRepository = ServiceProvider.GetRequiredService<IImageBuilderRepository>();

                    await UpdateImagePipelineForNewAmiAsync(imagePipelineArn, newAmiId, imageBuilderRepository, logger);

                    logger.LogInformation("Starting image pipeline execution for {ImagePipelineArn}.", imagePipelineArn);
                    await imageBuilderRepository.StartImagePipelineExecutionAsync(imagePipelineArn);
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Unhandled Exception During Lambda Invocation");

                   throw;
                }
            }
        }

        private async Task UpdateImagePipelineForNewAmiAsync(string imagePipelineArn, string newAmiId, IImageBuilderRepository imageBuilderRepository, ILogger logger) {
            ImagePipeline imagePipeline = await imageBuilderRepository.GetPipelineByIdAsync(imagePipelineArn);
            ImageRecipe imageRecipe = await imageBuilderRepository.GetRecipeByIdAsync(imagePipeline.ImageRecipeArn);

            string currentVersion = imageRecipe.Version;
            Version version = Version.Parse(currentVersion);

            imageRecipe.Version = $"{version.Major}.{version.Minor}.{version.Build + 1}";
            imageRecipe.ParentImage = newAmiId;

            logger.LogInformation("Incrementing Recipe {RecipeArn} from {CurrentVersion} to {NewVersion}, using AMI {NewAmiId}", imageRecipe.Arn, currentVersion, imageRecipe.Version, newAmiId);

            var newImageRecipe = await imageBuilderRepository.CreateImageRecipeAsync(imageRecipe);

            imagePipeline.ImageRecipeArn = newImageRecipe.Arn;

            logger.LogInformation("Updating Image Pipeline to use Recipe {NewRecipeArn}", newImageRecipe.Arn);

            await imageBuilderRepository.UpdateImagePipelineAsync(imagePipeline);
        }
    }
}
