// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions
{
    /// <summary>
    ///     规则集的基本实现被用来构建一个模型。这个基本实现是空规则集。
    /// </summary>
    public class ConventionSet
    {
        /// <summary>
        ///     当实体类型被添加到模型后规则运行。
        /// </summary>
        public virtual IList<IEntityTypeConvention> EntityTypeAddedConventions { get; } = new List<IEntityTypeConvention>();

        /// <summary>
        ///     当实体类型被忽略后规则运行。
        /// </summary>
        public virtual IList<IEntityTypeIgnoredConvention> EntityTypeIgnoredConventions { get; } = new List<IEntityTypeIgnoredConvention>();

        /// <summary>
        ///     当属性被忽略后规则被运行。
        /// </summary>
        public virtual IList<IEntityTypeMemberIgnoredConvention> EntityTypeMemberIgnoredConventions { get; } = new List<IEntityTypeMemberIgnoredConvention>();

        /// <summary>
        ///     当基础实体类型被设置或移除后规则运行。
        /// </summary>
        public virtual IList<IBaseTypeConvention> BaseEntityTypeSetConventions { get; } = new List<IBaseTypeConvention>();

        /// <summary>
        ///     当实体类型上的标注被设置或移除后规则运行。
        /// </summary>
        public virtual IList<IEntityTypeAnnotationSetConvention> EntityTypeAnnotationSetConventions { get; }
            = new List<IEntityTypeAnnotationSetConvention>();

        /// <summary>
        ///     当外键被添加后规则运行。
        /// </summary>
        public virtual IList<IForeignKeyConvention> ForeignKeyAddedConventions { get; } = new List<IForeignKeyConvention>();

        /// <summary>
        ///     当外键被移除后规则运行。
        /// </summary>
        public virtual IList<IForeignKeyRemovedConvention> ForeignKeyRemovedConventions { get; } = new List<IForeignKeyRemovedConvention>();

        /// <summary>
        ///     当键被添加后规则运行。
        /// </summary>
        public virtual IList<IKeyConvention> KeyAddedConventions { get; } = new List<IKeyConvention>();

        /// <summary>
        ///     当键被移除规则运行。
        /// </summary>
        public virtual IList<IKeyRemovedConvention> KeyRemovedConventions { get; } = new List<IKeyRemovedConvention>();

        /// <summary>
        ///     当主键被配置后规则运行。
        /// </summary>
        public virtual IList<IPrimaryKeyConvention> PrimaryKeySetConventions { get; } = new List<IPrimaryKeyConvention>();

        /// <summary>
        ///     当索引被添加后规则运行。
        /// </summary>
        public virtual IList<IIndexConvention> IndexAddedConventions { get; } = new List<IIndexConvention>();

        /// <summary>
        ///     当索引被移除后规则运行。
        /// </summary>
        public virtual IList<IIndexRemovedConvention> IndexRemovedConventions { get; } = new List<IIndexRemovedConvention>();

        /// <summary>
        ///     当索引的唯一性被改变后规则运行。
        /// </summary>
        public virtual IList<IIndexUniquenessConvention> IndexUniquenessConventions { get; } = new List<IIndexUniquenessConvention>();

        /// <summary>
        ///     当关系的主要端点被配置后规则运行。
        /// </summary>
        public virtual IList<IPrincipalEndConvention> PrincipalEndSetConventions { get; } = new List<IPrincipalEndConvention>();

        /// <summary>
        ///     当模型构建完成后规则运行。
        /// </summary>
        public virtual IList<IModelConvention> ModelBuiltConventions { get; } = new List<IModelConvention>();

        /// <summary>
        ///     Conventions to run to setup the initial model.
        /// </summary>
        public virtual IList<IModelConvention> ModelInitializedConventions { get; } = new List<IModelConvention>();

        /// <summary>
        ///     当导航属性被添加后规则运行。
        /// </summary>
        public virtual IList<INavigationConvention> NavigationAddedConventions { get; } = new List<INavigationConvention>();

        /// <summary>
        ///     当导航属性被移除后规则运行。
        /// </summary>
        public virtual IList<INavigationRemovedConvention> NavigationRemovedConventions { get; } = new List<INavigationRemovedConvention>();

        /// <summary>
        ///     当外键的唯一性被改变后规则运行。
        /// </summary>
        public virtual IList<IForeignKeyUniquenessConvention> ForeignKeyUniquenessConventions { get; } = new List<IForeignKeyUniquenessConvention>();

        /// <summary>
        ///     当属性被添中后规则运行。
        /// </summary>
        public virtual IList<IPropertyConvention> PropertyAddedConventions { get; } = new List<IPropertyConvention>();

        /// <summary>
        ///     当属性的可为空值被改变后规则运行。
        /// </summary>
        public virtual IList<IPropertyNullableConvention> PropertyNullableChangedConventions { get; } = new List<IPropertyNullableConvention>();

        /// <summary>
        ///     当属性的字段被改变后规则运行。Conventions to run when the field of a property is changed.
        /// </summary>
        public virtual IList<IPropertyFieldChangedConvention> PropertyFieldChangedConventions { get; } =
            new List<IPropertyFieldChangedConvention>();
    }
}
