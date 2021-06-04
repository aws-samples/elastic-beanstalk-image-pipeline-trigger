// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: MIT-0

namespace BeanstalkImageBuilderPipeline.Events {
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public sealed class ParameterStoreChangeDetail {
        public string Operation { get; set; }

        public string Name { get; set; }

        public string Exception { get; set; }
    }
}
