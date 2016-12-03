// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     描述了实体形状、它们之间的关系以及它们是如何映射到数据库的元数据。
    ///     模型通常是通过在上下文派生类上重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 方法，
    ///     或使用 <see cref="ModelBuilder" />。
    /// </summary>
    public interface IModel : IAnnotatable
    {
        /// <summary>
        ///     获取定义在模型中的所有实体类型。
        /// </summary>
        /// <returns> 在模型中定义的所有实体类型。 </returns>
        IEnumerable<IEntityType> GetEntityTypes();

        /// <summary>
        ///     获取给定名称的实体类型。如果没有找到给定名称的实体类型则返回空引用（<c>null</c>）。
        /// </summary>
        /// <param name="name"> 用来查找实体类型的名称。 </param>
        /// <returns> 实体类型，或空引用（<c>null</c>，未找到时）。 </returns>
        IEntityType FindEntityType([NotNull] string name);
    }
}
