﻿// This sample, non-production-ready project that demonstrates how to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.UnitTests.Repositories {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.ElasticBeanstalk;
    using Amazon.ElasticBeanstalk.Model;
    using BeanstalkImageBuilderPipeline.Repositories;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public sealed class BeanstalkRepositoryFixture {
        private BeanstalkRepository _repository;
        private Mock<ILogger<BeanstalkRepository>> _mockLogger;
        private Mock<IAmazonElasticBeanstalk> _mockBeanstalkClient;

        [TestInitialize]
        public void TestSetup()
        {
            _mockLogger = new Mock<ILogger<BeanstalkRepository>>();
            _mockBeanstalkClient = new Mock<IAmazonElasticBeanstalk>();

            _repository = new BeanstalkRepository(_mockBeanstalkClient.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GivenPlatformWithHvmVirtualization_WhenGetLatestAmiVersionCalled_ThenFirstHvmResultIsReturned()
        {
            var expectedAmi = new CustomAmi
            {
                VirtualizationType = "hvm",
                ImageId = "test"
            };

            _mockBeanstalkClient.Setup(r => r.DescribePlatformVersionAsync(It.IsAny<DescribePlatformVersionRequest>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(new DescribePlatformVersionResponse
                                {
                                    PlatformDescription = new PlatformDescription
                                    {
                                        CustomAmiList = new List<CustomAmi>(new[] { new CustomAmi { VirtualizationType = "unknown", ImageId = "bar" }, expectedAmi })
                                    }
                                });

            string result = await _repository.GetLatestAmiVersionAsync("test");

            Assert.AreEqual(expectedAmi.ImageId, result, "Expected HVM AMI was not returned.");
        }

        [TestMethod]
        public async Task GivenPlatformWithouHvmVirtualization_WhenGetLatestAmiVersionCalled_ThenNullIsReturned()
        {
            _mockBeanstalkClient.Setup(r => r.DescribePlatformVersionAsync(It.IsAny<DescribePlatformVersionRequest>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(new DescribePlatformVersionResponse
                                {
                                    PlatformDescription = new PlatformDescription
                                    {
                                        CustomAmiList = new List<CustomAmi>(new[] { new CustomAmi { VirtualizationType = "unknown", ImageId = "bar" } })
                                    }
                                });

            string result = await _repository.GetLatestAmiVersionAsync("test");

            Assert.IsNull(result, "Null is expected return value when no HVM virtualization is available for the queried platform.");
        }
    }
}
