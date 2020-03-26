using System;
using Xamarin.Forms;
using XF.BindableProperty;

public class TestClass : BindableObject {

	[Bindable( OnCoerceValue = nameof( CoerceValueDelegate ), OwningType = typeof(Color))]
	public string Auto { get; set; } = "This is a test";


	static bool ValidateValueDelegate( BindableObject bindable, object value ) => throw new NotImplementedException();
	static object CoerceValueDelegate( BindableObject bindable, object value ) => throw new NotImplementedException();
	static object CreateDefaultValueDelegate( BindableObject bindable ) => throw new NotImplementedException();
	static void BindingPropertyChangingDelegate( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
	static void BindingPropertyChangedDelegate( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
}
