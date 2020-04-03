﻿using System.Collections.Generic;

namespace HotChocolate.Types
{
    /// <summary>
    /// A complex output type can be an <see cref="ObjectType" />
    /// or an <see cref="InterfaceType" />.
    /// </summary>
    public interface IComplexOutputType
        : INamedOutputType
        , IHasDirectives
    {
        /// <summary>
        /// Gets the interfaces that are implemented by this type.
        /// </summary>
        IReadOnlyList<IInterfaceType> Interfaces { get; }

        /// <summary>
        /// Gets the field that the type exposes.
        /// </summary>
        IFieldCollection<IOutputField> Fields { get; }

        /// <summary>
        /// Defines if this type is implementing an interface
        /// with the given <paramref name="typeName">.
        /// </summary>
        /// <param name="typeName">
        /// The interface type name.
        /// </param>
        bool IsImplementing(NameString typeName);

        /// <summary>
        /// Defines if this type is implementing the
        /// the given <paramref name="interfaceType">.
        /// </summary>
        /// <param name="typeName">
        /// The interface type.
        /// </param>
        bool IsImplementing(IInterfaceType interfaceType);
    }
}
