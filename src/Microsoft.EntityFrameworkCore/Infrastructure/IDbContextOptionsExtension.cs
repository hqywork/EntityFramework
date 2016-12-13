// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         可被存储到 <see cref="DbContextOptions.Extensions" /> 中的扩展接口。
    ///     </para>
    ///     <para>
    ///         这个接口通常被数据库提供者（以及其它扩展）使用。This interface is typically used by database providers (and other extensions). It is generally
    ///         通常不被应用程序代码使用。
    ///     </para>
    /// </summary>
    public interface IDbContextOptionsExtension
    {
        /// <summary>
        ///     添加所需服务来完成待定的选项工作。
        ///     当 EF 没有外部 <see cref="IServiceProvider" /> 并且自己维护内部服务提供者时被使用。
        ///     这允许数据提供者（以及其它扩展）在 EF 创建服务提供者时注册它们所需的服务。
        /// </summary>
        /// <param name="services"> 由将要添加到扩展中的服务构成的集合。 </param>
        void ApplyServices([NotNull] IServiceCollection services);
    }
}
