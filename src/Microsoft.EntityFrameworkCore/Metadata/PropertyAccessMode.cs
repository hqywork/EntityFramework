// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     <para>
    ///         通过传递这个枚举值到 <see cref="ModelBuilder.UsePropertyAccessMode" />,
    ///         <see cref="EntityTypeBuilder.UsePropertyAccessMode" />, 或
    ///         <see cref="PropertyBuilder.UsePropertyAccessMode" /> 来修改读取以及写入时被使用的是属性还是字段。
    ///         
    ///     </para>
    ///     <para>
    ///         如果没有设置访问模式，那么当构造一个实体的新实例时在可能的情况下属性的支持字段将被使用。
    ///         如果可能，对于属性的所有其它访问， getter 或 setter 将被使用。
    ///         注意，当它不能使用字段（不能被规则发现并且不能指定使用 <see cref="PropertyBuilder.HasField" />）时，那么属性将被替代使用。
    ///         同样，当它不能使用属性的 getter 或 setter 时，例如当属性是只读时，那么字段将被替代使用。
    ///     </para>
    /// </summary>
    public enum PropertyAccessMode
    {
        /// <summary>
        ///     <para>
        ///         强制属性的所有访问必须通过字段进行。
        ///     </para>
        ///     <para>
        ///         如果这个模式被设置且它不可能读写字段，一个异常将被抛出。
        ///         
        ///     </para>
        /// </summary>
        Field,

        /// <summary>
        ///     <para>
        ///         强制对属性的所有访问必须通过正在被构造的新实例中的字段。
        ///         新实例通过是在从数据库中查询实体时被构造。
        ///         如果这个模式被设置且它不可能读写字段，一个异常将被抛出。
        ///         
        ///         
        ///     </para>
        ///     <para>
        ///         所有其它使用的属性将通过属性的 getters 和 setters，除非有不可能的原因，
        ///         例如属性是只读的，在这种情况下的访问将同样使用字段。
        ///         
        ///         
        ///     </para>
        ///     <para>
        ///         这种访问模式类似于被使用的默认模式，如果没有设置例外，
        ///         那么当实体构造器不可能写入字段时它将抛出一个异常。
        ///         默认访问模式将被使用属性替代。
        ///         
        ///         
        ///     </para>
        /// </summary>
        FieldDuringConstruction,

        /// <summary>
        ///     <para>
        ///         强制属性的所有访问必须通过属性的 getters 和 setters，即使是新对象被构造时。
        ///         
        ///     </para>
        ///     <para>
        ///         如果这个模式已被设置并且不可能读写属性时一个异常将被抛出，例如因为它是只读的。
        ///         
        ///     </para>
        /// </summary>
        Property
    }
}
