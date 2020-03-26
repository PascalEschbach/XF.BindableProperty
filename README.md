# <img src="/Icon.png" height="30px"> XF.BindableProperty.Fody

![Build Status](https://github.com/HeroicSoft/XF.BindableProperty/workflows/Build/badge.svg)
[![NuGet Status](https://img.shields.io/nuget/v/XF.BindableProperty.Fody.svg)](https://nuget.org/packages/XF.BindableProperty.Fody/)

### This is an add-in for [Fody](https://github.com/Fody/Home/).

Turns your auto properties into Xamarin.Forms [BindableProperties](https://docs.microsoft.com/de-de/dotnet/api/xamarin.forms.bindableproperty?view=xamarin-forms).

## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet installation

Install the [XF.BindableProperty.Fody NuGet package](https://nuget.org/packages/XF.BindableProperty.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package XF.BindableProperty.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<XF.BindableProperty />` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <XF.BindableProperty />
</Weavers>
```


## Overview

What you write:

```csharp
public class Foo : BindableObject
{
    [Bindable]
    public string Bar { get; set; }

    [Bindable( BindingMode = XFBindingMode.OneTime, OwningType = typeof(Color))]
    public string Baz { get; set; } = "abc123";
}
```

What gets compiled:

```csharp
public class Foo : BindableObject
{
    public static readonly BarProperty = BindableProperty.Create(nameof(Bar), typeof(string), typeof(Foo), default(string), BindingMode.OneWay);
    public string Bar {
        get => (string)GetValue(BarProperty);
        set => SetValue(BarProperty, value);
    }

    public static readonly BazProperty = BindableProperty.Create(nameof(Baz), typeof(string), typeof(Color), "abc123", BindingMode.OneTime);
    public string Baz {
        get => (string)GetValue(BazProperty);
        set => SetValue(BazProperty, value);
    }
}
```


## Configuration

XF.BindableProperties is highly customizable. Every option which you could normally specify on the BindableProperty.Create method is either implicitly or explicitly configureable.

### Callbacks

BindableProperty.Create supports five callbacks.
- OnPropertyChanged
- OnPropertyChanging
- OnCoerceValue
- OnValidateValue
- OnCreateDefaultValue

All those callbacks can be implicitly specified in your code:
```csharp
public class Foo : BindableObject
{
    [Bindable]
    public string Bar { get; set; }

    private static void OnBarChanged( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
    private static void OnBarChanging( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
    private static object OnCoerceBarValue( BindableObject bindable, object value ) => throw new NotImplementedException();
    private static bool OnValidateBarValue( BindableObject bindable, object value ) => throw new NotImplementedException();
    private static object OnCreateBarValue( BindableObject bindable ) => throw new NotImplementedException();
}
```

* The callbacks are automatically looked up. The pattern has to be exact, simply replace 'Bar' with your property name.
* Make sure the method signature and return types exactly match!
* You can specify any of those callbacks or none at all, they aren't required.

Furthermore, callbacks can be explicitly specified within the attribute:
```csharp
public class Foo : BindableObject
{
    [Bindable(
        OnPropertyChanged = nameof(PropertyChangedMethod),
        OnPropertyChanging= nameof(PropertyChangingMethod),
        OnCoerceValue = nameof(CoerceValueMethod),
        OnValidateValue = nameof(ValidateValueMethod),
        OnCreateValue = nameof(CreateValueMethod)
    )]
    public string Bar { get; set; }

    private static void PropertyChangedMethod( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
    private static void PropertyChangingMethod( BindableObject bindable, object oldValue, object newValue ) => throw new NotImplementedException();
    private static object CoerceValueMethod( BindableObject bindable, object value ) => throw new NotImplementedException();
    private static bool ValidateValueMethod( BindableObject bindable, object value ) => throw new NotImplementedException();
    private static object CreateValueMethod( BindableObject bindable ) => throw new NotImplementedException();
}
```

* If the method doesn't exist or any other error, such as signature mismatch, is found, the weaver will throw an exception.
* The method names can be anything.

### Default values

Initializing your property with a normal property initializer is enough to instruct the system with the necessary information. The property initializer will automatically be included in the BindableProperty.Create method call. This approach is constrained by normal field/property initializer rules.

If instead you need more fine grained control or instance level access, you can register (or implicitly) use the 'OnCreateValue' callback.

```csharp
public class Foo : BindableObject
{
    [Bindable]
    public string Default { get; set; } = "abc";
   
    [Bindable( OnCreateValue = nameof(CreateValueMethod))]
    public string Explicit { get; set; }
    private static object CreateValueMethod( BindableObject bindable ) => "abc"; //Instance level access throught 'bindable' argument
    
    [Bindable]
    public string Implicit { get; set; }
    private static object OnCreateImplicitValue( BindableObject bindable ) => "abc"; //Instance level access throught 'bindable' argument
}
```

### Other

* The binding mode can be controlled via the 'BindingMode' property of the attribute.
* The concrete owner of the BindableProperty can be controlled via the 'OwningType' property.


## Remarks
* **Auto-Properties**: Only auto properties are supported. Properties with getter/setter bodies are invalid by definition as Xamarin accesses the BindableProperty directly, not the actual property. As such, custom getter/setter implementations would cause diverging behaviours between XAML and code. 
* **OnCoerceValue**: Use this callback to constrain inputs instead of a custom getter/setter!
* **OnValidateValue**: Use this callback to do any input validation!
* **OnPropertyChanged/OnPropertyChanging**: Use those callbacks to notify dependant properties or trigger custom behaviour!
* **OnCreateValue**: Use this callback to construct a default value which required instance level access or runtime information!

## Roadmap
* Attached properties
* Dependant property notification

## Icon

[Icon](https://thenounproject.com/term/link/39562/) designed by [Matt Hawdon](https://thenounproject.com/matthawdon/) from The Noun Project.
