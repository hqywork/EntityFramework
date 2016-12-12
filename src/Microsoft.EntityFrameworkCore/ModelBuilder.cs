// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     <para>
    ///         提供了简单的 API 来配置 <see cref="IMutableModel" />，
    ///         它定义了你的实体形状，它们之间的关系以及它们如何映射到数据库。
    ///     </para>
    ///     <para>
    ///         你可以通过在你的上下文派生类上重载 <see cref="DbContext.OnModelCreating(ModelBuilder)" />，
    ///         使用 <see cref="ModelBuilder" /> 来为上下文配置一个模型。
    ///         你也可以创建一个外部的模型并设置在 <see cref="DbContextOptions" /> 实例上，传递这个实例到上下文的构造函数。
    ///     </para>
    /// </summary>
    public class ModelBuilder : IInfrastructure<InternalModelBuilder>
    {
        private readonly InternalModelBuilder _builder;

        /// <summary>
        ///     使用一组规则初始化 <see cref="ModelBuilder" /> 类的新实例。
        ///     
        /// </summary>
        /// <param name="conventions"> The conventions to be applied to the model. </param>
        public ModelBuilder([NotNull] ConventionSet conventions)
        {
            Check.NotNull(conventions, nameof(conventions));

            _builder = new InternalModelBuilder(new Model(conventions));
        }

        /// <summary>
        ///     The model being configured.
        /// </summary>
        public virtual IMutableModel Model => Builder.Metadata;

        /// <summary>
        ///     Adds or updates an annotation on the model. If an annotation with the key specified in
        ///     <paramref name="annotation" /> already exists it's value will be updated.
        /// </summary>
        /// <param name="annotation"> The key of the annotation to be added or updated. </param>
        /// <param name="value"> The value to be stored in the annotation. </param>
        /// <returns> The same <see cref="ModelBuilder" /> instance so that multiple configuration calls can be chained. </returns>
        public virtual ModelBuilder HasAnnotation([NotNull] string annotation, [NotNull] object value)
        {
            Check.NotEmpty(annotation, nameof(annotation));
            Check.NotNull(value, nameof(value));

            Builder.HasAnnotation(annotation, value, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         The internal <see cref="ModelBuilder" /> being used to configure this model.
        ///     </para>
        ///     <para>
        ///         This property is intended for use by extension methods to configure the model. It is not intended to be used in
        ///         application code.
        ///     </para>
        /// </summary>
        InternalModelBuilder IInfrastructure<InternalModelBuilder>.Instance => _builder;

        /// <summary>
        ///     返回一个可以被用来在模型中配置给定实体类型的对象。
        ///     如果实体类型不是模型一部分，它将被添加到模型。
        /// </summary>
        /// <typeparam name="TEntity"> 将要进行配置的实体类型。 </typeparam>
        /// <returns> 可用被用来配置实体类型的对象。 </returns>
        public virtual EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class
            => new EntityTypeBuilder<TEntity>(Builder.Entity(typeof(TEntity), ConfigurationSource.Explicit));

        /// <summary>
        ///     返回一个可以被用来在模型中配置给定实体类型的对象。
        ///     如果实体类型不是模型的一部分，它将被添加到模型。
        /// </summary>
        /// <param name="type"> 将要进行配置的实体类型。 </param>
        /// <returns> 可用被用来配置实体类型的对象。 </returns>
        public virtual EntityTypeBuilder Entity([NotNull] Type type)
        {
            Check.NotNull(type, nameof(type));

            return new EntityTypeBuilder(Builder.Entity(type, ConfigurationSource.Explicit));
        }

        /// <summary>
        ///     返回一个可以被用来在模型中配置给定实体类型的对象。
        ///     如果指定名称的实体类型不是模型的一部分，
        ///     一个没有对应 CLR 类型的新实体类型将被添加到模型。
        /// </summary>
        /// <param name="name"> 将被配置的实体类型名称。 </param>
        /// <returns> 可用被用来配置实体类型的对象。 </returns>
        public virtual EntityTypeBuilder Entity([NotNull] string name)
        {
            Check.NotEmpty(name, nameof(name));

            return new EntityTypeBuilder(Builder.Entity(name, ConfigurationSource.Explicit));
        }

        /// <summary>
        ///     <para>
        ///         在模型中执行给定实体类型的配置。
        ///         如果实体类型不是模型的一部分，它将被添加到模型。
        ///     </para>
        ///     <para>
        ///         这个重载允许在这个方法内部进行实体类型的配置，而不像 <see cref="Entity{TEntity}()" /> 调用后再进行链式访问。
        ///         这允许在实体类型配置后通过链式方式附加模型级别的配置。
        ///         
        ///     </para>
        /// </summary>
        /// <typeparam name="TEntity"> 将要进行配置的实体类型。 </typeparam>
        /// <param name="buildAction"> 执行实体类型配置的操作。 </param>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder Entity<TEntity>([NotNull] Action<EntityTypeBuilder<TEntity>> buildAction) where TEntity : class
        {
            Check.NotNull(buildAction, nameof(buildAction));

            buildAction(Entity<TEntity>());

            return this;
        }

        /// <summary>
        ///     <para>
        ///         在模型中执行给定实体类型的配置。
        ///         如果实体类型不是模型的一部分，它将被添加到模型。
        ///     </para>
        ///     <para>
        ///         这个重载允许在这个方法内部进行实体类型的配置，而不像 <see cref="Entity{TEntity}()" /> 调用后再进行链式访问。
        ///         这允许在实体类型配置后通过链式方式附加模型级别的配置。
        ///         
        ///     </para>
        /// </summary>
        /// <param name="type"> 将要进行配置的实体类型。 </param>
        /// <param name="buildAction"> 执行实体类型配置的操作。 </param>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder Entity([NotNull] Type type, [NotNull] Action<EntityTypeBuilder> buildAction)
        {
            Check.NotNull(type, nameof(type));
            Check.NotNull(buildAction, nameof(buildAction));

            buildAction(Entity(type));

            return this;
        }

        /// <summary>
        ///     <para>
        ///         在模型中执行给定实体类型的配置。
        ///         如果指定名称的实体类型不是模型的一部分，
        ///         一个没有对应 CLR 类型的新实体类型将被添加到模型。
        ///     </para>
        ///     <para>
        ///         这个重载允许在这个方法内部进行实体类型的配置，而不像 <see cref="Entity(string)" /> 调用后再进行链式访问。
        ///         这允许在实体类型配置后通过链式方式附加模型级别的配置。
        ///         
        ///     </para>
        /// </summary>
        /// <param name="name"> 将被配置的实体类型名称。 </param>
        /// <param name="buildAction"> 执行实体类型配置的操作。 </param>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder Entity([NotNull] string name, [NotNull] Action<EntityTypeBuilder> buildAction)
        {
            Check.NotEmpty(name, nameof(name));
            Check.NotNull(buildAction, nameof(buildAction));

            buildAction(Entity(name));

            return this;
        }

        /// <summary>
        ///     从模型中排队给定实体类型。
        ///     这个方法通常被用来从已添加规则的模型中移除类型。
        /// </summary>
        /// <typeparam name="TEntity"> 将要从模型中移除的实体类型。 </typeparam>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder Ignore<TEntity>() where TEntity : class
            => Ignore(typeof(TEntity));

        /// <summary>
        ///     从模型中排队给定实体类型。
        ///     这个方法通常被用来从已添加规则的模型中移除类型。
        /// </summary>
        /// <param name="type"> 将要从模型中移除的实体类型。 </param>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder Ignore([NotNull] Type type)
        {
            Check.NotNull(type, nameof(type));

            Builder.Ignore(type, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures the default <see cref="ChangeTrackingStrategy" /> to be used for this model.
        ///     This strategy indicates how the context detects changes to properties for an instance of an entity type.
        /// </summary>
        /// <param name="changeTrackingStrategy"> The change tracking strategy to be used. </param>
        /// <returns>
        ///     The same <see cref="ModelBuilder" /> instance so that additional configuration calls can be chained.
        /// </returns>
        public virtual ModelBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy)
        {
            Builder.Metadata.ChangeTrackingStrategy = changeTrackingStrategy;

            return this;
        }

        /// <summary>
        ///     <para>
        ///         为实体类型的所有属性设置要使用的 <see cref="PropertyAccessMode" />。
        ///     </para>
        ///     <para>
        ///         默认情况下，如果是通过规则发现或被指定，那么支持字段将被使用。
        ///         通常是实体是被从数据库中查询，一个新对象被构造时。
        ///         属性被所有其他访问使用。
        ///         调用这个方法将改变模型中所有属性的行为（在 <see cref="PropertyAccessMode" /> 枚举中进行了描述）。
        ///         
        ///     </para>
        /// </summary>
        /// <param name="propertyAccessMode"> 这个模型的属性要使用的 <see cref="PropertyAccessMode" /> </param>
        /// <returns>
        ///     <see cref="ModelBuilder" /> 实例，以便后序以链式方式附加配置。
        /// </returns>
        public virtual ModelBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode)
        {
            Builder.UsePropertyAccessMode(propertyAccessMode, ConfigurationSource.Explicit);

            return this;
        }

        private InternalModelBuilder Builder => this.GetInfrastructure();
    }
}
