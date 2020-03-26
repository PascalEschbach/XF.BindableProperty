# <img src="/Icon.png" height="30px"> XF.BindableProperty.Fody

[![Build Status]()]()
[![NuGet Status]()]()

### This is an add-in for [Fody](https://github.com/Fody/Home/).

Injects code which raises the [`PropertyChanged` event](https://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.propertychanged.aspx), into property setters of classes which implement [INotifyPropertyChanged](https://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx).

## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet installation

Install the [XF.BindableProperty.Fody NuGet package](https://nuget.org/packages/XF.BindableProperty.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package PropertyChanged.Fody
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
}
```


## Configuration



## Notes


## Icon

[Link](https://thenounproject.com/term/link/39562/) designed by [Matt Hawdon](https://thenounproject.com/matthawdon/) from The Noun Project.
