// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

// 有意放置在这个命名空间中，因为这是为其它相关提供者使用的，
// 而不是为顶级应用开发人员准备的。
namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     在 <see cref="IServiceCollection" /> 中设置 Entity Framework 相关服务的扩展方法。
    /// </summary>
    public static class EntityFrameworkServiceCollectionExtensions
    {
        /// <summary>
        ///     添加 Entity Framework 核心必须的服务到 <see cref="IServiceCollection" />。
        ///     当你在你的应用程序中使用依赖注入时使用这个方法，如 ASP.NET 应用程序。
        ///     有关设置依赖注入的更多信息，请参阅，see http://go.microsoft.com/fwlink/?LinkId=526890.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         当你想让 Entity Framework 解析从外部 <see cref="IServiceProvider" /> 获取的服务，你仅需要使用这个功能。
        ///         如果你不使用外部 <see cref="IServiceProvider" />，Entity Framework 将创建它需要的服务。
        ///         
        ///     </para>
        ///     <para>
        ///         你同样可以在返回的 <see cref="IServiceCollection" /> 上调用数据库定义的扩展方法来注册数据库所必需的服务。
        ///         例如，当使用 Microsoft.EntityFrameworkCore.SqlServer 时，你应该调用 
        ///         <c>collection.AddEntityFrameworkSqlServer()</c>。
        ///         
        ///     </para>
        ///     <para>
        ///         对于在 <see cref="IServiceProvider" /> 中注册派生上下文，以及从 <see cref="IServiceProvider" /> 解析它们的服务，
        ///         你必须在返回的 <see cref="IServiceCollection" /> 上以链式方式调用
        ///         <see
        ///             cref="Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.AddDbContext{TContext}(IServiceCollection, Action{DbContextOptionsBuilder}, ServiceLifetime)" />
        ///         方法。
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code>
        ///         public void ConfigureServices(IServiceCollection services) 
        ///         {
        ///             var connectionString = "connection string to database";
        /// 
        ///             services.AddDbContext&lt;MyContext&gt;(options => options.UseSqlServer(connectionString)); 
        ///         }
        ///     </code>
        /// </example>
        /// <param name="serviceCollection"> 服务将添加到的 <see cref="IServiceCollection" />。 </param>
        /// <returns>
        ///     明确设置了 Entity Framework 的 <see cref="IServiceCollection" />。
        /// </returns>
        public static IServiceCollection AddEntityFramework(
            [NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            serviceCollection.TryAddEnumerable(new ServiceCollection()
                .AddScoped<IEntityStateListener, INavigationFixer>(p => p.GetService<INavigationFixer>())
                .AddScoped<INavigationListener, INavigationFixer>(p => p.GetService<INavigationFixer>())
                .AddScoped<IKeyListener, INavigationFixer>(p => p.GetService<INavigationFixer>())
                .AddScoped<IQueryTrackingListener, INavigationFixer>(p => p.GetService<INavigationFixer>())
                .AddScoped<IPropertyListener, IChangeDetector>(p => p.GetService<IChangeDetector>())
                .AddScoped<IEntityStateListener, ILocalViewListener>(p => p.GetService<ILocalViewListener>()));

            serviceCollection.TryAdd(new ServiceCollection()
                .AddSingleton<IDbSetFinder, DbSetFinder>()
                .AddSingleton<IDbSetInitializer, DbSetInitializer>()
                .AddSingleton<IDbSetSource, DbSetSource>()
                .AddSingleton<IEntityFinderSource, EntityFinderSource>()
                .AddSingleton<IEntityMaterializerSource, EntityMaterializerSource>()
                .AddSingleton<ICoreConventionSetBuilder, CoreConventionSetBuilder>()
                .AddSingleton<IModelCustomizer, ModelCustomizer>()
                .AddSingleton<IModelCacheKeyFactory, ModelCacheKeyFactory>()
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddScoped<LoggingModelValidator>()
                .AddScoped<IKeyPropagator, KeyPropagator>()
                .AddScoped<INavigationFixer, NavigationFixer>()
                .AddScoped<ILocalViewListener, LocalViewListener>()
                .AddScoped<IStateManager, StateManager>()
                .AddScoped<IConcurrencyDetector, ConcurrencyDetector>()
                .AddScoped<IInternalEntityEntryFactory, InternalEntityEntryFactory>()
                .AddScoped<IInternalEntityEntryNotifier, InternalEntityEntryNotifier>()
                .AddScoped<IInternalEntityEntrySubscriber, InternalEntityEntrySubscriber>()
                .AddScoped<IValueGenerationManager, ValueGenerationManager>()
                .AddScoped<IChangeTrackerFactory, ChangeTrackerFactory>()
                .AddScoped<IChangeDetector, ChangeDetector>()
                .AddScoped<IEntityEntryGraphIterator, EntityEntryGraphIterator>()
                .AddScoped<IDbContextServices, DbContextServices>()
                .AddScoped<IDatabaseProviderSelector, DatabaseProviderSelector>()
                .AddScoped<IEntityGraphAttacher, EntityGraphAttacher>()
                .AddScoped<ValueGeneratorSelector>()
                .AddSingleton<ExecutionStrategyFactory>()
                .AddScoped(typeof(ISensitiveDataLogger<>), typeof(SensitiveDataLogger<>))
                .AddScoped(typeof(ILogger<>), typeof(InterceptingLogger<>))
                .AddScoped(p => GetContextServices(p).Model)
                .AddScoped(p => GetContextServices(p).CurrentContext)
                .AddScoped(p => GetContextServices(p).ContextOptions)
                .AddScoped(p => GetContextServices(p).DatabaseProviderServices)
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).Database))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).TransactionManager))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ValueGeneratorSelector))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).Creator))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ConventionSetBuilder))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ValueGeneratorCache))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ModelSource))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ModelValidator))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ExecutionStrategyFactory))
                .AddQuery());

            return serviceCollection;
        }

        private static IServiceCollection AddQuery(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddMemoryCache()
                .AddSingleton(_ => MethodInfoBasedNodeTypeRegistry.CreateFromRelinqAssembly())
                .AddScoped<ICompiledQueryCache, CompiledQueryCache>()
                .AddScoped<IAsyncQueryProvider, EntityQueryProvider>()
                .AddScoped<IQueryCompiler, QueryCompiler>()
                .AddScoped<IQueryAnnotationExtractor, QueryAnnotationExtractor>()
                .AddScoped<IQueryOptimizer, QueryOptimizer>()
                .AddScoped<IEntityTrackingInfoFactory, EntityTrackingInfoFactory>()
                .AddScoped<ISubQueryMemberPushDownExpressionVisitor, SubQueryMemberPushDownExpressionVisitor>()
                .AddScoped<ITaskBlockingExpressionVisitor, TaskBlockingExpressionVisitor>()
                .AddScoped<IEntityResultFindingExpressionVisitorFactory, EntityResultFindingExpressionVisitorFactory>()
                .AddScoped<IMemberAccessBindingExpressionVisitorFactory, MemberAccessBindingExpressionVisitorFactory>()
                .AddScoped<INavigationRewritingExpressionVisitorFactory, NavigationRewritingExpressionVisitorFactory>()
                .AddScoped<IOrderingExpressionVisitorFactory, OrderingExpressionVisitorFactory>()
                .AddScoped<IQuerySourceTracingExpressionVisitorFactory, QuerySourceTracingExpressionVisitorFactory>()
                .AddScoped<IRequiresMaterializationExpressionVisitorFactory, RequiresMaterializationExpressionVisitorFactory>()
                .AddScoped<CompiledQueryCacheKeyGenerator>()
                .AddScoped<ExpressionPrinter>()
                .AddScoped<ResultOperatorHandler>()
                .AddScoped<QueryCompilationContextFactory>()
                .AddScoped<ProjectionExpressionVisitorFactory>()
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).QueryContextFactory))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).QueryCompilationContextFactory))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).CompiledQueryCacheKeyGenerator))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).EntityQueryModelVisitorFactory))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).EntityQueryableExpressionVisitorFactory))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ExpressionPrinter))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ResultOperatorHandler))
                .AddScoped(p => p.InjectAdditionalServices(GetProviderServices(p).ProjectionExpressionVisitorFactory));

        private static IDbContextServices GetContextServices(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IDbContextServices>();

        private static IDatabaseProviderServices GetProviderServices(IServiceProvider serviceProvider)
            => GetContextServices(serviceProvider).DatabaseProviderServices;
    }
}
