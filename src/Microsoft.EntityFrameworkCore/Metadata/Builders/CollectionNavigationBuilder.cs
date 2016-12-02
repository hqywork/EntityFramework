// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    ///     <para>
    ///         Provides a simple API for configuring a relationship where configuration began on
    ///         an end of the relationship with a collection that contains instances of another entity type.
    ///     </para>
    ///     <para>
    ///         Instances of this class are returned from methods when using the <see cref="ModelBuilder" /> API
    ///         and it is not designed to be directly constructed in your application code.
    ///     </para>
    /// </summary>
    public class CollectionNavigationBuilder : IInfrastructure<InternalRelationshipBuilder>
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public CollectionNavigationBuilder(
            [NotNull] EntityType declaringEntityType,
            [NotNull] EntityType relatedEntityType,
            [CanBeNull] string navigationName,
            [NotNull] InternalRelationshipBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            DeclaringEntityType = declaringEntityType;
            RelatedEntityType = relatedEntityType;
            CollectionName = navigationName;
            Builder = builder;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public CollectionNavigationBuilder(
            [NotNull] EntityType declaringEntityType,
            [NotNull] EntityType relatedEntityType,
            [CanBeNull] PropertyInfo navigationProperty,
            [NotNull] InternalRelationshipBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            DeclaringEntityType = declaringEntityType;
            RelatedEntityType = relatedEntityType;
            CollectionProperty = navigationProperty;
            CollectionName = navigationProperty?.Name;
            Builder = builder;
        }

        private InternalRelationshipBuilder Builder { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual string CollectionName { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual PropertyInfo CollectionProperty { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual EntityType RelatedEntityType { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual EntityType DeclaringEntityType { get; }

        /// <summary>
        ///     <para>
        ///         Gets the internal builder being used to configure the relationship.
        ///     </para>
        ///     <para>
        ///         This property is intended for use by extension methods that need to make use of services
        ///         not directly exposed in the public API surface.
        ///     </para>
        /// </summary>
        InternalRelationshipBuilder IInfrastructure<InternalRelationshipBuilder>.Instance => Builder;

        /// <summary>
        ///     配置一对多关系。
        /// </summary>
        /// <param name="navigationName">
        ///     在关系的另一端引用导航属性的名称。
        ///     如果为空引用（<c>null</c>），则表示在关系的另一端没有导航属性。
        /// </param>
        /// <returns> 可用来深层次配置关系的对象。 </returns>
        public virtual ReferenceCollectionBuilder WithOne([CanBeNull] string navigationName = null)
            => new ReferenceCollectionBuilder(
                DeclaringEntityType,
                RelatedEntityType,
                WithOneBuilder(Check.NullButNotEmpty(navigationName, nameof(navigationName))));

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual InternalRelationshipBuilder WithOneBuilder([CanBeNull] string navigationName)
            => WithOneBuilder(PropertyIdentity.Create(navigationName));

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual InternalRelationshipBuilder WithOneBuilder([CanBeNull] PropertyInfo navigationProperty)
            => WithOneBuilder(PropertyIdentity.Create(navigationProperty));

        private InternalRelationshipBuilder WithOneBuilder(PropertyIdentity reference)
        {
            var foreingKey = Builder.Metadata;
            var referenceName = reference.Name;
            if (referenceName != null
                && foreingKey.DependentToPrincipal != null
                && foreingKey.GetDependentToPrincipalConfigurationSource() == ConfigurationSource.Explicit
                && foreingKey.DependentToPrincipal.Name != referenceName)
            {
                throw new InvalidOperationException(CoreStrings.ConflictingRelationshipNavigation(
                    foreingKey.PrincipalEntityType.DisplayName(),
                    foreingKey.PrincipalToDependent.Name,
                    foreingKey.DeclaringEntityType.DisplayName(),
                    referenceName,
                    foreingKey.PrincipalEntityType.DisplayName(),
                    foreingKey.PrincipalToDependent.Name,
                    foreingKey.DeclaringEntityType.DisplayName(),
                    foreingKey.DependentToPrincipal.Name));
            }

            if (referenceName != null
                && RelatedEntityType != foreingKey.DeclaringEntityType)
            {
                return reference.Property == null && CollectionProperty == null
                    ? Builder.Navigations(reference.Name, CollectionName, DeclaringEntityType, RelatedEntityType, ConfigurationSource.Explicit)
                    : Builder.Navigations(reference.Property, CollectionProperty, DeclaringEntityType, RelatedEntityType, ConfigurationSource.Explicit);
            }

            return reference.Property == null
                ? Builder.DependentToPrincipal(reference.Name, ConfigurationSource.Explicit)
                : Builder.DependentToPrincipal(reference.Property, ConfigurationSource.Explicit);
        }
    }
}
