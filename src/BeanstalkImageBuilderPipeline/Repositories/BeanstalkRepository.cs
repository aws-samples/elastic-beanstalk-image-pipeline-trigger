// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
