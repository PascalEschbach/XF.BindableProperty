using System;
using System.Collections.Generic;
using System.Text;

namespace XF.BindableProperty {
	
	[AttributeUsage( AttributeTargets.Property )]
	public class BindableAttribute : Attribute {

		public BindableAttribute( 
			XFBindingMode bindingMode = XFBindingMode.OneWay,

			string validateValueMethodName = null,
			string propertyChangedMethodName = null,
			string propertyChangingMethodName = null,
			string coerceValueMethodName = null,
			string defaultValueCreatorMethodName = null,

			Type declaringType = null
		) {}
	}
}
