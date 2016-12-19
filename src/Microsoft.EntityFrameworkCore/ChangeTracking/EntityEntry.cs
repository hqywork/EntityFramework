// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.ChangeTracking
{
    /// <summary>
    ///     <para>
    ///         提供了对给定实体的改变跟踪信息和操作的访问。
    ///     </para>
    ///     <para>
    ///         当使用 <see cref="ChangeTracker" /> API 时，这个类的实例被返回，
    ///         它不是被设计为在你的应用程序代码中直接构造。
    ///     </para>
    /// </summary>
    [DebuggerDisplay("{InternalEntry,nq}")]
    public class EntityEntry : IInfrastructure<InternalEntityEntry>
    {
        private static readonly int _maxEntityState = Enum.GetValues(typeof(EntityState)).Cast<int>().Max();

        private IEntityFinderSource _entityFinderSource;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual InternalEntityEntry InternalEntry { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public EntityEntry([NotNull] InternalEntityEntry internalEntry)
        {
            Check.NotNull(internalEntry, nameof(internalEntry));

            InternalEntry = internalEntry;
        }

        /// <summary>
        ///     获取当前项正在被跟踪的实体对象。
        /// </summary>
        public virtual object Entity => InternalEntry.Entity;

        /// <summary>
        ///     <para>
        ///         获取或设置正在被跟踪的实体状态。
        ///     </para>
        ///     <para>
        ///         当设置状态时，实体通常将以指定状态结束。For example, if you
        ///         例如，如果你将实体状态改变为 <see cref="EntityState.Deleted" />，那么无论实体的当前状态是什么都将被标记为删除。
        ///         这与调用 <see cref="DbSet{TEntity}.Remove(TEntity)" /> 是不同时，
        ///         如果它处在 <see cref="EntityState.Added" /> 状态，那它将与实体断开（而不是标记为删除）。
        ///     </para>
        /// </summary>
        public virtual EntityState State
        {
            get { return InternalEntry.EntityState; }
            set
            {
                if (value < 0
                    || (int)value > _maxEntityState)
                {
                    throw new ArgumentException(CoreStrings.InvalidEnumValue(nameof(value), typeof(EntityState)));
                }

                InternalEntry.SetEntityState(value);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        InternalEntityEntry IInfrastructure<InternalEntityEntry>.Instance => InternalEntry;

        /// <summary>
        ///     获取正在跟踪的实体所在的上下文。
        /// </summary>
        public virtual DbContext Context => InternalEntry.StateManager.Context;

        /// <summary>
        ///     获取有关实体形状、与其它实体的关系以及如果映射到数据库的元数据。
        /// </summary>
        public virtual IEntityType Metadata => InternalEntry.EntityType;

        /// <summary>
        ///     提供了对实体的给定属性或导航属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        /// <param name="propertyName"> 将要访问信息和操作的属性。 </param>
        /// <returns> 为给定属性公开了改变跟踪信息和操作的对象。 </returns>
        public virtual MemberEntry Member([NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            var property = InternalEntry.EntityType.FindProperty(propertyName);
            if (property != null)
            {
                return new PropertyEntry(InternalEntry, propertyName);
            }

            var navigation = InternalEntry.EntityType.FindNavigation(propertyName);
            if (navigation != null)
            {
                return navigation.IsCollection()
                    ? (MemberEntry)new CollectionEntry(InternalEntry, propertyName)
                    : new ReferenceEntry(InternalEntry, propertyName);
            }

            throw new InvalidOperationException(
                CoreStrings.PropertyNotFound(propertyName, InternalEntry.EntityType.DisplayName()));
        }

        /// <summary>
        ///     提供了对实体的给定属性或导航属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        public virtual IEnumerable<MemberEntry> Members
            => Properties.Cast<MemberEntry>().Concat(Navigations);

        /// <summary>
        ///     提供了对实体的给定导航属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        /// <param name="propertyName"> 将要访问信息和操作的属性。 </param>
        /// <returns> 为给定属性公开了改变跟踪信息和操作的对象。 </returns>
        public virtual NavigationEntry Navigation([NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            var navigation = InternalEntry.EntityType.FindNavigation(propertyName);
            if (navigation != null)
            {
                return navigation.IsCollection()
                    ? (NavigationEntry)new CollectionEntry(InternalEntry, propertyName)
                    : new ReferenceEntry(InternalEntry, propertyName);
            }

            if (InternalEntry.EntityType.FindProperty(propertyName) != null)
            {
                throw new InvalidOperationException(
                    CoreStrings.NavigationIsProperty(propertyName, InternalEntry.EntityType.DisplayName(),
                        nameof(Reference), nameof(Collection), nameof(Property)));
            }

            throw new InvalidOperationException(
                CoreStrings.PropertyNotFound(propertyName, InternalEntry.EntityType.DisplayName()));
        }

        /// <summary>
        ///     提供了对实体的所有导航属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        public virtual IEnumerable<NavigationEntry> Navigations
            => InternalEntry.EntityType.GetNavigations().Select(navigation => navigation.IsCollection()
                ? (NavigationEntry)new CollectionEntry(InternalEntry, navigation)
                : new ReferenceEntry(InternalEntry, navigation));

        /// <summary>
        ///     提供了对实体的给定属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        /// <param name="propertyName"> The property to access information and operations for. </param>
        /// <returns> An object that exposes change tracking information and operations for the given property. </returns>
        public virtual PropertyEntry Property([NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            return new PropertyEntry(InternalEntry, propertyName);
        }

        /// <summary>
        ///     提供了对实体的所有属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        public virtual IEnumerable<PropertyEntry> Properties
            => InternalEntry.EntityType.GetProperties().Select(property => new PropertyEntry(InternalEntry, property));

        /// <summary>
        ///     提供了对实体的给定引用（即非集合）导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体的关联。
        ///     
        /// </summary>
        /// <param name="propertyName"> 导航属性的名称。 </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual ReferenceEntry Reference([NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            return new ReferenceEntry(InternalEntry, propertyName);
        }

        /// <summary>
        ///     提供了对实体所有引用（即非集合）导航属性的改变跟踪和加载信息的访问。
        ///     
        /// </summary>
        public virtual IEnumerable<ReferenceEntry> References
            => InternalEntry.EntityType.GetNavigations().Where(n => !n.IsCollection())
                .Select(navigation => new ReferenceEntry(InternalEntry, navigation));

        /// <summary>
        ///     提供了对实体给定集合导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体集合的关联。
        /// </summary>
        /// <param name="propertyName"> 导航属性的名称。 </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual CollectionEntry Collection([NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            return new CollectionEntry(InternalEntry, propertyName);
        }

        /// <summary>
        ///     提供了对实体所有集合导航属性的改变跟踪和加载信息的访问，
        ///     
        /// </summary>
        public virtual IEnumerable<CollectionEntry> Collections
            => InternalEntry.EntityType.GetNavigations().Where(n => n.IsCollection())
                .Select(navigation => new CollectionEntry(InternalEntry, navigation));

        /// <summary>
        ///     获取一个值，指示实体的键值是否已被分派值。
        ///     如果一个或多个键属性被分派了 null 或 CLR 默认值则为假（<c>false</c>），否则为真（<c>true</c>）。
        ///     
        /// </summary>
        public virtual bool IsKeySet => InternalEntry.IsKeySet;

        /// <summary>
        ///     获取实体的当前属性值。
        /// </summary>
        /// <value> 当前值。 </value>
        public virtual PropertyValues CurrentValues => new CurrentPropertyValues(InternalEntry);

        /// <summary>
        ///     获取实体的原始属性值。
        ///     原始值是从数据库中取回实体时得到的属性值。
        ///     
        /// </summary>
        /// <value> 原始值。 </value>
        public virtual PropertyValues OriginalValues => new OriginalPropertyValues(InternalEntry);

        /// <summary>
        ///     <para>
        ///         为让跟踪实体的值的复本与当前数据库中已存在的一致而查询数据库。
        ///         如果在数据库中找不到实体，那么返回 null。
        ///     </para>
        ///     <para>
        ///         注意，在返回字典中改变的值将不会更新数据库中的值。
        ///         
        ///     </para>
        /// </summary>
        /// <returns> 存储值或 null（在实体在数据库中不存在时）。 </returns>
        public virtual PropertyValues GetDatabaseValues()
        {
            var values = Finder.GetDatabaseValues(InternalEntry);

            return values == null ? null : new ArrayPropertyValues(InternalEntry, values);
        }

        /// <summary>
        ///     <para>
        ///         为让跟踪实体的值的复本与当前数据库中已存在的一致而查询数据库。
        ///         如果在数据库中找不到实体，那么返回 null。
        ///     </para>
        ///     <para>
        ///         注意，在返回字典中改变的值将不会更新数据库中的值。
        ///         
        ///     </para>
        ///     <para>
        ///         在相同上下文实例上的多个激活操作是不被支持的。
        ///         使用 'await' 来确保调用这个上下文上的其它方法前任何异步操作都已完成。
        ///     </para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     观察等待任务完成的 <see cref="CancellationToken" />。
        /// </param>
        /// <returns>
        ///     表示异步操作的任务。任务结果包含了存储值或 null（当实体不存在于数据库时）。
        ///     
        /// </returns>
        public virtual async Task<PropertyValues> GetDatabaseValuesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var values = await Finder.GetDatabaseValuesAsync(InternalEntry, cancellationToken);

            return values == null ? null : new ArrayPropertyValues(InternalEntry, values);
        }

        /// <summary>
        ///     <para>
        ///         从数据库重新加载实体，使用数据库中的值覆盖任何的属性值。
        ///     </para>
        ///     <para>
        ///         调用了这个方法后实体将处于 <see cref="EntityState.Unchanged" /> 状态，
        ///         除非该实体在数据库中不存在，
        ///         在这种情况下实体将处于 <see cref="EntityState.Detached" /> 状态。
        ///         最后，在 <see cref="EntityState.Added" /> 状态的实体（在数据库中不存在）上调用 Reload 将执行空操作。
        ///         注意，添加的实体可能还尚未创建其持久键的值。
        ///     </para>
        /// </summary>
        public virtual void Reload() => Reload(GetDatabaseValues());

        /// <summary>
        ///     <para>
        ///         从数据库重新加载实体，使用数据库中的值覆盖任何的属性值。
        ///     </para>
        ///     <para>
        ///         调用了这个方法后实体将处于 <see cref="EntityState.Unchanged" /> 状态，
        ///         除非该实体在数据库中不存在，
        ///         在这种情况下实体将处于 <see cref="EntityState.Detached" /> 状态。
        ///         最后，在 <see cref="EntityState.Added" /> 状态的实体（在数据库中不存在）上调用 Reload 将执行空操作。
        ///         注意，添加的实体可能还尚未创建其持久键的值。
        ///     </para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     观察等待任务完成的 <see cref="CancellationToken" />。
        /// </param>
        /// <returns>
        ///     表示异步操作的任务。
        /// </returns>
        public virtual async Task ReloadAsync(CancellationToken cancellationToken = default(CancellationToken))
            => Reload(await GetDatabaseValuesAsync(cancellationToken));

        private void Reload(PropertyValues storeValues)
        {
            if (storeValues == null)
            {
                if (State != EntityState.Added)
                {
                    State = EntityState.Detached;
                }
            }
            else
            {
                CurrentValues.SetValues(storeValues);
                OriginalValues.SetValues(storeValues);
                State = EntityState.Unchanged;
            }
        }

        private IEntityFinder Finder
            => (_entityFinderSource
                ?? (_entityFinderSource = InternalEntry.StateManager.Context.GetService<IEntityFinderSource>()))
                .Create(InternalEntry.StateManager.Context, InternalEntry.EntityType.ClrType);
    }
}
