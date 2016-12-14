// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         为给定上下文构造模型。
    ///         这是通过调用上下文上的 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 方法构造模型的默认实现。
    ///     </para>
    ///     <para>
    ///         这个类型通常是被数据库提供者（以及其它扩展）使用。
    ///         它一般不被应用程序代码使用。
    ///     </para>
    /// </summary>
    public class ModelCustomizer : IModelCustomizer
    {
        /// <summary>
        ///     除了根据约定发现的模型外，还可以对模型执行附加的配置。
        ///     此默认实现为给定的上下文构造模型，是通过调用上下文上的 <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)" /> 方法。
        ///     
        /// </summary>
        /// <param name="modelBuilder">
        ///     一个生成器，被用来构造模型。
        /// </param>
        /// <param name="dbContext">
        ///     将要创建的模型所在的上下文实例。
        /// </param>
        public virtual void Customize(ModelBuilder modelBuilder, DbContext dbContext) => dbContext.OnModelCreating(modelBuilder);
    }
}
