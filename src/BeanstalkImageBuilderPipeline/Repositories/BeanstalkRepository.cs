// This sample, non-production-ready project that provides provides the ability to detect when an Amazon Elastic Beanstalk 
// platform's base AMI has been updated and starts an EC2 Image Builder Pipeline to automate the creation of a golden image.
// © 2021 Amazon Web Services, Inc. or its affiliates. All Rights Reserved.  
// This AWS Content is provided subject to the terms of the AWS Customer Agreement available at  
// http://aws.amazon.com/agreement or other written agreement between Customer and either
// Amazon Web Services, Inc. or Amazon Web Services EMEA SARL or both.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.ElasticBeanstalk;
    using Amazon.ElasticBeanstalk.Model;
    using Microsoft.Extensions.Logging;

    public sealed class BeanstalkRepository : IBeanstalkRepository
    {
        private readonly IAmazonElasticBeanstalk _beanstalkClient;
        private readonly ILogger<BeanstalkRepository> _logger;

        public BeanstalkRepository(IAmazonElasticBeanstalk beanstalkClient, ILogger<BeanstalkRepository> logger)
        {
            (_beanstalkClient, _logger) = (beanstalkClient, logger);
        }

        public async Task<string> GetLatestAmiVersionAsync(string platformArn)
        {
            _logger.LogInformation("Retrieving Beanstalk Platform {BeanstalkPlatformArn}", platformArn);

            DescribePlatformVersionResponse versionsResponse = await _beanstalkClient.DescribePlatformVersionAsync(new DescribePlatformVersionRequest
            {
                PlatformArn = platformArn
            });

            CustomAmi ami = versionsResponse.PlatformDescription
                                            .CustomAmiList
                                            .FirstOrDefault(a => a.VirtualizationType.Equals("hvm", StringComparison.OrdinalIgnoreCase));

            return ami?.ImageId;
        }
    }
}
