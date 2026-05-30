using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace AuthForge.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Пытаемся получить сохраненный ранее язык пользователя
            string languageCode = AuthForge.UI.Properties.Settings.Default.UserLanguage;

            // 2. Если это самый первый запуск (в настройках пусто), проверяем реестр после Inno Setup
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = GetLanguageFromRegistry();
            }

            // 3. Применяем язык к приложению
            ApplyLanguage(languageCode);
        }

        private string GetLanguageFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\AuthForge"))
                {
                    if (key != null)
                    {
                        object registryValue = key.GetValue("InstallerLanguage");
                        if (registryValue != null)
                        {
                            string innoLang = registryValue.ToString().ToLower();

                            // Маппим язык из Inno Setup в понятный для .NET код культуры
                            if (innoLang == "ru") return "ru";
                            if (innoLang == "en") return "en";
                        }
                    }
                }
            }
            catch
            {
                // На случай непредвиденных ограничений прав доступа к реестру
            }

            // Если в реестре ничего нет, берем текущий язык операционной системы (ru, en и т.д.)
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public static void ApplyLanguage(string langCode)
        {
            // Корректируем код для поиска файла, если пришел полный код типа "ru-RU"
            if (langCode.Contains("-"))
                langCode = langCode.Split('-')[0];

            try
            {
                // Формируем путь к словарю внутри вашей папки Localization
                string dictPath = $"Localization/Lang.{langCode}.xaml";
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(dictPath, UriKind.RelativeOrAbsolute)
                };

                // Находим старый словарь локализации, если он был загружен, и удаляем его
                var oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Localization/Lang."));

                if (oldDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                // Добавляем новый словарь ресурсов
                Application.Current.Resources.MergedDictionaries.Add(newDict);

                // Сохраняем выбранный язык в локальные настройки пользователя (.NET сами запишут в AppData)
                AuthForge.UI.Properties.Settings.Default.UserLanguage = langCode;
                AuthForge.UI.Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке локализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
