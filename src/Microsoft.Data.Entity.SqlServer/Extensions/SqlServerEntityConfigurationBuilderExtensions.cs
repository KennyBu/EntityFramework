// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Common;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Data.Entity.SqlServer.Utilities;

namespace Microsoft.Data.Entity
{
    public static class SqlServerEntityConfigurationBuilderExtensions
    {
        public static DbContextOptions UseSqlServer(
            [NotNull] this DbContextOptions builder, [NotNull] string connectionString)
        {
            Check.NotNull(builder, "builder");
            Check.NotEmpty(connectionString, "connectionString");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<SqlServerConfigurationExtension>(x => x.ConnectionString = connectionString));

            return builder;
        }

        // TODO: Use SqlConnection instead of DbConnection?
        public static DbContextOptions UseSqlServer(
            [NotNull] this DbContextOptions builder, [NotNull] DbConnection connection)
        {
            Check.NotNull(builder, "builder");
            Check.NotNull(connection, "connection");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<SqlServerConfigurationExtension>(x => x.Connection = connection));

            return builder;
        }

        // TODO: Consider having a separate extension for migrations.

        public static DbContextOptions MigrationAssembly(
            [NotNull] this DbContextOptions builder, [NotNull] Assembly assembly)
        {
            Check.NotNull(builder, "builder");
            Check.NotNull(assembly, "assembly");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<SqlServerConfigurationExtension>(x => x.MigrationAssembly = assembly));

            return builder;
        }

        public static DbContextOptions MigrationNamespace(
            [NotNull] this DbContextOptions builder, [NotNull] string @namespace)
        {
            Check.NotNull(builder, "builder");
            Check.NotEmpty(@namespace, "namespace");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<SqlServerConfigurationExtension>(x => x.MigrationNamespace = @namespace));

            return builder;
        }

        public static DbContextOptions MigrationDirectory(
            [NotNull] this DbContextOptions builder, [NotNull] string directory)
        {
            Check.NotNull(builder, "builder");
            Check.NotEmpty(directory, "directory");

            builder.AddBuildAction(c => c.AddOrUpdateExtension<SqlServerConfigurationExtension>(x => x.MigrationDirectory = directory));

            return builder;
        }
    }
}
