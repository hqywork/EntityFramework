// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         用于 <see cref="IInfrastructure{T}" /> 的扩展方法。
    ///     </para>
    ///     <para>
    ///         这些方法通常被数据库提供者（以及其它扩展）使用。
    ///         它一般不被应用程序代码使用。
    ///     </para>
    ///     <para>
    ///         <see cref="IInfrastructure{T}" /> 是用来隐藏属性，原打算不能被应用程序代码使用，
    ///         但可以被使用来为数据库提供者编写扩展方法是。
    ///     </para>
    /// </summary>
    public static class AccessorExtensions
    {
        /// <summary>
        ///     <para>
        ///         从 <see cref="IInfrastructure{IServiceProvider}" /> 的实现类型中公开的 <see cref="IServiceProvider" /> 中解析一个服务。
        ///         
        ///     </para>
        ///     <para>
        ///         这个类型通常是被数据库提供者（以及其它扩展）使用。
        ///         它一般不被应用程序代码使用。
        ///     </para>
        ///     <para>
        ///         <see cref="IInfrastructure{T}" /> 是用来隐藏属性，原打算不能被应用程序代码使用，
        ///         但可以被使用来为数据库提供者编写扩展方法是。
        ///     </para>
        /// </summary>
        /// <typeparam name="TService"> 将要被解析服务的类型。 </typeparam>
        /// <param name="accessor"> 公开了服务提供者的对象。 </param>
        /// <returns> 所需的服务。 </returns>
        public static TService GetService<TService>([NotNull] this IInfrastructure<IServiceProvider> accessor)
        {
            Check.NotNull(accessor, nameof(accessor));

            var service = accessor.Instance.GetService<TService>();
            if (service == null)
            {
                throw new InvalidOperationException(
                    CoreStrings.NoProviderConfiguredFailedToResolveService(typeof(TService).DisplayName()));
            }

            return service;
        }

        /// <summary>
        ///     <para>
        ///         获取被 <see cref="IInfrastructure{T}" /> 隐藏起来的属性的值。
        ///     </para>
        ///     <para>
        ///         这个类型通常是被数据库提供者（以及其它扩展）使用。
        ///         它一般不被应用程序代码使用。
        ///     </para>
        ///     <para>
        ///         <see cref="IInfrastructure{T}" /> 是用来隐藏属性，原打算不能被应用程序代码使用，
        ///         但可以被使用来为数据库提供者编写扩展方法是。
        ///     </para>
        /// </summary>
        /// <typeparam name="T"> 被 <see cref="IInfrastructure{T}" /> 隐藏起来的属性的类型。 </typeparam>
        /// <param name="accessor"> 公开属性的对象。 </param>
        /// <returns> 指向属性的对象。 </returns>
        public static T GetInfrastructure<T>([NotNull] this IInfrastructure<T> accessor)
            => Check.NotNull(accessor, nameof(accessor)).Instance;
    }
}
