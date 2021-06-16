// This sample, non-production-ready project that provides provides the ability to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.UnitTests {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.Imagebuilder.Model;
    using Amazon.Lambda.CloudWatchEvents;
    using Amazon.Lambda.Core;
    using BeanstalkImageBuilderPipeline.Events;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public sealed class ImageBuilderTriggerFixture {
        private ImageBuilderTrigger _imageBuilderTrigger;
        private Mock<ILogger<ImageBuilderTrigger>> _mockLogger;
        private Mock<IImageBuilderRepository> _mockImageBuilderRepo;
        private Mock<ISsmRepository> _mockSsmRepo;
        private Mock<ILambdaContext> _mockLambdaContext;

        [TestInitialize]
        public void TestSetup() {
            var services = new ServiceCollection();
            _mockLogger = new Mock<ILogger<ImageBuilderTrigger>>();
            _mockImageBuilderRepo = new Mock<IImageBuilderRepository>();
            _mockSsmRepo = new Mock<ISsmRepository>();
            _mockLambdaContext = new Mock<ILambdaContext>();

            services.AddScoped(_ => _mockLogger.Object);
            services.AddScoped(_ => _mockImageBuilderRepo.Object);
            services.AddScoped(_ => _mockSsmRepo.Object);

            _imageBuilderTrigger = new ImageBuilderTrigger(services.BuildServiceProvider());
        }

        [TestMethod]
        public async Task GivenRequest_WhenLambdaIsInvoked_ThenLoggingScopeContainsAwsRequestId() {
            await _imageBuilderTrigger.Handler(CreateEvent("Update"), _mockLambdaContext.Object);

            _mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(d => d["AwsRequestId"] == _mockLambdaContext.Object.AwsRequestId)));
        }

        [TestMethod]
        public async Task GivenUnhandledExceptionOccurs_WhenLambdaIsInvoked_ThenExceptionIsBubbledUp() {
            _mockSsmRepo.Setup(r => r.GetParameterValueAsync(It.IsAny<string>()))
                        .Throws<InvalidOperationException>();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _imageBuilderTrigger.Handler(CreateEvent("Update"), _mockLambdaContext.Object));
        }

        [TestMethod]
        public async Task GivenCloudWatchEventContainsException_WhenLambdaIsInvoked_ThenNothingHappens() {
            var eventWithError = new CloudWatchEvent<ParameterStoreChangeDetail> {
                Detail = new ParameterStoreChangeDetail {
                    Exception = "Failed"
                }
            };

            await _imageBuilderTrigger.Handler(eventWithError, _mockLambdaContext.Object);

            _mockLogger.VerifyMessageLogged(LogLevel.Information, "Event was for an exception during SSM Parameter operation");
            _mockSsmRepo.Verify(r => r.GetParameterValueAsync(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public async Task GivenCloudWatchEventIsNotForUpdateOrCreate_WhenLambdaIsInvoked_ThenNothingHappens() {
            var newAmiId = "ami=";
            _mockSsmRepo.Setup(r => r.GetParameterValueAsync(It.IsAny<string>()))
                        .ReturnsAsync(newAmiId);

            await _imageBuilderTrigger.Handler(CreateEvent("Delete"), _mockLambdaContext.Object);

            _mockLogger.VerifyMessageLogged(LogLevel.Information, "not for an update/create of SSM Parameter operation");
            _mockSsmRepo.Verify(r => r.GetParameterValueAsync(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public async Task GivenNewAmiIdIsNotFound_WhenLambdaIsInvoked_ThenNothingHappens() {
            _mockSsmRepo.Setup(r => r.GetParameterValueAsync(It.IsAny<string>()))
                        .ReturnsAsync(string.Empty);

            await _imageBuilderTrigger.Handler(CreateEvent("Update"), _mockLambdaContext.Object);

            _mockLogger.VerifyMessageLogged(LogLevel.Warning, "Could not find SSM Parameter named");
            _mockImageBuilderRepo.Verify(r => r.GetPipelineByIdAsync(It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public async Task GivenNewAmiIdIsFound_WhenLambdaIsInvoked_ThenNewRecipeIsCreatedAndPipelineIsStarted() {
            var newAmiId = "ami=";
            var currentRecipe = new ImageRecipe {
                Version = "1.0.0",
                Arn = "arn:aws:1.0"
            };
            var newRecipe = new ImageRecipe {
                Arn = "arn:aws:1.0.1"
            };
            _mockImageBuilderRepo.Setup(r => r.GetPipelineByIdAsync(It.IsAny<string>()))
                                 .ReturnsAsync(new ImagePipeline());
            _mockImageBuilderRepo.Setup(r => r.GetRecipeByIdAsync(It.IsAny<string>()))
                                 .ReturnsAsync(currentRecipe);
            _mockImageBuilderRepo.Setup(r => r.CreateImageRecipeAsync(currentRecipe))
                                 .ReturnsAsync(newRecipe);
            _mockSsmRepo.Setup(r => r.GetParameterValueAsync(null))
                        .ReturnsAsync(newAmiId);

            await _imageBuilderTrigger.Handler(CreateEvent("Update"), _mockLambdaContext.Object);

            _mockImageBuilderRepo.Verify(r => r.CreateImageRecipeAsync(It.Is<ImageRecipe>(i => i.Version == "1.0.1" &&
                                                                                               i.ParentImage == newAmiId)),
                                         "Image Recipe must be created with updated version and AMI ID.");
            _mockImageBuilderRepo.Verify(r => r.UpdateImagePipelineAsync(It.Is<ImagePipeline>(p => p.ImageRecipeArn == newRecipe.Arn)),
                                         "Image Pipeline must be updated with new recipe version's ARN.");
            _mockImageBuilderRepo.Verify(r => r.StartImagePipelineExecutionAsync(It.IsAny<string>()), "Pipeline must be started if AMI ID is updated.");
        }

        private CloudWatchEvent<ParameterStoreChangeDetail> CreateEvent(string operation) {
            return new() {
                Detail = new ParameterStoreChangeDetail {
                    Operation = operation
                }
            };
        }
    }
}
