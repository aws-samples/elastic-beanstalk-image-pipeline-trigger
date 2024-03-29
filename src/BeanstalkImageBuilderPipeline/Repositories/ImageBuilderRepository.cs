﻿// This sample, non-production-ready project that demonstrates how to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Amazon.Imagebuilder;
    using Amazon.Imagebuilder.Model;

    [ExcludeFromCodeCoverage/* No real logic other than data mapping so skipping unit tests for now. */]
    public sealed class ImageBuilderRepository : IImageBuilderRepository {
        private readonly IAmazonImagebuilder _imageBuilderClient;

        public ImageBuilderRepository(IAmazonImagebuilder imageBuilderClient) {
            _imageBuilderClient = imageBuilderClient;
        }

        public async Task<ImageRecipe> GetRecipeByIdAsync(string imageRecipeArn) {
            GetImageRecipeResponse getRecipeResponse = await _imageBuilderClient.GetImageRecipeAsync(new GetImageRecipeRequest {ImageRecipeArn = imageRecipeArn});

            return getRecipeResponse.ImageRecipe;
        }

        public async Task<ImagePipeline> GetPipelineByIdAsync(string imagePipelineArn) {
            GetImagePipelineResponse getPipelineResponse = await _imageBuilderClient.GetImagePipelineAsync(new GetImagePipelineRequest {ImagePipelineArn = imagePipelineArn});

            return getPipelineResponse.ImagePipeline;
        }

        public async Task<ImageRecipe> CreateImageRecipeAsync(ImageRecipe imageRecipe) {
            CreateImageRecipeResponse response = await _imageBuilderClient.CreateImageRecipeAsync(new() {
                Name = imageRecipe.Name,
                ParentImage = imageRecipe.ParentImage,
                Tags = imageRecipe.Tags,
                BlockDeviceMappings = imageRecipe.BlockDeviceMappings,
                Components = imageRecipe.Components,
                Description = imageRecipe.Description,
                SemanticVersion = imageRecipe.Version,
                WorkingDirectory = imageRecipe.WorkingDirectory
            });

            imageRecipe.Arn = response.ImageRecipeArn;

            return imageRecipe;
        }

        public async Task StartImagePipelineExecutionAsync(string imagePipelineArn) {
            await _imageBuilderClient.StartImagePipelineExecutionAsync(new StartImagePipelineExecutionRequest {
                ImagePipelineArn = imagePipelineArn
            });
        }

        public async Task UpdateImagePipelineAsync(ImagePipeline imagePipeline) {
            await _imageBuilderClient.UpdateImagePipelineAsync(new UpdateImagePipelineRequest {
                ImagePipelineArn = imagePipeline.Arn,
                Description = imagePipeline.Description,
                DistributionConfigurationArn = imagePipeline.DistributionConfigurationArn,
                ImageRecipeArn = imagePipeline.ImageRecipeArn,
                ContainerRecipeArn = imagePipeline.ContainerRecipeArn,
                EnhancedImageMetadataEnabled = imagePipeline.EnhancedImageMetadataEnabled,
                ImageTestsConfiguration = imagePipeline.ImageTestsConfiguration,
                InfrastructureConfigurationArn = imagePipeline.InfrastructureConfigurationArn,
                Schedule = imagePipeline.Schedule,
                Status = imagePipeline.Status
            });
        }
    }
}
