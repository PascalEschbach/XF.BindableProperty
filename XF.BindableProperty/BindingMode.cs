namespace XF.BindableProperty {

    //
    // Zusammenfassung:
    //     Die Richtung der Änderungsweitergabe für Bindungen.
    public enum XFBindingMode {
        //
        // Zusammenfassung:
        //     Gibt bei der Verwendung in Bindungen an, dass die Bindung die Xamarin.Forms.BindableProperty.DefaultBindingMode-Eigenschaft
        //     verwenden sollte. Legt bei der Verwendung in BindableProperty-Deklarationen standardmäßig
        //     die BindingMode.OneWay-Enumeration fest.
        Default = 0,
        //
        // Zusammenfassung:
        //     Gibt an, dass die Bindung Änderungen in beide Richtungen, d.h. von der Quelle
        //     (in der Regel das Ansichtsmodell) an das Ziel (BindableObject-Klasse) bzw. anders
        //     herum, weitergegeben soll.
        TwoWay = 1,
        //
        // Zusammenfassung:
        //     Gibt an, dass die Bindung nur Änderungen von der Quelle (in der Regel das Ansichtsmodell)
        //     an das Ziel (BindableObject-Klasse) weitergegeben soll. Dies ist der Standardmodus
        //     für die meisten BindableProperty-Werte.
        OneWay = 2,
        //
        // Zusammenfassung:
        //     Gibt an, dass die Bindung nur Änderungen vom Ziel (BindableObject-Klasse) an
        //     die Quelle (in der Regel das Ansichtsmodell) weitergegeben soll. Dies wird hauptsächlich
        //     für schreibgeschützte BindableProperty-Werte verwendet.
        OneWayToSource = 3,
        //
        // Zusammenfassung:
        //     Gibt an, dass die Bindung nur angewendet wird, wenn der Bindungskontext geändert
        //     wird und der Wert nicht mit der INotifyPropertyChanged-Schnittstelle auf Änderungen
        //     überwacht wird.
        OneTime = 4
    }
}