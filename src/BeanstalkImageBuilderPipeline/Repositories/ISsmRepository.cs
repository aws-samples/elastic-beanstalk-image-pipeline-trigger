// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories {
    using System.Threading.Tasks;

    public interface ISsmRepository {
        Task<string> GetParameterValueAsync(string parameterName);

        Task UpdateParameterAsync(string parameterName, string value);
    }
}
