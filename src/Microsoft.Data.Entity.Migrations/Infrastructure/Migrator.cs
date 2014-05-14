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

        public virtual DbContextConfiguration ContextConfiguration
        {
            get { return _contextConfiguration; }
        }

        public virtual HistoryRepository HistoryRepository
        {
            get { return _historyRepository; }
        }

        public virtual MigrationAssembly MigrationAssembly
        {
            get { return _migrationAssembly; }
        }

        public virtual MigrationScaffolder MigrationScaffolder
        {
            get { return _migrationScaffolder; }
        }

        public virtual ModelDiffer ModelDiffer
        {
            get { return _modelDiffer; }
        }

        public virtual MigrationOperationSqlGenerator SqlGenerator
        {
            get { return _sqlGenerator; }
        }

        public virtual SqlStatementExecutor SqlExecutor
        {
            get { return _sqlExecutor; }
        }

        public virtual void AddMigration([NotNull] string migrationName)
        {
            Check.NotEmpty(migrationName, "migrationName");

            // TODO: Handle duplicate migration names.

            var sourceModel = MigrationAssembly.Model;
            var targetModel = ContextConfiguration.Model;
            IReadOnlyList<MigrationOperation> upgradeOperations, downgradeOperations;

            if (sourceModel != null)
            {
                upgradeOperations = ModelDiffer.Diff(sourceModel, targetModel);
                downgradeOperations = ModelDiffer.Diff(targetModel, sourceModel);
            }
            else
            {
                upgradeOperations = ModelDiffer.DiffTarget(targetModel);
                downgradeOperations = ModelDiffer.DiffSource(targetModel);                
            }

            _migrationScaffolder.ScaffoldMigration(
                new MigrationMetadata(migrationName, CreateMigrationTimestamp())
                    {
                        SourceModel = sourceModel,
                        TargetModel = targetModel,
                        UpgradeOperations = upgradeOperations,
                        DowngradeOperations = downgradeOperations
                    });
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetDatabaseMigrations()
        {
            return HistoryRepository.Migrations;
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetLocalMigrations()
        {
            return HistoryRepository.Migrations;
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetPendingMigrations()
        {
            return GetLocalMigrations()
                .Except(GetDatabaseMigrations(), (x, y) => x.Timestamp == y.Timestamp && x.Name == y.Name)
                .ToArray();
        }

        public virtual IReadOnlyList<IMigrationMetadata> GetMigrationsSince(string migrationId)
        {
            return GetLocalMigrations()
                .Where(m => string.CompareOrdinal(migrationId, m.Timestamp + m.Name) < 0)
                .ToArray();
        }

        public virtual void UpdateDatabase()
        {
            var pendingMigrations = GetPendingMigrations();

            if (!pendingMigrations.Any())
            {
                return;
            }

            // TODO: Run the following if and foreach in a transaction.

            if (!ContextConfiguration.Context.Database.Exists())
            {
                var operations = ModelDiffer.DiffTarget(HistoryRepository.HistoryModel);

                UpdateDatabase(operations);
            }

            foreach (var migration in pendingMigrations)
            {
                UpdateDatabase(migration.UpgradeOperations);

                _historyRepository.AddMigration(migration);
            }

            _migrationScaffolder.ScaffoldModel(pendingMigrations.Last().TargetModel);
        }

        protected virtual string CreateMigrationTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssf", CultureInfo.InvariantCulture);
        }

        protected virtual void UpdateDatabase(IReadOnlyList<MigrationOperation> operations)
        {
            var statements = _sqlGenerator.Generate(operations, generateIdempotentSql: true);
            // TODO: Figure out what needs to be done to avoid the cast below.
            var dbConnection = ((RelationalConnection)_contextConfiguration.Connection).DbConnection;

            _sqlExecutor.ExecuteNonQuery(dbConnection, statements);
        }
    }
}
