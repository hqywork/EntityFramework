// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    ///     <para>
    ///         Provides a simple API for configuring a <see cref="Property" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are returned from methods when using the <see cref="ModelBuilder" /> API
    ///         and it is not designed to be directly constructed in your application code.
    ///     </para>
    /// </summary>
    public class PropertyBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalPropertyBuilder>
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public PropertyBuilder([NotNull] InternalPropertyBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            Builder = builder;
        }

        /// <summary>
        ///     The internal builder being used to configure the property.
        /// </summary>
        InternalPropertyBuilder IInfrastructure<InternalPropertyBuilder>.Instance => Builder;

        /// <summary>
        ///     The property being configured.
        /// </summary>
        public virtual IMutableProperty Metadata => Builder.Metadata;

        /// <summary>
        ///     The model that the property belongs to.
        /// </summary>
        IMutableModel IInfrastructure<IMutableModel>.Instance => Builder.ModelBuilder.Metadata;

        /// <summary>
        ///     Adds or updates an annotation on the property. If an annotation with the key specified in
        ///     <paramref name="annotation" /> already exists it's value will be updated.
        /// </summary>
        /// <param name="annotation"> The key of the annotation to be added or updated. </param>
        /// <param name="value"> The value to be stored in the annotation. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasAnnotation([NotNull] string annotation, [NotNull] object value)
        {
            Check.NotEmpty(annotation, nameof(annotation));
            Check.NotNull(value, nameof(value));

            Builder.HasAnnotation(annotation, value, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     配置属性是否可以为空值。
        ///     如果属性是基于可以指定空值的 CLR 类型，那它仅可以被配置为非必须值。
        ///     
        /// </summary>
        /// <param name="required"> 一个布尔值，指示属性是否为必须的。 </param>
        /// <returns> 当前生成器实例，以便多个配置可以进行链式调用。 </returns>
        public virtual PropertyBuilder IsRequired(bool required = true)
        {
            Builder.IsRequired(required, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures the maximum length of data that can be stored in this property.
        ///     Maximum length can only be set on array properties (including <see cref="string" /> properties).
        /// </summary>
        /// <param name="maxLength"> The maximum length of data allowed in the property. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasMaxLength(int maxLength)
        {
            Builder.HasMaxLength(maxLength, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures the property as capable of persisting unicode characters or not.
        ///     Can only be set on <see cref="string" /> properties.
        /// </summary>
        /// <param name="unicode"> A value indicating whether the property can contain unicode characters or not. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder IsUnicode(bool unicode = true)
        {
            Builder.IsUnicode(unicode, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         配置属性作为 <see cref="ValueGeneratedOnAddOrUpdate" /> 和
        ///         <see cref="IsConcurrencyToken" />。
        ///     </para>
        ///     <para>
        ///         数据库提供者可以选择不同的方式实现它，但它一般被用来实现乐观并发冲突检测所使用的自动行版本。
        ///         
        ///     </para>
        /// </summary>
        /// <returns> 当前生成器实例，以便多个配置可以进行链式调用。 </returns>
        public virtual PropertyBuilder IsRowVersion()
        {
            Builder.ValueGenerated(ValueGenerated.OnAddOrUpdate, ConfigurationSource.Explicit);
            Builder.IsConcurrencyToken(true, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         Configures the <see cref="ValueGenerator" /> that will generate values for this property.
        ///     </para>
        ///     <para>
        ///         Values are generated when the entity is added to the context using, for example,
        ///         <see cref="DbContext.Add{TEntity}" />. Values are generated only when the property is assigned
        ///         the CLR default value (null for string, 0 for int, Guid.Empty for Guid, etc.).
        ///     </para>
        ///     <para>
        ///         A single instance of this type will be created and used to generate values for this property in all
        ///         instances of the entity type. The type must be instantiable and have a parameterless constructor.
        ///     </para>
        ///     <para>
        ///         This method is intended for use with custom value generation. Value generation for common cases is
        ///         usually handled automatically by the database provider.
        ///     </para>
        /// </summary>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasValueGenerator<TGenerator>()
            where TGenerator : ValueGenerator
        {
            Builder.HasValueGenerator(typeof(TGenerator), ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         Configures the <see cref="ValueGenerator" /> that will generate values for this property.
        ///     </para>
        ///     <para>
        ///         Values are generated when the entity is added to the context using, for example,
        ///         <see cref="DbContext.Add{TEntity}" />. Values are generated only when the property is assigned
        ///         the CLR default value (null for string, 0 for int, Guid.Empty for Guid, etc.).
        ///     </para>
        ///     <para>
        ///         A single instance of this type will be created and used to generate values for this property in all
        ///         instances of the entity type. The type must be instantiable and have a parameterless constructor.
        ///     </para>
        ///     <para>
        ///         This method is intended for use with custom value generation. Value generation for common cases is
        ///         usually handled automatically by the database provider.
        ///     </para>
        ///     <para>
        ///         Setting null does not disable value generation for this property, it just clears any generator explicitly
        ///         configured for this property. The database provider may still have a value generator for the property type.
        ///     </para>
        /// </summary>
        /// <param name="valueGeneratorType"> A type that inherits from <see cref="ValueGenerator" /> </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasValueGenerator([CanBeNull] Type valueGeneratorType)
        {
            Builder.HasValueGenerator(valueGeneratorType, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         Configures a factory for creating a <see cref="ValueGenerator" /> to use to generate values
        ///         for this property.
        ///     </para>
        ///     <para>
        ///         Values are generated when the entity is added to the context using, for example,
        ///         <see cref="DbContext.Add{TEntity}" />. Values are generated only when the property is assigned
        ///         the CLR default value (null for string, 0 for int, Guid.Empty for Guid, etc.).
        ///     </para>
        ///     <para>
        ///         This factory will be invoked once to create a single instance of the value generator, and
        ///         this will be used to generate values for this property in all instances of the entity type.
        ///     </para>
        ///     <para>
        ///         This method is intended for use with custom value generation. Value generation for common cases is
        ///         usually handled automatically by the database provider.
        ///     </para>
        /// </summary>
        /// <param name="factory"> A delegate that will be used to create value generator instances. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasValueGenerator([NotNull] Func<IProperty, IEntityType, ValueGenerator> factory)
        {
            Check.NotNull(factory, nameof(factory));

            Builder.HasValueGenerator(factory, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     配置这个属性是否应该作为一个并发标记使用。
        ///     当一个属性被作为并发标记配置后。当这个实体类型的实例被更新或删除时，
        ///     <see cref="DbContext.SaveChanges()" /> 将通过检查数据库中的这个值确保它不会
        ///     被从数据库中检索的实例修改。
        ///     如果它被改变了，那么将抛出一个异常，并且这些改变将不能被应用到数据库。
        /// </summary>
        /// <param name="concurrencyToken"> 一个布尔值，指示这个属性是否为并发标记。 </param>
        /// <returns> 当前生成器实例，以便多个配置可以进行链式调用。 </returns>
        public virtual PropertyBuilder IsConcurrencyToken(bool concurrencyToken = true)
        {
            Builder.IsConcurrencyToken(concurrencyToken, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures a property to never have a value generated when an instance of this
        ///     entity type is saved.
        /// </summary>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        /// <remarks>
        ///     Note that temporary values may still be generated for use internally before a
        ///     new entity is saved.
        /// </remarks>
        public virtual PropertyBuilder ValueGeneratedNever()
        {
            Builder.ValueGenerated(ValueGenerated.Never, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures a property to have a value generated only when saving a new entity, unless a non-null,
        ///     non-temporary value has been set, in which case the set value will be saved instead. The value
        ///     may be generated by a client-side value generator or may be generated by the database as part
        ///     of saving the entity.
        /// </summary>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder ValueGeneratedOnAdd()
        {
            Builder.ValueGenerated(ValueGenerated.OnAdd, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     Configures a property to have a value generated only when saving a new or existing entity, unless
        ///     a non-null, non-temporary value has been set for a new entity, or the existing property value has
        ///     been modified for an existing entity, in which case the set value will be saved instead.
        /// </summary>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder ValueGeneratedOnAddOrUpdate()
        {
            Builder.ValueGenerated(ValueGenerated.OnAddOrUpdate, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         Sets the backing field to use for this property.
        ///     </para>
        ///     <para>
        ///         Backing fields are normally found by convention as described
        ///         here: http://go.microsoft.com/fwlink/?LinkId=723277.
        ///         This method is useful for setting backing fields explicitly in cases where the
        ///         correct field is not found by convention.
        ///     </para>
        ///     <para>
        ///         By default, the backing field, if one is found or has been specified, is used when
        ///         new objects are constructed, typically when entities are queried from the database.
        ///         Properties are used for all other accesses. This can be changed by calling
        ///         <see cref="UsePropertyAccessMode" />.
        ///     </para>
        /// </summary>
        /// <param name="fieldName"> The field name. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder HasField([NotNull] string fieldName)
        {
            Check.NotEmpty(fieldName, nameof(fieldName));

            Builder.HasField(fieldName, ConfigurationSource.Explicit);

            return this;
        }

        /// <summary>
        ///     <para>
        ///         Sets the <see cref="PropertyAccessMode" /> to use for this property.
        ///     </para>
        ///     <para>
        ///         By default, the backing field, if one is found by convention or has been specified, is used when
        ///         new objects are constructed, typically when entities are queried from the database.
        ///         Properties are used for all other accesses.  Calling this method witll change that behavior
        ///         for this property as described in the <see cref="PropertyAccessMode" /> enum.
        ///     </para>
        ///     <para>
        ///         Calling this method overrrides for this property any access mode that was set on the
        ///         entity type or model.
        ///     </para>
        /// </summary>
        /// <param name="propertyAccessMode"> The <see cref="PropertyAccessMode" /> to use for this property. </param>
        /// <returns> The same builder instance so that multiple configuration calls can be chained. </returns>
        public virtual PropertyBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode)
        {
            Builder.UsePropertyAccessMode(propertyAccessMode, ConfigurationSource.Explicit);

            return this;
        }

        private InternalPropertyBuilder Builder { get; }
    }
}
