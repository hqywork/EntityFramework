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
        ///     Initializes a new instance of the <see cref="DbContextOptions" /> class. You normally override
        ///     <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> or use a <see cref="DbContextOptionsBuilder" />
        ///     to create instances of this class and it is not designed to be directly constructed in your application code.
        /// </summary>
        /// <param name="extensions"> The extensions that store the configured options. </param>
        protected DbContextOptions(
            [NotNull] IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions)
        {
            Check.NotNull(extensions, nameof(extensions));

            _extensions = extensions;
        }

        /// <summary>
        ///     Gets the extensions that store the configured options.
        /// </summary>
        public virtual IEnumerable<IDbContextOptionsExtension> Extensions => _extensions.Values;

        /// <summary>
        ///     Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension, or null if none was found. </returns>
        public virtual TExtension FindExtension<TExtension>()
            where TExtension : class, IDbContextOptionsExtension
        {
            IDbContextOptionsExtension extension;
            return _extensions.TryGetValue(typeof(TExtension), out extension) ? (TExtension)extension : null;
        }

        /// <summary>
        ///     Gets the extension of the specified type. Throws if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension. </returns>
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
        ///     添加给定扩展到选项设置。
        /// </summary>
        /// <typeparam name="TExtension"> 将要添加扩展的类型。 </typeparam>
        /// <param name="extension"> 将要添加的扩展。 </param>
        /// <returns> 当前选项设置实例，以便多个调用可以被链式调用。 </returns>
        public abstract DbContextOptions WithExtension<TExtension>([NotNull] TExtension extension)
            where TExtension : class, IDbContextOptionsExtension;

        private readonly IReadOnlyDictionary<Type, IDbContextOptionsExtension> _extensions;

        /// <summary>
        ///     The type of context that these options are for. Will return <see cref="DbContext" /> if the
        ///     options are not built for a specific derived context.
        /// </summary>
        public abstract Type ContextType { get; }
    }
}
