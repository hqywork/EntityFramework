// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     特定于 SQL Server 的 <see cref="IServiceCollection" /> 扩展方法。
    /// </summary>
    public static class SqlServerServiceCollectionExtensions
    {
        /// <summary>
        ///     <para>
        ///         添加使用 Entity Framework 的 Microsoft SQL Server 数据库提供者必需的服务到 <see cref="IServiceCollection" />。
        ///         当你在应用程序中使用了依赖注入时你可以使用这个方法，如 ASP.NET。
        ///         关于设置依赖注入的更多信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=526890。
        ///         
        ///     </para>
        ///     <para>
        ///         当你想 Entity Framework 使用外部依赖注入容器来解析服务时，你只需要使用这个功能。
        ///         如果你不使用外部依赖注入容器，Entity Framework 将创建它需求的服务。
        ///         
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        ///          public void ConfigureServices(IServiceCollection services)
        ///          {
        ///              var connectionString = "connection string to database";
        /// 
        ///              services
        ///                  .AddEntityFrameworkSqlServer()
        ///                  .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
        ///                      options.UseSqlServer(connectionString)
        ///                             .UseInternalServiceProvider(serviceProvider));
        ///          }
        ///      </code>
        /// </example>
        /// <param name="services"> 要添加服务的 <see cref="IServiceCollection" />。 </param>
        /// <returns>
        ///     与调用时一样的服务集合，以便以链式方式进行多个调用。
        /// </returns>
        public static IServiceCollection AddEntityFrameworkSqlServer([NotNull] this IServiceCollection services)
        {
            Check.NotNull(services, nameof(services));

            services.AddRelational();

            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IDatabaseProvider, DatabaseProvider<SqlServerDatabaseProviderServices, SqlServerOptionsExtension>>());

            services.TryAdd(new ServiceCollection()
                .AddSingleton<ISqlServerValueGeneratorCache, SqlServerValueGeneratorCache>()
                .AddSingleton<SqlServerTypeMapper>()
                .AddSingleton<SqlServerSqlGenerationHelper>()
                .AddSingleton<SqlServerModelSource>()
                .AddSingleton<SqlServerAnnotationProvider>()
                .AddSingleton<SqlServerMigrationsAnnotationProvider>()
                .AddScoped<SqlServerConventionSetBuilder>()
                .AddScoped<ISqlServerUpdateSqlGenerator, SqlServerUpdateSqlGenerator>()
                .AddScoped<ISqlServerSequenceValueGeneratorFactory, SqlServerSequenceValueGeneratorFactory>()
                .AddScoped<SqlServerModificationCommandBatchFactory>()
                .AddScoped<SqlServerValueGeneratorSelector>()
                .AddScoped<SqlServerDatabaseProviderServices>()
                .AddScoped<ISqlServerConnection, SqlServerConnection>()
                .AddScoped<SqlServerMigrationsSqlGenerator>()
                .AddScoped<SqlServerDatabaseCreator>()
                .AddScoped<SqlServerHistoryRepository>()
                .AddScoped<SqlServerQueryModelVisitorFactory>()
                .AddScoped<SqlServerCompiledQueryCacheKeyGenerator>()
                .AddScoped<SqlServerExecutionStrategyFactory>()
                .AddQuery());

            return services;
        }

        private static IServiceCollection AddQuery(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddScoped<SqlServerQueryCompilationContextFactory>()
                .AddScoped<SqlServerCompositeMemberTranslator>()
                .AddScoped<SqlServerCompositeMethodCallTranslator>()
                .AddScoped<SqlServerQuerySqlGeneratorFactory>();
    }
}
