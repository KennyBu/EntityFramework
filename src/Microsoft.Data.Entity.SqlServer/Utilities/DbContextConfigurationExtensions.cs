// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Relational;

namespace Microsoft.Data.Entity.Migrations.Utilities
{
    internal static class DbContextConfigurationExtensions
    {
        public static T GetService<T>(this DbContextConfiguration configuration)
        {
            return (T)configuration.Services.ServiceProvider.GetService(typeof(T));
        }
    }
}
