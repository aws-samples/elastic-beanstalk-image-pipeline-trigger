// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Repositories {
    using System;
    using System.Threading.Tasks;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;
    using Microsoft.Extensions.Logging;

    public sealed class SsmRepository : ISsmRepository {
        private readonly ILogger<SsmRepository> _logger;
        private readonly IAmazonSimpleSystemsManagement _ssmClient;

        public SsmRepository(IAmazonSimpleSystemsManagement ssmClient, ILogger<SsmRepository> logger) {
            (_ssmClient, _logger) = (ssmClient, logger);
        }

        public async Task<string> GetParameterValueAsync(string parameterName) {
            return (await GetParameterAsync(parameterName))?.Value;
        }

        private async Task<Parameter> GetParameterAsync(string parameterName) {
            try {
                GetParameterResponse parameterResponse = await _ssmClient.GetParameterAsync(new GetParameterRequest {Name = parameterName, WithDecryption = true});

                return parameterResponse.Parameter;
            }
            catch (ParameterNotFoundException) {
                return null;
            }
        }

        public async Task UpdateParameterAsync(string parameterName, string value) {
            var currentAmiParameter = await GetParameterAsync(parameterName);

            if (currentAmiParameter == null || !currentAmiParameter.Value.Equals(value, StringComparison.OrdinalIgnoreCase)) {
                _logger.LogInformation("Parameter {ParameterName}, version {ParameterVersion} value is outdated and will be updated to {ProposedValue}.", 
                                       parameterName,
                                       currentAmiParameter?.Version,
                                       value);

                await _ssmClient.PutParameterAsync(new PutParameterRequest {
                    Name = parameterName,
                    Type = ParameterType.String,
                    Overwrite = true,
                    Value = value
                });
            }
            else {
                _logger.LogInformation("Parameter {ParameterName}, version {ParameterVersion} value has not changed from {ProposedValue}. Skipping update.", 
                                       parameterName,
                                       currentAmiParameter.Version, 
                                       value);
            }
        }
    }
}
