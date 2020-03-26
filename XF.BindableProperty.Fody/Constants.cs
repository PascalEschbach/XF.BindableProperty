using System;
using System.Collections.Generic;
using System.Text;

public static class Constants {

	public const string BindableAttribute = "XF.BindableProperty." + nameof( BindableAttribute );
	public const string BindingMode = nameof( BindingMode );
	public const string OwningType = nameof( OwningType );
	public const string OnPropertyChanged = nameof( OnPropertyChanged );
	public const string OnPropertyChanging = nameof( OnPropertyChanging );
	public const string OnCoerceValue = nameof( OnCoerceValue );
	public const string OnCreateValue = nameof( OnCreateValue );
	public const string OnValidateValue = nameof( OnValidateValue );
}