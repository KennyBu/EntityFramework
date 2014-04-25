// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Migrations.Utilities;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public static class MigratorEntityConfigurationBuilderExtensions
    {
        public static DbContextOptions MigrationDirectory(
            [NotNull] this DbContextOptions builder, [NotNull] string directory)
        {
            Check.NotNull(builder, "builder");
            Check.NotEmpty(directory, "directory");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<MigratorConfigurationExtension>(x => x.Directory = directory));

            return builder;            
        }
    }
}
