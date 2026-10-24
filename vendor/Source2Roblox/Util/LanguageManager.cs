using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace Source2Roblox.Util
{
    public static class LanguageManager
    {
        private static readonly ResourceManager _rm =
            new ResourceManager(
                "Source2Roblox.Strings.Strings",
                typeof(LanguageManager).Assembly);

        public static string Get(string key)
        {
            try
            {
                return _rm.GetString(key, Thread.CurrentThread.CurrentUICulture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        public static void SetCulture(string ietfTag)
        {
            CultureInfo culture;

            if (string.IsNullOrWhiteSpace(ietfTag))
            {
                culture = CultureInfo.InstalledUICulture;
            }
            else
            {
                try   { culture = CultureInfo.GetCultureInfo(ietfTag); }
                catch { culture = CultureInfo.InvariantCulture; }
            }

            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public static string CurrentTag =>
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        public static string CurrentDisplayName =>
            Thread.CurrentThread.CurrentUICulture.NativeName;
    }
}
