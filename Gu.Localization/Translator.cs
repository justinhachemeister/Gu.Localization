﻿namespace Gu.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Threading;

    /// <summary> Class for translating resources </summary>
    public static class Translator
    {
        private static CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;
        private static DirectoryInfo resourceDirectory = ResourceCultures.DefaultResourceDirectory();
        private static List<CultureInfo> cultures = GetAllCultures();

        /// <summary>
        /// Notifies when the current language changes.
        /// </summary>
        public static event EventHandler<CultureInfo> CurrentCultureChanged;

        /// <summary>
        /// Gets or sets set the current directory where resources are found.
        /// Default is Directory.GetCurrentDirectory()
        /// Changing the default is perhaps useful in tests.
        /// </summary>
        public static DirectoryInfo ResourceDirectory
        {
            get
            {
                return resourceDirectory;
            }

            set
            {
                resourceDirectory = value;
                cultures = GetAllCultures();
            }
        }

        /// <summary>
        /// Gets or sets the culture to translate to
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                return currentCulture ??
                       Cultures.FirstOrDefault() ??
                       CultureInfo.InvariantCulture;
            }

            set
            {
                if (CultureInfoComparer.Equals(currentCulture, value))
                {
                    return;
                }

                currentCulture = value;
                OnCurrentCultureChanged(value);
            }
        }

        /// <summary>Gets or sets a value indicating how errors are handled. The default is throw</summary>
        public static ErrorHandling ErrorHandling { get; set; } = ErrorHandling.Throw;

        /// <summary> Gets a list with all cultures found for the application </summary>
        public static IReadOnlyList<CultureInfo> Cultures => cultures;

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key)
        {
            return Translate(resourceManager, key, CurrentCulture);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="errorHandling">Specifies how error handling is performed.</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, ErrorHandling errorHandling)
        {
            return Translate(resourceManager, key, CurrentCulture, errorHandling);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="culture">The culture, if null CultureInfo.InvariantCulture is used</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, CultureInfo culture)
        {
            return Translate(resourceManager, key, culture, ErrorHandling);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="culture">The culture, if null CultureInfo.InvariantCulture is used</param>
        /// <param name="errorHandling">How to handle errors.</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(
            ResourceManager resourceManager,
            string key,
            CultureInfo culture,
            ErrorHandling errorHandling)
        {
            if (errorHandling == ErrorHandling.Default)
            {
                errorHandling = ErrorHandling;
            }

            var shouldThrow = errorHandling != ErrorHandling.ReturnErrorInfo;
            if (resourceManager == null)
            {
                if (shouldThrow)
                {
                    throw new ArgumentNullException(nameof(resourceManager));
                }

                return string.Format(Properties.Resources.NullManagerFormat, key);
            }

            if (string.IsNullOrEmpty(key))
            {
                if (shouldThrow)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return "key == null";
            }

            if (culture != null &&
                !CultureInfoComparer.Equals(culture, CultureInfo.InvariantCulture) &&
                cultures?.Contains(culture, CultureInfoComparer.Default) == false)
            {
                if (resourceManager.HasCulture(culture))
                {
                    if (cultures == null)
                    {
                        cultures = new List<CultureInfo>();
                    }

                    cultures.Add(culture);
                }
                else
                {
                    if (shouldThrow)
                    {
                        var message = $"The resourcemanager {resourceManager.BaseName} does not have a translation to the culture: {culture.Name} for the key: {key}";
                        throw new ArgumentOutOfRangeException($"{nameof(culture)}, {nameof(key)}", message);
                    }

                    var trnslated = resourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(trnslated))
                    {
                        return string.Format(Properties.Resources.MissingCultureFormat, trnslated);
                    }

                    return string.Format(Properties.Resources.MissingCultureFormat, key);
                }
            }

            var translated = resourceManager.GetString(key, culture);
            if (translated == null)
            {
                if (shouldThrow)
                {
                    var message = $"The resourcemanager {resourceManager.BaseName} does not have the key: {key}";
                    throw new ArgumentOutOfRangeException(nameof(key), message);
                }

                return string.Format(Properties.Resources.MissingKeyFormat, key);
            }

            if (translated == string.Empty)
            {
                if (!resourceManager.HasKey(key, culture, false))
                {
                    if (shouldThrow)
                    {
                        var message = $"The resourcemanager {resourceManager.BaseName} does not have a translation for the key: {key} for the culture: {culture.Name}";
                        throw new ArgumentOutOfRangeException(nameof(key), message);
                    }

                    return string.Format(Properties.Resources.MissingTranslationFormat, key);
                }
            }

            return translated;
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// This assumes that the resource is something like 'Value: {0}' i.e. having one format parameter.
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="arg">The argument will be used as string.Format(format, <paramref name="arg"/>)</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, object arg)
        {
            return Translate(resourceManager, key, CurrentCulture, arg);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// This assumes that the resource is something like 'Value: {0}' i.e. having one format parameter.
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="culture">The culture.</param>
        /// <param name="arg">The argument will be used as string.Format(format, <paramref name="arg"/>)</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, CultureInfo culture, object arg)
        {
            var format = Translate(resourceManager, key, culture);
            return string.Format(format, arg);
        }

        private static void OnCurrentCultureChanged(CultureInfo e)
        {
            CurrentCultureChanged?.Invoke(null, e);
        }

        private static List<CultureInfo> GetAllCultures()
        {
            Debug.WriteLine(resourceDirectory);
            return resourceDirectory?.Exists == true
                       ? ResourceCultures.GetAllCultures(resourceDirectory).ToList()
                       : new List<CultureInfo>();
        }
    }
}
