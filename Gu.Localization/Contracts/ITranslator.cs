﻿namespace Gu.Localization
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq.Expressions;

    public interface ITranslator : INotifyPropertyChanged
    {
        CultureInfo CurrentCulture { get; set; }

        IObservableSet<CultureInfo> AllCultures { get; }

        string Translate(Expression<Func<string>> key);

        string Translate(Type typeInAssembly, string key);
    }
}