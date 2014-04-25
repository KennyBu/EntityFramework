// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.SqlServer.Utilities;

namespace Microsoft.Data.Entity.SqlServer
{
    public class SqlServerMigrator : Migrator
    {
        public SqlServerMigrator(
            DbContextConfiguration contextConfiguration,
            HistoryRepository historyRepository,
            MigrationAssembly migrationAssembly,
            MigrationScaffolder migrationScaffolder)
            : base(
                Check.NotNull(contextConfiguration, "contextConfiguration"), 
                historyRepository, 
                migrationAssembly, 
                migrationScaffolder, 
                GetService<ModelDiffer>(contextConfiguration), 
                GetService<SqlServerMigrationOperationSqlGenerator>(contextConfiguration), 
                GetService<SqlStatementExecutor>(contextConfiguration))
        {
        }
    }
}
