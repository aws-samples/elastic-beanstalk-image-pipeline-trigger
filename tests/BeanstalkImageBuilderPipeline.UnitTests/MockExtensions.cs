// This sample, non-production-ready project that demonstrates how to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.UnitTests {
    using System;
    using Microsoft.Extensions.Logging;
    using Moq;

    public static class MockExtensions {
        public static Mock<ILogger<T>> VerifyMessageLogged<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, string expectedMessage, Times? times = null) {
            times ??= Times.Once();

            logger.Verify(x => x.Log(expectedLogLevel,
                                     It.IsAny<EventId>(),
                                     It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                                     It.IsAny<Exception>(),
                                     It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                                     times.Value);

            return logger;
        }
    }
}
