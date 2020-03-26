using System;
using System.Collections.Generic;
using System.Text;

namespace XF.BindableProperty {
	
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
	public class BindableAttribute : Attribute {

		public XFBindingMode BindingMode { get; set; } = XFBindingMode.OneWay;

		public Type OwningType { get; set; }

		public string OnPropertyChanged { get; set; }
		public string OnPropertyChanging { get; set; }
		public string OnCoerceValue { get; set; }
		public string OnCreateValue { get; set; }
		public string OnValidateValue { get; set; }
	}
}
