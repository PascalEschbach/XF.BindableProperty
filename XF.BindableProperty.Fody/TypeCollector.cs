using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

public partial class ModuleWeaver {

	private IEnumerable<BindableProperty> CollectProperties()
		=> from type in ModuleDefinition.Types
		   where type.Inherits( WeavingTypes.BindableObjectDef )
		   from property in type.Properties
		   where property.HasAttribute( BINDABLE_ATTRIBUTE_NAME )
		   select new BindableProperty( property, this );
}
