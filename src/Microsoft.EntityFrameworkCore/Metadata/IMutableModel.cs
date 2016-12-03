// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     <para>
    ///         描述了实体形状、它们之间的关系以及它们是如何映射到数据库的元数据。A model is typically
    ///         模型通常是通过在上下文派生类上重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 方法，
    ///         或使用 <see cref="ModelBuilder" />。
    ///     </para>
    ///     <para>
    ///         这个接口在模型创建期间被使用，允许元数据被修改。
    ///         一旦模型被创建，<see cref="IModel" /> 代表同一元数据的一个只读视图。
    ///     </para>
    /// </summary>
    public interface IMutableModel : IModel, IMutableAnnotatable
    {
        /// <summary>
        ///     <para>
        ///         添加一个影像状态的实体类型到模型。
        ///     </para>
        ///     <para>
        ///         影像实体在 <see cref="DbContext" /> 运行时模型中目前是不被支持的。
        ///         因此，影像状态实体类型将仅存在于迁移模型快照中。
        ///     </para>
        /// </summary>
        /// <param name="name"> 将添加的实体名称。 </param>
        /// <returns> 新的实体类型。 </returns>
        IMutableEntityType AddEntityType([NotNull] string name);

        /// <summary>
        ///     添加一个实体类型到模型。
        /// </summary>
        /// <param name="clrType"> 代表这个实体类型的 CLR 类。 </param>
        /// <returns> 新的实体类型。 </returns>
        IMutableEntityType AddEntityType([CanBeNull] Type clrType);

        /// <summary>
        ///     获取给定名称的实体。如果未找到则返回空引用（<c>null</c>）。
        /// </summary>
        /// <param name="name"> 查找的实体类型名称。 </param>
        /// <returns> 实体类型，或空引用（<c>null</c>，未找到时） </returns>
        new IMutableEntityType FindEntityType([NotNull] string name);

        /// <summary>
        ///     从模型中移除一个实体类型。
        /// </summary>
        /// <param name="name"> 将要移除的实体类型名称。 </param>
        /// <returns> The entity type that was removed. </returns>
        IMutableEntityType RemoveEntityType([NotNull] string name);

        /// <summary>
        ///     获取在模型中定义的所有实体类型。
        /// </summary>
        /// <returns> 在模型中已定义的所有实体类型。 </returns>
        new IEnumerable<IMutableEntityType> GetEntityTypes();
    }
}
