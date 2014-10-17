﻿namespace Gu.Wpf.Localization
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Gu.Wpf.Localization.Annotations;

    public class TranslationManager : INotifyPropertyChanged
    {
        private static readonly ConcurrentDictionary<Assembly, TranslationManager> Cache = new ConcurrentDictionary<Assembly, TranslationManager>();

        private ITranslationProvider _translationProvider;

        private TranslationManager()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler LanguageChanged;

        public static TranslationManager Instance
        {
            get
            {
                var assembly = Assembly.GetCallingAssembly();
                return GetInstance(assembly);
            }
        }

        public ITranslationProvider TranslationProvider
        {
            get
            {
                return this._translationProvider;
            }
            set
            {
                if (Equals(value, this._translationProvider))
                {
                    return;
                }
                this._translationProvider = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("Languages");
            }
        }

        public CultureInfo CurrentLanguage
        {
            get
            {
                return Thread.CurrentThread.CurrentUICulture;
            }

            set
            {
                if (!Equals(value, Thread.CurrentThread.CurrentUICulture))
                {
                    Thread.CurrentThread.CurrentUICulture = value;
                    this.OnLanguageChanged();
                    this.OnPropertyChanged();
                }
            }
        }

        public IEnumerable<CultureInfo> Languages
        {
            get
            {
                if (this.TranslationProvider != null)
                {
                    return this.TranslationProvider.Languages;
                }

                return Enumerable.Empty<CultureInfo>();
            }
        }

        public string Translate(string key)
        {
            if (this.TranslationProvider != null)
            {
                try
                {
                    string translatedValue = this.TranslationProvider.Translate(key);
                    if (translatedValue != null)
                    {
                        return translatedValue;
                    }
                }
                catch (Exception)
                {
                    return string.Format("!{0}!", key);
                }
            }

            return string.Format("!{0}!", key);
        }

        public bool HasKey(string key, CultureInfo culture)
        {
            return this.TranslationProvider.HasKey(key, culture);
        }

        /// <summary>
        /// Use this to get the translationmanager for another assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static TranslationManager GetInstance(Assembly assembly)
        {
            if (assembly == null || assembly.GetName().Name == "System.Xaml")
            {
                assembly = Assembly.GetEntryAssembly();
            }
            var manager = Cache.GetOrAdd(assembly, a => CreateManager(a));
            return manager;
        }

        private static TranslationManager CreateManager(Assembly assembly)
        {
            if (assembly.GetName().Name == "System.Xaml")
            {
                assembly = Assembly.GetEntryAssembly();
            }
            var resourceManager = new ResourceManager(assembly.GetName().Name + ".Properties.Resources", assembly);
            return new TranslationManager
                              {
                                  Assembly = assembly,
                                  TranslationProvider = new ResxTranslationProvider(resourceManager)
                              };
        }

        public Assembly Assembly { get; private set; }

        private void OnLanguageChanged()
        {
            if (this.LanguageChanged != null)
            {
                this.LanguageChanged(this, EventArgs.Empty);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public TranslationInfo GetInfo(string key)
        {
            var provider = TranslationProvider as ResxTranslationProvider;
            if (_translationProvider == null)
            {
                return TranslationInfo.NoProvider;
            }
            if (!Languages.Any())
            {

                if (provider == null)
                {
                    return TranslationInfo.NoResources;
                    //var manifestResourceNames = Assembly.GetManifestResourceNames();
                    //if (provider.ResourceManager.GetResourceSet())
                    //{

                    //}
                }
                return TranslationInfo.NoLanguages;
            }
            if (_translationProvider.HasKey(key, CultureInfo.CurrentUICulture))
            {
                return TranslationInfo.CanTranslate;
            }
            if (provider == null)
            {
                return TranslationInfo.NoTranslation;
            }
            var s = provider.ResourceManager.GetString(key, CurrentLanguage);
            if (s == null) // Possibly undocumented behavior
            {
                return TranslationInfo.MissingKey;
            }
            return TranslationInfo.NoTranslation;
        }
    }
}