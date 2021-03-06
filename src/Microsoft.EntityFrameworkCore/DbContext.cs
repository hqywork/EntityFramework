// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     一个 `DbContext` 实例代表一个数据库会话，可以用来查询和保存实体的实例。
    ///     DbContext 是工作单元（Unit Of Work）和仓储（Repository）模式的组合。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         通常你创建一个从 DbContext 派生的类，并且包含了为模型中的每个实体创建 <see cref="DbSet{TEntity}" /> 属性。
    ///         如果 <see cref="DbSet{TEntity}" /> 属性拥有一个公共 setter，
    ///         那么当派生上下文的实例被创建时它们会被自动初始化。
    ///     </para>
    ///     <para>
    ///         重载 <see cref="OnConfiguring(DbContextOptionsBuilder)" /> 方法来配置数据库（以及其它选项）。
    ///         另外，如果你想在外部执行配置而不是上下文内容，你可以使用 <see cref="DbContextOptionsBuilder{TContext}" />（或 <see cref="DbContextOptionsBuilder" />）
    ///         在外部创建一个 <see cref="DbContextOptions{TContext}" /> （或 <see cref="DbContextOptions" />）的实例，
    ///         并传递给 <see cref="DbContext" /> 的基类构造函数。
    ///         
    ///     </para>
    ///     <para>
    ///         模型是通过在上下文派生类上的 <see cref="DbSet{TEntity}" /> 属性中运行一系列贯例的实体类来发现的。
    ///         要进一步配置通过贯例发现的模型，你可以重写 <see cref="OnModelCreating(ModelBuilder)" /> 方法。
    ///         
    ///     </para>
    /// </remarks>
    public class DbContext : IDisposable, IInfrastructure<IServiceProvider>
    {
        private readonly DbContextOptions _options;

        private IDbContextServices _contextServices;
        private IDbSetInitializer _setInitializer;
        private IEntityFinderSource _entityFinderSource;
        private ChangeTracker _changeTracker;
        private DatabaseFacade _database;
        private IStateManager _stateManager;
        private IChangeDetector _changeDetector;
        private IEntityGraphAttacher _graphAttacher;
        private IModel _model;
        private ILogger _logger;
        private IAsyncQueryProvider _queryProvider;

        private bool _initializing;
        private IServiceScope _serviceScope;
        private bool _disposed;

        /// <summary>
        ///     <para>
        ///         初始化 <see cref="DbContext" /> 类的新实例。Initializes a new instance of the <see cref="DbContext" />。
        ///         <see cref="OnConfiguring(DbContextOptionsBuilder)" /> 方法将被调用来配置当前上下文使用的数据（以及其它选项）。
        ///         
        ///     </para>
        /// </summary>
        protected DbContext()
            : this(new DbContextOptions<DbContext>())
        {
        }

        /// <summary>
        ///     <para>
        ///         使用指定的选项设置初始化 <see cref="DbContext" /> 的新实例。
        ///         <see cref="OnConfiguring(DbContextOptionsBuilder)" /> 方法仍然会被调用，允许进一步的配置选项。
        ///         
        ///     </para>
        /// </summary>
        /// <param name="options">用于当前上下文的选项设置。</param>
        public DbContext([NotNull] DbContextOptions options)
        {
            Check.NotNull(options, nameof(options));

            if (!options.ContextType.GetTypeInfo().IsAssignableFrom(GetType().GetTypeInfo()))
            {
                throw new InvalidOperationException(CoreStrings.NonGenericOptions(GetType().ShortDisplayName()));
            }

            _options = options;

            var initializer = GetServiceProvider(options).GetService<IDbSetInitializer>();
            if (initializer == null)
            {
                throw new InvalidOperationException(CoreStrings.NoEfServices);
            }

            initializer.InitializeSets(this);
        }

        private IChangeDetector ChangeDetector
            => _changeDetector
               ?? (_changeDetector = InternalServiceProvider.GetRequiredService<IChangeDetector>());

        private IStateManager StateManager
            => _stateManager
               ?? (_stateManager = InternalServiceProvider.GetRequiredService<IStateManager>());

        internal IAsyncQueryProvider QueryProvider
            => _queryProvider ?? (_queryProvider = this.GetService<IAsyncQueryProvider>());

        private IServiceProvider InternalServiceProvider
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().ShortDisplayName(), CoreStrings.ContextDisposed);
                }
                return (_contextServices ?? (_contextServices = InitializeServices())).InternalServiceProvider;
            }
        }

        private IDbContextServices InitializeServices()
        {
            if (_initializing)
            {
                throw new InvalidOperationException(CoreStrings.RecursiveOnConfiguring);
            }

            try
            {
                _initializing = true;

                var optionsBuilder = new DbContextOptionsBuilder(_options);

                OnConfiguring(optionsBuilder);

                var options = optionsBuilder.Options;

                _serviceScope = GetServiceProvider(options)
                    .GetRequiredService<IServiceScopeFactory>()
                    .CreateScope();

                var scopedServiceProvider = _serviceScope.ServiceProvider;

                var contextServices = scopedServiceProvider.GetService<IDbContextServices>();

                if (contextServices == null)
                {
                    throw new InvalidOperationException(CoreStrings.NoEfServices);
                }

                contextServices.Initialize(scopedServiceProvider, options, this);

                _logger = scopedServiceProvider.GetRequiredService<ILogger<DbContext>>();

                return contextServices;
            }
            finally
            {
                _initializing = false;
            }
        }

        private static IServiceProvider GetServiceProvider(DbContextOptions options)
        {
            var coreExtension = options.FindExtension<CoreOptionsExtension>();
            if (coreExtension?.InternalServiceProvider != null
                && coreExtension.ReplacedServices != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.InvalidReplaceService(
                        nameof(DbContextOptionsBuilder.ReplaceService), nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
            }

            return coreExtension?.InternalServiceProvider
                   ?? ServiceProviderCache.Instance.GetOrAdd(options);
        }

        /// <summary>
        ///     <para>
        ///         Gets the scoped <see cref="IServiceProvider" /> being used to resolve services.
        ///     </para>
        ///     <para>
        ///         This property is intended for use by extension methods that need to make use of services
        ///         not directly exposed in the public API surface.
        ///     </para>
        /// </summary>
        IServiceProvider IInfrastructure<IServiceProvider>.Instance => InternalServiceProvider;

        /// <summary>
        ///     <para>
        ///         重写这个方法来配置被当前上下文使用的数据库（及其它选项）。
        ///         这个方法在上下文实例每次被创建时调用。
        ///     </para>
        ///     <para>
        ///         在具体的情景下， <see cref="DbContextOptions" /> 的实例可能会或可能不会被传递给构建函数，
        ///         你可以使用 <see cref="DbContextOptionsBuilder.IsConfigured" /> 来确定选项是否已被设置，
        ///         以及忽略一些或所有在 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 中的逻辑。
        ///         
        ///     </para>
        /// </summary>
        /// <param name="optionsBuilder">
        ///     用来创建或修改当前上下文选项的构建器。
        ///     数据库（及其它护展）通常在这个对象上定义了扩展方法，允许你去配置上下文。
        /// </param>
        protected internal virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        /// <summary>
        ///     重载这个方法来进一步的配置模型，这些模型是依照约定从你派生的上下文中公开的 
        ///     <see cref="DbSet{TEntity}" /> 类型的属性中发现的。
        ///     由此产生的模型可能会被缓存，并可以被随后派生的上下文实例重用。
        /// </summary>
        /// <remarks>
        ///     如果模型是在你的上下文中明确设置的（通过 <see cref="DbContextOptionsBuilder.UseModel(IModel)" />），
        ///     那么这个方法将不会运行。
        /// </remarks>
        /// <param name="modelBuilder">
        ///     一个生成器，被用来为当前上下文构造模型。
        ///     数据库（以及其它扩展方法）通常在这个对象上定义了扩展方法，
        ///     允许你为特定的给定数据库进行模型方面的配置。
        /// </param>
        protected internal virtual void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        /// <summary>
        ///     保存当前上下文中所有的更改到数据库。
        /// </summary>
        /// <remarks>
        ///     这个方法将自动调用 <see cref="ChangeTracking.ChangeTracker.DetectChanges" /> 来发现保存到底层数据库之前的所有对实体实例的变更。
        ///     这可以通过 <see cref="ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" /> 来禁用。
        ///     
        /// </remarks>
        /// <returns>
        ///     被写入到数据库的状态条目的个数。
        /// </returns>
        [DebuggerStepThrough]
        public virtual int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

        /// <summary>
        ///     保存当前上下文中所有的更改到数据库。
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">
        ///     指示在更改已经被成功发送到数据库后是否调用 <see cref="ChangeTracking.ChangeTracker.AcceptAllChanges" />。
        ///     
        /// </param>
        /// <remarks>
        ///     这个方法将自动调用 <see cref="ChangeTracking.ChangeTracker.DetectChanges" /> 来发现保存到底层数据库之前的所有对实体实例的变更。
        ///     这可以通过 <see cref="ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" /> 来禁用。
        ///     
        /// </remarks>
        /// <returns>
        ///     被写入到数据库的状态条目的个数。
        /// </returns>
        [DebuggerStepThrough]
        public virtual int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            TryDetectChanges();

            try
            {
                return StateManager.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    CoreEventId.DatabaseError,
                    () => new DatabaseErrorLogState(GetType()),
                    exception,
                    e => CoreStrings.LogExceptionDuringSaveChanges(Environment.NewLine, e));

                throw;
            }
        }

        private void TryDetectChanges()
        {
            if (ChangeTracker.AutoDetectChangesEnabled)
            {
                ChangeTracker.DetectChanges();
            }
        }

        /// <summary>
        ///     异步保存当前上下文中所有的更改到数据库。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         这个方法将自动调用 <see cref="ChangeTracking.ChangeTracker.DetectChanges" /> 来发现保存到底层数据库之前的所有对实体实例的变更。
        ///         这可以通过 <see cref="ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" /> 来禁用。
        ///         
        ///     </para>
        ///     <para>
        ///         在相同上下文实例上的多个激活操作是不被支持的。
        ///         使用 'await' 来确保调用这个上下文上的其它方法前任何异步操作都已完成。
        ///     </para>
        /// </remarks>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> 观察等待任务完成的 <see cref="CancellationToken" />。 </param>
        /// <returns>
        ///     一个任务，表示异步保存操作。
        ///     任务结果包含被写入到数据库的状态条目的个数。
        /// </returns>
        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken: cancellationToken);

        /// <summary>
        ///     异步保存当前上下文中所有的更改到数据库。
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">
        ///     指示在更改已经被成功发送到数据库后是否调用 <see cref="ChangeTracking.ChangeTracker.AcceptAllChanges" />。
        /// </param>
        /// <remarks>
        ///     <para>
        ///         这个方法将自动调用 <see cref="ChangeTracking.ChangeTracker.DetectChanges" /> 来发现保存到底层数据库之前的所有对实体实例的变更。
        ///         这可以通过 <see cref="ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" /> 来禁用。
        ///         
        ///     </para>
        ///     <para>
        ///         在相同上下文实例上的多个激活操作是不被支持的。
        ///         使用 'await' 来确保调用这个上下文上的其它方法前任何异步操作都已完成。
        ///     </para>
        /// </remarks>
        /// <param name="cancellationToken"> 观察等待任务完成的 <see cref="CancellationToken" />。 </param>
        /// <returns>
        ///     一个任务，表示异步保存操作。
        ///     任务结果包含被写入到数据库的状态条目的个数。
        /// </returns>
        public virtual async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            TryDetectChanges();

            try
            {
                return await StateManager.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    CoreEventId.DatabaseError,
                    () => new DatabaseErrorLogState(GetType()),
                    exception,
                    e => CoreStrings.LogExceptionDuringSaveChanges(Environment.NewLine, e));

                throw;
            }
        }

        /// <summary>
        ///     Releases the allocated resources for this context.
        /// </summary>
        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _stateManager?.Unsubscribe();

                _serviceScope?.Dispose();
                _setInitializer = null;
                _changeTracker = null;
                _stateManager = null;
                _changeDetector = null;
                _graphAttacher = null;
                _model = null;
            }
        }

        /// <summary>
        ///     获取给定实体的 <see cref="EntityEntry{TEntity}" /> 对象。
        ///     该入口提供了对改变跟踪信息以及实体操作的访问。
        /// </summary>
        /// <typeparam name="TEntity"> 实体的类型。 </typeparam>
        /// <param name="entity"> 将要获取入口的实体对象。 </param>
        /// <returns> 给定实体的入口对象。 </returns>
        public virtual EntityEntry<TEntity> Entry<TEntity>([NotNull] TEntity entity) where TEntity : class
        {
            Check.NotNull(entity, nameof(entity));

            TryDetectChanges();

            return EntryWithoutDetectChanges(entity);
        }

        private EntityEntry<TEntity> EntryWithoutDetectChanges<TEntity>(TEntity entity) where TEntity : class
            => new EntityEntry<TEntity>(StateManager.GetOrCreateEntry(entity));

        /// <summary>
        ///     <para>
        ///         获取给定实体的 <see cref="EntityEntry{TEntity}" /> 对象。
        ///         该入口提供了对改变跟踪信息以及实体操作的访问。
        ///     </para>
        ///     <para>
        ///         这个方法可能在不被跟踪的实体上调用。This method may be called on an entity that is not tracked. You can then
        ///         你可以在返回的入口上设置 <see cref="EntityEntry.State" /> 属性，
        ///         让所属的上下文使用指定状态开始跟踪实体。
        ///     </para>
        /// </summary>
        /// <param name="entity"> 将要获取入口的实体对象。 </param>
        /// <returns> 给定实体的入口对象。 </returns>
        public virtual EntityEntry Entry([NotNull] object entity)
        {
            Check.NotNull(entity, nameof(entity));

            TryDetectChanges();

            return EntryWithoutDetectChanges(entity);
        }

        private EntityEntry EntryWithoutDetectChanges(object entity)
            => new EntityEntry(StateManager.GetOrCreateEntry(entity));

        private void SetEntityState(InternalEntityEntry entry, EntityState entityState)
        {
            if (entry.EntityState == EntityState.Detached)
            {
                (_graphAttacher
                 ?? (_graphAttacher = InternalServiceProvider.GetRequiredService<IEntityGraphAttacher>()))
                    .AttachGraph(entry, entityState);
            }
            else
            {
                entry.SetEntityState(entityState, acceptChanges: true);
            }
        }

        /// <summary>
        ///     Begins tracking the given entity, and any other reachable entities that are
        ///     not already being tracked, in the <see cref="EntityState.Added" /> state such that
        ///     they will be inserted into the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <param name="entity"> The entity to add. </param>
        /// <returns>
        ///     The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry<TEntity> Add<TEntity>([NotNull] TEntity entity) where TEntity : class
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Added);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <param name="entity"> The entity to add. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous Add operation. The task result contains the
        ///     <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides access to change tracking
        ///     information and operations for the entity.
        /// </returns>
        public virtual async Task<EntityEntry<TEntity>> AddAsync<TEntity>(
            [NotNull] TEntity entity,
            CancellationToken cancellationToken = default(CancellationToken))
            where TEntity : class
        {
            var entry = EntryWithoutDetectChanges(entity);

            await entry.GetInfrastructure().SetEntityStateAsync(
                EntityState.Added,
                acceptChanges: true,
                cancellationToken: cancellationToken);

            return entry;
        }

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity in the <see cref="EntityState.Unchanged" /> state 
        ///         such that no operation will be performed when <see cref="DbContext.SaveChanges()" /> 
        ///         is called.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Unchanged" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <param name="entity"> The entity to attach. </param>
        /// <returns>
        ///     The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry<TEntity> Attach<TEntity>([NotNull] TEntity entity) where TEntity : class
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Unchanged);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity in the <see cref="EntityState.Modified" /> state such that it will
        ///         be updated in the database when <see cref="DbContext.SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         All properties of the entity will be marked as modified. To mark only some properties as modified, use
        ///         <see cref="Attach{TEntity}(TEntity)" /> to begin tracking the entity in the <see cref="EntityState.Unchanged" />
        ///         state and then use the returned <see cref="EntityEntry" /> to mark the desired properties as modified.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Modified" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <param name="entity"> The entity to update. </param>
        /// <returns>
        ///     The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry<TEntity> Update<TEntity>([NotNull] TEntity entity) where TEntity : class
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Modified);

        /// <summary>
        ///     Begins tracking the given entity in the <see cref="EntityState.Deleted" /> state such that it will
        ///     be removed from the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the entity is already tracked in the <see cref="EntityState.Added" /> state then the context will
        ///         stop tracking the entity (rather than marking it as <see cref="EntityState.Deleted" />) since the
        ///         entity was previously added to the context and does not exist in the database.
        ///     </para>
        ///     <para>
        ///         Any other reachable entities that are not already being tracked will be tracked in the same way that
        ///         they would be if <see cref="Attach{TEntity}(TEntity)" /> was called before calling this method.
        ///         This allows any cascading actions to be applied when <see cref="SaveChanges()" /> is called.
        ///     </para>
        /// </remarks>
        /// <typeparam name="TEntity"> The type of the entity. </typeparam>
        /// <param name="entity"> The entity to remove. </param>
        /// <returns>
        ///     The <see cref="EntityEntry{TEntity}" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry<TEntity> Remove<TEntity>([NotNull] TEntity entity) where TEntity : class
        {
            Check.NotNull(entity, nameof(entity));

            var entry = EntryWithoutDetectChanges(entity);

            var initialState = entry.State;
            if (initialState == EntityState.Detached)
            {
                SetEntityState(entry.GetInfrastructure(), EntityState.Unchanged);
            }

            // An Added entity does not yet exist in the database. If it is then marked as deleted there is
            // nothing to delete because it was not yet inserted, so just make sure it doesn't get inserted.
            entry.State =
                initialState == EntityState.Added
                    ? EntityState.Detached
                    : EntityState.Deleted;

            return entry;
        }

        private EntityEntry<TEntity> SetEntityState<TEntity>(
            TEntity entity,
            EntityState entityState) where TEntity : class
        {
            var entry = EntryWithoutDetectChanges(entity);

            SetEntityState(entry.GetInfrastructure(), entityState);

            return entry;
        }

        /// <summary>
        ///     Begins tracking the given entity, and any other reachable entities that are
        ///     not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///     be inserted into the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <param name="entity"> The entity to add. </param>
        /// <returns>
        ///     The <see cref="EntityEntry" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry Add([NotNull] object entity)
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Added);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        /// </summary>
        /// <param name="entity"> The entity to add. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous Add operation. The task result contains the
        ///     <see cref="EntityEntry" /> for the entity. The entry provides access to change tracking
        ///     information and operations for the entity.
        /// </returns>
        public virtual async Task<EntityEntry> AddAsync(
            [NotNull] object entity,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = EntryWithoutDetectChanges(entity);

            await entry.GetInfrastructure().SetEntityStateAsync(
                EntityState.Added,
                acceptChanges: true,
                cancellationToken: cancellationToken);

            return entry;
        }

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity in the <see cref="EntityState.Unchanged" /> state 
        ///         such that no operation will be performed when <see cref="DbContext.SaveChanges()" /> 
        ///         is called.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Unchanged" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entity"> The entity to attach. </param>
        /// <returns>
        ///     The <see cref="EntityEntry" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry Attach([NotNull] object entity)
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Unchanged);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity in the <see cref="EntityState.Modified" /> state such that it will
        ///         be updated in the database when <see cref="DbContext.SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         All properties of the entity will be marked as modified. To mark only some properties as modified, use
        ///         <see cref="Attach(object)" /> to begin tracking the entity in the <see cref="EntityState.Unchanged" />
        ///         state and then use the returned <see cref="EntityEntry" /> to mark the desired properties as modified.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Modified" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entity"> The entity to update. </param>
        /// <returns>
        ///     The <see cref="EntityEntry" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry Update([NotNull] object entity)
            => SetEntityState(Check.NotNull(entity, nameof(entity)), EntityState.Modified);

        /// <summary>
        ///     Begins tracking the given entity in the <see cref="EntityState.Deleted" /> state such that it will
        ///     be removed from the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the entity is already tracked in the <see cref="EntityState.Added" /> state then the context will
        ///         stop tracking the entity (rather than marking it as <see cref="EntityState.Deleted" />) since the
        ///         entity was previously added to the context and does not exist in the database.
        ///     </para>
        ///     <para>
        ///         Any other reachable entities that are not already being tracked will be tracked in the same way that
        ///         they would be if <see cref="Attach(object)" /> was called before calling this method.
        ///         This allows any cascading actions to be applied when <see cref="SaveChanges()" /> is called.
        ///     </para>
        /// </remarks>
        /// <param name="entity"> The entity to remove. </param>
        /// <returns>
        ///     The <see cref="EntityEntry" /> for the entity. The entry provides
        ///     access to change tracking information and operations for the entity.
        /// </returns>
        public virtual EntityEntry Remove([NotNull] object entity)
        {
            Check.NotNull(entity, nameof(entity));

            var entry = EntryWithoutDetectChanges(entity);

            var initialState = entry.State;
            if (initialState == EntityState.Detached)
            {
                SetEntityState(entry.GetInfrastructure(), EntityState.Unchanged);
            }

            // An Added entity does not yet exist in the database. If it is then marked as deleted there is
            // nothing to delete because it was not yet inserted, so just make sure it doesn't get inserted.
            entry.State =
                initialState == EntityState.Added
                    ? EntityState.Detached
                    : EntityState.Deleted;

            return entry;
        }

        private EntityEntry SetEntityState(object entity, EntityState entityState)
        {
            var entry = EntryWithoutDetectChanges(entity);

            SetEntityState(entry.GetInfrastructure(), entityState);

            return entry;
        }

        /// <summary>
        ///     Begins tracking the given entities, and any other reachable entities that are
        ///     not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///     be inserted into the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <param name="entities"> The entities to add. </param>
        public virtual void AddRange([NotNull] params object[] entities)
            => AddRange((IEnumerable<object>)entities);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to add. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        public virtual Task AddRangeAsync([NotNull] params object[] entities)
            => AddRangeAsync((IEnumerable<object>)entities);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entities in the <see cref="EntityState.Unchanged" /> state 
        ///         such that no operation will be performed when <see cref="DbContext.SaveChanges()" /> 
        ///         is called.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Unchanged" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to attach. </param>
        public virtual void AttachRange([NotNull] params object[] entities)
            => AttachRange((IEnumerable<object>)entities);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entities in the <see cref="EntityState.Modified" /> state such that they will
        ///         be updated in the database when <see cref="DbContext.SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         All properties of each entity will be marked as modified. To mark only some properties as modified, use
        ///         <see cref="Attach(object)" /> to begin tracking each entity in the <see cref="EntityState.Unchanged" />
        ///         state and then use the returned <see cref="EntityEntry" /> to mark the desired properties as modified.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Modified" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to update. </param>
        public virtual void UpdateRange([NotNull] params object[] entities)
            => UpdateRange((IEnumerable<object>)entities);

        /// <summary>
        ///     Begins tracking the given entity in the <see cref="EntityState.Deleted" /> state such that it will
        ///     be removed from the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If any of the entities are already tracked in the <see cref="EntityState.Added" /> state then the context will
        ///         stop tracking those entities (rather than marking them as <see cref="EntityState.Deleted" />) since those
        ///         entities were previously added to the context and do not exist in the database.
        ///     </para>
        ///     <para>
        ///         Any other reachable entities that are not already being tracked will be tracked in the same way that
        ///         they would be if <see cref="AttachRange(object[])" /> was called before calling this method.
        ///         This allows any cascading actions to be applied when <see cref="SaveChanges()" /> is called.
        ///     </para>
        /// </remarks>
        /// <param name="entities"> The entities to remove. </param>
        public virtual void RemoveRange([NotNull] params object[] entities)
            => RemoveRange((IEnumerable<object>)entities);

        private void SetEntityStates(IEnumerable<object> entities, EntityState entityState)
        {
            var stateManager = StateManager;

            foreach (var entity in entities)
            {
                SetEntityState(stateManager.GetOrCreateEntry(entity), entityState);
            }
        }

        /// <summary>
        ///     Begins tracking the given entities, and any other reachable entities that are
        ///     not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///     be inserted into the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <param name="entities"> The entities to add. </param>
        public virtual void AddRange([NotNull] IEnumerable<object> entities)
            => SetEntityStates(Check.NotNull(entities, nameof(entities)), EntityState.Added);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entity, and any other reachable entities that are
        ///         not already being tracked, in the <see cref="EntityState.Added" /> state such that they will
        ///         be inserted into the database when <see cref="SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         This method is async only to allow special value generators, such as the one used by
        ///         'Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.SequenceHiLo',
        ///         to access the database asynchronously. For all other cases the non async method should be used.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to add. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        public virtual async Task AddRangeAsync(
            [NotNull] IEnumerable<object> entities,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var stateManager = StateManager;

            foreach (var entity in entities)
            {
                await stateManager.GetOrCreateEntry(entity).SetEntityStateAsync(
                    EntityState.Added,
                    acceptChanges: true,
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entities in the <see cref="EntityState.Unchanged" /> state 
        ///         such that no operation will be performed when <see cref="DbContext.SaveChanges()" /> 
        ///         is called.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Unchanged" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to attach. </param>
        public virtual void AttachRange([NotNull] IEnumerable<object> entities)
            => SetEntityStates(Check.NotNull(entities, nameof(entities)), EntityState.Unchanged);

        /// <summary>
        ///     <para>
        ///         Begins tracking the given entities in the <see cref="EntityState.Modified" /> state such that they will
        ///         be updated in the database when <see cref="DbContext.SaveChanges()" /> is called.
        ///     </para>
        ///     <para>
        ///         All properties of each entity will be marked as modified. To mark only some properties as modified, use
        ///         <see cref="Attach(object)" /> to begin tracking each entity in the <see cref="EntityState.Unchanged" />
        ///         state and then use the returned <see cref="EntityEntry" /> to mark the desired properties as modified.
        ///     </para>
        ///     <para>
        ///         A recursive search of the navigation properties will be performed to find reachable entities
        ///         that are not already being tracked by the context. These entities will also begin to be tracked 
        ///         by the context. If a reachable entity has its primary key value set
        ///         then it will be tracked in the <see cref="EntityState.Modified" /> state. If the primary key
        ///         value is not set then it will be tracked in the <see cref="EntityState.Added" /> state. 
        ///         An entity is considered to have its primary key value set if the primary key property is set 
        ///         to anything other than the CLR default for the property type.
        ///     </para>
        /// </summary>
        /// <param name="entities"> The entities to update. </param>
        public virtual void UpdateRange([NotNull] IEnumerable<object> entities)
            => SetEntityStates(Check.NotNull(entities, nameof(entities)), EntityState.Modified);

        /// <summary>
        ///     Begins tracking the given entity in the <see cref="EntityState.Deleted" /> state such that it will
        ///     be removed from the database when <see cref="SaveChanges()" /> is called.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If any of the entities are already tracked in the <see cref="EntityState.Added" /> state then the context will
        ///         stop tracking those entities (rather than marking them as <see cref="EntityState.Deleted" />) since those
        ///         entities were previously added to the context and do not exist in the database.
        ///     </para>
        ///     <para>
        ///         Any other reachable entities that are not already being tracked will be tracked in the same way that
        ///         they would be if <see cref="AttachRange(IEnumerable{object})" /> was called before calling this method.
        ///         This allows any cascading actions to be applied when <see cref="SaveChanges()" /> is called.
        ///     </para>
        /// </remarks>
        /// <param name="entities"> The entities to remove. </param>
        public virtual void RemoveRange([NotNull] IEnumerable<object> entities)
        {
            Check.NotNull(entities, nameof(entities));

            var stateManager = StateManager;

            // An Added entity does not yet exist in the database. If it is then marked as deleted there is
            // nothing to delete because it was not yet inserted, so just make sure it doesn't get inserted.
            foreach (var entity in entities)
            {
                var entry = stateManager.GetOrCreateEntry(entity);

                var initialState = entry.EntityState;
                if (initialState == EntityState.Detached)
                {
                    SetEntityState(entry, EntityState.Unchanged);
                }

                entry.SetEntityState(initialState == EntityState.Added
                    ? EntityState.Detached
                    : EntityState.Deleted);
            }
        }

        /// <summary>
        ///     提供了对当前上下文相关的数据库信息和操作的访问。
        /// </summary>
        public virtual DatabaseFacade Database => _database ?? (_database = new DatabaseFacade(this));

        /// <summary>
        ///     提供了对当前上下文正在跟踪的实体实例信息和操作的访问。
        /// </summary>
        public virtual ChangeTracker ChangeTracker
            => _changeTracker
               ?? (_changeTracker = InternalServiceProvider.GetRequiredService<IChangeTrackerFactory>().Create());

        /// <summary>
        ///     The metadata about the shape of entities, the relationships between them, and how they map to the database.
        /// </summary>
        public virtual IModel Model
            => _model
               ?? (_model = InternalServiceProvider.GetRequiredService<IModel>());

        /// <summary>
        ///     Creates a <see cref="DbSet{TEntity}" /> that can be used to query and save instances of <typeparamref name="TEntity" />.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity for which a set should be returned. </typeparam>
        /// <returns> A set for the given entity type. </returns>
        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            if (Model.FindEntityType(typeof(TEntity)) == null)
            {
                throw new InvalidOperationException(CoreStrings.InvalidSetType(typeof(TEntity).ShortDisplayName()));
            }

            return (_setInitializer
                    ?? (_setInitializer = InternalServiceProvider.GetRequiredService<IDbSetInitializer>())).CreateSet<TEntity>(this);
        }

        private IEntityFinder Finder(Type entityType)
            => (_entityFinderSource
                ?? (_entityFinderSource = InternalServiceProvider.GetRequiredService<IEntityFinderSource>())).Create(this, entityType);

        /// <summary>
        ///     使用给定的主键值查找一个实体。 
        ///     如果正在这被上下文跟踪的实体带有给定主键值，那么它将直接返回而不需要请求数据库。
        ///     否则，使用给定的主键值以及实体去查询数据库，
        ///     如果找到，它附加实体到上下文并返回。
        ///     如果没有找到那么返回空引用(<c>null</c>)。
        /// </summary>
        /// <param name="entityType"> 查找的实体类型。 </param>
        /// <param name="keyValues">将要查找的实体的主键值。</param>
        /// <returns>查找到的实体，或空引用（<c>null</c>）。</returns>
        public virtual object Find([NotNull] Type entityType, [NotNull] params object[] keyValues)
            => Finder(entityType).Find(keyValues);

        /// <summary>
        ///     Finds an entity with the given primary key values. If an entity with the given primary key values
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given primary key values
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <param name="entityType"> The type of entity to find. </param>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <returns>The entity found, or null.</returns>
        public virtual Task<object> FindAsync([NotNull] Type entityType, [NotNull] params object[] keyValues)
            => Finder(entityType).FindAsync(keyValues);

        /// <summary>
        ///     Finds an entity with the given primary key values. If an entity with the given primary key values
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given primary key values
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <param name="entityType"> The type of entity to find. </param>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        public virtual Task<object> FindAsync([NotNull] Type entityType, [NotNull] object[] keyValues, CancellationToken cancellationToken)
            => Finder(entityType).FindAsync(keyValues, cancellationToken);

        /// <summary>
        ///     Finds an entity with the given primary key values. If an entity with the given primary key values
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given primary key values
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity to find. </typeparam>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <returns>The entity found, or null.</returns>
        public virtual TEntity Find<TEntity>([NotNull] params object[] keyValues) where TEntity : class
            => ((IEntityFinder<TEntity>)Finder(typeof(TEntity))).Find(keyValues);

        /// <summary>
        ///     Finds an entity with the given primary key values. If an entity with the given primary key values
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given primary key values
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity to find. </typeparam>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <returns>The entity found, or null.</returns>
        public virtual Task<TEntity> FindAsync<TEntity>([NotNull] params object[] keyValues) where TEntity : class
            => ((IEntityFinder<TEntity>)Finder(typeof(TEntity))).FindAsync(keyValues);

        /// <summary>
        ///     Finds an entity with the given primary key values. If an entity with the given primary key values
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given primary key values
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity to find. </typeparam>
        /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        public virtual Task<TEntity> FindAsync<TEntity>([NotNull] object[] keyValues, CancellationToken cancellationToken) where TEntity : class
            => ((IEntityFinder<TEntity>)Finder(typeof(TEntity))).FindAsync(keyValues, cancellationToken);
    }
}
