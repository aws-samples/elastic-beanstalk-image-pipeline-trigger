// This sample, non-production-ready project that provides provides the ability to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories {
    using System.Threading.Tasks;

    public interface ISsmRepository {
        Task<string> GetParameterValueAsync(string parameterName);

        Task UpdateParameterAsync(string parameterName, string value);
    }
}
