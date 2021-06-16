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
    using Amazon.Lambda.Core;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public sealed class AmiMonitorFixture {
        private AmiMonitor _amiMonitor;
        private Mock<ILogger<AmiMonitor>> _mockLogger;
        private Mock<IBeanstalkRepository> _mockBeanstalkRepo;
        private Mock<ISsmRepository> _mockSsmRepo;
        private Mock<ILambdaContext> _mockLambdaContext;

        [TestInitialize]
        public void TestSetup() {
            var services = new ServiceCollection();
            _mockLogger = new Mock<ILogger<AmiMonitor>>();
            _mockBeanstalkRepo = new Mock<IBeanstalkRepository>();
            _mockSsmRepo = new Mock<ISsmRepository>();
            _mockLambdaContext = new Mock<ILambdaContext>();

            services.AddScoped(_ => _mockLogger.Object);
            services.AddScoped(_ => _mockBeanstalkRepo.Object);
            services.AddScoped(_ => _mockSsmRepo.Object);

            _amiMonitor = new AmiMonitor(services.BuildServiceProvider());
        }

        [TestMethod]
        public async Task GivenRequest_WhenLambdaIsInvoked_ThenLoggingScopeContainsAwsRequestId() {
            await _amiMonitor.Handler(null, _mockLambdaContext.Object);

            _mockLogger.Verify(logger => logger.BeginScope(It.Is<Dictionary<string, string>>(d => d["AwsRequestId"] == _mockLambdaContext.Object.AwsRequestId)));
        }

        [TestMethod]
        public async Task GivenUnhandledExceptionOccurs_WhenLambdaIsInvoked_ThenExceptionIsBubbledUp() {
            _mockBeanstalkRepo.Setup(r => r.GetLatestAmiVersionAsync(null))
                              .Throws<InvalidOperationException>();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _amiMonitor.Handler(null, _mockLambdaContext.Object));
        }

        [TestMethod]
        public async Task GivenPlatformHasHvmAmi_WhenLambdaIsInvoked_ThenSsmParameterIsUpdated() {
            string expectedAmiId = "ami-";
            _mockBeanstalkRepo.Setup(r => r.GetLatestAmiVersionAsync(null))
                              .ReturnsAsync(expectedAmiId);

            await _amiMonitor.Handler(null, _mockLambdaContext.Object);

            _mockSsmRepo.Verify(r => r.UpdateParameterAsync(It.IsAny<string>(), expectedAmiId), Times.Once(), "Parameter should be updated with latest AMI ID.");
        }

        [TestMethod]
        public async Task GivenAmiIsNotFoundForPlatform_WhenLambdaIsInvoked_ThenSsmParameterIsNotUpdated() {
            _mockBeanstalkRepo.Setup(m => m.GetLatestAmiVersionAsync(It.IsAny<string>()))
                              .ReturnsAsync(string.Empty);

            await _amiMonitor.Handler(null, _mockLambdaContext.Object);

            _mockSsmRepo.Verify(r => r.UpdateParameterAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never(),
                                "Parameter should not be updated unless AMI was successfully retrieved.");
        }
    }
}
