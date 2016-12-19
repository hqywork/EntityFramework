// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
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
    /// <typeparam name="TEntity"> 正在被当前项跟踪的实体类型。 </typeparam>
    public class EntityEntry<TEntity> : EntityEntry
        where TEntity : class
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public EntityEntry([NotNull] InternalEntityEntry internalEntry)
            : base(internalEntry)
        {
        }

        /// <summary>
        ///     获取当前项正在被跟踪的实体对象。
        /// </summary>
        public new virtual TEntity Entity => (TEntity)base.Entity;

        /// <summary>
        ///     提供了对实体的给定属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        /// <param name="propertyExpression">
        ///     一个 Lambda 表示式，表示将要访问信息及操作的属性
        ///     (<c>t => t.Property1</c>)。
        /// </param>
        /// <returns> 为给定属性公开了改变跟踪信息和操作的对象。 </returns>
        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(
            [NotNull] Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));

            return new PropertyEntry<TEntity, TProperty>(InternalEntry, propertyExpression.GetPropertyAccess().Name);
        }

        /// <summary>
        ///     提供了对实体的给定引用（即非集合）导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体的关联。
        /// </summary>
        /// <param name="propertyExpression">
        ///     一个 Lambda 表示式，表示将要访问信息及操作的属性
        ///     (<c>t => t.Property1</c>)。
        /// </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>(
            [NotNull] Expression<Func<TEntity, TProperty>> propertyExpression)
            where TProperty : class
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));

            return new ReferenceEntry<TEntity, TProperty>(InternalEntry, propertyExpression.GetPropertyAccess().Name);
        }

        /// <summary>
        ///     提供了对实体给定集合导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体集合的关联。
        /// </summary>
        /// <param name="propertyExpression">
        ///     一个 Lambda 表示式，表示将要访问信息及操作的属性
        ///     (<c>t => t.Property1</c>)。
        /// </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>(
            [NotNull] Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression)
            where TProperty : class
        {
            Check.NotNull(propertyExpression, nameof(propertyExpression));

            return new CollectionEntry<TEntity, TProperty>(InternalEntry, propertyExpression.GetPropertyAccess().Name);
        }

        /// <summary>
        ///     提供了对实体的给定引用（即非集合）导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体的关联。
        /// </summary>
        /// <param name="propertyName"> 导航属性的名称。 </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>([NotNull] string propertyName)
            where TProperty : class
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            return new ReferenceEntry<TEntity, TProperty>(InternalEntry, propertyName);
        }

        /// <summary>
        ///     提供了对实体给定集合导航属性的改变跟踪和加载信息的访问，
        ///     该属性用于这个实体与其它实体集合的关联。
        /// </summary>
        /// <param name="propertyName"> 导航属性的名称。 </param>
        /// <returns>
        ///     为给定导航属性公开了改变跟踪信息和操作的对象。
        ///     
        /// </returns>
        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>([NotNull] string propertyName)
            where TProperty : class
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            return new CollectionEntry<TEntity, TProperty>(InternalEntry, propertyName);
        }

        /// <summary>
        ///     提供了对实体的给定属性的改变跟踪和操作的访问。
        ///     
        /// </summary>
        /// <typeparam name="TProperty"> 属性的类型。 </typeparam>
        /// <param name="propertyName"> 将要访问信息及操作的属性。 </param>
        /// <returns> 为给定属性公开了改变跟踪信息和操作的对象。 </returns>
        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(
            [NotNull] string propertyName)
        {
            Check.NotEmpty(propertyName, nameof(propertyName));

            ValidateType<TProperty>(InternalEntry.EntityType.FindProperty(propertyName));

            return new PropertyEntry<TEntity, TProperty>(InternalEntry, propertyName);
        }

        private static void ValidateType<TProperty>(IProperty property)
        {
            if (property != null
                && property.ClrType != typeof(TProperty))
            {
                throw new ArgumentException(
                    CoreStrings.WrongGenericPropertyType(
                        property.Name,
                        property.DeclaringEntityType.DisplayName(),
                        property.ClrType.ShortDisplayName(),
                        typeof(TProperty).ShortDisplayName()));
            }
        }
    }
}
