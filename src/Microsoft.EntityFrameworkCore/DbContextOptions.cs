// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     被 <see cref="DbContext" /> 使用的选项设置。
    ///     你通常是重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 或
    ///     使用 <see cref="DbContextOptionsBuilder" /> 来创建这个类的实例，并且它不能在你的应用程序代码中被直接实例化。
    /// </summary>
    public abstract class DbContextOptions : IDbContextOptions
    {
        /// <summary>
        ///     初始化 <see cref="DbContextOptions" /> 的新实例。Initializes a new instance of the <see cref="DbContextOptions" /> class. 
        ///     你通常重载 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 或使用 <see cref="DbContextOptionsBuilder" />
        ///     来创建这个类的实例。并且它被设计为不能在应用程序代码中直接构造。
        /// </summary>
        /// <param name="extensions"> 存储配置选项的扩展。 </param>
        protected DbContextOptions(
            [NotNull] IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
        {
            Check.NotNull(extensions, nameof(extensions));

            _extensions = extensions;
        }

        /// <summary>
        ///     获取存储配置选项的扩展。
        /// </summary>
        public virtual IEnumerable<IDbContextOptionsExtension> Extensions => _extensions.Values;

        /// <summary>
        ///     获取指定类型的扩展。如果指定类型的扩展未配置则返回空引用（<c>null</c>）。
        /// </summary>
        /// <typeparam name="TExtension"> 要获取扩展的类型。 </typeparam>
        /// <returns> 扩展对象，或空引用（<c>null</c>，未找到时）。 </returns>
        public virtual TExtension FindExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension
        {
            IDbContextOptionsExtension extension;
            return _extensions.TryGetValue(typeof(TExtension), out extension) ? (TExtension)extension : null;
        }

        /// <summary>
        ///     获取指定类型的扩展。如果指定类型的扩展未配置则抛出异常。
        /// </summary>
        /// <typeparam name="TExtension"> 要获取扩展的类型。 </typeparam>
        /// <returns> 扩展对象。 </returns>
        public virtual TExtension GetExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension
        {
            var extension = FindExtension<TExtension>();
            if (extension == null)
            {
                throw new InvalidOperationException(CoreStrings.OptionsExtensionNotFound(typeof(TExtension).ShortDisplayName()));
            }
            return extension;
        }

        /// <summary>
        ///     添加给定扩展到选项。
        /// </summary>
        /// <typeparam name="TExtension"> 将要添加扩展的类型。 </typeparam>
        /// <param name="extension"> 将要添加的扩展。 </param>
        /// <returns> 当前选项设置实例，以便多个调用可以被链式调用。 </returns>
        public abstract DbContextOptions WithExtension<TExtension>([NotNull] TExtension extension)
            where TExtension : class, IDbContextOptionsExtension;

        private readonly IReadOnlyDictionary<Type, IDbContextOptionsExtension> _extensions;

        /// <summary>
        ///     这些选项服务的上下文类型。
        ///     如果选项不是为指定派生上下文服务的，那么将返回 <see cref="DbContext" />。
        /// </summary>
        public abstract Type ContextType { get; }
    }
}
