// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class MigratorConfigurationExtension : EntityConfigurationExtension
    {
        private string _directory;

        public virtual string Directory
        {
            get { return _directory; }

            [param: NotNull]
            set { _directory = Check.NotEmpty(value, "value"); }
        }

        protected override void ApplyServices(EntityServicesBuilder builder)
        {
        }
    }
}
