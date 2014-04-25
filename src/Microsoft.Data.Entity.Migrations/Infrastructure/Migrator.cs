// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Migrations.Model;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;
using System.Globalization;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class Migrator
    {
        private readonly DbContextConfiguration _contextConfiguration;
        private readonly HistoryRepository _historyRepository;
        private readonly MigrationAssembly _migrationAssembly;
        private readonly MigrationScaffolder _migrationScaffolder;
        private readonly ModelDiffer _modelDiffer;
        private readonly MigrationOperationSqlGenerator _sqlGenerator;
        private readonly SqlStatementExecutor _sqlExecutor;

        public Migrator(
            [NotNull] DbContextConfiguration contextConfiguration,
            [NotNull] HistoryRepository historyRepository,
            [NotNull] MigrationAssembly migrationAssembly,
            [NotNull] MigrationScaffolder migrationScaffolder,
            [NotNull] ModelDiffer modelDiffer,
            [NotNull] MigrationOperationSqlGenerator sqlGenerator,
            [NotNull] SqlStatementExecutor sqlExecutor)
        {
            Check.NotNull(contextConfiguration, "contextConfiguration");
            Check.NotNull(historyRepository, "historyRepository");
            Check.NotNull(migrationAssembly, "migrationAssembly");
            Check.NotNull(migrationScaffolder, "migrationScaffolder");
            Check.NotNull(modelDiffer, "modelDiffer");
            Check.NotNull(sqlGenerator, "sqlGenerator");
            Check.NotNull(sqlExecutor, "sqlExecutor");

            _contextConfiguration = contextConfiguration;
            _historyRepository = historyRepository;
            _migrationAssembly = migrationAssembly;
            _migrationScaffolder = migrationScaffolder;
            _modelDiffer = modelDiffer;
            _sqlGenerator = sqlGenerator;
            _sqlExecutor = sqlExecutor;
        }

        public virtual void AddMigration([NotNull] string migrationName)
        {
            Check.NotEmpty(migrationName, "migrationName");

            // TODO: Handle duplicate migration names.

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssf", CultureInfo.InvariantCulture);
            var sourceModel = _migrationAssembly.Model;
            var targetModel = _contextConfiguration.Model;

            IReadOnlyList<MigrationOperation> upgradeOperations, downgradeOperations;
            if (sourceModel != null)
            {
                upgradeOperations = _modelDiffer.Diff(sourceModel, targetModel);
                downgradeOperations = _modelDiffer.Diff(targetModel, sourceModel);
            }
            else
            {
                upgradeOperations = _modelDiffer.DiffTarget(targetModel);
                downgradeOperations = _modelDiffer.DiffSource(targetModel);                
            }

            _migrationScaffolder.ScaffoldMigration(
                new MigrationMetadata(migrationName, timestamp)
                    {
                        SourceModel = sourceModel,
                        TargetModel = targetModel,
                        UpgradeOperations = upgradeOperations,
                        DowngradeOperations = downgradeOperations
                    });
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetDatabaseMigrations()
        {
            return _historyRepository.Migrations;
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetLocalMigrations()
        {
            return _migrationAssembly.Migrations;
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetPendingMigrations()
        {
            return GetLocalMigrations()
                .Except(GetDatabaseMigrations(), (x, y) => x.Name == y.Name)
                .ToArray();
        }

        public virtual void UpdateDatabase()
        {
            var pendingMigrations = GetPendingMigrations();

            if (!pendingMigrations.Any())
            {
                return;
            }

            foreach (var migration in pendingMigrations)
            {
                var statements = _sqlGenerator.Generate(migration.UpgradeOperations, generateIdempotentSql: true);
                // TODO: Figure out what needs to be done to avoid the following cast.
                var dbConnection = ((RelationalConnection)_contextConfiguration.Connection).DbConnection;

                _sqlExecutor.ExecuteNonQuery(dbConnection, statements);

                _historyRepository.AddMigration(migration);
            }

            _migrationScaffolder.ScaffoldModel(pendingMigrations.Last().TargetModel);
        }

        protected static TService GetService<TService>(DbContextConfiguration contextConfiguration)
        {
            return (TService)contextConfiguration.Services.ServiceProvider.GetService(typeof(TService));
        }
    }
}
