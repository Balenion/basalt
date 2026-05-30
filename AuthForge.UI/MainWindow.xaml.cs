using AuthForge.Core.Interfaces;
using AuthForge.Core.Models;
using AuthForge.Core.Services;
using BCrypt.Net;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace AuthForge.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ExcelService _excelService = new();
        private string? _selectedFile;
        private readonly Argon2Service _argon2 = new();
        private readonly BCryptService _bcrypt = new();
        private readonly BuiltInHashService _pbkdf2 = new();
        private readonly LegacyHashService _legacy = new();

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            string currentLang = AuthForge.UI.Properties.Settings.Default.UserLanguage;
            _isRussian = (currentLang == "ru");
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new OpenFileDialog { Filter = "Excel Files|*.xlsx" };
            if (openPicker.ShowDialog() == true)
            {
                if (_excelService.IsTemplateValid(openPicker.FileName))
                {
                    _selectedFile = openPicker.FileName;
                    FilePathTxt.Text = Path.GetFileName(_selectedFile);
                    FilePathTxt.BorderBrush = System.Windows.Media.Brushes.Cyan;
                    StartBtn.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("Invalid Template! Please use the 'Generate Template' button to get the correct format.",
                                    "Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FilePathTxt.Text = "Invalid file selected";
                    FilePathTxt.BorderBrush = System.Windows.Media.Brushes.Red;
                    StartBtn.IsEnabled = false;
                }
            }
        }

        private void CreateTemplate_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "AuthForge_Template.xlsx" };
            if (savePicker.ShowDialog() == true)
            {
                _excelService.CreateTemplate(savePicker.FileName);
            }
        }

        private void AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Argon2Settings == null || LegacySettings == null) return;

            Argon2Settings.Visibility = Visibility.Collapsed;
            LegacySettings.Visibility = Visibility.Collapsed;

            switch (AlgoSelector.SelectedIndex)
            {
                case 0:
                    Argon2Settings.Visibility = Visibility.Visible;
                    break;
                case 3:
                    LegacySettings.Visibility = Visibility.Visible;
                    break;
            }

            UpdateActiveSettingsInfo();
        }

        private async void StartHashing_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFile)) return;

            int selectedIndex = AlgoSelector.SelectedIndex;
            bool useSaltForLegacy = UseSaltLegacy.IsChecked ?? false;

            int argonMemory = (int)MemorySlider.Value;
            int argonIter = (int)IterSlider.Value;
            int argonParallel = (int)ThreadSlider.Value;

            IPasswordService selectedHasher = selectedIndex switch
            {
                0 => _argon2,
                1 => _bcrypt,
                2 => _pbkdf2,
                _ => _legacy
            };

            if (selectedHasher is Argon2Service a2)
            {
                a2.MemoryKb = argonMemory * 1024;
                a2.Iterations = argonIter;
                a2.Parallelism = argonParallel;
            }

            try
            {
                ProgressArea.Visibility = Visibility.Visible;
                StartBtn.IsEnabled = false;
                StatusTxt.Text = "Reading Excel...";
                HashProgress.Value = 0;

                var users = await Task.Run(() => _excelService.ReadEmployees(_selectedFile));
                var results = new List<UserOutput>();

                await Task.Run(() =>
                {
                    for (int i = 0; i < users.Count; i++)
                    {
                        HashResult hashResult;

                        if (selectedHasher is LegacyHashService legacy && !useSaltForLegacy)
                        {
                            hashResult = legacy.HashPlain(users[i].Password);
                        }
                        else
                        {
                            hashResult = selectedHasher.Hash(users[i].Password);
                        }

                        results.Add(new UserOutput(
                            users[i].Login,
                            hashResult.Hash,
                            hashResult.Salt,
                            selectedHasher.AlgorithmName + (!useSaltForLegacy && selectedHasher is LegacyHashService ? " (No Salt)" : "")));

                        Dispatcher.Invoke(() => HashProgress.Value = (i + 1) * 100 / users.Count);
                    }
                });

                StatusTxt.Text = "Saving...";
                var savePicker = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "Results.xlsx" };
                if (savePicker.ShowDialog() == true)
                {
                    await Task.Run(() => _excelService.SaveResults(savePicker.FileName, results));
                    MessageBox.Show("Success!", "Forge Complete");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                ProgressArea.Visibility = Visibility.Collapsed;
                StartBtn.IsEnabled = true;
            }
        }

        private void UpdateActiveSettingsInfo()
        {
            if (ActiveMethodTxt == null) return;

            var selectedItem = (AlgoSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            ActiveMethodTxt.Text = selectedItem ?? "Not Selected";

            if (AlgoSelector.SelectedIndex == 0)
            {
                ActiveDetailsTxt.Text = $"{MemorySlider.Value} MB, {IterSlider.Value} Iter, {ThreadSlider.Value} Threads";
                ActiveDetailsTxt.Visibility = Visibility.Visible;
            }
            else if (AlgoSelector.SelectedIndex == 3)
            {
                ActiveDetailsTxt.Text = UseSaltLegacy.IsChecked == true ? "With Salt" : "No Salt (Unsafe)";
                ActiveDetailsTxt.Visibility = Visibility.Visible;
            }
            else
            {
                ActiveDetailsTxt.Visibility = Visibility.Collapsed;
            }
        }

        private void GenerateSingleHash_Click(object sender, RoutedEventArgs e)
        {
            string password = SinglePasswordBox.Password;
            if (string.IsNullOrEmpty(password)) return;

            IPasswordService selectedHasher = AlgoSelector.SelectedIndex switch
            {
                0 => _argon2,
                1 => _bcrypt,
                2 => _pbkdf2,
                _ => _legacy
            };

            if (selectedHasher is Argon2Service a2)
            {
                a2.MemoryKb = (int)MemorySlider.Value * 1024;
                a2.Iterations = (int)IterSlider.Value;
                a2.Parallelism = (int)ThreadSlider.Value;
            }

            HashResult result;
            if (selectedHasher is LegacyHashService legacy && UseSaltLegacy.IsChecked == false)
            {
                result = legacy.HashPlain(password);
            }
            else
            {
                result = selectedHasher.Hash(password);
            }

            ResultHashTxt.Text = result.Hash;
            ResultSaltTxt.Text = result.Salt ?? "No salt used";
            AlgoInfoTxt.Text = $"Algorithm: {selectedHasher.AlgorithmName}";
        }

        private void CopyHash_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(ResultHashTxt.Text);
        private void CopySalt_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(ResultSaltTxt.Text);

        private readonly JwtService _jwtService = new();

        private void GenerateKey_Click(object sender, RoutedEventArgs e)
        {
            int bits = 256;
            if (Rb128.IsChecked == true) bits = 128;
            if (Rb512.IsChecked == true) bits = 512;

            string newKey = _jwtService.GenerateSecretKey(bits);

            GeneratedKeyTxt.Text = newKey;
        }

        private void CopyGeneratedKey_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedKeyTxt.Text))
            {
                Clipboard.SetText(GeneratedKeyTxt.Text);
            }
        }

        private void UseSaltLegacy_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateActiveSettingsInfo();
        }

        private void UseSaltLegacy_Checked(object sender, RoutedEventArgs e)
        {
            UpdateActiveSettingsInfo();
        }

        private bool _isRussian = false;

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            _isRussian = !_isRussian;

            string targetLangCode = _isRussian ? "ru" : "en";

            string langPath = $"Localization/Lang.{targetLangCode}.xaml";

            try
            {
                ResourceDictionary newLangDict = new ResourceDictionary
                {
                    Source = new Uri(langPath, UriKind.Relative)
                };

                var appResources = Application.Current.Resources.MergedDictionaries;
                bool isReplaced = false;

                for (int i = 0; i < appResources.Count; i++)
                {
                    var dict = appResources[i];
                    
                    if (dict.Source != null && dict.Source.OriginalString.Contains("Localization/Lang."))
                    {
                        appResources.RemoveAt(i);
                        appResources.Insert(i, newLangDict);
                        isReplaced = true;
                        break;
                    }
                }

                if (!isReplaced)
                {
                    appResources.Add(newLangDict);
                }

                AuthForge.UI.Properties.Settings.Default.UserLanguage = targetLangCode;
                AuthForge.UI.Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching language: {ex.Message}", "Localization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}