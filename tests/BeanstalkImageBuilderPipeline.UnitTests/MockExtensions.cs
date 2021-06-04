// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
