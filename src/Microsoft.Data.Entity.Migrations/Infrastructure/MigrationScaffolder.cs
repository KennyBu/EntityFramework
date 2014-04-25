// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Data.Entity.Migrations.Utilities;

namespace Microsoft.Data.Entity.Migrations.Infrastructure
{
    public class MigrationScaffolder
    {
        private readonly DbContextConfiguration _contextConfiguration;
        // TODO: Create and use language agnostic abstraction if we plan to support anything other than CSharp.
        private readonly CSharpMigrationCodeGenerator _migrationCodeGenerator;

        public MigrationScaffolder(
            [NotNull] DbContextConfiguration contextConfiguration,
            [NotNull] CSharpMigrationCodeGenerator migrationCodeGenerator)
        {
            Check.NotNull(contextConfiguration, "contextConfiguration");
            Check.NotNull(migrationCodeGenerator, "migrationCodeGenerator");

            _contextConfiguration = contextConfiguration;
            _migrationCodeGenerator = migrationCodeGenerator;
        }

        public virtual string Namespace
        {
            get { return _contextConfiguration.Context.GetType().Namespace + ".Migrations"; }
        }

        public virtual string Directory
        {
            get { return _contextConfiguration.ContextOptions.Extensions.OfType<MigratorConfigurationExtension>().Single().Directory; }
        }

        public virtual void ScaffoldMigration([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            var stringBuilder = new IndentedStringBuilder();
            var designerStringBuilder = new IndentedStringBuilder();
            var className = GetClassName(migration);

            _migrationCodeGenerator.GenerateClass(Namespace, className, migration, stringBuilder);
            _migrationCodeGenerator.GenerateDesignerClass(Namespace, className, migration, designerStringBuilder);

            OnMigrationScaffolded(className, stringBuilder.ToString(), designerStringBuilder.ToString());
        }

        // TODO: Consider splitting model scaffolding to its own class.
        public virtual void ScaffoldModel([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            var stringBuilder = new IndentedStringBuilder();
            var className = GetClassName(model);

            _migrationCodeGenerator.ModelGenerator.GenerateClass(Namespace, className, model, stringBuilder);

            OnModelScaffolded(className, stringBuilder.ToString());
        }

        protected virtual string GetClassName([NotNull] IMigrationMetadata migration)
        {
            Check.NotNull(migration, "migration");

            return migration.Name;
        }

        protected virtual string GetClassName([NotNull] IModel model)
        {
            Check.NotNull(model, "model");

            return _contextConfiguration.Context.GetType().Name + "ModelSnapshot";
        }

        protected virtual void OnMigrationScaffolded(string className, string migration, string metadata)
        {
            var fileName = className + _migrationCodeGenerator.CodeFileExtension;

            WriteFile(fileName, migration, FileMode.CreateNew);

            var designerFileName = className + ".Designer" + _migrationCodeGenerator.CodeFileExtension;

            WriteFile(designerFileName, metadata, FileMode.Create);
        }

        protected virtual void OnModelScaffolded(string className, string model)
        {
            var fileName = className + _migrationCodeGenerator.ModelGenerator.CodeFileExtension;

            WriteFile(fileName, model, FileMode.Create);
        }

        protected virtual void WriteFile(string fileName, string content, FileMode fileMode)
        {
#if NET45
            var filePath = Path.Combine(Directory, fileName);

            using (var stream = new FileStream(filePath, fileMode, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }            
#endif
        }
    }
}
