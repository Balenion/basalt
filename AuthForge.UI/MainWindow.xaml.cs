using AuthForge.Core.Interfaces;
using AuthForge.Core.Models;
using AuthForge.Core.Services;
using BCrypt.Net;
using Microsoft.Win32;
using OtpNet;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

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

        private DispatcherTimer _totpTimer;
        private string _currentBase32Secret = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            InitTotpTimer();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            string currentLang = AuthForge.UI.Properties.Settings.Default.UserLanguage;
            _isRussian = (currentLang == "ru");
            string pattern = GetLocalizedText("m_ArgonDetails", "{0} MB, {1} Iterations, {2} Threads");
            ActiveDetailsTxt.Text = string.Format(pattern, MemorySlider.Value, IterSlider.Value, ThreadSlider.Value);
            ActiveDetailsTxt.Visibility = Visibility.Visible;
        }

        private void InitTotpTimer()
        {
            _totpTimer = new DispatcherTimer();
            _totpTimer.Interval = TimeSpan.FromSeconds(1);
            _totpTimer.Tick += TotpTimer_Tick;
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
                    string errorMsg = GetLocalizedText("m_InvalidTemplateMessage", "Invalid Template!");
                    string errorTitle = GetLocalizedText("m_InvalidTemplateTitle", "Format Error");
                    string errorFile = GetLocalizedText("m_InvalidFile", "Invalid file selected");

                    MessageBox.Show(errorMsg, errorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    FilePathTxt.Text = errorFile;
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
            if (Argon2Settings == null || LegacySettings == null || BCryptSettings == null || PBKDF2Settings == null) return;

            Argon2Settings.Visibility = Visibility.Collapsed;
            LegacySettings.Visibility = Visibility.Collapsed;
            BCryptSettings.Visibility = Visibility.Collapsed;
            PBKDF2Settings.Visibility = Visibility.Collapsed;

            switch (AlgoSelector.SelectedIndex)
            {
                case 0:
                    Argon2Settings.Visibility = Visibility.Visible;
                    break;
                case 1:
                    BCryptSettings.Visibility= Visibility.Visible;
                    break;
                case 2:
                    PBKDF2Settings.Visibility = Visibility.Visible;
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

            int bcryptWorkFactor = (int)WorkFactorSlider.Value;

            int pbkdf2Iterations = (int)Pbkdf2IterSlider.Value;
            HashAlgorithmName pbkdf2HashAlgo = Pbkdf2AlgoSelector.SelectedIndex == 1
                ? HashAlgorithmName.SHA512
                : HashAlgorithmName.SHA256;

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

            else if (selectedHasher is BCryptService bc)
            {
                bc.WorkFactor = bcryptWorkFactor;
            }

            else if (selectedHasher is BuiltInHashService pbkdf2)
            {
                pbkdf2.Iterations = pbkdf2Iterations;
                pbkdf2.SelectedHashAlgorithm = pbkdf2HashAlgo;
            }

            try
            {
                ProgressArea.Visibility = Visibility.Visible;
                StartBtn.IsEnabled = false;
                StatusTxt.Text = GetLocalizedText("m_StatusReading", "Reading Excel...");
                HashProgress.Value = 0;

                var users = await Task.Run(() => _excelService.ReadEmployees(_selectedFile));

                var resultsArray = new UserOutput[users.Count];

                int processedCount = 0;

                await Task.Run(() =>
                {
                    Parallel.For(0, users.Count, i =>
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

                        resultsArray[i] = new UserOutput(
                            users[i].Login,
                            hashResult.Hash,
                            hashResult.Salt,
                            selectedHasher.AlgorithmName + (!useSaltForLegacy && selectedHasher is LegacyHashService ? " (No Salt)" : ""));

                        int currentProcessed = Interlocked.Increment(ref processedCount);

                        Dispatcher.Invoke(() => HashProgress.Value = currentProcessed * 100 / users.Count);
                    });
                });
                var results = resultsArray.ToList();

                StatusTxt.Text = GetLocalizedText("m_StatusSaving", "Saving...");
                var savePicker = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "Results.xlsx" };
                if (savePicker.ShowDialog() == true)
                {
                    await Task.Run(() => _excelService.SaveResults(savePicker.FileName, results));
                    string successMsg = GetLocalizedText("m_SuccessMessage", "Success!");
                    string successTitle = GetLocalizedText("m_SuccessTitle", "Forge Complete");
                    MessageBox.Show(successMsg, successTitle);
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

        private string GetLocalizedText(string resourceKey, string defaultValue = "")
        {
            return Application.Current.TryFindResource(resourceKey) as string ?? defaultValue;
        }

        private void UpdateActiveSettingsInfo()
        {
            if (ActiveMethodTxt == null) return;

            var selectedItem = (AlgoSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            ActiveMethodTxt.Text = selectedItem ?? GetLocalizedText("m_NotSelected", "Not Selected"); ;

            if (AlgoSelector.SelectedIndex == 0)
            {
                string pattern = GetLocalizedText("m_ArgonDetails", "{0} MB, {1} Iterations, {2} Threads");
                ActiveDetailsTxt.Text = string.Format(pattern, MemorySlider.Value, IterSlider.Value, ThreadSlider.Value);
                ActiveDetailsTxt.Visibility = Visibility.Visible;
            }
            else if (AlgoSelector.SelectedIndex == 1)
            {
                ActiveDetailsTxt.Text = $"WorkFactor {WorkFactorSlider.Value}";
                ActiveDetailsTxt.Visibility = Visibility.Visible;
            }
            else if (AlgoSelector.SelectedIndex == 2)
            {
                string hashAlgorithm = Pbkdf2AlgoSelector.SelectedIndex == 0 ? "SHA256" : "SHA512";
                string pattern = GetLocalizedText("m_Pbkdf2Details", "Iterations {0}, HashAlgorithm {1}");
                ActiveDetailsTxt.Text = string.Format(pattern, Pbkdf2IterSlider.Value, hashAlgorithm);
                ActiveDetailsTxt.Visibility = Visibility.Visible;
            }
            else if (AlgoSelector.SelectedIndex == 3)
            {
                ActiveDetailsTxt.Text = UseSaltLegacy.IsChecked == true ? GetLocalizedText("m_LegacyWithSalt", "With Salt") : GetLocalizedText("m_LegacyNoSalt", "No Salt (Unsafe)");
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

            else if (selectedHasher is BCryptService bc)
            {
                bc.WorkFactor = (int)WorkFactorSlider.Value;
            }

            else if (selectedHasher is BuiltInHashService pbkdf2)
            {
                pbkdf2.Iterations = (int)Pbkdf2IterSlider.Value;
                pbkdf2.SelectedHashAlgorithm = Pbkdf2AlgoSelector.SelectedIndex == 1
                    ? HashAlgorithmName.SHA512
                    : HashAlgorithmName.SHA256;
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
            ResultSaltTxt.Text = result.Salt ?? GetLocalizedText("m_NoSaltUsed", "No salt used");
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
                UpdateActiveSettingsInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching language: {ex.Message}", "Localization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MemorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateActiveSettingsInfo();
        }

        private void IterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateActiveSettingsInfo();
        }

        private void ThreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateActiveSettingsInfo();
        }

        private void WorkFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateActiveSettingsInfo();
        }

        private void Pbkdf2IterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateActiveSettingsInfo();
        }

        private void Pbkdf2AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActiveSettingsInfo();
        }

        private void TotpSecretInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TotpDisplayCard != null && TotpDisplayCard.Visibility == Visibility.Visible)
            {
                TotpDisplayCard.Visibility = Visibility.Collapsed;
                _totpTimer?.Stop();
            }
        }

        private void ActivateTotp_Click(object sender, RoutedEventArgs e)
        {
            string input = TotpSecretInput.Text?.Trim().ToUpper() ?? string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                TotpDisplayCard.Visibility = Visibility.Collapsed;
                _totpTimer.Stop();
                return;
            }

            try
            {
                Base32Encoding.ToBytes(input);

                _currentBase32Secret = input;
                TotpDisplayCard.Visibility = Visibility.Visible;

                UpdateTotpUI();
                _totpTimer.Start();
            }
            catch
            {
                _totpTimer.Stop();
                TotpDisplayCard.Visibility = Visibility.Collapsed;
                string errorMsg = Application.Current.FindResource("m_TotpValidationError") as string ?? "Error";
                string errorTitle = Application.Current.FindResource("m_TotpValidationTitle") as string ?? "Error";

                MessageBox.Show(errorMsg, errorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TotpTimer_Tick(object sender, EventArgs e)
        {
            UpdateTotpUI();
        }

        private void UpdateTotpUI()
        {
            if (string.IsNullOrEmpty(_currentBase32Secret)) return;

            try
            {
                byte[] secretBytes = Base32Encoding.ToBytes(_currentBase32Secret);
                var totp = new Totp(secretBytes);

                string rawCode = totp.ComputeTotp();

                if (rawCode.Length == 6)
                {
                    TotpCodeTxt.Text = $"{rawCode.Substring(0, 3)} {rawCode.Substring(3, 3)}";
                }
                else
                {
                    TotpCodeTxt.Text = rawCode;
                }

                long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int remainingSeconds = (int)(30 - (currentUnixTime % 30));

                TotpTimeProgress.Value = remainingSeconds;
                string countdownTemplate = Application.Current.FindResource("m_TotpCountdownFormat") as string ?? "{0}s";
                TotpCountdownTxt.Text = string.Format(countdownTemplate, remainingSeconds);
            }
            catch
            {
                _totpTimer.Stop();
                TotpDisplayCard.Visibility = Visibility.Collapsed;
            }
        }

        private void CopyTotpCode_Click(object sender, RoutedEventArgs e)
        {
            string codeToCopy = TotpCodeTxt.Text.Replace(" ", "");
            if (!string.IsNullOrEmpty(codeToCopy) && codeToCopy != "000000")
            {
                Clipboard.SetText(codeToCopy);
            }
        }
    }
}