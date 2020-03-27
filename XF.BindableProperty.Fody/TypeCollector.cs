using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

public partial class ModuleWeaver {

    private IEnumerable<BindableProperty> CollectProperties()
        => from type in ModuleDefinition.Types
           where type.Inherits( WeaverTypes.BindableObject.Resolve() )
           from property in type.Properties
           where property.HasAttribute( WeaverConstants.BindableAttribute )
           select new BindableProperty( property );
}
