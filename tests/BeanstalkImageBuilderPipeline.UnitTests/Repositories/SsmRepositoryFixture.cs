// This sample, non-production-ready project that demonstrates how to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.UnitTests.Repositories {
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public sealed class SsmRepositoryFixture {
        private SsmRepository _repository;
        private Mock<ILogger<SsmRepository>> _mockLogger;
        private Mock<IAmazonSimpleSystemsManagement> _mockSsmClient;

        [TestInitialize]
        public void TestSetup() {
            _mockLogger = new Mock<ILogger<SsmRepository>>();
            _mockSsmClient = new Mock<IAmazonSimpleSystemsManagement>();

            _repository = new SsmRepository(_mockSsmClient.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GivenExceptionOccurs_WhenGetParameterIsCalled_ThenNullIsReturned() {
            _mockSsmClient.Setup(m => m.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new ParameterNotFoundException("Not found"));

            string paramValue = await _repository.GetParameterValueAsync("foo");

            Assert.IsNull(paramValue, "If AWS SDK throws ParameterNotFoundException, it should be treated as null return value");
        }

        [TestMethod]
        public async Task GivenOldAndNewParamValuesAreTheSame_WhenUpdateParameterIsCalled_ThenParameterIsNotUpdated() {
            Parameter currentParameter = new Parameter {
                Name = "Current Param",
                Value = "Test"
            };
            _mockSsmClient.Setup(m => m.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new GetParameterResponse { Parameter = currentParameter });

            await _repository.UpdateParameterAsync(currentParameter.Name, currentParameter.Value);

            _mockSsmClient.Verify(m => m.PutParameterAsync(It.IsAny<PutParameterRequest>(), It.IsAny<CancellationToken>()), 
                                  Times.Never(), 
                                  "Parameter Store should not be updated if current parameter's value in Parameter Store is the same as the proposed new value");
        }

        [TestMethod]
        public async Task GivenNewParamValueIsDifferentThanExisting_WhenUpdateParameterIsCalled_ThenParameterIsUpdated() {
            Parameter currentParameter = new Parameter {
                Name = "Current Param",
                Value = "Test"
            };
            _mockSsmClient.Setup(m => m.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new GetParameterResponse {Parameter = currentParameter });
            string newValue = "New Value";

            await _repository.UpdateParameterAsync(currentParameter.Name, newValue);

            _mockSsmClient.Verify(m => m.PutParameterAsync(It.Is<PutParameterRequest>(r => r.Value == newValue), It.IsAny<CancellationToken>()),
                                  "Parameter Store should be updated new value");
        }

        [TestMethod]
        public async Task GivenParameterDoesNotExistInParameterStore_WhenUpdateParameterIsCalled_ThenParameterIsCreated() {
            string newValue = "New Value";
            string paramName = "New Parameter";
            _mockSsmClient.Setup(m => m.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new ParameterNotFoundException("Not found"));

            await _repository.UpdateParameterAsync(paramName, newValue);

            _mockSsmClient.Verify(m => m.PutParameterAsync(It.Is<PutParameterRequest>(r => r.Name == paramName && r.Value == newValue), It.IsAny<CancellationToken>()),
                                  "New parameter should be created if one does not exist");
        }
    }
}
