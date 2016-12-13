// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     被 <see cref="DbContext" /> 使用的选项设置。
    ///     你通常是重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 或
    ///     使用 <see cref="DbContextOptionsBuilder{TContext}" /> 来创建这个类的实例。
    /// </summary>
    /// <typeparam name="TContext"> 这些选项被应用到的上下文类型。 </typeparam>
    public class DbContextOptions<TContext> : DbContextOptions
        where TContext : DbContext
    {
        /// <summary>
        ///     初始化 <see cref="DbContextOptions{TContext}" /> 的新实例。
        ///     你通常是重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 或
        ///     使用 <see cref="DbContextOptionsBuilder{TContext}" /> 来创建这个类的实例。
        /// </summary>
        public DbContextOptions()
            : base(new Dictionary<Type, IDbContextOptionsExtension>())
        {
        }

        /// <summary>
        ///     初始化 <see cref="DbContextOptions{TContext}" /> 的新实例。
        ///     你通常是重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 或
        ///     使用 <see cref="DbContextOptionsBuilder{TContext}" /> 来创建这个类的实例。
        /// </summary>
        /// <param name="extensions"> 存储配置选项的扩展。 </param>
        public DbContextOptions(
            [NotNull] IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
            : base(extensions)
        {
        }

        /// <summary>
        ///     添加给定扩展到选项。
        /// </summary>
        /// <typeparam name="TExtension"> 将要添加扩展的类型。 </typeparam>
        /// <param name="extension"> 将要添加的扩展。 </param>
        /// <returns> 当前选项设置实例，以便多个调用可以被链式调用。 </returns>
        public override DbContextOptions WithExtension<TExtension>(TExtension extension)
        {
            Check.NotNull(extension, nameof(extension));

            var extensions = Extensions.ToDictionary(p => p.GetType(), p => p);
            extensions[typeof(TExtension)] = extension;

            return new DbContextOptions<TContext>(extensions);
        }

        /// <summary>
        ///     这些选项服务的上下文类型（<typeparamref name="TContext" />）。
        /// </summary>
        public override Type ContextType => typeof(TContext);
    }
}
