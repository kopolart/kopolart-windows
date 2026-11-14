using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Services;
using Kopolart.Windows.UI.Constants;
using Kopolart.Windows.UI.Helpers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace Kopolart.Windows.UI.Controls
{
    public class LoaderView : UserControl
    {
        #region Variables

        private Label loaderLabel = null!;
        private PictureBox loaderLogo = null!;
        private ProgressBar loaderProgressBar = null!;
        private Label loaderWarningLabel = null!;
        private Button loaderButton = null!;
        private Panel dotsPanel = null!;
        private Label versionLabel = null!;
        private Label firstRunWelcomeLabel = null!;
        private Label firstRunWelcomeBackLabel = null!;

        private Timer? _loadingTimer;
        private double _currentPercentage;
        // Use the shared, thread-safe Random instance - no per-instance PRNG allocation needed
        private Random _random = Random.Shared;
        private bool _isFinishing;
        private bool _manualControl;
        private string _manualMessage = AppConstants.Strings.LoaderConnecting;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CustomUserName { get; set; }


        private bool _showFirstRunWelcome;
        private static bool _hasShownWelcomeThisSession = false;

        private readonly AnimatedDotsRenderer _dotsRenderer = new(dotDiameter: 16, dotSpacing: 14);

        #endregion

        public LoaderView()
        {
            _showFirstRunWelcome = !_hasShownWelcomeThisSession;

            InitializeComponent();
            FontManager.ApplyCustomFont(this);
            InitializeAnimation();
            ApplyThemeColors();
        }

        private void InitializeComponent()
        {
            loaderLabel = new Label();
            loaderLogo = new PictureBox();
            loaderProgressBar = new ProgressBar();
            loaderWarningLabel = new Label();
            loaderButton = new Button();
            dotsPanel = new Panel();
            versionLabel = new Label();
            firstRunWelcomeLabel = new Label();
            firstRunWelcomeBackLabel = new Label();

            SuspendLayout();

            // loaderLabel - now shows only percentage or status text (no "Loading..." prefix)
            loaderLabel.AutoSize = false;
            loaderLabel.TextAlign = ContentAlignment.MiddleCenter;
            loaderLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            loaderLabel.ForeColor = UIConstants.LoaderLabelColor;
            Controls.Add(loaderLabel);

            // loaderLogo
            loaderLogo.SizeMode = PictureBoxSizeMode.Zoom;
            Controls.Add(loaderLogo);

            // dotsPanel - owner-drawn panel for animated dots (initial size, LayoutLoader will DPI-scale)
            dotsPanel.Size = _dotsRenderer.GetPanelSize(this);
            dotsPanel.BackColor = Color.Transparent;
            dotsPanel.Paint += DotsPanel_Paint;
            Controls.Add(dotsPanel);

            // loaderProgressBar
            loaderProgressBar.Style = ProgressBarStyle.Continuous;
            loaderProgressBar.Visible = false;
            Controls.Add(loaderProgressBar);

            // loaderWarningLabel
            loaderWarningLabel.AutoSize = false;
            loaderWarningLabel.TextAlign = ContentAlignment.MiddleCenter;
            loaderWarningLabel.ForeColor = UIConstants.LoaderWarningColor;
            loaderWarningLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            loaderWarningLabel.Visible = false;
            Controls.Add(loaderWarningLabel);

            // loaderButton
            loaderButton.Text = AppConstants.Strings.LoaderBrowse;
            loaderButton.AutoSize = true;
            loaderButton.Padding = new Padding(15, 8, 15, 8);
            loaderButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            loaderButton.Cursor = Cursors.Hand;
            loaderButton.FlatStyle = FlatStyle.Flat;
            loaderButton.FlatAppearance.BorderSize = 0;
            loaderButton.BackColor = UIConstants.LoaderButtonBackColor;
            loaderButton.ForeColor = UIConstants.LoaderButtonForeColor;
            loaderButton.Visible = false;
            Controls.Add(loaderButton);

            // firstRunWelcomeLabel - shown only on first loader appearance in current app session
            firstRunWelcomeLabel.AutoSize = false;
            firstRunWelcomeLabel.TextAlign = ContentAlignment.MiddleCenter;
            firstRunWelcomeLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            firstRunWelcomeLabel.ForeColor = UIConstants.LoaderLabelColor;
            firstRunWelcomeLabel.Text = GetWelcomeText();
            firstRunWelcomeLabel.Visible = _showFirstRunWelcome;
            Controls.Add(firstRunWelcomeLabel);

            // firstRunWelcomeBackLabel - shown only under the greeting when there is a welcome back msg
            firstRunWelcomeBackLabel.AutoSize = false;
            firstRunWelcomeBackLabel.TextAlign = ContentAlignment.MiddleCenter;
            firstRunWelcomeBackLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            firstRunWelcomeBackLabel.ForeColor = UIConstants.LoaderLabelColor;
            firstRunWelcomeBackLabel.Text = "";
            firstRunWelcomeBackLabel.Visible = false;
            Controls.Add(firstRunWelcomeBackLabel);

            // versionLabel - shows app version at bottom of loader
            versionLabel.AutoSize = false;
            versionLabel.TextAlign = ContentAlignment.MiddleCenter;
            versionLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            versionLabel.ForeColor = Color.FromArgb(140, 110, 80);
            versionLabel.Text = GetVersionText();
            Controls.Add(versionLabel);

            Name = "LoaderView";
            Size = new Size(800, 600); // Default size
            Resize += (s, e) => LayoutLoader();
            ResumeLayout(false);
            PerformLayout();
        }

        public void SetLogo(Image logo)
        {
            if (loaderLogo.Image != null)
            {
                var old = loaderLogo.Image;
                loaderLogo.Image = logo;
                old.Dispose();
            }
            else
            {
                loaderLogo.Image = logo;
            }
            LayoutLoader();
        }

        private IAssetService? _assetService;

        public void SetAssetService(IAssetService assetService)
        {
            _assetService = assetService;
            UpdateThemeLogo();
        }

        private void UpdateThemeLogo()
        {
            if (_assetService != null)
            {
                try
                {
                    var logo = _assetService.LoadLogo(_isDarkMode);
                    if (logo != null)
                    {
                        SetLogo(logo);
                    }
                }
                catch
                {
                    // Ignore asset loading error in UI
                }
            }
        }

        private void ApplyThemeColors()
        {
            RightToLeft = RightToLeft.Yes;
            ApplyDarkMode(_isDarkMode);
        }

        private bool _isDarkMode;

        public void ApplyDarkMode(bool isDark)
        {
            _isDarkMode = isDark;
            _dotsRenderer.SetDarkMode(isDark);
            UpdateThemeLogo();
            
            if (isDark)
            {
                BackColor = UIConstants.DarkMode.BrandBackground;
                loaderLabel.ForeColor = ColorTranslator.FromHtml("#e8e0d8");
                firstRunWelcomeLabel.ForeColor = ColorTranslator.FromHtml("#e8e0d8");
                firstRunWelcomeBackLabel.ForeColor = ColorTranslator.FromHtml("#c4b8ac");
                if (versionLabel != null)
                {
                    versionLabel.ForeColor = ColorTranslator.FromHtml("#9a9088");
                }
            }
            else
            {
                BackColor = UIConstants.BrandBackground;
                loaderLabel.ForeColor = UIConstants.LoaderLabelColor;
                firstRunWelcomeLabel.ForeColor = UIConstants.LoaderLabelColor;
                firstRunWelcomeBackLabel.ForeColor = Color.FromArgb(100, 100, 100);
                if (versionLabel != null)
                {
                    versionLabel.ForeColor = Color.FromArgb(140, 110, 80);
                }
            }
            
            dotsPanel.Invalidate();
        }

        private static string GetVersionText()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                return $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            return "";
        }

        private void LayoutLoader()
        {
            var panelWidth = ClientSize.Width;
            var panelHeight = ClientSize.Height;

            if (panelWidth <= 0 || panelHeight <= 0) return;

            // DPI-scale the dot dimensions
            dotsPanel.Size = _dotsRenderer.GetPanelSize(this);

            var horizontalPadding = 80;
            var maxLogoWidth = 360;
            var minLogoWidth = 117;
            var maxLogoHeight = 252;
            var minLogoHeight = 81;
            var spacing = 18;
            var dotsHeight = dotsPanel.Height;
            var labelHeight = loaderLabel.Height > 0 ? loaderLabel.Height : 30;
            var welcomeLabelHeight = firstRunWelcomeLabel.Height > 0 ? firstRunWelcomeLabel.Height : 24;
            var welcomeBackLabelHeight = firstRunWelcomeBackLabel.Height > 0 ? firstRunWelcomeBackLabel.Height : 20;
            var marginTop = 32;
            var marginBottom = 24;

            bool showWelcomeBack = _showFirstRunWelcome && !string.IsNullOrEmpty(firstRunWelcomeBackLabel.Text);
            var extraWelcomeHeight = _showFirstRunWelcome ? (welcomeLabelHeight + 8 + (showWelcomeBack ? welcomeBackLabelHeight + 6 : 0)) : 0;
            var availableHeightForLogo = panelHeight - marginTop - marginBottom - spacing * 2 - dotsHeight - labelHeight - extraWelcomeHeight;
            if (availableHeightForLogo < minLogoHeight) availableHeightForLogo = minLogoHeight;

            var targetLogoWidth = Math.Min(maxLogoWidth, Math.Max(minLogoWidth, panelWidth - horizontalPadding));
            var targetLogoHeight = Math.Min(maxLogoHeight, availableHeightForLogo);

            var logoSize = AnimatedDotsRenderer.FitLogoSize(loaderLogo.Image, targetLogoWidth, targetLogoHeight);
            targetLogoWidth = logoSize.Width;
            targetLogoHeight = logoSize.Height;

            loaderLogo.Size = new Size(targetLogoWidth, targetLogoHeight);

            var top = marginTop + Math.Max(0, (availableHeightForLogo - targetLogoHeight) / 2);
            loaderLogo.Location = new Point((panelWidth - targetLogoWidth) / 2, top);

            // Dots panel centered below logo
            var dotsTop = top + targetLogoHeight + spacing;
            dotsPanel.Location = new Point((panelWidth - dotsPanel.Width) / 2, dotsTop);

            // Label (percentage / status text) below dots
            var labelTop = dotsTop + dotsHeight + 8;
            loaderLabel.Location = new Point(0, labelTop);
            loaderLabel.Size = new Size(panelWidth, labelHeight);

            if (firstRunWelcomeLabel != null)
            {
                firstRunWelcomeLabel.Size = new Size(panelWidth, welcomeLabelHeight);
                firstRunWelcomeLabel.Location = new Point(0, loaderLabel.Bottom + 20);
            }

            if (firstRunWelcomeBackLabel != null)
            {
                firstRunWelcomeBackLabel.Size = new Size(panelWidth, welcomeBackLabelHeight);
                firstRunWelcomeBackLabel.Location = new Point(0, firstRunWelcomeLabel!.Bottom + 6);
                firstRunWelcomeBackLabel.Visible = showWelcomeBack;
            }

            if (loaderProgressBar != null)
            {
                var barWidth = Math.Min(400, panelWidth - 80);
                loaderProgressBar.Size = new Size(barWidth, 10);
                loaderProgressBar.Location = new Point(
                    (panelWidth - barWidth) / 2,
                    (showWelcomeBack ? (firstRunWelcomeBackLabel?.Bottom ?? 0) : (firstRunWelcomeLabel?.Bottom ?? 0)) + 15
                );
            }

            if (loaderWarningLabel != null)
            {
                loaderWarningLabel.Size = new Size(panelWidth, 20);
                loaderWarningLabel.Location = new Point(0, (loaderProgressBar?.Bottom ?? loaderLabel.Bottom) + 10);
            }

            if (loaderButton != null)
            {
                var preferredSize = loaderButton.GetPreferredSize(Size.Empty);
                loaderButton.Size = preferredSize;
                loaderButton.Location = new Point((panelWidth - preferredSize.Width) / 2, (loaderWarningLabel?.Bottom ?? loaderProgressBar?.Bottom ?? loaderLabel.Bottom) + 20);
            }

            // Version label anchored at the bottom
            if (versionLabel != null)
            {
                versionLabel.Size = new Size(panelWidth, 20);
                versionLabel.Location = new Point(0, panelHeight - 30);
            }
        }

        // -- Dots painting ----------------------------------------------

        private void DotsPanel_Paint(object? sender, PaintEventArgs e)
        {
            _dotsRenderer.Paint(e.Graphics, dotsPanel, new Rectangle(0, 0, dotsPanel.Width, dotsPanel.Height));
        }

        // -- Animation Logic --------------------------------------------

        private void InitializeAnimation()
        {
            _loadingTimer = new Timer();
            _loadingTimer.Interval = 50;
            _loadingTimer.Tick += (sender, args) =>
            {
                if (_isFinishing) return;

                if (!_manualControl)
                {
                    UpdateProgress();
                }
                UpdateLabel();
            };
        }

        /// <summary>
        /// Advances loading percentage with a fake progress curve.
        /// </summary>
        private void UpdateProgress()
        {
            double increment = 0;
            if (_currentPercentage < 30)
                increment = _random.NextDouble() * 8.0 + 5.0;
            else if (_currentPercentage < 70)
                increment = _random.NextDouble() * 5.0 + 3.0;
            else if (_currentPercentage < 95)
                increment = _random.NextDouble() * 3.0 + 1.0;
            else if (_currentPercentage < 99)
                increment = 0.5;

            _currentPercentage += increment;
            if (_currentPercentage > 99) _currentPercentage = 99;
        }

        private void UpdateLabel()
        {
            // Advance the active dot every ~200ms (4 ticks x 50ms)
            if (_dotsRenderer.Tick(advanceEveryNTicks: 4))
            {
                // Repaint the dots panel
                if (dotsPanel.InvokeRequired)
                    dotsPanel.Invoke(new Action(() => dotsPanel.Invalidate()));
                else
                    dotsPanel.Invalidate();
            }

            // Build the text: status + percentage
            string text;
            if (_manualControl && _manualMessage == AppConstants.Strings.LoaderSuccess)
            {
                text = AppConstants.Strings.LoaderSuccess;
            }
            else
            {
                string baseText = _manualControl ? _manualMessage : "";
                string pctStr = PersianNumberHelper.ToPersianDigits(((int)_currentPercentage).ToString());
                text = string.IsNullOrEmpty(baseText)
                    ? $"({pctStr}%)"
                    : $"{baseText} ({pctStr}%)";
            }

            if (loaderLabel.InvokeRequired)
                loaderLabel.Invoke(new Action(() => loaderLabel.Text = text));
            else
                loaderLabel.Text = text;

            if (loaderProgressBar != null)
            {
                int val = (int)_currentPercentage;
                if (val < 0) val = 0;
                if (val > 100) val = 100;

                if (loaderProgressBar.InvokeRequired)
                    loaderProgressBar.Invoke(new Action(() => loaderProgressBar.Value = val));
                else
                    loaderProgressBar.Value = val;
            }
        }

        public void StartLoadingAnimation(string? welcomeMessage = null)
        {
            _currentPercentage = 0;
            _dotsRenderer.Reset();
            _isFinishing = false;
            _manualControl = false;
            _showFirstRunWelcome = !_hasShownWelcomeThisSession;

            Action updateUI = () =>
            {
                loaderLabel.Text = "(0%)";
                loaderLabel.ForeColor = _isDarkMode ? ColorTranslator.FromHtml("#e8e0d8") : UIConstants.LoaderLabelColor;
                dotsPanel.Visible = true;
                loaderProgressBar.Value = 0;
                loaderProgressBar.Visible = false;
                loaderWarningLabel.Visible = false;
                loaderButton.Visible = false;
                
                firstRunWelcomeLabel.Text = GetWelcomeText();
                firstRunWelcomeLabel.Visible = _showFirstRunWelcome;

                if (!string.IsNullOrEmpty(welcomeMessage))
                {
                    firstRunWelcomeBackLabel.Text = welcomeMessage;
                    firstRunWelcomeBackLabel.Visible = _showFirstRunWelcome;
                }
                else
                {
                    firstRunWelcomeBackLabel.Text = "";
                    firstRunWelcomeBackLabel.Visible = false;
                }

                LayoutLoader();
                BringToFront();
                Visible = true;
            };

            if (InvokeRequired) Invoke(updateUI);
            else updateUI();

            _loadingTimer?.Start();
        }

        private string GetWelcomeText()
        {
            if (!string.IsNullOrWhiteSpace(CustomUserName))
            {
                return $"{CustomUserName} عزیز، خوش آمدید!";
            }
            return string.Format(TextManager.GetString("LoaderWelcome"), Environment.UserName);
        }

        public void SetManualProgress(double percentage, string message)
        {
            _manualControl = true;
            _currentPercentage = percentage;
            _manualMessage = message;

            Action showProgress = () =>
            {
                loaderProgressBar.Visible = true;
                _loadingTimer?.Start(); // Progress states animate - ensure the timer runs.
            };
            if (InvokeRequired) Invoke(showProgress);
            else showProgress();
        }

        private EventHandler? _successHandler;

        public void ShowSuccess(Action onContinue)
        {
            _manualControl = true;
            _currentPercentage = 100;
            _manualMessage = "Success!";

            Action updateUI = () =>
            {
                // Static state: stop the 50ms animation timer (nothing animates here,
                // and its tick used to overwrite this label with a hardcoded string).
                _loadingTimer?.Stop();

                loaderLabel.ForeColor = Color.SeaGreen;
                loaderLabel.Text = TextManager.GetString("LoaderSuccess");
                dotsPanel.Visible = false;
                loaderProgressBar.Visible = false;
                loaderWarningLabel.Visible = false;

                loaderButton.Text = TextManager.GetString("LoaderBrowse");
                loaderButton.Visible = true;

                // Remove previous handler to prevent accumulation
                loaderButton.Click -= OnLoaderButtonClick;
                if (_successHandler != null)
                {
                    loaderButton.Click -= _successHandler;
                }

                _successHandler = (s, e) =>
                {
                    loaderButton.Click -= _successHandler;
                    _successHandler = null;
                    loaderButton.Visible = false;
                    StartLoadingAnimation();
                    onContinue?.Invoke();
                };
                loaderButton.Click += _successHandler;
            };

            if (InvokeRequired) Invoke(updateUI);
            else updateUI();
        }

        public void ShowError(string message, string buttonText, Action onRetry)
        {
            _manualControl = true;
            _currentPercentage = 100;
            _manualMessage = AppConstants.Strings.Error;

            Action updateUI = () =>
            {
                // Static error state: stop the 50ms animation timer - it previously kept
                // ticking (string formatting + invalidation) for as long as the error
                // screen was displayed, potentially hours when offline.
                _loadingTimer?.Stop();

                loaderLabel.ForeColor = _isDarkMode ? UIConstants.DarkMode.DangerColor : UIConstants.DangerColor;
                loaderLabel.Text = AppConstants.Strings.LoaderConnectionFailed;
                dotsPanel.Visible = false;

                loaderProgressBar.Visible = false;

                loaderWarningLabel.Text = message;
                loaderWarningLabel.Visible = true;
                loaderWarningLabel.ForeColor = _isDarkMode ? UIConstants.DarkMode.DangerColor : UIConstants.DangerColor;

                loaderButton.Text = buttonText;
                loaderButton.Visible = true;

                // Unsubscribe previous handlers to avoid multiple firings
                if (_successHandler != null)
                {
                    loaderButton.Click -= _successHandler;
                    _successHandler = null;
                }
                loaderButton.Click -= OnLoaderButtonClick;
                loaderButton.Click += OnLoaderButtonClick;
            };

            if (InvokeRequired) Invoke(updateUI);
            else updateUI();

            // Set the action
            _currentAction = onRetry;
        }

        private Action? _currentAction;

        private void OnLoaderButtonClick(object? sender, EventArgs e)
        {
            // Hide button immediately to prevent double clicks
            loaderButton.Visible = false;
            _currentAction?.Invoke();
        }

        public void SetLoading(string message)
        {
            _manualControl = true;
            _manualMessage = message;

            Action updateUI = () =>
            {
                loaderLabel.ForeColor = _isDarkMode ? ColorTranslator.FromHtml("#e8e0d8") : UIConstants.LoaderLabelColor;
                loaderWarningLabel.Visible = false;
                loaderButton.Visible = false;
                dotsPanel.Visible = true;
                loaderProgressBar.Visible = true;
                _loadingTimer?.Start(); // Restart animation after a static error/success state.
            };

            if (InvokeRequired) Invoke(updateUI);
            else updateUI();
        }

        public void ShowWarning(string message)
        {
            Action updateUI = () =>
            {
                loaderWarningLabel.Text = message;
                loaderWarningLabel.Visible = true;
                loaderWarningLabel.ForeColor = _isDarkMode ? ColorTranslator.FromHtml("#e8e0d8") : UIConstants.LoaderWarningColor;
            };
            if (InvokeRequired) Invoke(updateUI);
            else updateUI();
        }

        public async Task StopLoadingAnimationAsync(bool instant = false)
        {
            _isFinishing = true;
            _loadingTimer?.Stop();

            if (instant)
            {
                _currentPercentage = 100;
                UpdateLabel();
                await Task.Delay(50);
            }
            else
            {
                while (_currentPercentage < 100)
                {
                    double remaining = 100 - _currentPercentage;
                    double step = Math.Max(5.0, remaining / 3.0);
                    _currentPercentage += step;
                    if (_currentPercentage > 100) _currentPercentage = 100;
                    UpdateLabel();
                    await Task.Delay(6);
                }
            }

            Action finishUI = () =>
            {
                 loaderLabel.Text = $"({PersianNumberHelper.ToPersianDigits("100")}%)";
                 dotsPanel.Visible = false;
                 firstRunWelcomeLabel.Visible = false;
                 firstRunWelcomeBackLabel.Visible = false;
                 Visible = false;
                 SendToBack();
                 loaderProgressBar.Visible = false;
                 loaderWarningLabel.Visible = false;
                 loaderButton.Visible = false;
            };

            if (InvokeRequired) Invoke(finishUI);
            else finishUI();

            if (_showFirstRunWelcome)
            {
                _hasShownWelcomeThisSession = true;
                _showFirstRunWelcome = false;
            }

            _currentPercentage = 0;
            _isFinishing = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadingTimer?.Stop();
                _loadingTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}


