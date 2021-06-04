// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

using System.Threading.Tasks;

namespace BeanstalkImageBuilderPipeline.Repositories
{
    public interface IBeanstalkRepository
    {
        Task<string> GetLatestAmiVersionAsync(string platformArn);
    }
}