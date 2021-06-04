// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories {
    using System.Threading.Tasks;
    using Amazon.Imagebuilder.Model;

    public interface IImageBuilderRepository {
        Task<ImageRecipe> GetRecipeByIdAsync(string imageRecipeArn);

        Task<ImagePipeline> GetPipelineByIdAsync(string imagePipelineArn);

        Task<ImageRecipe> CreateImageRecipeAsync(ImageRecipe imageRecipe);

        Task StartImagePipelineExecutionAsync(string imagePipelineArn);

        Task UpdateImagePipelineAsync(ImagePipeline imagePipeline);
    }
}
