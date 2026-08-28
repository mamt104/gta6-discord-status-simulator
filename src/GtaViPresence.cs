using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Grand Theft Auto VI")]
[assembly: AssemblyDescription("Grand Theft Auto VI")]
[assembly: AssemblyCompany("Community Project")]
[assembly: AssemblyProduct("Grand Theft Auto VI")]
[assembly: AssemblyVersion("1.4.1.0")]
[assembly: AssemblyFileVersion("1.4.1.0")]

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            Application.Run(new PresenceForm());
        }
        catch (Exception error)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-crash.log"), error.ToString());
            }
            catch { }
            MessageBox.Show("GTA VI Presence Studio could not start.\r\n\r\n" + error.Message,
                "Startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class PresenceForm : Form
{
    private const string DefaultAppId = "";
    private const string RealisticAutoModeLabel = "Realistic Auto (Resume)";
    private const string DefaultActivityButtonLabel = "Join";
    private const string DefaultActivityButtonUrl = "https://store.rockstargames.com/game/buy-gta-vi";
    private const string DefaultSecondButtonLabel = "Official Website";
    private const string DefaultSecondButtonUrl = "https://www.rockstargames.com/VI";
    private const string ProjectUrl = "https://github.com/mamt104/gta6-discord-status-simulator";
    private readonly TextBox appIdBox;
    private readonly Label connectionLabel;
    private readonly Button connectButton;
    private readonly RadioButton officialModeBox;
    private readonly RadioButton customModeBox;
    private readonly CardPanel nativeModeCard;
    private readonly CardPanel customModeCard;
    private readonly Label customModeDescriptionLabel;
    private readonly CheckBox startupDelayBox;
    private readonly CheckBox joinGameBox;
    private readonly CheckBox secondButtonBox;
    private readonly Button editJoinButton;
    private readonly Button editSecondButton;
    private readonly Button applyPresetButton;
    private readonly Button nextSceneButton;
    private readonly Button applyCustomButton;
    private readonly ComboBox sessionModeBox;
    private readonly ComboBox characterBox;
    private readonly ComboBox statusBox;
    private readonly ComboBox rotationIntervalBox;
    private readonly TextBox customStatusBox;
    private readonly Label customCountLabel;
    private readonly Label currentStatusLabel;
    private readonly Label modeExplanationLabel;
    private readonly Label sessionHeaderLabel;
    private readonly Label previewTitleLabel;
    private readonly Label previewStateLabel;
    private readonly Label previewDetailsLabel;
    private readonly Label previewTimerLabel;
    private readonly Label previewModeNoteLabel;
    private readonly CardPanel discordPreviewCard;
    private readonly Button previewJoinButton;
    private readonly Button previewSecondButton;
    private readonly Label presenceModeValueLabel;
    private readonly Label sessionModeValueLabel;
    private readonly Label playingAsValueLabel;
    private readonly Label activityValueLabel;
    private readonly Label nextChangeValueLabel;
    private readonly Label contextInfoLabel;
    private readonly Label diagnosticDiscordValue;
    private readonly Label diagnosticAppIdValue;
    private readonly Label diagnosticRpcValue;
    private readonly Label diagnosticAssetValue;
    private readonly Label diagnosticButtonsValue;
    private readonly Label diagnosticStorageValue;
    private readonly Label diagnosticLastSendValue;
    private readonly ComboBox profileBox;
    private readonly Label profileStatusLabel;
    private readonly Button updateCheckButton;
    private readonly Label updateStatusLabel;
    private readonly PictureBox previewImageBox;
    private readonly System.Windows.Forms.Timer uiCountdownTimer;
    private readonly NotifyIcon trayIcon;
    private readonly DiscordRpcClient rpc;
    private readonly string configPath;
    private readonly string profilePath;
    private readonly string profilesDirectory;
    private Action openSettingsPage;
    private bool exiting;
    private bool loadingProfile;
    private bool onboardingComplete;
    private bool officialModeEnabled = true;
    private string activeMode = "Auto";
    private string activeStatus = "";
    private string activeState = "";
    private string activeSessionMode = RealisticAutoModeLabel;
    private string activeCharacter = "Automatic";
    private string activeJoinLabel = DefaultActivityButtonLabel;
    private string activeJoinUrl = DefaultActivityButtonUrl;
    private string activeSecondButtonLabel = DefaultSecondButtonLabel;
    private string activeSecondButtonUrl = DefaultSecondButtonUrl;
    private string appliedApplicationId = "";
    private int activeRotationMinutes;
    private string currentActivityTemplate = "";
    private long currentActivityDeadline;
    private long customSessionStarted;
    private bool rpcConnected;
    private string lastRpcMessage = "Not connected";
    private DateTime lastActivitySentUtc = DateTime.MinValue;

    private const int EmSetCueBanner = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public PresenceForm()
    {
        Text = "Grand Theft Auto VI";
        ClientSize = new Size(1360, 760);
        MinimumSize = new Size(720, 540);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(8, 9, 14);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.2f);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-presence.txt");
        profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-profile.ini");
        profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");
        rpc = new DiscordRpcClient();
        rpc.StatusChanged += OnRpcStatusChanged;
        rpc.ActivityChanged += OnActivityChanged;
        uiCountdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        uiCountdownTimer.Tick += delegate { UpdateCurrentStatusCountdown(); RefreshDashboard(); };
        uiCountdownTimer.Start();

        Color pink = Color.FromArgb(235, 51, 145);
        Color purple = Color.FromArgb(85, 82, 184);
        Color orange = Color.FromArgb(230, 133, 16);
        Color green = Color.FromArgb(70, 204, 105);
        Color muted = Color.FromArgb(165, 165, 181);
        Color card = Color.FromArgb(18, 19, 27);
        Color cardRaised = Color.FromArgb(24, 25, 35);
        Color field = Color.FromArgb(20, 21, 30);
        Color border = Color.FromArgb(43, 44, 58);

        TableLayoutPanel root = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
            BackColor = BackColor, Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        Controls.Add(root);

        Panel header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(9, 10, 16), Margin = Padding.Empty };
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = border });
        PictureBox headerIcon = new PictureBox
        {
            Location = new Point(17, 13), Size = new Size(32, 32), SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadPreviewImage()
        };
        header.Controls.Add(headerIcon);
        Label headerTitle = new Label
        {
            Location = new Point(58, 3), Size = new Size(150, 33), Text = "GTA VI",
            Font = new Font("Arial Black", 16f, FontStyle.Bold), ForeColor = Color.White,
            BackColor = Color.Transparent
        };
        header.Controls.Add(headerTitle);
        Label headerSubtitle = new Label
        {
            Location = new Point(60, 36), Size = new Size(180, 18), Text = "PRESENCE STUDIO",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = pink,
            BackColor = Color.Transparent
        };
        header.Controls.Add(headerSubtitle);
        connectionLabel = new Label
        {
            Location = new Point(796, 16), Size = new Size(530, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = "●  Native Discord Detection", TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI Semibold", 9.8f, FontStyle.Bold), ForeColor = green
        };
        header.Controls.Add(connectionLabel);

        Panel sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 11, 18), Margin = Padding.Empty };
        root.Controls.Add(sidebar, 0, 1);
        sidebar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = border });
        Panel activeRail = new Panel { Location = new Point(0, 14), Size = new Size(4, 82), BackColor = pink };
        sidebar.Controls.Add(activeRail);
        Button studioNav = MakeSidebarButton("STUDIO", new Point(8, 18), pink, true);
        Button settingsNav = MakeSidebarButton("SETTINGS", new Point(8, 102), muted, false);
        Button profilesNav = MakeSidebarButton("PROFILES", new Point(8, 186), muted, false);
        Button diagnosticsNav = MakeSidebarButton("DIAGNOSTICS", new Point(8, 270), muted, false);
        Button aboutNav = MakeSidebarButton("ABOUT", new Point(8, 354), muted, false);
        sidebar.Controls.Add(studioNav);
        sidebar.Controls.Add(settingsNav);
        sidebar.Controls.Add(profilesNav);
        sidebar.Controls.Add(diagnosticsNav);
        sidebar.Controls.Add(aboutNav);
        Label sidebarState = new Label
        {
            Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(16, 7, 6, 0),
            Text = "●  READY\r\n    Local controls", Font = new Font("Segoe UI Semibold", 8.4f),
            ForeColor = green
        };
        sidebar.Controls.Add(sidebarState);

        TableLayoutPanel dashboard = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = BackColor, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty
        };
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        root.Controls.Add(dashboard, 1, 1);

        FlowLayoutPanel leftStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(0, 0, 6, 0), Margin = new Padding(0, 0, 7, 0)
        };
        FlowLayoutPanel rightStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(7, 0, 0, 0)
        };
        dashboard.Controls.Add(leftStack, 0, 0);
        dashboard.Controls.Add(rightStack, 1, 0);
        FlowLayoutPanel compactStack = MakeCompactStack(pink, BackColor);
        dashboard.Controls.Add(compactStack, 0, 0);
        dashboard.SetColumnSpan(compactStack, 2);

        TableLayoutPanel settingsDashboard = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Visible = false,
            BackColor = BackColor, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty
        };
        settingsDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        settingsDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.Controls.Add(settingsDashboard, 1, 1);

        FlowLayoutPanel settingsLeftStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(0, 0, 6, 0), Margin = new Padding(0, 0, 7, 0)
        };
        FlowLayoutPanel settingsRightStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(7, 0, 0, 0)
        };
        settingsDashboard.Controls.Add(settingsLeftStack, 0, 0);
        settingsDashboard.Controls.Add(settingsRightStack, 1, 0);
        FlowLayoutPanel settingsCompactStack = MakeCompactStack(pink, BackColor);
        settingsDashboard.Controls.Add(settingsCompactStack, 0, 0);
        settingsDashboard.SetColumnSpan(settingsCompactStack, 2);

        TableLayoutPanel aboutDashboard = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Visible = false,
            BackColor = BackColor, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty
        };
        aboutDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        aboutDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.Controls.Add(aboutDashboard, 1, 1);

        FlowLayoutPanel aboutLeftStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(0, 0, 6, 0), Margin = new Padding(0, 0, 7, 0)
        };
        FlowLayoutPanel aboutRightStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(7, 0, 0, 0)
        };
        aboutDashboard.Controls.Add(aboutLeftStack, 0, 0);
        aboutDashboard.Controls.Add(aboutRightStack, 1, 0);
        FlowLayoutPanel aboutCompactStack = MakeCompactStack(pink, BackColor);
        aboutDashboard.Controls.Add(aboutCompactStack, 0, 0);
        aboutDashboard.SetColumnSpan(aboutCompactStack, 2);

        TableLayoutPanel profilesDashboard = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Visible = false,
            BackColor = BackColor, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty
        };
        profilesDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        profilesDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.Controls.Add(profilesDashboard, 1, 1);
        FlowLayoutPanel profilesLeftStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(0, 0, 6, 0), Margin = new Padding(0, 0, 7, 0)
        };
        FlowLayoutPanel profilesRightStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(7, 0, 0, 0)
        };
        profilesDashboard.Controls.Add(profilesLeftStack, 0, 0);
        profilesDashboard.Controls.Add(profilesRightStack, 1, 0);
        FlowLayoutPanel profilesCompactStack = MakeCompactStack(pink, BackColor);
        profilesDashboard.Controls.Add(profilesCompactStack, 0, 0);
        profilesDashboard.SetColumnSpan(profilesCompactStack, 2);

        TableLayoutPanel diagnosticsDashboard = new BufferedTableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Visible = false,
            BackColor = BackColor, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty
        };
        diagnosticsDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        diagnosticsDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.Controls.Add(diagnosticsDashboard, 1, 1);
        FlowLayoutPanel diagnosticsLeftStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(0, 0, 6, 0), Margin = new Padding(0, 0, 7, 0)
        };
        FlowLayoutPanel diagnosticsRightStack = new StyledFlowLayoutPanel(pink)
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = BackColor, Padding = new Padding(6, 0, 0, 0), Margin = new Padding(7, 0, 0, 0)
        };
        diagnosticsDashboard.Controls.Add(diagnosticsLeftStack, 0, 0);
        diagnosticsDashboard.Controls.Add(diagnosticsRightStack, 1, 0);
        FlowLayoutPanel diagnosticsCompactStack = MakeCompactStack(pink, BackColor);
        diagnosticsDashboard.Controls.Add(diagnosticsCompactStack, 0, 0);
        diagnosticsDashboard.SetColumnSpan(diagnosticsCompactStack, 2);

        CardPanel aboutHeroCard = MakeCard(720, 190, card, border);
        aboutHeroCard.AccentColor = pink;
        aboutHeroCard.AccentWidth = 4;
        aboutLeftStack.Controls.Add(aboutHeroCard);
        aboutHeroCard.Controls.Add(MakeSectionTitle("ABOUT THE PROJECT", new Point(18, 14), pink));
        aboutHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 48), Size = new Size(650, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "GTA VI PRESENCE STUDIO", Font = new Font("Arial Black", 18f, FontStyle.Bold), ForeColor = Color.White
        });
        aboutHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 92), Size = new Size(650, 46), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "An open-source Windows utility for native Discord game detection and configurable GTA VI-style Rich Presence.",
            Font = new Font("Segoe UI", 10.2f), ForeColor = muted
        });
        aboutHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 147), Size = new Size(160, 27), Text = "VERSION 1.4.1",
            TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(29, 42, 37), ForeColor = green,
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold)
        });
        Label creatorCreditLabel = new Label
        {
            Location = new Point(194, 147), Size = new Size(430, 27),
            Text = "CREATED BY CYBERPINO  •  @MAMT104 ON GITHUB",
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = pink,
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold)
        };
        aboutHeroCard.Controls.Add(creatorCreditLabel);
        aboutHeroCard.Resize += delegate
        {
            creatorCreditLabel.Width = Math.Max(220, aboutHeroCard.ClientSize.Width - creatorCreditLabel.Left - 18);
        };

        CardPanel capabilitiesCard = MakeCard(720, 252, card, border);
        capabilitiesCard.AccentColor = orange;
        capabilitiesCard.AccentWidth = 4;
        aboutLeftStack.Controls.Add(capabilitiesCard);
        capabilitiesCard.Controls.Add(MakeSectionTitle("WHAT IT DOES", new Point(18, 14), orange));
        capabilitiesCard.Controls.Add(MakeSettingsHint("NATIVE DETECTION", "Lets Discord manage the game card, icon, timer, click behavior and recent activity.", 52, green));
        capabilitiesCard.Controls.Add(MakeSettingsHint("CUSTOM PRESENCE", "Unlocks scenes, characters, custom descriptions, rotation and an optional activity button.", 100, pink));
        capabilitiesCard.Controls.Add(MakeSettingsHint("LOCAL PROFILES", "Saves your choices beside the executable so the next session is ready immediately.", 148, purple));
        capabilitiesCard.Controls.Add(MakeSettingsHint("COMMUNITY BUILT", "Source, releases, feedback and bug reports are public on GitHub.", 196, orange));

        CardPanel trustCard = MakeCard(720, 190, card, border);
        trustCard.AccentColor = green;
        trustCard.AccentWidth = 4;
        aboutLeftStack.Controls.Add(trustCard);
        trustCard.Controls.Add(MakeSectionTitle("LOCAL, TRANSPARENT, REVERSIBLE", new Point(18, 14), green));
        trustCard.Controls.Add(new Label
        {
            Location = new Point(18, 51), Size = new Size(660, 112), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "No Bot Token, Client Secret, Discord password or administrator access is required.\r\n\r\nThis is an unofficial fan-made utility. It does not include GTA VI, modify Discord, or prove access to a game build.",
            Font = new Font("Segoe UI", 9.5f), ForeColor = Color.White
        });

        CardPanel supportCard = MakeCard(500, 292, card, border);
        supportCard.AccentColor = pink;
        supportCard.AccentWidth = 4;
        aboutRightStack.Controls.Add(supportCard);
        supportCard.Controls.Add(MakeSectionTitle("SUPPORT THE PROJECT", new Point(18, 14), pink));
        supportCard.Controls.Add(new Label
        {
            Location = new Point(18, 52), Size = new Size(452, 34), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "ENJOYING THE STUDIO?", Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold), ForeColor = Color.White
        });
        supportCard.Controls.Add(new Label
        {
            Location = new Point(18, 91), Size = new Size(452, 70), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "A GitHub star helps other people discover it. Sharing the project with a friend helps even more.",
            Font = new Font("Segoe UI", 9.7f), ForeColor = muted
        });
        Button starButton = MakeButton("STAR ON GITHUB", new Point(18, 174), 215, pink);
        starButton.Click += delegate { Process.Start(ProjectUrl); };
        supportCard.Controls.Add(starButton);
        Button copyProjectButton = MakeButton("COPY PROJECT LINK", new Point(245, 174), 225, purple);
        copyProjectButton.Click += delegate
        {
            try
            {
                Clipboard.SetText(ProjectUrl);
                MessageBox.Show("Project link copied. Thanks for sharing it!", "Link copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { MessageBox.Show(ProjectUrl, "Project link", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        };
        supportCard.Controls.Add(copyProjectButton);
        supportCard.Controls.Add(new Label
        {
            Location = new Point(18, 230), Size = new Size(452, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Open the repository, press Star in the top-right corner, or send the copied link to someone who would enjoy it.",
            Font = new Font("Segoe UI", 8.6f), ForeColor = green
        });

        CardPanel feedbackCard = MakeCard(500, 222, card, border);
        feedbackCard.AccentColor = purple;
        feedbackCard.AccentWidth = 4;
        aboutRightStack.Controls.Add(feedbackCard);
        feedbackCard.Controls.Add(MakeSectionTitle("FEEDBACK & IDEAS", new Point(18, 14), purple));
        feedbackCard.Controls.Add(new Label
        {
            Location = new Point(18, 51), Size = new Size(452, 64), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Suggest new scenes, UI improvements or future support for other games. Reproducible bugs belong in Issues.",
            Font = new Font("Segoe UI", 9.5f), ForeColor = Color.White
        });
        Button feedbackButton = MakeButton("SHARE FEEDBACK", new Point(18, 136), 215, purple);
        feedbackButton.Click += delegate { Process.Start(ProjectUrl + "/discussions/1"); };
        feedbackCard.Controls.Add(feedbackButton);
        Button issueButton = MakeButton("REPORT AN ISSUE", new Point(245, 136), 225, Color.FromArgb(50, 42, 50));
        issueButton.Click += delegate { Process.Start(ProjectUrl + "/issues"); };
        feedbackCard.Controls.Add(issueButton);

        CardPanel licenseCard = MakeCard(500, 238, card, border);
        licenseCard.AccentColor = green;
        licenseCard.AccentWidth = 4;
        aboutRightStack.Controls.Add(licenseCard);
        licenseCard.Controls.Add(MakeSectionTitle("OPEN SOURCE", new Point(18, 14), green));
        licenseCard.Controls.Add(new Label
        {
            Location = new Point(18, 52), Size = new Size(452, 54), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Released under the MIT License. Questions, pull requests and community testing are welcome.",
            Font = new Font("Segoe UI", 9.5f), ForeColor = muted
        });
        updateCheckButton = MakeButton("CHECK FOR UPDATES", new Point(18, 122), 215, purple);
        updateCheckButton.Click += delegate { CheckForUpdates(); };
        licenseCard.Controls.Add(updateCheckButton);
        updateStatusLabel = new Label
        {
            Location = new Point(245, 122), Size = new Size(225, 64), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Manual check only. No analytics or background telemetry.",
            Font = new Font("Segoe UI", 8.4f), ForeColor = muted
        };
        licenseCard.Controls.Add(updateStatusLabel);

        CardPanel settingsHeroCard = MakeCard(720, 106, card, border);
        settingsHeroCard.AccentColor = pink;
        settingsHeroCard.AccentWidth = 4;
        settingsLeftStack.Controls.Add(settingsHeroCard);
        settingsHeroCard.Controls.Add(MakeSectionTitle("SETTINGS", new Point(18, 14), pink));
        settingsHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 44), Size = new Size(670, 46), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Configure the Discord connection and startup behavior. Activity-button controls live in Studio.",
            Font = new Font("Segoe UI", 9.4f), ForeColor = Color.White
        });

        CardPanel presenceModeCard = MakeCard(690, 138, card, border);
        presenceModeCard.AccentColor = pink;
        presenceModeCard.AccentWidth = 4;
        leftStack.Controls.Add(presenceModeCard);
        presenceModeCard.Controls.Add(MakeSectionTitle("PRESENCE MODE", new Point(18, 13), muted));

        customModeCard = MakeCard(320, 78, cardRaised, border);
        customModeCard.Location = new Point(18, 43);
        customModeCard.Cursor = Cursors.Hand;
        presenceModeCard.Controls.Add(customModeCard);
        customModeBox = new RadioButton
        {
            Location = new Point(15, 16), Size = new Size(24, 24), Text = "", Checked = false,
            AutoCheck = false, BackColor = cardRaised, ForeColor = pink
        };
        customModeCard.Controls.Add(customModeBox);
        Label customModeTitleLabel = new Label
        {
            Location = new Point(49, 11), Size = new Size(250, 24), Text = "Custom Rich Presence",
            Font = new Font("Segoe UI Semibold", 10.8f, FontStyle.Bold), ForeColor = Color.White, BackColor = cardRaised
        };
        customModeCard.Controls.Add(customModeTitleLabel);
        customModeDescriptionLabel = new Label
        {
            Location = new Point(49, 36), Size = new Size(252, 35),
            Text = "LOCKED  •  Complete Application ID setup first.",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = orange, BackColor = cardRaised
        };
        customModeCard.Controls.Add(customModeDescriptionLabel);

        nativeModeCard = MakeCard(320, 78, cardRaised, border);
        nativeModeCard.Location = new Point(352, 43);
        nativeModeCard.Cursor = Cursors.Hand;
        presenceModeCard.Controls.Add(nativeModeCard);
        officialModeBox = new RadioButton
        {
            Location = new Point(15, 16), Size = new Size(24, 24), Text = "", Checked = true,
            BackColor = cardRaised, ForeColor = green
        };
        nativeModeCard.Controls.Add(officialModeBox);
        Label nativeModeTitleLabel = new Label
        {
            Location = new Point(49, 11), Size = new Size(250, 24), Text = "Discord Game Detection",
            Font = new Font("Segoe UI Semibold", 10.8f, FontStyle.Bold), ForeColor = Color.White, BackColor = cardRaised
        };
        nativeModeCard.Controls.Add(nativeModeTitleLabel);
        Label nativeModeDescriptionLabel = new Label
        {
            Location = new Point(49, 36), Size = new Size(252, 35),
            Text = "Uses GTA6.exe and Discord Registered Games.",
            Font = new Font("Segoe UI", 8.5f), ForeColor = muted, BackColor = cardRaised
        };
        nativeModeCard.Controls.Add(nativeModeDescriptionLabel);
        customModeCard.Click += delegate { TryActivateCustomMode(); };
        nativeModeCard.Click += delegate { officialModeBox.Checked = true; };
        foreach (Control control in customModeCard.Controls) control.Click += delegate { TryActivateCustomMode(); };
        foreach (Control control in nativeModeCard.Controls) control.Click += delegate { officialModeBox.Checked = true; };
        officialModeBox.CheckedChanged += delegate
        {
            if (loadingProfile || !officialModeBox.Checked) return;
            customModeBox.Checked = false;
            officialModeEnabled = true;
            ApplyPresenceMode();
            SaveProfile();
        };
        customModeBox.CheckedChanged += delegate
        {
            if (loadingProfile || !customModeBox.Checked) return;
            officialModeBox.Checked = false;
            officialModeEnabled = false;
            ApplyPresenceMode();
            SaveProfile();
        };
        presenceModeCard.Resize += delegate
        {
            bool narrow = presenceModeCard.ClientSize.Width < 600;
            int optionWidth;
            if (narrow)
            {
                optionWidth = Math.Max(300, presenceModeCard.ClientSize.Width - 36);
                customModeCard.SetBounds(18, 43, optionWidth, 78);
                nativeModeCard.SetBounds(18, 129, optionWidth, 78);
                presenceModeCard.Height = 224;
            }
            else
            {
                int gap = 14;
                optionWidth = Math.Max(250, (presenceModeCard.ClientSize.Width - 36 - gap) / 2);
                customModeCard.SetBounds(18, 43, optionWidth, 78);
                nativeModeCard.SetBounds(18 + optionWidth + gap, 43, optionWidth, 78);
                presenceModeCard.Height = 138;
            }
            customModeTitleLabel.Width = Math.Max(180, optionWidth - 62);
            customModeDescriptionLabel.Width = Math.Max(180, optionWidth - 62);
            nativeModeTitleLabel.Width = Math.Max(180, optionWidth - 62);
            nativeModeDescriptionLabel.Width = Math.Max(180, optionWidth - 62);
        };

        CardPanel applicationCard = MakeCard(690, 150, card, border);
        applicationCard.AccentColor = purple;
        applicationCard.AccentWidth = 4;
        settingsLeftStack.Controls.Add(applicationCard);
        applicationCard.Controls.Add(MakeSectionTitle("APPLICATION SETTINGS", new Point(18, 13), muted));
        applicationCard.Controls.Add(new Label
        {
            Location = new Point(18, 43), Size = new Size(210, 19), Text = "DISCORD APPLICATION ID",
            Font = new Font("Segoe UI Semibold", 8.2f, FontStyle.Bold), ForeColor = muted
        });

        appIdBox = new TextBox
        {
            Location = new Point(18, 65), Size = new Size(410, 32), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 10.3f), BackColor = field,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        appIdBox.Leave += delegate { SaveApplicationIdDraft(); };
        appIdBox.TextChanged += delegate
        {
            if (loadingProfile) return;
            if (customModeBox.Checked && !IsCustomSetupReady())
            {
                officialModeBox.Checked = true;
                customModeBox.Checked = false;
            }
            ApplyPresenceMode();
        };
        applicationCard.Controls.Add(appIdBox);

        connectButton = MakeButton("APPLY ID", new Point(442, 63), 108, pink);
        connectButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        connectButton.Click += delegate { ConnectPresence(); };
        applicationCard.Controls.Add(connectButton);

        Button portalButton = MakeButton("PORTAL", new Point(562, 63), 108, purple);
        portalButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        portalButton.Click += delegate { Process.Start("https://discord.com/developers/applications"); };
        applicationCard.Controls.Add(portalButton);

        startupDelayBox = new CheckBox
        {
            Location = new Point(18, 110), Size = new Size(210, 24), Checked = false,
            Text = "5-minute startup delay",
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = card
        };
        startupDelayBox.CheckedChanged += delegate
        {
            rpc.SetStartupDelayEnabled(startupDelayBox.Checked);
            SaveProfile();
        };
        applicationCard.Controls.Add(startupDelayBox);

        CardPanel activityButtonsCard = MakeCard(690, 206, card, border);
        activityButtonsCard.AccentColor = purple;
        activityButtonsCard.AccentWidth = 4;
        activityButtonsCard.Controls.Add(MakeSectionTitle("ACTIVITY BUTTONS", new Point(18, 13), purple));
        activityButtonsCard.Controls.Add(new Label
        {
            Location = new Point(18, 43), Size = new Size(652, 34), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Optional links for other Discord users. Configure their text and destination directly from Studio.",
            Font = new Font("Segoe UI", 8.6f), ForeColor = muted
        });

        joinGameBox = new CheckBox
        {
            Location = new Point(18, 88), Size = new Size(360, 24), Checked = false,
            Text = "Button 1 — Join",
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = card
        };
        joinGameBox.CheckedChanged += delegate
        {
            rpc.SetJoinButtonEnabled(joinGameBox.Checked);
            SaveProfile();
            RefreshDashboard();
        };
        activityButtonsCard.Controls.Add(joinGameBox);

        editJoinButton = MakeButton("CONFIGURE", new Point(532, 82), 138, purple);
        editJoinButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        editJoinButton.Size = new Size(138, 32);
        editJoinButton.Click += delegate { EditActivityButtonSettings(1); };
        activityButtonsCard.Controls.Add(editJoinButton);

        secondButtonBox = new CheckBox
        {
            Location = new Point(18, 130), Size = new Size(360, 24), Checked = false,
            Text = "Button 2 — Official Website",
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = card
        };
        secondButtonBox.CheckedChanged += delegate
        {
            rpc.SetSecondButtonEnabled(secondButtonBox.Checked);
            SaveProfile();
            RefreshDashboard();
        };
        activityButtonsCard.Controls.Add(secondButtonBox);

        editSecondButton = MakeButton("CONFIGURE", new Point(532, 124), 138, purple);
        editSecondButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        editSecondButton.Size = new Size(138, 32);
        editSecondButton.Click += delegate { EditActivityButtonSettings(2); };
        activityButtonsCard.Controls.Add(editSecondButton);
        Label buttonVisibilityNote = new Label
        {
            Location = new Point(18, 169), Size = new Size(652, 26), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Discord supports up to two URL buttons. They are visible to other users, not on your own profile.",
            Font = new Font("Segoe UI", 7.9f), ForeColor = muted
        };
        activityButtonsCard.Controls.Add(buttonVisibilityNote);
        ToolTip tips = new ToolTip();
        tips.SetToolTip(startupDelayBox, "Starts Custom Rich Presence without a description for the first five minutes.");
        tips.SetToolTip(joinGameBox, "Adds a customizable URL button that other Discord users may see.");
        tips.SetToolTip(secondButtonBox, "Adds the second and final URL button allowed by Discord.");
        activityButtonsCard.Resize += delegate
        {
            int clientWidth = activityButtonsCard.ClientSize.Width;
            int configureWidth = Math.Min(138, Math.Max(112, clientWidth / 4));
            editJoinButton.SetBounds(clientWidth - configureWidth - 18, 82, configureWidth, 32);
            editSecondButton.SetBounds(clientWidth - configureWidth - 18, 124, configureWidth, 32);
            joinGameBox.Width = Math.Max(210, clientWidth - configureWidth - 54);
            secondButtonBox.Width = Math.Max(210, clientWidth - configureWidth - 54);
            buttonVisibilityNote.Width = Math.Max(260, clientWidth - 36);
        };

        CardPanel sessionCard = MakeCard(690, 394, card, border);
        sessionCard.AccentColor = orange;
        sessionCard.AccentWidth = 4;
        leftStack.Controls.Add(sessionCard);
        sessionHeaderLabel = new Label
        {
            Location = new Point(18, 13), Size = new Size(360, 24), Text = "SESSION DIRECTOR",
            Font = new Font("Segoe UI Semibold", 10.4f, FontStyle.Bold), ForeColor = orange, BackColor = card
        };
        sessionCard.Controls.Add(sessionHeaderLabel);
        Label modeLabel = new Label
        {
            Location = new Point(18, 46), Size = new Size(300, 19), Text = "MODE",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        };
        Label characterLabel = new Label
        {
            Location = new Point(352, 46), Size = new Size(300, 19), Text = "PLAYING AS",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        };
        sessionCard.Controls.Add(modeLabel);
        sessionCard.Controls.Add(characterLabel);

        sessionModeBox = MakeComboBox(new Point(18, 68), 318);
        sessionModeBox.Items.AddRange(new object[]
        {
            RealisticAutoModeLabel, "Free Roam", "Story Mission", "Main Menu", "Paused", "Manual"
        });
        sessionModeBox.SelectedIndex = 0;
        sessionModeBox.SelectedIndexChanged += delegate { if (!loadingProfile) RefreshDashboard(); };
        sessionCard.Controls.Add(sessionModeBox);

        characterBox = MakeComboBox(new Point(352, 68), 318);
        characterBox.Items.AddRange(new object[] { "Automatic", "Jason Duval", "Lucia Caminos", "Jason & Lucia" });
        characterBox.SelectedIndex = 0;
        characterBox.SelectedIndexChanged += delegate { if (!loadingProfile) RefreshDashboard(); };
        sessionCard.Controls.Add(characterBox);

        Label activityLabel = new Label
        {
            Location = new Point(18, 112), Size = new Size(400, 19), Text = "ACTIVITY / SCENE",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        };
        sessionCard.Controls.Add(activityLabel);
        statusBox = MakeComboBox(new Point(18, 134), 390);
        statusBox.Items.Add("Suggested activity for this mode");
        foreach (string activity in DiscordRpcClient.GetActivities()) statusBox.Items.Add(activity);
        statusBox.SelectedIndex = 0;
        sessionCard.Controls.Add(statusBox);

        applyPresetButton = MakeButton("SET STATUS", new Point(422, 132), 116, pink);
        applyPresetButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        applyPresetButton.Click += delegate { ApplySessionProfile(); };
        sessionCard.Controls.Add(applyPresetButton);

        nextSceneButton = MakeButton("NEXT SCENE", new Point(550, 132), 120, Color.FromArgb(44, 45, 58));
        nextSceneButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        nextSceneButton.Click += delegate { AdvanceAutomaticScene(); };
        sessionCard.Controls.Add(nextSceneButton);

        Label customLabel = new Label
        {
            Location = new Point(18, 181), Size = new Size(300, 19), Text = "CUSTOM DESCRIPTION",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        };
        sessionCard.Controls.Add(customLabel);
        customCountLabel = new Label
        {
            Location = new Point(330, 181), Size = new Size(78, 19), Text = "0 / 128",
            TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8f), ForeColor = muted
        };
        sessionCard.Controls.Add(customCountLabel);
        customStatusBox = new TextBox
        {
            Location = new Point(18, 203), Size = new Size(520, 32), MaxLength = 128,
            Font = new Font("Segoe UI", 10f), BackColor = field,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        customStatusBox.HandleCreated += delegate { SendMessage(customStatusBox.Handle, EmSetCueBanner, (IntPtr)1, "Leave empty for automatic"); };
        customStatusBox.TextChanged += delegate { customCountLabel.Text = customStatusBox.TextLength + " / 128"; };
        customStatusBox.KeyDown += delegate(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter) { ApplyCustomStatus(); args.SuppressKeyPress = true; }
        };
        sessionCard.Controls.Add(customStatusBox);

        applyCustomButton = MakeButton("USE TEXT", new Point(550, 201), 120, orange);
        applyCustomButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        applyCustomButton.Click += delegate { ApplyCustomStatus(); };
        sessionCard.Controls.Add(applyCustomButton);

        Label rotationLabel = new Label
        {
            Location = new Point(18, 251), Size = new Size(300, 19), Text = "AUTO ROTATION",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        };
        sessionCard.Controls.Add(rotationLabel);

        rotationIntervalBox = MakeComboBox(new Point(18, 273), 318);
        rotationIntervalBox.Items.AddRange(new object[]
        {
            "Realistic (variable)", "Every 1 minute", "Every 3 minutes", "Every 5 minutes",
            "Every 10 minutes", "Every 15 minutes", "Every 30 minutes"
        });
        rotationIntervalBox.SelectedIndex = 0;
        rotationIntervalBox.SelectedIndexChanged += delegate
        {
            if (loadingProfile) return;
            activeRotationMinutes = RotationMinutesFromSelection();
            rpc.SetRotationIntervalMinutes(activeRotationMinutes);
            SaveProfile();
            RefreshDashboard();
        };
        sessionCard.Controls.Add(rotationIntervalBox);

        Label rotationHelp = new Label
        {
            Location = new Point(352, 263), Size = new Size(318, 48),
            Text = "Resumes your last automatic scene after relaunch.\r\nVariable timing changes scenes naturally; NEXT SCENE skips ahead.",
            Font = new Font("Segoe UI", 8.3f), ForeColor = muted
        };
        sessionCard.Controls.Add(rotationHelp);

        modeExplanationLabel = new Label
        {
            Location = new Point(18, 326), Size = new Size(652, 42), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(11, 7, 8, 0), Text = "", BackColor = Color.FromArgb(25, 26, 36),
            Font = new Font("Segoe UI Semibold", 8.3f, FontStyle.Bold), ForeColor = green
        };
        sessionCard.Controls.Add(modeExplanationLabel);
        Label autoSaveLabel = new Label
        {
            Location = new Point(18, 371), Size = new Size(652, 17), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Settings are saved automatically.", Font = new Font("Segoe UI", 7.8f), ForeColor = Color.FromArgb(119, 119, 134)
        };
        sessionCard.Controls.Add(autoSaveLabel);
        sessionCard.Resize += delegate
        {
            int clientWidth = sessionCard.ClientSize.Width;
            bool narrow = clientWidth < 600;
            if (narrow)
            {
                int full = Math.Max(300, clientWidth - 36);
                int gap = 10;
                int halfButton = Math.Max(120, (full - gap) / 2);
                modeLabel.SetBounds(18, 46, full, 19);
                sessionModeBox.SetBounds(18, 68, full, sessionModeBox.Height);
                characterLabel.SetBounds(18, 112, full, 19);
                characterBox.SetBounds(18, 134, full, characterBox.Height);
                activityLabel.SetBounds(18, 178, full, 19);
                statusBox.SetBounds(18, 200, full, statusBox.Height);
                applyPresetButton.SetBounds(18, 238, halfButton, applyPresetButton.Height);
                nextSceneButton.SetBounds(18 + halfButton + gap, 238, halfButton, nextSceneButton.Height);
                customLabel.SetBounds(18, 286, full, 19);
                customCountLabel.SetBounds(clientWidth - 18 - customCountLabel.Width, 286, customCountLabel.Width, 19);
                customStatusBox.SetBounds(18, 308, full, customStatusBox.Height);
                applyCustomButton.SetBounds(18, 346, full, applyCustomButton.Height);
                rotationLabel.SetBounds(18, 394, full, 19);
                rotationIntervalBox.SetBounds(18, 416, full, rotationIntervalBox.Height);
                rotationHelp.SetBounds(18, 454, full, 48);
                modeExplanationLabel.SetBounds(18, 512, full, 54);
                autoSaveLabel.SetBounds(18, 574, full, 17);
                sessionCard.Height = 600;
            }
            else
            {
                int gap = 16;
                int half = Math.Max(240, (clientWidth - 36 - gap) / 2);
                modeLabel.SetBounds(18, 46, half, 19);
                sessionModeBox.SetBounds(18, 68, half, sessionModeBox.Height);
                characterLabel.SetBounds(18 + half + gap, 46, half, 19);
                characterBox.SetBounds(characterLabel.Left, 68, half, characterBox.Height);
                activityLabel.SetBounds(18, 112, 400, 19);
                statusBox.SetBounds(18, 134, Math.Max(230, clientWidth - 18 - 132 - 128 - 28), statusBox.Height);
                applyPresetButton.SetBounds(clientWidth - 248, 132, 116, applyPresetButton.Height);
                nextSceneButton.SetBounds(clientWidth - 138, 132, 120, nextSceneButton.Height);
                customLabel.SetBounds(18, 181, 300, 19);
                customStatusBox.SetBounds(18, 203, Math.Max(300, clientWidth - 18 - 120 - 30), customStatusBox.Height);
                applyCustomButton.SetBounds(clientWidth - 138, 201, 120, applyCustomButton.Height);
                customCountLabel.SetBounds(customStatusBox.Right - customCountLabel.Width, 181, customCountLabel.Width, 19);
                rotationLabel.SetBounds(18, 251, 300, 19);
                rotationIntervalBox.SetBounds(18, 273, half, rotationIntervalBox.Height);
                rotationHelp.SetBounds(18 + half + gap, 263, half, 48);
                modeExplanationLabel.SetBounds(18, 326, clientWidth - 36, 42);
                autoSaveLabel.SetBounds(18, 371, clientWidth - 36, 17);
                sessionCard.Height = 394;
            }
        };
        leftStack.Controls.Add(activityButtonsCard);

        CardPanel connectionShortcutCard = MakeCard(690, 112, card, border);
        connectionShortcutCard.AccentColor = purple;
        connectionShortcutCard.AccentWidth = 4;
        leftStack.Controls.Add(connectionShortcutCard);
        connectionShortcutCard.Controls.Add(MakeSectionTitle("CONNECTION", new Point(18, 14), purple));
        connectionShortcutCard.Controls.Add(new Label
        {
            Location = new Point(18, 45), Size = new Size(470, 44), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Application ID and startup delay are managed in Settings. Activity buttons are directly above.",
            Font = new Font("Segoe UI", 8.8f), ForeColor = muted
        });
        Button openSettingsButton = MakeButton("OPEN SETTINGS", new Point(526, 44), 144, purple);
        openSettingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        openSettingsButton.Click += delegate { settingsNav.PerformClick(); };
        connectionShortcutCard.Controls.Add(openSettingsButton);

        CardPanel previewCard = MakeCard(550, 374, card, border);
        previewCard.AccentColor = purple;
        previewCard.AccentWidth = 4;
        rightStack.Controls.Add(previewCard);
        previewCard.Controls.Add(MakeSectionTitle("DISCORD PREVIEW", new Point(18, 13), Color.White));
        Label livePreviewLabel = new Label
        {
            Location = new Point(166, 13), Size = new Size(130, 22), Text = "●  Live preview",
            Font = new Font("Segoe UI", 8.5f), ForeColor = green
        };
        previewCard.Controls.Add(livePreviewLabel);
        discordPreviewCard = MakeCard(514, 312, Color.FromArgb(30, 31, 39), Color.FromArgb(38, 39, 50));
        discordPreviewCard.Location = new Point(18, 47);
        previewCard.Controls.Add(discordPreviewCard);
        discordPreviewCard.Controls.Add(new Label
        {
            Location = new Point(20, 18), Size = new Size(150, 26), Text = "Playing",
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), ForeColor = Color.White
        });
        discordPreviewCard.Controls.Add(new Label
        {
            Location = new Point(462, 15), Size = new Size(35, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = "•••", Font = new Font("Segoe UI Semibold", 11f), ForeColor = muted, TextAlign = ContentAlignment.MiddleRight
        });
        previewImageBox = new PictureBox
        {
            Location = new Point(20, 68), Size = new Size(124, 124), SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent, Image = LoadPreviewImage(), BorderStyle = BorderStyle.None
        };
        discordPreviewCard.Controls.Add(previewImageBox);
        previewTitleLabel = new Label
        {
            Location = new Point(164, 72), Size = new Size(326, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Grand Theft Auto VI", Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold), ForeColor = Color.White
        };
        previewStateLabel = new Label
        {
            Location = new Point(164, 111), Size = new Size(326, 25), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Preview managed by Discord", Font = new Font("Segoe UI", 10.2f), ForeColor = Color.White, AutoEllipsis = true
        };
        previewDetailsLabel = new Label
        {
            Location = new Point(164, 143), Size = new Size(326, 25), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Discord controls the visible native game card.", Font = new Font("Segoe UI", 9.7f), ForeColor = muted, AutoEllipsis = true
        };
        previewTimerLabel = new Label
        {
            Location = new Point(164, 178), Size = new Size(326, 26), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "GTA6.exe • Registered Games", Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = green
        };
        previewModeNoteLabel = new Label
        {
            Location = new Point(22, 215), Size = new Size(468, 34), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Preview managed by Discord — icon, timer, hover behavior and recent activity may differ.",
            Font = new Font("Segoe UI", 8.2f), ForeColor = muted
        };
        previewJoinButton = MakeButton(DefaultActivityButtonLabel, new Point(22, 211), 468, Color.FromArgb(76, 78, 94));
        previewJoinButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        previewJoinButton.Visible = false;
        previewJoinButton.Click += delegate { OpenPreviewActivityButton(1); };
        previewSecondButton = MakeButton(DefaultSecondButtonLabel, new Point(22, 253), 468, Color.FromArgb(76, 78, 94));
        previewSecondButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        previewSecondButton.Visible = false;
        previewSecondButton.Click += delegate { OpenPreviewActivityButton(2); };
        discordPreviewCard.Controls.Add(previewTitleLabel);
        discordPreviewCard.Controls.Add(previewStateLabel);
        discordPreviewCard.Controls.Add(previewDetailsLabel);
        discordPreviewCard.Controls.Add(previewTimerLabel);
        discordPreviewCard.Controls.Add(previewModeNoteLabel);
        discordPreviewCard.Controls.Add(previewJoinButton);
        discordPreviewCard.Controls.Add(previewSecondButton);
        previewCard.Resize += delegate
        {
            discordPreviewCard.Width = Math.Max(420, previewCard.ClientSize.Width - 36);
            LayoutPreviewButtons();
        };

        CardPanel currentCard = MakeCard(550, 262, card, border);
        currentCard.AccentColor = green;
        currentCard.AccentWidth = 4;
        rightStack.Controls.Add(currentCard);
        currentCard.Controls.Add(MakeSectionTitle("CURRENT PRESENCE", new Point(18, 13), green));
        currentStatusLabel = new Label
        {
            Location = new Point(18, 43), Size = new Size(514, 48), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(11, 7, 8, 3), Text = "Native detection active",
            Font = new Font("Segoe UI Semibold", 9.3f, FontStyle.Bold), BackColor = Color.FromArgb(24, 31, 29), ForeColor = green
        };
        currentCard.Controls.Add(currentStatusLabel);
        presenceModeValueLabel = AddInfoRow(currentCard, "Presence Mode", "Discord Game Detection", 102, muted);
        sessionModeValueLabel = AddInfoRow(currentCard, "Session Mode", "—", 130, muted);
        playingAsValueLabel = AddInfoRow(currentCard, "Playing As", "—", 158, muted);
        activityValueLabel = AddInfoRow(currentCard, "Current Activity", "Discord controlled", 186, muted);
        nextChangeValueLabel = AddInfoRow(currentCard, "Next Change", "—", 214, muted);

        CardPanel informationCard = MakeCard(550, 136, card, border);
        rightStack.Controls.Add(informationCard);
        informationCard.Controls.Add(MakeSectionTitle("INFORMATION", new Point(18, 13), Color.White));
        contextInfoLabel = new Label
        {
            Location = new Point(18, 44), Size = new Size(514, 76), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Discord controls the native game card. Custom Rich Presence controls are locked and no Application ID is used.",
            Font = new Font("Segoe UI", 9f), ForeColor = muted
        };
        informationCard.Controls.Add(contextInfoLabel);

        CardPanel behaviorCard = MakeCard(720, 252, card, border);
        behaviorCard.AccentColor = orange;
        behaviorCard.AccentWidth = 4;
        settingsLeftStack.Controls.Add(behaviorCard);
        behaviorCard.Controls.Add(MakeSectionTitle("HOW THESE SETTINGS WORK", new Point(18, 14), orange));
        behaviorCard.Controls.Add(MakeSettingsHint("APPLICATION ID", "Required only for Custom Rich Presence. It is a public identifier, not a token.", 48, pink));
        behaviorCard.Controls.Add(MakeSettingsHint("STARTUP DELAY", "Optional realism: hides the description for five minutes while the timer keeps running.", 94, orange));
        behaviorCard.Controls.Add(MakeSettingsHint("ACTIVITY BUTTONS", "Configure up to two links directly in Studio; Discord controls their final style.", 140, purple));
        behaviorCard.Controls.Add(MakeSettingsHint("AUTOMATIC SAVE", "Mode, scene rotation and button configuration are restored on the next launch.", 186, green));

        CardPanel modeReferenceCard = MakeCard(500, 286, card, border);
        modeReferenceCard.AccentColor = purple;
        modeReferenceCard.AccentWidth = 4;
        settingsRightStack.Controls.Add(modeReferenceCard);
        modeReferenceCard.Controls.Add(MakeSectionTitle("MODE REFERENCE", new Point(18, 14), Color.White));
        modeReferenceCard.Controls.Add(new Label
        {
            Location = new Point(18, 48), Size = new Size(454, 20), Text = "DISCORD GAME DETECTION",
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold), ForeColor = green
        });
        modeReferenceCard.Controls.Add(new Label
        {
            Location = new Point(18, 70), Size = new Size(454, 50), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Uses GTA6.exe and Discord Registered Games. Discord owns the card, icon, click behavior and recent activity.",
            Font = new Font("Segoe UI", 8.6f), ForeColor = muted
        });
        modeReferenceCard.Controls.Add(new Label
        {
            Location = new Point(18, 134), Size = new Size(454, 20), Text = "CUSTOM RICH PRESENCE",
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold), ForeColor = pink
        });
        modeReferenceCard.Controls.Add(new Label
        {
            Location = new Point(18, 156), Size = new Size(454, 64), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Uses your Application ID. Session Director, custom text, rotation and two link buttons become editable.",
            Font = new Font("Segoe UI", 8.6f), ForeColor = muted
        });
        modeReferenceCard.Controls.Add(new Label
        {
            Location = new Point(18, 235), Size = new Size(454, 32), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Fresh installs start safely in Discord Game Detection until a valid Application ID is configured.",
            Font = new Font("Segoe UI", 8.3f), ForeColor = muted
        });

        CardPanel localDataCard = MakeCard(500, 188, card, border);
        localDataCard.AccentColor = green;
        localDataCard.AccentWidth = 4;
        settingsRightStack.Controls.Add(localDataCard);
        localDataCard.Controls.Add(MakeSectionTitle("LOCAL & PRIVATE", new Point(18, 14), green));
        localDataCard.Controls.Add(new Label
        {
            Location = new Point(18, 48), Size = new Size(454, 112), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Settings are stored beside GTA6.exe.\r\n\r\nThe app never needs a Bot Token, Client Secret, Discord password or administrator access.\r\n\r\nClosing the window keeps the app in the system tray.",
            Font = new Font("Segoe UI", 9f), ForeColor = Color.White
        });

        CardPanel profilesHeroCard = MakeCard(720, 106, card, border);
        profilesHeroCard.AccentColor = purple;
        profilesHeroCard.AccentWidth = 4;
        profilesLeftStack.Controls.Add(profilesHeroCard);
        profilesHeroCard.Controls.Add(MakeSectionTitle("PROFILES", new Point(18, 14), purple));
        profilesHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 44), Size = new Size(670, 46), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Save complete Studio presets without copying your Discord Application ID. Profiles remain local and portable.",
            Font = new Font("Segoe UI", 9.4f), ForeColor = Color.White
        });

        CardPanel profileManagerCard = MakeCard(720, 326, card, border);
        profileManagerCard.AccentColor = pink;
        profileManagerCard.AccentWidth = 4;
        profilesLeftStack.Controls.Add(profileManagerCard);
        profileManagerCard.Controls.Add(MakeSectionTitle("PROFILE MANAGER", new Point(18, 14), pink));
        profileManagerCard.Controls.Add(new Label
        {
            Location = new Point(18, 49), Size = new Size(650, 19), Text = "SELECTED PROFILE",
            Font = new Font("Segoe UI Semibold", 8.3f, FontStyle.Bold), ForeColor = muted
        });
        profileBox = MakeComboBox(new Point(18, 72), 652);
        profileManagerCard.Controls.Add(profileBox);
        Button createProfileButton = MakeButton("NEW PROFILE", new Point(18, 124), 205, pink);
        createProfileButton.Click += delegate { CreateNamedProfile(); };
        profileManagerCard.Controls.Add(createProfileButton);
        Button saveProfileButton = MakeButton("SAVE SELECTED", new Point(233, 124), 205, purple);
        saveProfileButton.Click += delegate { SaveSelectedProfile(); };
        profileManagerCard.Controls.Add(saveProfileButton);
        Button loadProfileButton = MakeButton("LOAD PROFILE", new Point(448, 124), 222, green);
        loadProfileButton.Click += delegate { LoadSelectedProfile(); };
        profileManagerCard.Controls.Add(loadProfileButton);
        Button importProfileButton = MakeButton("IMPORT", new Point(18, 176), 205, Color.FromArgb(44, 45, 58));
        importProfileButton.Click += delegate { ImportProfile(); };
        profileManagerCard.Controls.Add(importProfileButton);
        Button exportProfileButton = MakeButton("EXPORT", new Point(233, 176), 205, Color.FromArgb(44, 45, 58));
        exportProfileButton.Click += delegate { ExportSelectedProfile(); };
        profileManagerCard.Controls.Add(exportProfileButton);
        Button deleteProfileButton = MakeButton("DELETE", new Point(448, 176), 222, Color.FromArgb(92, 39, 52));
        deleteProfileButton.Click += delegate { DeleteSelectedProfile(); };
        profileManagerCard.Controls.Add(deleteProfileButton);
        profileStatusLabel = new Label
        {
            Location = new Point(18, 234), Size = new Size(652, 68), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(11, 9, 8, 0), Text = "Default settings are active. Create a named profile to keep a reusable snapshot.",
            Font = new Font("Segoe UI Semibold", 8.6f), BackColor = Color.FromArgb(25, 26, 36), ForeColor = green
        };
        profileManagerCard.Controls.Add(profileStatusLabel);
        profileManagerCard.Resize += delegate
        {
            int clientWidth = profileManagerCard.ClientSize.Width;
            int available = Math.Max(330, clientWidth - 36);
            int gap = 10;
            int buttonWidth = Math.Max(100, (available - gap * 2) / 3);
            profileBox.Width = available;
            createProfileButton.SetBounds(18, 124, buttonWidth, createProfileButton.Height);
            saveProfileButton.SetBounds(18 + buttonWidth + gap, 124, buttonWidth, saveProfileButton.Height);
            loadProfileButton.SetBounds(18 + (buttonWidth + gap) * 2, 124, buttonWidth, loadProfileButton.Height);
            importProfileButton.SetBounds(18, 176, buttonWidth, importProfileButton.Height);
            exportProfileButton.SetBounds(18 + buttonWidth + gap, 176, buttonWidth, exportProfileButton.Height);
            deleteProfileButton.SetBounds(18 + (buttonWidth + gap) * 2, 176, buttonWidth, deleteProfileButton.Height);
            profileStatusLabel.Width = available;
        };

        CardPanel profileInfoCard = MakeCard(500, 286, card, border);
        profileInfoCard.AccentColor = green;
        profileInfoCard.AccentWidth = 4;
        profilesRightStack.Controls.Add(profileInfoCard);
        profileInfoCard.Controls.Add(MakeSectionTitle("SAFE PROFILE FILES", new Point(18, 14), green));
        profileInfoCard.Controls.Add(new Label
        {
            Location = new Point(18, 52), Size = new Size(452, 205), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Profiles contain scene mode, character, rotation and activity-button preferences.\r\n\r\nThe Discord Application ID is stored separately and is never included when a profile is exported.\r\n\r\nLoading a profile changes only Studio settings; it never creates a bot, token or Discord account connection.",
            Font = new Font("Segoe UI", 9.3f), ForeColor = Color.White
        });

        CardPanel diagnosticsHeroCard = MakeCard(720, 106, card, border);
        diagnosticsHeroCard.AccentColor = green;
        diagnosticsHeroCard.AccentWidth = 4;
        diagnosticsLeftStack.Controls.Add(diagnosticsHeroCard);
        diagnosticsHeroCard.Controls.Add(MakeSectionTitle("SETUP HEALTH", new Point(18, 14), green));
        diagnosticsHeroCard.Controls.Add(new Label
        {
            Location = new Point(18, 44), Size = new Size(670, 46), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "See exactly what is ready, what Discord controls, and what still needs attention. No diagnostic data is uploaded.",
            Font = new Font("Segoe UI", 9.4f), ForeColor = Color.White
        });

        CardPanel diagnosticStatusCard = MakeCard(720, 324, card, border);
        diagnosticStatusCard.AccentColor = purple;
        diagnosticStatusCard.AccentWidth = 4;
        diagnosticsLeftStack.Controls.Add(diagnosticStatusCard);
        diagnosticStatusCard.Controls.Add(MakeSectionTitle("LIVE CHECKS", new Point(18, 14), purple));
        diagnosticDiscordValue = AddInfoRow(diagnosticStatusCard, "Discord Desktop", "Checking…", 54, muted);
        diagnosticAppIdValue = AddInfoRow(diagnosticStatusCard, "Application ID", "Not configured", 88, muted);
        diagnosticRpcValue = AddInfoRow(diagnosticStatusCard, "RPC Connection", "Not connected", 122, muted);
        diagnosticAssetValue = AddInfoRow(diagnosticStatusCard, "Large Image", "gtavi_cover", 156, muted);
        diagnosticButtonsValue = AddInfoRow(diagnosticStatusCard, "Activity Buttons", "Off", 190, muted);
        diagnosticStorageValue = AddInfoRow(diagnosticStatusCard, "Settings Folder", "Checking…", 224, muted);
        diagnosticLastSendValue = AddInfoRow(diagnosticStatusCard, "Last Presence Send", "Not sent this session", 258, muted);

        CardPanel diagnosticActionsCard = MakeCard(500, 276, card, border);
        diagnosticActionsCard.AccentColor = orange;
        diagnosticActionsCard.AccentWidth = 4;
        diagnosticsRightStack.Controls.Add(diagnosticActionsCard);
        diagnosticActionsCard.Controls.Add(MakeSectionTitle("ACTIONS", new Point(18, 14), orange));
        Button refreshDiagnosticsButton = MakeButton("REFRESH CHECKS", new Point(18, 54), 215, green);
        refreshDiagnosticsButton.Click += delegate { RefreshDiagnostics(); };
        diagnosticActionsCard.Controls.Add(refreshDiagnosticsButton);
        Button copyDiagnosticsButton = MakeButton("COPY SAFE REPORT", new Point(245, 54), 225, purple);
        copyDiagnosticsButton.Click += delegate { CopySafeDiagnosticReport(); };
        diagnosticActionsCard.Controls.Add(copyDiagnosticsButton);
        Button resetApplicationButton = MakeButton("RESET APPLICATION", new Point(18, 112), 452, Color.FromArgb(92, 39, 52));
        resetApplicationButton.Click += delegate { ResetApplicationSettings(); };
        diagnosticActionsCard.Controls.Add(resetApplicationButton);
        diagnosticActionsCard.Controls.Add(new Label
        {
            Location = new Point(18, 168), Size = new Size(452, 78), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "The copied report contains no Application ID, button URL or personal path. Reset requires confirmation and does not delete named profiles.",
            Font = new Font("Segoe UI", 8.7f), ForeColor = muted
        });

        leftStack.SizeChanged += delegate { ResizeStackCards(leftStack, 500); };
        rightStack.SizeChanged += delegate { ResizeStackCards(rightStack, 380); };
        settingsLeftStack.SizeChanged += delegate { ResizeStackCards(settingsLeftStack, 500); };
        settingsRightStack.SizeChanged += delegate { ResizeStackCards(settingsRightStack, 380); };
        profilesLeftStack.SizeChanged += delegate { ResizeStackCards(profilesLeftStack, 500); };
        profilesRightStack.SizeChanged += delegate { ResizeStackCards(profilesRightStack, 380); };
        diagnosticsLeftStack.SizeChanged += delegate { ResizeStackCards(diagnosticsLeftStack, 500); };
        diagnosticsRightStack.SizeChanged += delegate { ResizeStackCards(diagnosticsRightStack, 380); };
        aboutLeftStack.SizeChanged += delegate { ResizeStackCards(aboutLeftStack, 500); };
        aboutRightStack.SizeChanged += delegate { ResizeStackCards(aboutRightStack, 380); };
        compactStack.SizeChanged += delegate { ResizeStackCards(compactStack, 420); };
        settingsCompactStack.SizeChanged += delegate { ResizeStackCards(settingsCompactStack, 420); };
        profilesCompactStack.SizeChanged += delegate { ResizeStackCards(profilesCompactStack, 420); };
        diagnosticsCompactStack.SizeChanged += delegate { ResizeStackCards(diagnosticsCompactStack, 420); };
        aboutCompactStack.SizeChanged += delegate { ResizeStackCards(aboutCompactStack, 420); };

        Control[] studioLeftCards = SnapshotControls(leftStack);
        Control[] studioRightCards = SnapshotControls(rightStack);
        Control[] settingsLeftCards = SnapshotControls(settingsLeftStack);
        Control[] settingsRightCards = SnapshotControls(settingsRightStack);
        Control[] profilesLeftCards = SnapshotControls(profilesLeftStack);
        Control[] profilesRightCards = SnapshotControls(profilesRightStack);
        Control[] diagnosticsLeftCards = SnapshotControls(diagnosticsLeftStack);
        Control[] diagnosticsRightCards = SnapshotControls(diagnosticsRightStack);
        Control[] aboutLeftCards = SnapshotControls(aboutLeftStack);
        Control[] aboutRightCards = SnapshotControls(aboutRightStack);
        Control[] studioCompactCards =
        {
            presenceModeCard, previewCard, currentCard, sessionCard,
            activityButtonsCard, informationCard, connectionShortcutCard
        };
        Control[] settingsCompactCards = CombineControls(settingsLeftCards, settingsRightCards);
        Control[] profilesCompactCards = CombineControls(profilesLeftCards, profilesRightCards);
        Control[] diagnosticsCompactCards = CombineControls(diagnosticsLeftCards, diagnosticsRightCards);
        Control[] aboutCompactCards = CombineControls(aboutLeftCards, aboutRightCards);

        Panel footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(9, 10, 16), Margin = Padding.Empty };
        root.Controls.Add(footer, 0, 2);
        root.SetColumnSpan(footer, 2);
        footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = border });
        Label footerStatus = new Label
        {
            Location = new Point(18, 20), Size = new Size(590, 28), Text = "●  READY  •  by Cyberpino  •  settings save automatically",
            Font = new Font("Segoe UI Semibold", 8.4f), ForeColor = green
        };
        footer.Controls.Add(footerStatus);
        Button minimize = MakeButton("HIDE TO TRAY", new Point(838, 15), 160, Color.FromArgb(39, 40, 52));
        minimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        minimize.Click += delegate { HideToTray(); };
        footer.Controls.Add(minimize);
        Button recentHelp = MakeButton("SETUP GUIDE", new Point(1010, 15), 160, purple);
        recentHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        recentHelp.Click += delegate { ShowRecentGamesHelp(); };
        footer.Controls.Add(recentHelp);
        Button exit = MakeButton("EXIT", new Point(1182, 15), 160, Color.FromArgb(50, 42, 50));
        exit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        exit.Click += delegate { exiting = true; Close(); };
        footer.Controls.Add(exit);

        Button currentNavigationButton = studioNav;
        bool responsiveLayoutBusy = false;
        bool? compactLayoutState = null;
        Action applyResponsiveLayout = delegate
        {
            if (WindowState == FormWindowState.Minimized || responsiveLayoutBusy || IsDisposed) return;
            responsiveLayoutBusy = true;
            root.SuspendLayout();
            bool layoutModeChanged = false;
            try
            {
                bool compactPages = ClientSize.Width < 1120;
                layoutModeChanged = !compactLayoutState.HasValue || compactLayoutState.Value != compactPages;
                if (layoutModeChanged)
                {
                    SetCompactDashboard(leftStack, rightStack, compactStack, studioLeftCards, studioRightCards, studioCompactCards, compactPages);
                    SetCompactDashboard(settingsLeftStack, settingsRightStack, settingsCompactStack, settingsLeftCards, settingsRightCards, settingsCompactCards, compactPages);
                    SetCompactDashboard(profilesLeftStack, profilesRightStack, profilesCompactStack, profilesLeftCards, profilesRightCards, profilesCompactCards, compactPages);
                    SetCompactDashboard(diagnosticsLeftStack, diagnosticsRightStack, diagnosticsCompactStack, diagnosticsLeftCards, diagnosticsRightCards, diagnosticsCompactCards, compactPages);
                    SetCompactDashboard(aboutLeftStack, aboutRightStack, aboutCompactStack, aboutLeftCards, aboutRightCards, aboutCompactCards, compactPages);
                    compactLayoutState = compactPages;
                }

                bool iconOnlySidebar = ClientSize.Width < 900;
                int sidebarWidth = iconOnlySidebar ? 74 : 118;
                root.ColumnStyles[0].Width = sidebarWidth;
                bool shortNavigation = sidebar.ClientSize.Height < 500;
                int buttonHeight = shortNavigation ? 58 : 76;
                int buttonGap = shortNavigation ? 4 : 8;
                int firstTop = shortNavigation ? 8 : 18;
                Button[] navigationButtons = { studioNav, settingsNav, profilesNav, diagnosticsNav, aboutNav };
                for (int i = 0; i < navigationButtons.Length; i++)
                {
                    Button navigationButton = navigationButtons[i];
                    navigationButton.SetBounds(iconOnlySidebar ? 5 : 8, firstTop + i * (buttonHeight + buttonGap),
                        iconOnlySidebar ? sidebarWidth - 10 : sidebarWidth - 16, buttonHeight);
                    navigationButton.Text = iconOnlySidebar ? "" : (navigationButton.Tag == null ? "" : navigationButton.Tag.ToString());
                    navigationButton.TextAlign = ContentAlignment.BottomCenter;
                    navigationButton.ImageAlign = iconOnlySidebar ? ContentAlignment.MiddleCenter : ContentAlignment.TopCenter;
                    navigationButton.TextImageRelation = iconOnlySidebar ? TextImageRelation.Overlay : TextImageRelation.ImageAboveText;
                    navigationButton.Padding = iconOnlySidebar ? Padding.Empty : new Padding(0, shortNavigation ? 3 : 8, 0, shortNavigation ? 3 : 7);
                    tips.SetToolTip(navigationButton, navigationButton.Tag == null ? "" : navigationButton.Tag.ToString());
                }
                int selectedIndex = Array.IndexOf(navigationButtons, currentNavigationButton);
                if (selectedIndex < 0) selectedIndex = 0;
                activeRail.SetBounds(0, firstTop + selectedIndex * (buttonHeight + buttonGap) - 4, 4, buttonHeight + 6);
                sidebarState.Visible = !shortNavigation && !iconOnlySidebar;

                connectionLabel.Visible = ClientSize.Width >= 820;
                if (connectionLabel.Visible)
                {
                    int connectionWidth = Math.Min(530, Math.Max(240, header.ClientSize.Width - 300));
                    connectionLabel.SetBounds(header.ClientSize.Width - connectionWidth - 18, 16, connectionWidth, 26);
                }
                headerSubtitle.Visible = ClientSize.Width >= 520;

                bool compactFooter = footer.ClientSize.Width < 980;
                footerStatus.Visible = !compactFooter;
                int footerGap = 10;
                int footerButtonWidth = compactFooter
                    ? Math.Max(120, (footer.ClientSize.Width - 36 - footerGap * 2) / 3)
                    : 160;
                int footerStart = compactFooter
                    ? 18
                    : footer.ClientSize.Width - 18 - footerButtonWidth * 3 - footerGap * 2;
                minimize.SetBounds(footerStart, 15, footerButtonWidth, 36);
                recentHelp.SetBounds(footerStart + footerButtonWidth + footerGap, 15, footerButtonWidth, 36);
                exit.SetBounds(footerStart + (footerButtonWidth + footerGap) * 2, 15, footerButtonWidth, 36);
                if (!compactFooter) footerStatus.Width = Math.Max(300, footerStart - 36);

                if (compactPages)
                {
                    ResizeStackCards(compactStack, 420);
                    ResizeStackCards(settingsCompactStack, 420);
                    ResizeStackCards(profilesCompactStack, 420);
                    ResizeStackCards(diagnosticsCompactStack, 420);
                    ResizeStackCards(aboutCompactStack, 420);
                }
            }
            finally
            {
                root.ResumeLayout(true);
                responsiveLayoutBusy = false;
            }
            if (layoutModeChanged)
            {
                Invalidate(true);
                Update();
            }
        };

        Action<Button> activateNavigation = delegate(Button activeButton)
        {
            foreach (Button navigationButton in new[] { studioNav, settingsNav, profilesNav, diagnosticsNav, aboutNav })
            {
                bool selected = navigationButton == activeButton;
                navigationButton.BackColor = selected ? Color.FromArgb(31, 20, 36) : Color.FromArgb(10, 11, 18);
                navigationButton.ForeColor = selected ? pink : muted;
                Image previousIcon = navigationButton.Image;
                navigationButton.Image = MakeSidebarIcon(navigationButton.Tag == null ? navigationButton.Text : navigationButton.Tag.ToString(),
                    selected ? pink : muted);
                if (previousIcon != null) previousIcon.Dispose();
            }
        };
        studioNav.Click += delegate
        {
            activateNavigation(studioNav);
            settingsDashboard.Visible = false;
            profilesDashboard.Visible = false;
            diagnosticsDashboard.Visible = false;
            aboutDashboard.Visible = false;
            dashboard.Visible = true;
            dashboard.BringToFront();
            currentNavigationButton = studioNav;
            applyResponsiveLayout();
        };
        openSettingsPage = delegate
        {
            activateNavigation(settingsNav);
            dashboard.Visible = false;
            profilesDashboard.Visible = false;
            diagnosticsDashboard.Visible = false;
            aboutDashboard.Visible = false;
            settingsDashboard.Visible = true;
            settingsDashboard.BringToFront();
            currentNavigationButton = settingsNav;
            applyResponsiveLayout();
        };
        settingsNav.Click += delegate { openSettingsPage(); };
        profilesNav.Click += delegate
        {
            activateNavigation(profilesNav);
            dashboard.Visible = false;
            settingsDashboard.Visible = false;
            diagnosticsDashboard.Visible = false;
            aboutDashboard.Visible = false;
            profilesDashboard.Visible = true;
            profilesDashboard.BringToFront();
            currentNavigationButton = profilesNav;
            applyResponsiveLayout();
            RefreshProfileList();
        };
        diagnosticsNav.Click += delegate
        {
            activateNavigation(diagnosticsNav);
            dashboard.Visible = false;
            settingsDashboard.Visible = false;
            profilesDashboard.Visible = false;
            aboutDashboard.Visible = false;
            diagnosticsDashboard.Visible = true;
            diagnosticsDashboard.BringToFront();
            currentNavigationButton = diagnosticsNav;
            applyResponsiveLayout();
            RefreshDiagnostics();
        };
        aboutNav.Click += delegate
        {
            activateNavigation(aboutNav);
            dashboard.Visible = false;
            settingsDashboard.Visible = false;
            profilesDashboard.Visible = false;
            diagnosticsDashboard.Visible = false;
            aboutDashboard.Visible = true;
            aboutDashboard.BringToFront();
            currentNavigationButton = aboutNav;
            applyResponsiveLayout();
        };

        trayIcon = new NotifyIcon { Icon = Icon, Text = "GTA VI Rich Presence", Visible = true };
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open", null, delegate { RestoreFromTray(); });
        trayMenu.Items.Add("Exit", null, delegate { exiting = true; Close(); });
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += delegate { RestoreFromTray(); };

        FormClosing += delegate(object sender, FormClosingEventArgs args)
        {
            if (!exiting && args.CloseReason == CloseReason.UserClosing)
            {
                args.Cancel = true;
                HideToTray();
            }
        };
        FormClosed += delegate
        {
            uiCountdownTimer.Stop();
            uiCountdownTimer.Dispose();
            SaveProfile();
            rpc.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
        };

        SizeChanged += delegate { applyResponsiveLayout(); };
        applyResponsiveLayout();

        LoadProfile();
        LoadSavedId();
        RefreshProfileList();
        ResizeStackCards(leftStack, 500);
        ResizeStackCards(rightStack, 380);
        ResizeStackCards(settingsLeftStack, 500);
        ResizeStackCards(settingsRightStack, 380);
        ResizeStackCards(profilesLeftStack, 500);
        ResizeStackCards(profilesRightStack, 380);
        ResizeStackCards(diagnosticsLeftStack, 500);
        ResizeStackCards(diagnosticsRightStack, 380);
        ResizeStackCards(aboutLeftStack, 500);
        ResizeStackCards(aboutRightStack, 380);
        RefreshDiagnostics();
        RefreshDashboard();
        Shown += delegate { BeginInvoke(new MethodInvoker(ShowFirstRunGuideIfNeeded)); };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int enabled = 1;
            DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int));
            DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
        }
        catch { }
    }

    private static CardPanel MakeCard(int width, int height, Color background, Color borderColor)
    {
        return new CardPanel
        {
            Size = new Size(width, height), BackColor = background, BorderColor = borderColor,
            BorderRadius = 10, BorderThickness = 1, Margin = new Padding(0, 0, 0, 12)
        };
    }

    private static FlowLayoutPanel MakeCompactStack(Color accent, Color background)
    {
        return new StyledFlowLayoutPanel(accent)
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = background,
            Padding = new Padding(0, 0, 6, 0),
            Margin = Padding.Empty,
            Visible = false
        };
    }

    private static Control[] SnapshotControls(Control parent)
    {
        Control[] controls = new Control[parent.Controls.Count];
        parent.Controls.CopyTo(controls, 0);
        return controls;
    }

    private static Control[] CombineControls(Control[] first, Control[] second)
    {
        Control[] combined = new Control[first.Length + second.Length];
        Array.Copy(first, 0, combined, 0, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    private static void MoveControls(Control[] controls, Control target)
    {
        target.SuspendLayout();
        try
        {
            foreach (Control control in controls) target.Controls.Add(control);
        }
        finally { target.ResumeLayout(true); }
    }

    private static void SetCompactDashboard(FlowLayoutPanel left, FlowLayoutPanel right, FlowLayoutPanel compact,
        Control[] leftCards, Control[] rightCards, Control[] compactCards, bool compactMode)
    {
        if (compactMode)
        {
            if (compact.Controls.Count == 0)
            {
                MoveControls(compactCards, compact);
            }
            left.Visible = false;
            right.Visible = false;
            compact.Visible = true;
            compact.BringToFront();
            ResizeStackCards(compact, 420);
        }
        else
        {
            if (compact.Controls.Count > 0)
            {
                MoveControls(leftCards, left);
                MoveControls(rightCards, right);
            }
            compact.Visible = false;
            left.Visible = true;
            right.Visible = true;
            ResizeStackCards(left, 420);
            ResizeStackCards(right, 360);
        }
    }

    private static Label MakeSectionTitle(string text, Point location, Color color)
    {
        return new Label
        {
            Location = location, Size = new Size(400, 26), Text = text,
            Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold), ForeColor = color,
            BackColor = Color.Transparent
        };
    }

    private static Panel MakeSettingsHint(string title, string description, int top, Color accent)
    {
        Panel block = new Panel
        {
            Location = new Point(18, top), Size = new Size(650, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };
        block.Controls.Add(new Label
        {
            Location = new Point(0, 0), Size = new Size(180, 18), Text = title,
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = accent,
            BackColor = Color.Transparent
        });
        block.Controls.Add(new Label
        {
            Location = new Point(0, 18), Size = new Size(640, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = description, Font = new Font("Segoe UI", 8.3f),
            ForeColor = Color.FromArgb(165, 165, 181), BackColor = Color.Transparent
        });
        return block;
    }

    private static Button MakeSidebarButton(string text, Point location, Color color, bool active)
    {
        Button button = new Button
        {
            Location = location, Size = new Size(102, 76), Text = text,
            FlatStyle = FlatStyle.Flat, BackColor = active ? Color.FromArgb(31, 20, 36) : Color.FromArgb(10, 11, 18),
            ForeColor = color, Font = new Font("Segoe UI Semibold", 8.2f, FontStyle.Bold),
            Cursor = Cursors.Hand, TextAlign = ContentAlignment.BottomCenter,
            Image = MakeSidebarIcon(text, color), ImageAlign = ContentAlignment.TopCenter,
            TextImageRelation = TextImageRelation.ImageAboveText, Padding = new Padding(0, 8, 0, 7), Tag = text
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(27, 27, 38);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 29, 43);
        return button;
    }

    private static Bitmap MakeSidebarIcon(string kind, Color color)
    {
        Bitmap bitmap = new Bitmap(26, 26);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (Pen pen = new Pen(color, 2f))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            if (kind == "STUDIO")
            {
                graphics.DrawLines(pen, new[] { new Point(3, 12), new Point(13, 3), new Point(23, 12) });
                graphics.DrawRectangle(pen, 6, 11, 14, 11);
                graphics.DrawLine(pen, 13, 16, 13, 22);
            }
            else if (kind == "SETTINGS")
            {
                graphics.DrawEllipse(pen, 7, 7, 12, 12);
                graphics.DrawEllipse(pen, 11, 11, 4, 4);
                for (int angle = 0; angle < 360; angle += 45)
                {
                    double radians = angle * Math.PI / 180.0;
                    Point inner = new Point(13 + (int)(8 * Math.Cos(radians)), 13 + (int)(8 * Math.Sin(radians)));
                    Point outer = new Point(13 + (int)(11 * Math.Cos(radians)), 13 + (int)(11 * Math.Sin(radians)));
                    graphics.DrawLine(pen, inner, outer);
                }
            }
            else if (kind == "PROFILES")
            {
                graphics.DrawRectangle(pen, 3, 8, 20, 14);
                graphics.DrawLines(pen, new[] { new Point(3, 8), new Point(8, 8), new Point(10, 5), new Point(16, 5), new Point(18, 8) });
            }
            else if (kind == "DIAGNOSTICS")
            {
                graphics.DrawLines(pen, new[]
                {
                    new Point(2, 14), new Point(7, 14), new Point(10, 7),
                    new Point(14, 20), new Point(17, 11), new Point(20, 14), new Point(24, 14)
                });
            }
            else
            {
                graphics.DrawEllipse(pen, 3, 3, 20, 20);
                graphics.DrawLine(pen, 13, 11, 13, 19);
                graphics.DrawEllipse(pen, 12, 7, 2, 2);
            }
        }
        return bitmap;
    }

    private static void ResizeStackCards(FlowLayoutPanel stack, int minimumWidth)
    {
        if (stack == null || stack.IsDisposed) return;
        int available = stack.ClientSize.Width - stack.Padding.Horizontal - 5;
        if (stack.VerticalScroll.Visible) available -= SystemInformation.VerticalScrollBarWidth;
        int width = Math.Max(320, available);
        foreach (Control control in stack.Controls)
            control.Width = width;
    }

    private static Label AddInfoRow(Control parent, string name, string value, int top, Color muted)
    {
        parent.Controls.Add(new Panel
        {
            Location = new Point(18, top - 7), Size = new Size(parent.ClientSize.Width - 36, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(39, 40, 51)
        });
        parent.Controls.Add(new Label
        {
            Location = new Point(18, top), Size = new Size(180, 22), Text = name,
            Font = new Font("Segoe UI", 8.8f), ForeColor = muted
        });
        Label result = new Label
        {
            Location = new Point(202, top), Size = new Size(parent.ClientSize.Width - 220, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = value, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true,
            Font = new Font("Segoe UI Semibold", 8.8f), ForeColor = Color.White
        };
        parent.Controls.Add(result);
        return result;
    }

    private static Image LoadPreviewImage()
    {
        try
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GtaViPreview.png"))
            {
                if (stream != null)
                {
                    using (Image image = Image.FromStream(stream)) return new Bitmap(image);
                }
            }
        }
        catch { }

        try
        {
            using (Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                return icon == null ? new Bitmap(1, 1) : icon.ToBitmap();
        }
        catch { return new Bitmap(1, 1); }
    }

    private void ApplyModeCardVisuals()
    {
        if (nativeModeCard == null || customModeCard == null) return;
        Color neutral = Color.FromArgb(43, 44, 58);
        Color orange = Color.FromArgb(255, 151, 62);
        bool customReady = IsCustomSetupReady();
        nativeModeCard.BorderColor = officialModeBox.Checked ? Color.FromArgb(70, 204, 105) : neutral;
        customModeCard.BorderColor = !customReady ? orange : (customModeBox.Checked ? Color.FromArgb(235, 51, 145) : neutral);
        nativeModeCard.BorderThickness = officialModeBox.Checked ? 2 : 1;
        customModeCard.BorderThickness = customModeBox.Checked || !customReady ? 2 : 1;
        customModeCard.Cursor = customReady ? Cursors.Hand : Cursors.Help;
        customModeDescriptionLabel.Text = customReady
            ? "READY  •  Custom scenes, rotation and buttons."
            : "LOCKED  •  Create and apply a Discord Application ID.";
        customModeDescriptionLabel.ForeColor = customReady ? Color.FromArgb(184, 181, 196) : orange;
        nativeModeCard.Invalidate();
        customModeCard.Invalidate();
    }

    private void TryActivateCustomMode()
    {
        if (!IsCustomSetupReady())
        {
            ShowCustomModeGuide();
            return;
        }
        customModeBox.Checked = true;
    }

    private bool IsCustomSetupReady()
    {
        string enteredId = appIdBox == null ? "" : appIdBox.Text.Trim();
        return IsValidAppId(enteredId) && string.Equals(enteredId, appliedApplicationId, StringComparison.Ordinal);
    }

    private void RefreshDashboard()
    {
        if (previewTitleLabel == null || IsDisposed) return;
        ApplyModeCardVisuals();
        joinGameBox.Text = "Button 1 — " + NormalizeJoinLabel(activeJoinLabel);
        secondButtonBox.Text = "Button 2 — " + NormalizeButtonLabel(activeSecondButtonLabel, DefaultSecondButtonLabel);

        Color green = Color.FromArgb(70, 204, 105);
        Color muted = Color.FromArgb(165, 165, 181);
        if (officialModeEnabled)
        {
            previewJoinButton.Visible = false;
            previewSecondButton.Visible = false;
            previewModeNoteLabel.Visible = true;
            previewTitleLabel.Text = "Native Discord Game Card";
            previewStateLabel.Text = "Preview managed by Discord";
            previewDetailsLabel.Text = "Discord controls the final icon, timer and recent activity.";
            previewTimerLabel.Text = "GTA6.exe  •  Registered Games";
            previewModeNoteLabel.Text = "Native mode is detected from the running executable. Custom session data is not being sent.";
            currentStatusLabel.Text = "Native detection active  •  Discord RPC is off";
            currentStatusLabel.ForeColor = green;
            currentStatusLabel.BackColor = Color.FromArgb(24, 31, 29);
            presenceModeValueLabel.Text = "Discord Game Detection";
            sessionModeValueLabel.Text = "—";
            playingAsValueLabel.Text = "—";
            activityValueLabel.Text = "Discord controlled";
            nextChangeValueLabel.Text = "—";
            contextInfoLabel.Text = "Discord controls the native game card. Custom Rich Presence controls are visible but locked, and the Application ID is not used.";
            return;
        }

        string state = activeState;
        string details = activeStatus;
        string liveText = currentStatusLabel.Text ?? "";
        string[] lines = liveText.Replace("\r", "").Split('\n');
        if (lines.Length > 0 && lines[0].Trim().Length > 0) state = lines[0].Trim();
        if (lines.Length > 1 && lines[1].Trim().Length > 0) details = TrimActivityTiming(lines[1].Trim());
        if (string.IsNullOrWhiteSpace(state)) state = BuildStateForSelection();
        if (string.IsNullOrWhiteSpace(details)) details = "Waiting for the first activity";

        previewTitleLabel.Text = "Grand Theft Auto VI";
        previewStateLabel.Text = state;
        previewDetailsLabel.Text = details;
        long elapsed = customSessionStarted <= 0 ? 0 : Math.Max(0, UiUnixNow() - customSessionStarted);
        previewTimerLabel.Text = "●  " + FormatUiCountdown(elapsed);
        bool showPreviewButton = joinGameBox.Checked && IsCustomSetupReady();
        bool showSecondPreviewButton = secondButtonBox.Checked && IsCustomSetupReady();
        previewJoinButton.Text = NormalizeJoinLabel(activeJoinLabel);
        previewSecondButton.Text = NormalizeButtonLabel(activeSecondButtonLabel, DefaultSecondButtonLabel);
        previewJoinButton.Visible = showPreviewButton;
        previewSecondButton.Visible = showSecondPreviewButton;
        LayoutPreviewButtons();
        previewModeNoteLabel.Visible = !showPreviewButton && !showSecondPreviewButton;
        previewModeNoteLabel.Text = "Enable an activity button in Studio to preview its text and destination here.";
        presenceModeValueLabel.Text = rpcConnected ? "Custom Rich Presence" : "Custom mode — connecting";
        sessionModeValueLabel.Text = sessionModeBox.SelectedItem == null ? activeSessionMode : sessionModeBox.SelectedItem.ToString();
        playingAsValueLabel.Text = characterBox.SelectedItem == null ? activeCharacter : characterBox.SelectedItem.ToString();
        activityValueLabel.Text = details;
        nextChangeValueLabel.Text = currentActivityDeadline > 0
            ? FormatUiCountdown(Math.Max(0, currentActivityDeadline - UiUnixNow()))
            : (activeMode == "Auto" ? "Pending" : "Manual");
        contextInfoLabel.Text = rpcConnected
            ? "Custom Rich Presence is connected. Session Director changes update this read-only preview and are saved automatically."
            : "Enter a valid Discord Application ID and press APPLY ID. Native Discord card features are unavailable in custom mode.";
        activityValueLabel.ForeColor = Color.White;
        nextChangeValueLabel.ForeColor = currentActivityDeadline > 0 ? green : muted;
    }

    private void LayoutPreviewButtons()
    {
        if (discordPreviewCard == null || previewJoinButton == null || previewSecondButton == null) return;
        int left = 22;
        int available = Math.Max(250, discordPreviewCard.ClientSize.Width - 44);
        if (previewJoinButton.Visible && previewSecondButton.Visible && available < 420)
        {
            previewJoinButton.SetBounds(left, 211, available, previewJoinButton.Height);
            previewSecondButton.SetBounds(left, 253, available, previewSecondButton.Height);
        }
        else if (previewJoinButton.Visible && previewSecondButton.Visible)
        {
            int gap = 10;
            int width = Math.Max(110, (available - gap) / 2);
            previewJoinButton.SetBounds(left, 211, width, previewJoinButton.Height);
            previewSecondButton.SetBounds(left + width + gap, 211, width, previewSecondButton.Height);
        }
        else
        {
            previewJoinButton.SetBounds(left, 211, available, previewJoinButton.Height);
            previewSecondButton.SetBounds(left, 211, available, previewSecondButton.Height);
        }
    }

    private static string TrimActivityTiming(string value)
    {
        int marker = value.IndexOf("  •  changes in ", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) marker = value.IndexOf("  •  manual", StringComparison.OrdinalIgnoreCase);
        return marker > 0 ? value.Substring(0, marker).Trim() : value;
    }

    private void LoadSavedId()
    {
        bool previousLoadingState = loadingProfile;
        try
        {
            loadingProfile = true;
            string savedId = DefaultAppId;
            if (File.Exists(configPath))
                savedId = File.ReadAllText(configPath).Trim();
            appliedApplicationId = IsValidAppId(savedId) ? savedId : "";
            appIdBox.Text = savedId;
            if (!officialModeEnabled && !IsValidAppId(savedId))
            {
                officialModeBox.Checked = true;
                customModeBox.Checked = false;
                officialModeEnabled = true;
            }
        }
        catch { }
        finally { loadingProfile = previousLoadingState; }
        ApplyPresenceMode();
    }

    private void ApplyPresenceMode()
    {
        officialModeEnabled = officialModeBox.Checked;
        if (customModeBox.Checked == officialModeEnabled) customModeBox.Checked = !officialModeEnabled;
        ApplyModeCardVisuals();
        bool customEnabled = !officialModeEnabled && IsCustomSetupReady();
        // Native detection is entirely controlled by Discord. Dim every
        // custom-only input so it is immediately obvious what can be changed.
        // Application setup itself always stays available, even in native mode.
        appIdBox.Enabled = true;
        connectButton.Enabled = true;
        startupDelayBox.Enabled = customEnabled;
        joinGameBox.Enabled = customEnabled;
        secondButtonBox.Enabled = customEnabled;
        editJoinButton.Enabled = customEnabled;
        editSecondButton.Enabled = customEnabled;
        sessionModeBox.Enabled = customEnabled;
        characterBox.Enabled = customEnabled;
        statusBox.Enabled = customEnabled;
        customStatusBox.Enabled = customEnabled;
        rotationIntervalBox.Enabled = customEnabled;
        applyPresetButton.Enabled = customEnabled;
        nextSceneButton.Enabled = customEnabled;
        applyCustomButton.Enabled = customEnabled;

        if (officialModeEnabled)
        {
            rpc.Stop();
            rpcConnected = false;
            currentActivityTemplate = "";
            currentActivityDeadline = 0;
            connectionLabel.Text = "●  Native Discord Detection";
            connectionLabel.ForeColor = Color.FromArgb(70, 204, 105);
            modeExplanationLabel.Text = "Discord controls the native game card. Custom Rich Presence controls are locked.";
            modeExplanationLabel.ForeColor = Color.FromArgb(70, 204, 105);
            sessionHeaderLabel.Text = "SESSION DIRECTOR";
            sessionHeaderLabel.ForeColor = Color.FromArgb(132, 128, 148);
        }
        else if (IsValidAppId(appIdBox.Text.Trim()))
        {
            modeExplanationLabel.Text = "Custom controls are active. Activity buttons are visible to other users, not to you.";
            modeExplanationLabel.ForeColor = Color.FromArgb(255, 151, 62);
            sessionHeaderLabel.Text = "SESSION DIRECTOR";
            sessionHeaderLabel.ForeColor = Color.FromArgb(255, 151, 62);
            ConnectPresence();
        }
        else
        {
            currentActivityTemplate = "";
            currentActivityDeadline = 0;
            rpcConnected = false;
            connectionLabel.Text = "●  Rich Presence requires an Application ID";
            connectionLabel.ForeColor = Color.FromArgb(255, 190, 80);
            modeExplanationLabel.Text = "Enter a Discord Application ID and press APPLY ID.";
            modeExplanationLabel.ForeColor = Color.FromArgb(255, 151, 62);
            sessionHeaderLabel.Text = "SESSION DIRECTOR";
            sessionHeaderLabel.ForeColor = Color.FromArgb(255, 151, 62);
            currentStatusLabel.Text = "Enter an Application ID above, then press APPLY ID";
            currentStatusLabel.ForeColor = Color.FromArgb(255, 190, 80);
        }
        RefreshDashboard();
        RefreshDiagnostics();
    }

    private void LoadProfile()
    {
        loadingProfile = true;
        try
        {
            bool startupDelay = false;
            bool joinButton = false;
            bool secondButton = false;
            bool officialMode = true;
            int rotationMinutes = 0;
            string mode = "Auto";
            string status = "";
            string state = "";
            string sessionMode = RealisticAutoModeLabel;
            string character = "Automatic";
            string joinLabel = DefaultActivityButtonLabel;
            string joinUrl = DefaultActivityButtonUrl;
            string secondButtonLabel = DefaultSecondButtonLabel;
            string secondButtonUrl = DefaultSecondButtonUrl;
            bool onboardingDone = false;

            if (File.Exists(profilePath))
            {
                foreach (string line in File.ReadAllLines(profilePath))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    bool parsed;
                    if (key == "OfficialMode" && bool.TryParse(value, out parsed)) officialMode = parsed;
                    else if (key == "StartupDelay" && bool.TryParse(value, out parsed)) startupDelay = parsed;
                    else if (key == "JoinButton" && bool.TryParse(value, out parsed)) joinButton = parsed;
                    else if (key == "SecondButton" && bool.TryParse(value, out parsed)) secondButton = parsed;
                    else if (key == "RotationMinutes" && int.TryParse(value, out rotationMinutes))
                        rotationMinutes = Math.Max(0, Math.Min(120, rotationMinutes));
                    else if (key == "Mode") mode = value;
                    else if (key == "SessionMode") sessionMode = DecodeProfileValue(value, RealisticAutoModeLabel);
                    else if (key == "Character") character = DecodeProfileValue(value, "Automatic");
                    else if (key == "JoinLabelBase64" && value.Length > 0)
                        joinLabel = DecodeProfileValue(value, DefaultActivityButtonLabel);
                    else if (key == "JoinUrlBase64" && value.Length > 0)
                        joinUrl = DecodeProfileValue(value, DefaultActivityButtonUrl);
                    else if (key == "SecondLabelBase64" && value.Length > 0)
                        secondButtonLabel = DecodeProfileValue(value, DefaultSecondButtonLabel);
                    else if (key == "SecondUrlBase64" && value.Length > 0)
                        secondButtonUrl = DecodeProfileValue(value, DefaultSecondButtonUrl);
                    else if (key == "OnboardingComplete" && bool.TryParse(value, out parsed)) onboardingDone = parsed;
                    else if (key == "StatusBase64" && value.Length > 0)
                        status = DecodeProfileValue(value, "");
                    else if (key == "StateBase64" && value.Length > 0)
                        state = DecodeProfileValue(value, "");
                }
            }

            // Profiles from 1.3.2 used the shorter label. Keep them compatible
            // while making the resume behavior explicit in the current UI.
            if (string.Equals(sessionMode, "Realistic Auto", StringComparison.Ordinal))
                sessionMode = RealisticAutoModeLabel;

            startupDelayBox.Checked = startupDelay;
            joinGameBox.Checked = joinButton;
            secondButtonBox.Checked = secondButton;
            officialModeBox.Checked = officialMode;
            customModeBox.Checked = !officialMode;
            officialModeEnabled = officialMode;
            activeMode = mode;
            activeStatus = status;
            activeState = state;
            activeSessionMode = sessionMode;
            activeCharacter = character;
            activeJoinLabel = NormalizeJoinLabel(joinLabel);
            activeJoinUrl = NormalizeJoinUrl(joinUrl);
            activeSecondButtonLabel = NormalizeButtonLabel(secondButtonLabel, DefaultSecondButtonLabel);
            activeSecondButtonUrl = NormalizeButtonUrl(secondButtonUrl, DefaultSecondButtonUrl);
            activeRotationMinutes = rotationMinutes;
            onboardingComplete = onboardingDone;
            SelectComboValue(sessionModeBox, sessionMode, 0);
            SelectComboValue(characterBox, character, 0);
            SelectRotationValue(rotationMinutes);
            rpc.SetRotationIntervalMinutes(rotationMinutes);
            rpc.SetJoinButtonSettings(activeJoinLabel, activeJoinUrl);
            rpc.SetJoinButtonEnabled(joinButton);
            rpc.SetSecondButtonSettings(activeSecondButtonLabel, activeSecondButtonUrl);
            rpc.SetSecondButtonEnabled(secondButton);

            if (mode == "Custom" && status.Length > 0)
            {
                customStatusBox.Text = status;
                if (state.Length == 0) state = BuildStateForSelection();
                activeState = state;
                rpc.UseActivity(status, state);
            }
            else if (mode == "Preset" && status.Length > 0)
            {
                int matchingIndex = -1;
                for (int i = 1; i < statusBox.Items.Count; i++)
                {
                    if (string.Equals(statusBox.Items[i].ToString(), status, StringComparison.Ordinal))
                    {
                        matchingIndex = i;
                        break;
                    }
                }
                if (matchingIndex > 0)
                {
                    statusBox.SelectedIndex = matchingIndex;
                    if (state.Length == 0) state = BuildStateForSelection();
                    activeState = state;
                    rpc.UseActivity(status, state);
                }
                else
                {
                    activeMode = "Auto";
                    activeStatus = "";
                    activeState = "";
                    sessionModeBox.SelectedIndex = 0;
                    statusBox.SelectedIndex = 0;
                    rpc.UseAutomaticSequence(characterBox.SelectedItem.ToString());
                }
            }
            else
            {
                activeMode = "Auto";
                sessionModeBox.SelectedIndex = 0;
                statusBox.SelectedIndex = 0;
                if (status.Length > 0 && state.Length > 0)
                {
                    activeStatus = status;
                    activeState = state;
                    rpc.ResumeAutomaticSequence(characterBox.SelectedItem.ToString(), status, state);
                }
                else
                {
                    activeStatus = "";
                    activeState = "";
                    rpc.UseAutomaticSequence(characterBox.SelectedItem.ToString());
                }
            }
        }
        catch { }
        finally { loadingProfile = false; }
    }

    private void SaveProfile()
    {
        if (loadingProfile || string.IsNullOrEmpty(profilePath)) return;
        try
        {
            string encodedStatus = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeStatus ?? ""));
            string encodedState = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeState ?? ""));
            string encodedSessionMode = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSessionMode ?? RealisticAutoModeLabel));
            string encodedCharacter = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeCharacter ?? "Automatic"));
            string encodedJoinLabel = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeJoinLabel ?? DefaultActivityButtonLabel));
            string encodedJoinUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeJoinUrl ?? DefaultActivityButtonUrl));
            string encodedSecondLabel = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSecondButtonLabel ?? DefaultSecondButtonLabel));
            string encodedSecondUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSecondButtonUrl ?? DefaultSecondButtonUrl));
            File.WriteAllLines(profilePath, new[]
            {
                "Version=6",
                "OnboardingComplete=" + onboardingComplete,
                "OfficialMode=" + officialModeEnabled,
                "StartupDelay=" + startupDelayBox.Checked,
                "JoinButton=" + joinGameBox.Checked,
                "SecondButton=" + secondButtonBox.Checked,
                "RotationMinutes=" + activeRotationMinutes,
                "Mode=" + activeMode,
                "SessionMode=" + encodedSessionMode,
                "Character=" + encodedCharacter,
                "JoinLabelBase64=" + encodedJoinLabel,
                "JoinUrlBase64=" + encodedJoinUrl,
                "SecondLabelBase64=" + encodedSecondLabel,
                "SecondUrlBase64=" + encodedSecondUrl,
                "StatusBase64=" + encodedStatus,
                "StateBase64=" + encodedState
            });
        }
        catch { }
    }

    private void ConnectPresence()
    {
        string id = appIdBox.Text.Trim();
        if (!IsValidAppId(id))
        {
            MessageBox.Show("The Application ID must contain digits only and be at least 15 digits long.",
                "Invalid Application ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool newApplication = !string.Equals(id, appliedApplicationId, StringComparison.Ordinal);
        if (newApplication) ResetAutomaticDefaultsForNewApplication();

        try { File.WriteAllText(configPath, id); }
        catch (Exception ex)
        {
            MessageBox.Show("The Application ID could not be saved next to the executable:\r\n" + ex.Message,
                "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        appliedApplicationId = id;
        ApplyModeCardVisuals();
        if (officialModeEnabled)
        {
            ApplyPresenceMode();
            SaveProfile();
            MessageBox.Show("Application ID saved. Custom Rich Presence is now unlocked with Realistic Auto (Resume) and Automatic selected.\r\n\r\n" +
                "A newly created Discord app and its image can take a few minutes to synchronize. If the card is incomplete, wait briefly, select Custom Rich Presence, then press SET STATUS again. Repeated presses keep the same scene.",
                "Custom mode unlocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Keep both controls available so a different Application ID can be
        // applied immediately without restarting the desktop app.
        connectButton.Enabled = true;
        appIdBox.Enabled = true;
        rpcConnected = false;
        customSessionStarted = UiUnixNow();
        connectionLabel.Text = "●  Connecting to Discord…";
        connectionLabel.ForeColor = Color.FromArgb(255, 190, 80);
        rpc.Start(id);
        RefreshDashboard();

        if (newApplication)
        {
            MessageBox.Show("Realistic Auto (Resume) is ready and starts with Automatic character selection.\r\n\r\n" +
                "Discord may need a few minutes to synchronize a new application or image. If needed, wait and press SET STATUS again; it will refresh the same scene instead of choosing another one.",
                "First Discord sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ResetAutomaticDefaultsForNewApplication()
    {
        bool previousLoadingState = loadingProfile;
        loadingProfile = true;
        try
        {
            sessionModeBox.SelectedIndex = 0;
            characterBox.SelectedIndex = 0;
            statusBox.SelectedIndex = 0;
            customStatusBox.Text = "";
            rotationIntervalBox.SelectedIndex = 0;
            activeMode = "Auto";
            activeStatus = "";
            activeState = "";
            activeSessionMode = RealisticAutoModeLabel;
            activeCharacter = "Automatic";
            activeRotationMinutes = 0;
            currentActivityTemplate = "";
            currentActivityDeadline = 0;
            rpc.SetRotationIntervalMinutes(0);
            rpc.UseAutomaticSequence("Automatic");
        }
        finally
        {
            loadingProfile = previousLoadingState;
        }
    }

    private void SaveApplicationIdDraft()
    {
        try
        {
            File.WriteAllText(configPath, appIdBox.Text.Trim());
        }
        catch
        {
            // The field remains usable even if this folder is read-only.
        }
    }

    private void EditActivityButtonSettings(int buttonNumber)
    {
        Color pink = Color.FromArgb(255, 70, 156);
        Color muted = Color.FromArgb(184, 181, 196);
        bool editingSecondButton = buttonNumber == 2;
        string currentLabel = editingSecondButton ? activeSecondButtonLabel : activeJoinLabel;
        string currentUrl = editingSecondButton ? activeSecondButtonUrl : activeJoinUrl;
        string defaultLabel = editingSecondButton ? DefaultSecondButtonLabel : DefaultActivityButtonLabel;
        string defaultUrl = editingSecondButton ? DefaultSecondButtonUrl : DefaultActivityButtonUrl;

        using (Form dialog = new Form())
        {
            dialog.Text = "GTA VI — Activity button " + (editingSecondButton ? "2" : "1");
            dialog.ClientSize = new Size(570, 330);
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowInTaskbar = false;
            dialog.BackColor = Color.FromArgb(10, 9, 15);
            dialog.ForeColor = Color.White;
            dialog.Icon = Icon;
            dialog.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 7, BackColor = pink });
            dialog.Controls.Add(new Label
            {
                Location = new Point(28, 25), Size = new Size(510, 38), Text = "ACTIVITY BUTTON " + (editingSecondButton ? "2" : "1"),
                Font = new Font("Arial Black", 18f, FontStyle.Bold), ForeColor = Color.White
            });
            dialog.Controls.Add(new Label
            {
                Location = new Point(30, 68), Size = new Size(510, 36),
                Text = "Visible to other users, not on your own profile. Native game detection ignores it.",
                Font = new Font("Segoe UI", 9.4f), ForeColor = muted
            });
            dialog.Controls.Add(new Label
            {
                Location = new Point(30, 112), Size = new Size(510, 20), Text = "BUTTON TEXT (MAX 32 CHARACTERS)",
                Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
            });
            TextBox labelBox = new TextBox
            {
                Location = new Point(30, 136), Size = new Size(510, 30), MaxLength = 32,
                Text = currentLabel, Font = new Font("Segoe UI", 10.2f),
                BackColor = Color.FromArgb(34, 31, 44), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            dialog.Controls.Add(labelBox);
            dialog.Controls.Add(new Label
            {
                Location = new Point(30, 181), Size = new Size(510, 20), Text = "DESTINATION URL (HTTP OR HTTPS)",
                Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
            });
            TextBox urlBox = new TextBox
            {
                Location = new Point(30, 205), Size = new Size(510, 30), MaxLength = 512,
                Text = currentUrl, Font = new Font("Segoe UI", 9.8f),
                BackColor = Color.FromArgb(34, 31, 44), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            dialog.Controls.Add(urlBox);

            Button cancel = MakeButton("CANCEL", new Point(30, 264), 130, Color.FromArgb(62, 59, 74));
            cancel.DialogResult = DialogResult.Cancel;
            dialog.CancelButton = cancel;
            dialog.Controls.Add(cancel);

            Button reset = MakeButton("RESET DEFAULTS", new Point(175, 264), 200, Color.FromArgb(74, 79, 177));
            reset.Click += delegate
            {
                labelBox.Text = defaultLabel;
                urlBox.Text = defaultUrl;
                labelBox.Focus();
                labelBox.SelectAll();
            };
            dialog.Controls.Add(reset);

            Button save = MakeButton("SAVE BUTTON", new Point(390, 264), 150, pink);
            save.Click += delegate
            {
                string label = labelBox.Text.Trim();
                string url = urlBox.Text.Trim();
                Uri parsed;
                if (label.Length == 0)
                {
                    MessageBox.Show("Enter the text displayed on the activity button.", "Missing button text",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                    (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
                {
                    MessageBox.Show("Enter a complete http:// or https:// URL.", "Invalid button URL",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (editingSecondButton)
                {
                    activeSecondButtonLabel = NormalizeButtonLabel(label, DefaultSecondButtonLabel);
                    activeSecondButtonUrl = NormalizeButtonUrl(url, DefaultSecondButtonUrl);
                    rpc.SetSecondButtonSettings(activeSecondButtonLabel, activeSecondButtonUrl);
                }
                else
                {
                    activeJoinLabel = NormalizeJoinLabel(label);
                    activeJoinUrl = NormalizeJoinUrl(url);
                    rpc.SetJoinButtonSettings(activeJoinLabel, activeJoinUrl);
                }
                SaveProfile();
                RefreshDashboard();
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            dialog.AcceptButton = save;
            dialog.Controls.Add(save);
            dialog.ShowDialog(this);
        }
    }

    private static string NormalizeJoinLabel(string value)
    {
        return NormalizeButtonLabel(value, DefaultActivityButtonLabel);
    }

    private static string NormalizeButtonLabel(string value, string fallback)
    {
        string label = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return label.Length > 32 ? label.Substring(0, 32) : label;
    }

    private static string NormalizeJoinUrl(string value)
    {
        return NormalizeButtonUrl(value, DefaultActivityButtonUrl);
    }

    private static string NormalizeButtonUrl(string value, string fallback)
    {
        Uri parsed;
        if (Uri.TryCreate(value, UriKind.Absolute, out parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            return value.Length > 512 ? value.Substring(0, 512) : value;
        return fallback;
    }

    private void OpenPreviewActivityButton(int buttonNumber)
    {
        bool secondButton = buttonNumber == 2;
        if (officialModeEnabled || (secondButton ? !secondButtonBox.Checked : !joinGameBox.Checked)) return;
        string url = secondButton
            ? NormalizeButtonUrl(activeSecondButtonUrl, DefaultSecondButtonUrl)
            : NormalizeJoinUrl(activeJoinUrl);
        try
        {
            Process.Start(url);
        }
        catch
        {
            MessageBox.Show("Windows could not open the configured activity-button URL.",
                "Unable to open link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool IsValidAppId(string value)
    {
        if (value.Length < 15 || value.Length > 25) return false;
        for (int i = 0; i < value.Length; i++) if (!char.IsDigit(value[i])) return false;
        return true;
    }

    private void OnRpcStatusChanged(string message, bool connected)
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke((MethodInvoker)delegate
            {
                if (officialModeEnabled) return;
                rpcConnected = connected;
                lastRpcMessage = message ?? "No status message";
                connectionLabel.Text = "●  " + message;
                connectionLabel.ForeColor = connected ? Color.FromArgb(63, 214, 127) : Color.FromArgb(255, 190, 80);
                if (!connected && message.StartsWith("Error"))
                {
                    connectButton.Enabled = true;
                    appIdBox.Enabled = true;
                }
                RefreshDashboard();
                RefreshDiagnostics();
            });
        }
        catch { }
    }

    private void OnActivityChanged(string message, long deadline)
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke((MethodInvoker)delegate
            {
                if (officialModeEnabled) return;
                currentActivityTemplate = message ?? "";
                currentActivityDeadline = deadline;
                lastActivitySentUtc = DateTime.UtcNow;
                if (activeMode == "Auto" && !string.IsNullOrEmpty(message) &&
                    !message.StartsWith("Starting", StringComparison.OrdinalIgnoreCase))
                {
                    string[] activityLines = message.Replace("\r", "").Split('\n');
                    if (activityLines.Length > 0) activeState = activityLines[0].Trim();
                    if (activityLines.Length > 1) activeStatus = TrimActivityTiming(activityLines[1].Trim());
                    activeSessionMode = RealisticAutoModeLabel;
                    activeCharacter = characterBox.SelectedItem == null ? "Automatic" : characterBox.SelectedItem.ToString();
                    SaveProfile();
                }
                UpdateCurrentStatusCountdown();
                currentStatusLabel.ForeColor = message.StartsWith("Starting")
                    ? Color.FromArgb(255, 190, 80) : Color.FromArgb(63, 214, 127);
                RefreshDashboard();
                RefreshDiagnostics();
            });
        }
        catch { }
    }

    private void UpdateCurrentStatusCountdown()
    {
        if (officialModeEnabled || string.IsNullOrEmpty(currentActivityTemplate)) return;
        if (currentActivityDeadline <= 0 || currentActivityTemplate.IndexOf("{0}", StringComparison.Ordinal) < 0)
        {
            currentStatusLabel.Text = currentActivityTemplate;
            return;
        }

        long remaining = Math.Max(0, currentActivityDeadline - UiUnixNow());
        currentStatusLabel.Text = string.Format(currentActivityTemplate, FormatUiCountdown(remaining));
    }

    private static string FormatUiCountdown(long totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;
        return hours > 0
            ? hours + ":" + minutes.ToString("00") + ":" + seconds.ToString("00")
            : minutes + ":" + seconds.ToString("00");
    }

    private static long UiUnixNow()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    private void ApplyCustomStatus()
    {
        if (officialModeEnabled) { ShowCustomModeRequired(); return; }
        string value = customStatusBox.Text.Trim();
        if (value.Length == 0)
        {
            MessageBox.Show("Enter a custom status first.", "Empty status",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        activeMode = "Custom";
        activeStatus = value;
        activeSessionMode = sessionModeBox.SelectedItem.ToString();
        activeCharacter = characterBox.SelectedItem.ToString();
        activeState = BuildStateForSelection();
        rpc.UseActivity(value, activeState);
        SaveProfile();
        RefreshDashboard();
    }

    private void ApplySessionProfile()
    {
        if (officialModeEnabled) { ShowCustomModeRequired(); return; }
        string sessionMode = sessionModeBox.SelectedItem.ToString();
        string character = characterBox.SelectedItem.ToString();
        bool keepingCurrentAutomaticScene = activeMode == "Auto" &&
            string.Equals(activeSessionMode, sessionMode, StringComparison.Ordinal) &&
            string.Equals(activeCharacter, character, StringComparison.Ordinal);
        activeSessionMode = sessionMode;
        activeCharacter = character;

        if (sessionMode == RealisticAutoModeLabel)
        {
            activeMode = "Auto";
            if (!keepingCurrentAutomaticScene)
            {
                activeStatus = "";
                activeState = "";
            }
            rpc.UseAutomaticSequence(character);
        }
        else
        {
            string details;
            if (sessionMode == "Main Menu") details = "Main Menu";
            else if (sessionMode == "Paused") details = "Paused";
            else if (statusBox.SelectedIndex > 0) details = statusBox.SelectedItem.ToString();
            else if (sessionMode == "Free Roam") details = "Exploring Leonida";
            else if (sessionMode == "Story Mission") details = "Planning The Next Score";
            else
            {
                MessageBox.Show("Choose an activity from the list before using Manual mode.",
                    "Activity required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            activeMode = "Preset";
            activeStatus = details;
            activeState = BuildStateForSelection();
            rpc.UseActivity(activeStatus, activeState);
        }
        SaveProfile();
        RefreshDashboard();
    }

    private void AdvanceAutomaticScene()
    {
        if (officialModeEnabled) { ShowCustomModeRequired(); return; }
        sessionModeBox.SelectedIndex = 0;
        activeMode = "Auto";
        activeStatus = "";
        activeState = "";
        activeSessionMode = RealisticAutoModeLabel;
        activeCharacter = characterBox.SelectedItem.ToString();
        rpc.AdvanceAutomaticScene(activeCharacter);
        SaveProfile();
        RefreshDashboard();
    }

    private string BuildStateForSelection()
    {
        string mode = sessionModeBox.SelectedItem == null ? "Manual" : sessionModeBox.SelectedItem.ToString();
        string character = characterBox.SelectedItem == null ? "Automatic" : characterBox.SelectedItem.ToString();
        string actor = character == "Automatic" ? "" : " • " + character;
        if (mode == "Main Menu") return "Story Mode";
        if (mode == "Paused") return "Story Mode" + actor;
        if (mode == "Free Roam") return "Free Roam" + actor;
        if (mode == "Story Mission") return "Story Mission" + actor;
        return "Story Mode" + actor;
    }

    private static string DecodeProfileValue(string value, string fallback)
    {
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        catch { return fallback; }
    }

    private void RefreshProfileList()
    {
        if (profileBox == null) return;
        string previous = profileBox.SelectedItem == null ? "Default Settings" : profileBox.SelectedItem.ToString();
        profileBox.BeginUpdate();
        try
        {
            profileBox.Items.Clear();
            profileBox.Items.Add("Default Settings");
            Directory.CreateDirectory(profilesDirectory);
            foreach (string path in Directory.GetFiles(profilesDirectory, "*.ini"))
                profileBox.Items.Add(Path.GetFileNameWithoutExtension(path));
            SelectComboValue(profileBox, previous, 0);
        }
        catch
        {
            if (profileBox.Items.Count == 0) profileBox.Items.Add("Default Settings");
            profileBox.SelectedIndex = 0;
        }
        finally { profileBox.EndUpdate(); }
    }

    private string SelectedProfilePath()
    {
        if (profileBox == null || profileBox.SelectedItem == null ||
            string.Equals(profileBox.SelectedItem.ToString(), "Default Settings", StringComparison.Ordinal))
            return profilePath;
        string name = SanitizeProfileName(profileBox.SelectedItem.ToString());
        string root = Path.GetFullPath(profilesDirectory) + Path.DirectorySeparatorChar;
        string result = Path.GetFullPath(Path.Combine(profilesDirectory, name + ".ini"));
        return result.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? result : null;
    }

    private static string SanitizeProfileName(string value)
    {
        string name = string.IsNullOrWhiteSpace(value) ? "Profile" : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        name = name.Replace('.', '_');
        if (name.Length > 48) name = name.Substring(0, 48);
        return string.IsNullOrWhiteSpace(name) ? "Profile" : name;
    }

    private string PromptForProfileName()
    {
        using (Form dialog = new Form())
        {
            dialog.Text = "New profile";
            dialog.ClientSize = new Size(440, 176);
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowInTaskbar = false;
            dialog.BackColor = Color.FromArgb(10, 9, 15);
            dialog.ForeColor = Color.White;
            dialog.Icon = Icon;
            dialog.Controls.Add(new Label
            {
                Location = new Point(22, 20), Size = new Size(396, 24), Text = "PROFILE NAME",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(235, 51, 145)
            });
            TextBox nameBox = new TextBox
            {
                Location = new Point(22, 55), Size = new Size(396, 31), MaxLength = 48,
                Font = new Font("Segoe UI", 10f), BackColor = Color.FromArgb(25, 26, 36),
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            dialog.Controls.Add(nameBox);
            Button cancel = MakeButton("CANCEL", new Point(134, 112), 130, Color.FromArgb(55, 56, 69));
            cancel.DialogResult = DialogResult.Cancel;
            dialog.Controls.Add(cancel);
            Button create = MakeButton("CREATE", new Point(278, 112), 140, Color.FromArgb(235, 51, 145));
            create.DialogResult = DialogResult.OK;
            dialog.Controls.Add(create);
            dialog.CancelButton = cancel;
            dialog.AcceptButton = create;
            nameBox.Focus();
            return dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(nameBox.Text)
                ? SanitizeProfileName(nameBox.Text) : null;
        }
    }

    private void CreateNamedProfile()
    {
        string name = PromptForProfileName();
        if (name == null) return;
        try
        {
            Directory.CreateDirectory(profilesDirectory);
            SaveProfile();
            string destination = Path.Combine(profilesDirectory, name + ".ini");
            if (File.Exists(destination) && MessageBox.Show("Replace the existing profile ‘" + name + "’?",
                "Profile exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            File.Copy(profilePath, destination, true);
            RefreshProfileList();
            SelectComboValue(profileBox, name, 0);
            profileStatusLabel.Text = "Saved ‘" + name + "’. The Discord Application ID was not included.";
            profileStatusLabel.ForeColor = Color.FromArgb(70, 204, 105);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The profile could not be created:\r\n" + ex.Message, "Profile error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SaveSelectedProfile()
    {
        if (profileBox == null || profileBox.SelectedIndex <= 0)
        {
            CreateNamedProfile();
            return;
        }
        string target = SelectedProfilePath();
        if (target == null) return;
        try
        {
            SaveProfile();
            File.Copy(profilePath, target, true);
            profileStatusLabel.Text = "Updated ‘" + profileBox.SelectedItem + "’. Application ID still remains separate.";
            profileStatusLabel.ForeColor = Color.FromArgb(70, 204, 105);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The profile could not be saved:\r\n" + ex.Message, "Profile error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadSelectedProfile()
    {
        string source = SelectedProfilePath();
        if (source == null || !File.Exists(source))
        {
            MessageBox.Show("That profile file is not available.", "Profile not found",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            if (!string.Equals(source, profilePath, StringComparison.OrdinalIgnoreCase))
                File.Copy(source, profilePath, true);
            LoadProfile();
            ApplyPresenceMode();
            RefreshDashboard();
            profileStatusLabel.Text = "Loaded ‘" + profileBox.SelectedItem + "’. Your Application ID was left unchanged.";
            profileStatusLabel.ForeColor = Color.FromArgb(70, 204, 105);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The profile could not be loaded:\r\n" + ex.Message, "Profile error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ImportProfile()
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "Import a GTA VI Presence profile";
            dialog.Filter = "Presence profiles (*.ini)|*.ini";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            string name = SanitizeProfileName(Path.GetFileNameWithoutExtension(dialog.FileName));
            try
            {
                Directory.CreateDirectory(profilesDirectory);
                string destination = Path.Combine(profilesDirectory, name + ".ini");
                if (File.Exists(destination) && MessageBox.Show("Replace the existing profile ‘" + name + "’?",
                    "Profile exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                File.Copy(dialog.FileName, destination, true);
                RefreshProfileList();
                SelectComboValue(profileBox, name, 0);
                profileStatusLabel.Text = "Imported ‘" + name + "’. Load it when you are ready.";
                profileStatusLabel.ForeColor = Color.FromArgb(70, 204, 105);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The profile could not be imported:\r\n" + ex.Message, "Import failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void ExportSelectedProfile()
    {
        string source = SelectedProfilePath();
        if (string.Equals(source, profilePath, StringComparison.OrdinalIgnoreCase)) SaveProfile();
        if (source == null || !File.Exists(source)) return;
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Title = "Export a GTA VI Presence profile";
            dialog.Filter = "Presence profiles (*.ini)|*.ini";
            dialog.FileName = SanitizeProfileName(profileBox.SelectedItem == null ? "GTA-VI-Presence" : profileBox.SelectedItem.ToString()) + ".ini";
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                File.Copy(source, dialog.FileName, true);
                profileStatusLabel.Text = "Profile exported without the Discord Application ID.";
                profileStatusLabel.ForeColor = Color.FromArgb(70, 204, 105);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The profile could not be exported:\r\n" + ex.Message, "Export failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void DeleteSelectedProfile()
    {
        if (profileBox == null || profileBox.SelectedIndex <= 0)
        {
            MessageBox.Show("Default Settings cannot be deleted.", "Profile protected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string target = SelectedProfilePath();
        string name = profileBox.SelectedItem.ToString();
        if (target == null || !File.Exists(target)) return;
        if (MessageBox.Show("Delete the local profile ‘" + name + "’?", "Delete profile",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            File.Delete(target);
            RefreshProfileList();
            profileStatusLabel.Text = "Deleted ‘" + name + "’. Current Studio settings were not changed.";
            profileStatusLabel.ForeColor = Color.FromArgb(255, 190, 80);
        }
        catch (Exception ex)
        {
            MessageBox.Show("The profile could not be deleted:\r\n" + ex.Message, "Delete failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshDiagnostics()
    {
        if (diagnosticDiscordValue == null || IsDisposed) return;
        Color green = Color.FromArgb(70, 204, 105);
        Color warning = Color.FromArgb(255, 190, 80);
        bool discordFound = false;
        try
        {
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.ProcessName.StartsWith("Discord", StringComparison.OrdinalIgnoreCase))
                    {
                        discordFound = true;
                        process.Dispose();
                        break;
                    }
                    process.Dispose();
                }
                catch { }
            }
        }
        catch { }
        diagnosticDiscordValue.Text = discordFound ? "Running" : "Not found";
        diagnosticDiscordValue.ForeColor = discordFound ? green : warning;
        bool appIdReady = IsCustomSetupReady();
        diagnosticAppIdValue.Text = appIdReady ? "Configured and applied" : "Not ready";
        diagnosticAppIdValue.ForeColor = appIdReady ? green : warning;
        diagnosticRpcValue.Text = officialModeEnabled ? "Native mode — RPC off" : lastRpcMessage;
        diagnosticRpcValue.ForeColor = rpcConnected || officialModeEnabled ? green : warning;
        diagnosticAssetValue.Text = ResolveConfiguredLargeImageKey();
        diagnosticAssetValue.ForeColor = green;
        int buttonCount = (joinGameBox.Checked ? 1 : 0) + (secondButtonBox.Checked ? 1 : 0);
        diagnosticButtonsValue.Text = buttonCount == 0 ? "Off" : buttonCount + (buttonCount == 1 ? " enabled" : " enabled (Discord maximum)");
        diagnosticButtonsValue.ForeColor = green;
        bool writable = CanWriteSettingsFolder();
        diagnosticStorageValue.Text = writable ? "Writable" : "Read-only or blocked";
        diagnosticStorageValue.ForeColor = writable ? green : warning;
        diagnosticLastSendValue.Text = lastActivitySentUtc == DateTime.MinValue
            ? "Not sent this session" : lastActivitySentUtc.ToLocalTime().ToString("HH:mm:ss");
        diagnosticLastSendValue.ForeColor = lastActivitySentUtc == DateTime.MinValue ? warning : green;
    }

    private static string ResolveConfiguredLargeImageKey()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-image.txt");
            if (File.Exists(path))
            {
                string value = File.ReadAllText(path).Trim();
                if (value.Length >= 2 && value.Length <= 256) return value;
            }
        }
        catch { }
        return "gtavi_cover";
    }

    private static bool CanWriteSettingsFolder()
    {
        string testPath = null;
        try
        {
            testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".presence-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            try { if (testPath != null && File.Exists(testPath)) File.Delete(testPath); }
            catch { }
            return false;
        }
    }

    private void CopySafeDiagnosticReport()
    {
        RefreshDiagnostics();
        string report = "GTA VI Presence Studio 1.4.1\r\n" +
            "Windows: " + Environment.OSVersion.VersionString + "\r\n" +
            "Presence mode: " + (officialModeEnabled ? "Discord Game Detection" : "Custom Rich Presence") + "\r\n" +
            "Discord: " + diagnosticDiscordValue.Text + "\r\n" +
            "Application ID: " + diagnosticAppIdValue.Text + " (value intentionally omitted)\r\n" +
            "RPC: " + diagnosticRpcValue.Text + "\r\n" +
            "Large image: " + diagnosticAssetValue.Text + "\r\n" +
            "Buttons: " + diagnosticButtonsValue.Text + "\r\n" +
            "Settings folder: " + diagnosticStorageValue.Text + "\r\n" +
            "Last send: " + diagnosticLastSendValue.Text + "\r\n" +
            "No Application ID, URL, token, username, or local path is included.";
        try
        {
            Clipboard.SetText(report);
            MessageBox.Show("A privacy-safe diagnostic report was copied.", "Report copied",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch
        {
            MessageBox.Show(report, "Safe diagnostic report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ResetApplicationSettings()
    {
        if (MessageBox.Show("Reset the Application ID and Default Settings?\r\n\r\nNamed profiles will be kept.",
            "Reset application", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        bool previousLoading = loadingProfile;
        loadingProfile = true;
        try
        {
            rpc.Stop();
            rpcConnected = false;
            lastRpcMessage = "Not connected";
            appliedApplicationId = "";
            appIdBox.Text = "";
            officialModeEnabled = true;
            officialModeBox.Checked = true;
            customModeBox.Checked = false;
            startupDelayBox.Checked = false;
            joinGameBox.Checked = false;
            secondButtonBox.Checked = false;
            activeJoinLabel = DefaultActivityButtonLabel;
            activeJoinUrl = DefaultActivityButtonUrl;
            activeSecondButtonLabel = DefaultSecondButtonLabel;
            activeSecondButtonUrl = DefaultSecondButtonUrl;
            ResetAutomaticDefaultsForNewApplication();
            if (File.Exists(configPath)) File.Delete(configPath);
            if (File.Exists(profilePath)) File.Delete(profilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Some settings could not be reset:\r\n" + ex.Message, "Reset warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { loadingProfile = previousLoading; }
        ApplyPresenceMode();
        SaveProfile();
        RefreshDiagnostics();
        MessageBox.Show("Default Settings were restored. Named profiles were kept.", "Reset complete",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CheckForUpdates()
    {
        if (updateCheckButton == null) return;
        updateCheckButton.Enabled = false;
        updateStatusLabel.Text = "Checking the latest GitHub release…";
        ThreadPool.QueueUserWorkItem(delegate
        {
            string result;
            bool updateAvailable = false;
            string latestTag = "";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "GTA-VI-Presence-Studio/1.4.1");
                    string json = client.DownloadString("https://api.github.com/repos/mamt104/gta6-discord-status-simulator/releases/latest");
                    Match match = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                    if (!match.Success) throw new InvalidOperationException("GitHub did not return a release tag.");
                    latestTag = match.Groups[1].Value;
                    Version current = new Version("1.4.1");
                    Version latest;
                    string numericTag = latestTag.TrimStart('v', 'V');
                    if (!Version.TryParse(numericTag, out latest))
                        result = "Latest GitHub release: " + latestTag;
                    else if (latest > current)
                    {
                        updateAvailable = true;
                        result = "Update available: " + latestTag;
                    }
                    else if (latest < current)
                        result = "This test build is newer than the public release (" + latestTag + ").";
                    else
                        result = "You are up to date (" + latestTag + ").";
                }
            }
            catch (Exception ex)
            {
                result = "Could not check: " + ex.Message;
            }
            if (IsDisposed) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    updateCheckButton.Enabled = true;
                    updateStatusLabel.Text = result;
                    if (updateAvailable && MessageBox.Show("Version " + latestTag + " is available. Open GitHub Releases?",
                        "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        Process.Start(ProjectUrl + "/releases/latest");
                });
            }
            catch { }
        });
    }

    private void ShowCustomModeRequired()
    {
        MessageBox.Show(
            "This feature uses custom Rich Presence and temporarily replaces the detected " +
            "game card. Disable ‘Discord detection only’ to use it.",
            "Custom mode required", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void SelectComboValue(ComboBox combo, string value, int fallbackIndex)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (string.Equals(combo.Items[i].ToString(), value, StringComparison.Ordinal))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = fallbackIndex;
    }

    private int RotationMinutesFromSelection()
    {
        switch (rotationIntervalBox.SelectedIndex)
        {
            case 1: return 1;
            case 2: return 3;
            case 3: return 5;
            case 4: return 10;
            case 5: return 15;
            case 6: return 30;
            default: return 0;
        }
    }

    private void SelectRotationValue(int minutes)
    {
        if (minutes == 1) rotationIntervalBox.SelectedIndex = 1;
        else if (minutes == 3) rotationIntervalBox.SelectedIndex = 2;
        else if (minutes == 5) rotationIntervalBox.SelectedIndex = 3;
        else if (minutes == 10) rotationIntervalBox.SelectedIndex = 4;
        else if (minutes == 15) rotationIntervalBox.SelectedIndex = 5;
        else if (minutes == 30) rotationIntervalBox.SelectedIndex = 6;
        else rotationIntervalBox.SelectedIndex = 0;
    }

    private void ShowRecentGamesHelp()
    {
        ShowSetupGuide(false);
    }

    private void ShowFirstRunGuideIfNeeded()
    {
        if (onboardingComplete || IsDisposed || Disposing) return;
        ShowSetupGuide(true);
    }

    private void ShowCustomModeGuide()
    {
        Color pink = Color.FromArgb(255, 70, 156);
        Color orange = Color.FromArgb(255, 151, 62);
        Color green = Color.FromArgb(63, 214, 127);
        Color muted = Color.FromArgb(184, 181, 196);
        Color card = Color.FromArgb(24, 22, 32);
        DialogResult result;

        using (Form guide = new Form())
        {
            guide.Text = "Unlock Custom Rich Presence";
            guide.ClientSize = new Size(640, 690);
            guide.StartPosition = FormStartPosition.CenterParent;
            guide.FormBorderStyle = FormBorderStyle.FixedDialog;
            guide.MaximizeBox = false;
            guide.MinimizeBox = false;
            guide.ShowInTaskbar = false;
            guide.BackColor = Color.FromArgb(10, 9, 15);
            guide.ForeColor = Color.White;
            guide.Icon = Icon;

            guide.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 7, BackColor = pink });
            guide.Controls.Add(new Label
            {
                Location = new Point(30, 25), Size = new Size(580, 44), Text = "CUSTOM MODE IS LOCKED",
                Font = new Font("Arial Black", 19f, FontStyle.Bold), ForeColor = Color.White
            });
            guide.Controls.Add(new Label
            {
                Location = new Point(33, 72), Size = new Size(575, 50),
                Text = "Complete this one-time Discord setup. Session Director unlocks only after a valid Application ID is applied.",
                Font = new Font("Segoe UI", 10f), ForeColor = muted
            });

            Panel steps = new Panel
            {
                Location = new Point(30, 128), Size = new Size(580, 318), BackColor = card,
                BorderStyle = BorderStyle.FixedSingle
            };
            guide.Controls.Add(steps);
            steps.Controls.Add(MakeGuideStep("1", "CREATE THE APPLICATION", "Click New Application and name it Grand Theft Auto VI.", 18, pink));
            steps.Controls.Add(MakeGuideStep("2", "COMPLETE GENERAL INFORMATION", "Upload gtavi_cover.png as App Icon, save, then copy Application ID.", 90, orange));
            steps.Controls.Add(MakeGuideStep("3", "ADD THE RICH PRESENCE ASSET", "Upload the same PNG, then name the resource gtavi_cover.", 162, green));
            steps.Controls.Add(MakeGuideStep("4", "UNLOCK CUSTOM MODE", "Paste Application ID in Settings and press APPLY ID.", 234, Color.FromArgb(120, 130, 255)));

            Panel safety = new Panel
            {
                Location = new Point(30, 462), Size = new Size(580, 108), BackColor = Color.FromArgb(29, 25, 37)
            };
            guide.Controls.Add(safety);
            safety.Controls.Add(new Label
            {
                Location = new Point(16, 13), Size = new Size(548, 23), Text = "WHAT TO COPY",
                Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold), ForeColor = green
            });
            safety.Controls.Add(new Label
            {
                Location = new Point(16, 39), Size = new Size(548, 55),
                Text = "Use the Application ID from General Information — not your Discord user ID. Never paste a Bot Token, Client Secret, password or OAuth code.",
                Font = new Font("Segoe UI", 9.2f), ForeColor = Color.White
            });

            guide.Controls.Add(new Label
            {
                Location = new Point(33, 576), Size = new Size(575, 52),
                Text = "After APPLY ID, Custom changes from LOCKED to READY. New Discord apps and images may need a few minutes to sync; wait, then press SET STATUS again if needed.",
                Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold), ForeColor = orange
            });

            Button portal = MakeButton("OPEN DEV PORTAL", new Point(30, 634), 190, Color.FromArgb(74, 79, 177));
            portal.Click += delegate { Process.Start("https://discord.com/developers/applications"); };
            guide.Controls.Add(portal);

            Button settings = MakeButton("GO TO SETTINGS", new Point(230, 634), 190, pink);
            settings.DialogResult = DialogResult.OK;
            guide.AcceptButton = settings;
            guide.Controls.Add(settings);

            Button cancel = MakeButton("NOT NOW", new Point(430, 634), 180, Color.FromArgb(50, 42, 50));
            cancel.DialogResult = DialogResult.Cancel;
            guide.CancelButton = cancel;
            guide.Controls.Add(cancel);

            result = guide.ShowDialog(this);
        }

        if (result == DialogResult.OK && openSettingsPage != null)
        {
            openSettingsPage();
            appIdBox.Focus();
        }
    }

    private void ShowSetupGuide(bool firstRun)
    {
        Color pink = Color.FromArgb(255, 70, 156);
        Color orange = Color.FromArgb(255, 151, 62);
        Color green = Color.FromArgb(63, 214, 127);
        Color muted = Color.FromArgb(184, 181, 196);
        Color card = Color.FromArgb(24, 22, 32);

        using (Form guide = new Form())
        {
            guide.Text = "GTA VI — Discord setup";
            guide.ClientSize = new Size(640, 735);
            guide.StartPosition = FormStartPosition.CenterParent;
            guide.FormBorderStyle = FormBorderStyle.FixedDialog;
            guide.MaximizeBox = false;
            guide.MinimizeBox = false;
            guide.ShowInTaskbar = false;
            guide.BackColor = Color.FromArgb(10, 9, 15);
            guide.ForeColor = Color.White;
            guide.Icon = Icon;

            guide.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 7, BackColor = pink });
            guide.Controls.Add(new Label
            {
                Location = new Point(30, 26), Size = new Size(580, 44),
                Text = firstRun ? "COMPLETE DISCORD SETUP" : "DISCORD GAME SETUP",
                Font = new Font("Arial Black", 20f, FontStyle.Bold), ForeColor = Color.White
            });
            guide.Controls.Add(new Label
            {
                Location = new Point(33, 73), Size = new Size(575, 48),
                Text = "Discord needs this exact GTA6.exe registered once before it can show the native game card.",
                Font = new Font("Segoe UI", 10.2f), ForeColor = muted
            });

            Panel steps = new Panel
            {
                Location = new Point(30, 128), Size = new Size(580, 246),
                BackColor = card, BorderStyle = BorderStyle.FixedSingle
            };
            guide.Controls.Add(steps);
            steps.Controls.Add(MakeGuideStep("1", "KEEP THIS APP RUNNING", "Do not close or rename GTA6.exe while registering it.", 18, pink));
            steps.Controls.Add(MakeGuideStep("2", "OPEN DISCORD SETTINGS", "Go to User Settings  >  Registered Games.", 90, orange));
            steps.Controls.Add(MakeGuideStep("3", "ADD THE RUNNING GAME", "Select Add it! and choose the running GTA6.exe.", 162, green));

            guide.Controls.Add(new Label
            {
                Location = new Point(33, 390), Size = new Size(575, 24), Text = "WHAT THE CONTROLS DO",
                Font = new Font("Segoe UI Semibold", 9.3f, FontStyle.Bold), ForeColor = orange
            });

            Panel controlsGuide = new Panel
            {
                Location = new Point(30, 417), Size = new Size(580, 205), BackColor = card
            };
            guide.Controls.Add(controlsGuide);
            controlsGuide.Controls.Add(MakeControlHint("DISCORD GAME DETECTION", "Official card, icon, timer and voice tile. Custom controls stay locked.", 12, green));
            controlsGuide.Controls.Add(MakeControlHint("APPLY ID", "Starts Custom Rich Presence using the Application ID entered above.", 50, pink));
            controlsGuide.Controls.Add(MakeControlHint("SET STATUS / NEXT SCENE", "SET refreshes the current automatic scene; NEXT SCENE is the only manual skip.", 88, orange));
            controlsGuide.Controls.Add(MakeControlHint("USE TEXT / AUTO ROTATION", "Publishes your own description and controls how often scenes change.", 126, orange));
            controlsGuide.Controls.Add(MakeControlHint("ACTIVITY BUTTONS", "Adds up to two links visible to other Discord users.", 164, Color.FromArgb(120, 130, 255)));

            guide.Controls.Add(new Label
            {
                Location = new Point(33, 635), Size = new Size(575, 40),
                Text = "FIRST-RUN DEFAULTS  •  Realistic Auto (Resume)  •  activity buttons OFF\r\nLike it? Open About to star, share or send feedback.",
                Font = new Font("Segoe UI Semibold", 8.9f, FontStyle.Bold), ForeColor = green
            });

            Button openDiscord = MakeButton("OPEN DISCORD", new Point(30, 684), 180, Color.FromArgb(74, 79, 177));
            openDiscord.Click += delegate { OpenDiscordForSetup(); };
            guide.Controls.Add(openDiscord);

            Button done = MakeButton(firstRun ? "I'VE ADDED IT" : "DONE", new Point(430, 684), 180, pink);
            done.DialogResult = DialogResult.OK;
            guide.AcceptButton = done;
            guide.Controls.Add(done);

            guide.ShowDialog(this);
        }

        if (firstRun)
        {
            onboardingComplete = true;
            SaveProfile();
        }
    }

    private static Panel MakeGuideStep(string number, string title, string description, int top, Color accent)
    {
        Panel row = new Panel { Location = new Point(16, top), Size = new Size(546, 62), BackColor = Color.FromArgb(30, 27, 40) };
        Label badge = new Label
        {
            Location = new Point(12, 12), Size = new Size(38, 38), Text = number,
            TextAlign = ContentAlignment.MiddleCenter, BackColor = accent, ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold)
        };
        row.Controls.Add(badge);
        row.Controls.Add(new Label
        {
            Location = new Point(62, 8), Size = new Size(460, 22), Text = title,
            Font = new Font("Segoe UI Semibold", 9.3f, FontStyle.Bold), ForeColor = accent
        });
        row.Controls.Add(new Label
        {
            Location = new Point(62, 31), Size = new Size(460, 23), Text = description,
            Font = new Font("Segoe UI", 9.2f), ForeColor = Color.White
        });
        return row;
    }

    private static Label MakeControlHint(string title, string description, int top, Color accent)
    {
        return new Label
        {
            Location = new Point(16, top), Size = new Size(548, 34),
            Text = title + "  —  " + description,
            Font = new Font("Segoe UI", 8.8f), ForeColor = accent
        };
    }

    private static void OpenDiscordForSetup()
    {
        try
        {
            Process.Start("discord://-/channels/@me");
        }
        catch
        {
            try { Process.Start("https://discord.com/app"); }
            catch
            {
                MessageBox.Show("Open Discord, then go to User Settings > Registered Games.",
                    "Open Discord", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void HideToTray()
    {
        Hide();
        trayIcon.ShowBalloonTip(1700, "GTA VI",
            officialModeEnabled ? "Discord detection continues in the background."
                : "Custom Rich Presence continues in the background.", ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private static Button MakeButton(string text, Point location, int width, Color background)
    {
        Button button = new RoundedButton
        {
            Text = text, Location = location, Size = new Size(width, 36), BackColor = background,
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Lighten(background, 14);
        button.FlatAppearance.MouseDownBackColor = Darken(background, 14);
        button.Tag = background;
        button.EnabledChanged += delegate
        {
            Color enabledColor = button.Tag is Color ? (Color)button.Tag : background;
            button.BackColor = button.Enabled ? enabledColor : Color.FromArgb(38, 36, 48);
            button.ForeColor = button.Enabled ? Color.White : Color.FromArgb(105, 102, 119);
            button.Cursor = button.Enabled ? Cursors.Hand : Cursors.Default;
        };
        return button;
    }

    private static Color Lighten(Color color, int amount)
    {
        return Color.FromArgb(Math.Min(255, color.R + amount), Math.Min(255, color.G + amount), Math.Min(255, color.B + amount));
    }

    private static Color Darken(Color color, int amount)
    {
        return Color.FromArgb(Math.Max(0, color.R - amount), Math.Max(0, color.G - amount), Math.Max(0, color.B - amount));
    }

    private static ComboBox MakeComboBox(Point location, int width)
    {
        ComboBox combo = new DarkComboBox
        {
            Location = location, Size = new Size(width, 31),
            Font = new Font("Segoe UI", 10.2f), BackColor = Color.FromArgb(34, 31, 44),
            ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 24
        };
        combo.DrawItem += DrawComboItem;
        return combo;
    }

    private static void DrawComboItem(object sender, DrawItemEventArgs args)
    {
        ComboBox combo = (ComboBox)sender;
        Color background = (args.State & DrawItemState.Selected) != 0
            ? Color.FromArgb(74, 79, 177) : Color.FromArgb(34, 31, 44);
        using (SolidBrush backgroundBrush = new SolidBrush(background))
            args.Graphics.FillRectangle(backgroundBrush, args.Bounds);
        if (args.Index >= 0)
        {
            string value = combo.Items[args.Index].ToString();
            using (SolidBrush textBrush = new SolidBrush(Color.White))
                args.Graphics.DrawString(value, combo.Font, textBrush,
                    new RectangleF(args.Bounds.X + 4, args.Bounds.Y + 2, args.Bounds.Width - 8, args.Bounds.Height - 4));
        }
        args.DrawFocusRectangle();
    }
}

internal sealed class BufferedTableLayoutPanel : TableLayoutPanel
{
    public BufferedTableLayoutPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);
    }
}

internal sealed class RoundedButton : Button
{
    public int Radius { get; set; }

    public RoundedButton()
    {
        Radius = 7;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        if (Width <= 0 || Height <= 0) return;
        int diameter = Math.Min(Math.Min(Width, Height), Math.Max(2, Radius * 2));
        using (GraphicsPath path = new GraphicsPath())
        {
            Rectangle arc = new Rectangle(0, 0, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = Width - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = Height - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = 0;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            Region oldRegion = Region;
            Region = new Region(path);
            if (oldRegion != null) oldRegion.Dispose();
        }
    }
}

internal sealed class DarkComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private const int WmPrintClient = 0x0318;

    protected override void OnMouseWheel(MouseEventArgs eventArgs)
    {
        if (DroppedDown)
        {
            base.OnMouseWheel(eventArgs);
            return;
        }

        Control current = Parent;
        while (current != null)
        {
            ScrollableControl scrollable = current as ScrollableControl;
            if (scrollable != null && scrollable.AutoScroll)
            {
                int lines = SystemInformation.MouseWheelScrollLines;
                if (lines <= 0 || lines > 12) lines = 3;
                int currentY = Math.Max(0, -scrollable.AutoScrollPosition.Y);
                int targetY = Math.Max(0, currentY - Math.Sign(eventArgs.Delta) * lines * 24);
                scrollable.AutoScrollPosition = new Point(Math.Max(0, -scrollable.AutoScrollPosition.X), targetY);
                return;
            }
            current = current.Parent;
        }
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg == WmPaint || message.Msg == WmNcPaint || message.Msg == WmPrintClient)
            PaintDarkChrome();
    }

    protected override void OnGotFocus(EventArgs eventArgs)
    {
        base.OnGotFocus(eventArgs);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs eventArgs)
    {
        base.OnLostFocus(eventArgs);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        base.OnEnabledChanged(eventArgs);
        Invalidate();
    }

    private void PaintDarkChrome()
    {
        if (!IsHandleCreated || Width < 34 || Height < 12) return;
        Color surface = Enabled ? Color.FromArgb(34, 31, 44) : Color.FromArgb(27, 26, 35);
        Color border = Focused ? Color.FromArgb(235, 51, 145) : Color.FromArgb(65, 65, 82);
        Color arrow = Enabled ? Color.FromArgb(184, 181, 196) : Color.FromArgb(91, 89, 103);
        const int buttonWidth = 28;
        using (Graphics graphics = CreateGraphics())
        using (SolidBrush surfaceBrush = new SolidBrush(surface))
        using (SolidBrush borderBrush = new SolidBrush(border))
        using (SolidBrush arrowBrush = new SolidBrush(arrow))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(surfaceBrush, ClientRectangle);
            graphics.FillRectangle(borderBrush, 0, 0, Width, 1);
            graphics.FillRectangle(borderBrush, 0, Height - 1, Width, 1);
            graphics.FillRectangle(borderBrush, 0, 0, 1, Height);
            graphics.FillRectangle(borderBrush, Width - 1, 0, 1, Height);
            graphics.FillRectangle(borderBrush, Width - buttonWidth, 5, 1, Height - 10);

            string value = SelectedItem == null ? Text : SelectedItem.ToString();
            Color textColor = Enabled ? Color.White : Color.FromArgb(126, 123, 139);
            TextRenderer.DrawText(graphics, value, Font,
                new Rectangle(9, 1, Width - buttonWidth - 13, Height - 2), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            int centerX = Width - (buttonWidth / 2);
            int centerY = Height / 2;
            graphics.FillPolygon(arrowBrush, new[]
            {
                new Point(centerX - 4, centerY - 2),
                new Point(centerX + 4, centerY - 2),
                new Point(centerX, centerY + 3)
            });
        }
    }
}

internal sealed class CardPanel : Panel
{
    private Color borderColor = Color.FromArgb(43, 44, 58);
    private Color accentColor = Color.Transparent;
    private int accentWidth;
    private int borderRadius = 10;
    private int borderThickness = 1;

    public Color BorderColor
    {
        get { return borderColor; }
        set { borderColor = value; Invalidate(); }
    }

    public int BorderRadius
    {
        get { return borderRadius; }
        set { borderRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
    }

    public Color AccentColor
    {
        get { return accentColor; }
        set { accentColor = value; Invalidate(); }
    }

    public int AccentWidth
    {
        get { return accentWidth; }
        set { accentWidth = Math.Max(0, value); Invalidate(); }
    }

    public int BorderThickness
    {
        get { return borderThickness; }
        set { borderThickness = Math.Max(1, value); Invalidate(); }
    }

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (Width < 2 || Height < 2) return;
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new Rectangle(borderThickness / 2, borderThickness / 2,
            Width - borderThickness, Height - borderThickness);
        using (GraphicsPath path = CreateRoundedPath(bounds, borderRadius))
        using (Pen pen = new Pen(borderColor, borderThickness))
            eventArgs.Graphics.DrawPath(pen, path);
        if (accentWidth > 0 && accentColor.A > 0 && Height > 28)
        {
            using (SolidBrush accentBrush = new SolidBrush(accentColor))
                eventArgs.Graphics.FillRectangle(accentBrush, 1, 14, accentWidth, Height - 28);
        }
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0) return;
        using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, Width, Height), borderRadius))
        {
            Region oldRegion = Region;
            Region = new Region(path);
            if (oldRegion != null) oldRegion.Dispose();
        }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = Math.Min(Math.Min(bounds.Width, bounds.Height), Math.Max(1, radius * 2));
        if (diameter <= 2)
        {
            path.AddRectangle(bounds);
            return path;
        }
        Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class StyledFlowLayoutPanel : FlowLayoutPanel
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr windowHandle, string subAppName, string subIdList);

    public StyledFlowLayoutPanel(Color accent)
    {
        AutoScroll = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        try
        {
            SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
        catch { }
    }
}

internal sealed class DiscordRpcClient : IDisposable
{
    private const string DefaultActivityButtonLabel = "Join";
    private const string DefaultActivityButtonUrl = "https://store.rockstargames.com/game/buy-gta-vi";
    private const string DefaultSecondButtonLabel = "Official Website";
    private const string DefaultSecondButtonUrl = "https://www.rockstargames.com/VI";
    private const long StartupDelaySeconds = 300;
    private const string DefaultLargeImageKey = "gtavi_cover";
    private static readonly string[] Activities =
    {
        "Driving In Leonida",
        "Cruising Through Vice City",
        "Exploring The Leonida Keys",
        "Crossing The Grassrivers",
        "Laying Low In Port Gellhorn",
        "Chasing Trouble In Ambrosia",
        "Riding Through Mount Kalaga",
        "Watching The Neon Glow",
        "Planning A Score With Lucia",
        "Running A Job With Jason",
        "Pulling Off An Easy Score",
        "Escaping A Botched Robbery",
        "Working For Local Runners",
        "Moving Product Through The Keys",
        "Handling A Local Shakedown",
        "Meeting At The Boatyard",
        "Hanging Out At The Safehouse",
        "Customizing A Getaway Car",
        "Searching For Classic Cars",
        "Tracking Down A Project Car",
        "Stocking A Personal Garage",
        "Fencing Stolen Goods",
        "Customizing The Loadout",
        "Looking For The Next Big Score",
        "Hitting The Vice City Clubs",
        "Recording At Only Raw Records",
        "Chasing A Viral Hit",
        "Snooping On Coast Guard Comms",
        "Living Another Day In Paradise",
        "Cruising The Coast At Sunset",
        "Kayaking Off The Leonida Coast",
        "Fishing In The Leonida Keys",
        "Diving Beneath The Keys",
        "Riding A Dirt Bike In Leonida",
        "Playing A Round Of Mini Golf",
        "Tuning The Getaway Ride",
        "Changing The Look In Vice City",
        "Causing Chaos Across Leonida",
        "Meeting Raul Bautista",
        "Visiting Jack Of Hearts",
        "Working With Brian Heder",
        "Hanging Out With Cal Hampton",
        "Recording With Dre'Quan Priest",
        "Following Real Dimez",
        "Leaving Leonida Penitentiary",
        "Talking Business With Boobie Ike"
    };

    private sealed class PresenceScene
    {
        public readonly string Details;
        public readonly string State;
        public readonly string Actor;
        public readonly int MinSeconds;
        public readonly int MaxSeconds;
        public readonly bool Repeatable;

        public PresenceScene(string details, string state, string actor, int minSeconds, int maxSeconds, bool repeatable)
        {
            Details = details;
            State = state;
            Actor = actor;
            MinSeconds = minSeconds;
            MaxSeconds = maxSeconds;
            Repeatable = repeatable;
        }
    }

    // This is a believable play-session arc, not a claim about Rockstar's
    // unreleased mission order. It begins quietly, introduces the two public
    // character threads separately, then moves into shared and higher-action
    // scenes. Ambient scenes may recur after several other activities.
    private static readonly PresenceScene[] RealisticScenes =
    {
        new PresenceScene("Starting A New Session", "Story Mode", "Any", 300, 540, false),
        new PresenceScene("Exploring The Leonida Keys", "Free Roam • Jason Duval", "Jason", 600, 1080, true),
        new PresenceScene("Working For Local Runners", "Story Mode • Jason Duval", "Jason", 480, 840, false),
        new PresenceScene("Leaving Leonida Penitentiary", "Story Mode • Lucia Caminos", "Lucia", 360, 600, false),
        new PresenceScene("Laying Low In Port Gellhorn", "Story Mode • Lucia Caminos", "Lucia", 480, 840, true),
        new PresenceScene("Hanging Out At The Safehouse", "Story Mode • Jason & Lucia", "Both", 480, 840, true),
        new PresenceScene("Driving In Leonida", "Free Roam • Jason & Lucia", "Both", 600, 1080, true),
        new PresenceScene("Planning The Next Score", "Story Mode • Jason & Lucia", "Both", 480, 840, false),
        new PresenceScene("Meeting Raul Bautista", "Preparing A Score • Jason & Lucia", "Both", 420, 720, false),
        new PresenceScene("Pulling Off An Easy Score", "Story Mission • Jason & Lucia", "Both", 720, 1200, false),
        new PresenceScene("Escaping A Botched Robbery", "Wanted • Jason & Lucia", "Both", 420, 720, false),
        new PresenceScene("Meeting At The Boatyard", "With Brian Heder • Jason", "Jason", 420, 720, false),
        new PresenceScene("Moving Product Through The Keys", "Working For Brian Heder • Jason", "Jason", 540, 900, false),
        new PresenceScene("Hanging Out With Cal Hampton", "Story Mode • Jason Duval", "Jason", 420, 720, true),
        new PresenceScene("Snooping On Coast Guard Comms", "With Cal Hampton • Jason", "Jason", 420, 720, true),
        new PresenceScene("Cruising Through Vice City", "Free Roam • Lucia Caminos", "Lucia", 600, 1080, true),
        new PresenceScene("Changing The Look In Vice City", "Story Mode • Lucia Caminos", "Lucia", 360, 660, true),
        new PresenceScene("Crossing The Grassrivers", "Free Roam • Jason Duval", "Jason", 600, 1080, true),
        new PresenceScene("Riding Through Mount Kalaga", "Free Roam • Lucia Caminos", "Lucia", 600, 1080, true),
        new PresenceScene("Hitting The Vice City Clubs", "Nightlife • Lucia Caminos", "Lucia", 540, 900, true),
        new PresenceScene("Talking Business With Boobie Ike", "Jack Of Hearts • Vice City", "Any", 480, 780, false),
        new PresenceScene("Recording At Only Raw Records", "With Dre'Quan Priest", "Any", 480, 780, false),
        new PresenceScene("Chasing A Viral Hit", "With Real Dimez", "Lucia", 420, 720, false),
        new PresenceScene("Watching The Neon Glow", "Vice City • Night", "Both", 720, 1200, true),
        new PresenceScene("Living Another Day In Paradise", "Story Mode • Jason & Lucia", "Both", 720, 1200, true)
    };

    public event Action<string, bool> StatusChanged;
    public event Action<string, long> ActivityChanged;
    private Thread worker;
    private NamedPipeClientStream pipe;
    private System.Threading.Timer rotationTimer;
    private volatile bool stopping;
    private volatile bool ready;
    private string clientId;
    private readonly object writeLock = new object();
    private readonly object stateLock = new object();
    private readonly Random random = new Random();
    private long sessionStarted;
    private int nextActivityIndex;
    private string selectedActivity;
    private string selectedState;
    private string currentActivity;
    private string currentState;
    private string actorFilter = "Automatic";
    private bool automaticSequenceFresh = true;
    private bool automaticSequenceConfigured;
    private string resumeActivity;
    private string resumeState;
    private int automaticStepCount;
    private int scenesSinceRepeat;
    private int highestVisitedIndex = -1;
    private int lastSceneIndex = -1;
    private int previousSceneIndex = -1;
    private bool startupDelayEnabled;
    private bool joinButtonEnabled;
    private string joinButtonLabel = DefaultActivityButtonLabel;
    private string joinButtonUrl = DefaultActivityButtonUrl;
    private bool secondButtonEnabled;
    private string secondButtonLabel = DefaultSecondButtonLabel;
    private string secondButtonUrl = DefaultSecondButtonUrl;
    private int rotationIntervalMinutes;

    public static string[] GetActivities()
    {
        return (string[])Activities.Clone();
    }

    public void Start(string id)
    {
        Stop();
        clientId = id;
        stopping = false;
        ready = false;
        // Keep the selected automatic scene across an RPC restart. A new App
        // ID explicitly resets it, while ordinary reconnects resume it.
        sessionStarted = UnixNow();
        worker = new Thread(WorkerLoop) { IsBackground = true, Name = "Discord RPC" };
        worker.Start();
    }

    private void WorkerLoop()
    {
        while (!stopping)
        {
            try
            {
                pipe = FindDiscordPipe();
                if (pipe == null)
                {
                    Raise("Discord desktop was not found — retrying…", false);
                    SleepInterruptibly(2000);
                    continue;
                }

                SendFrame(0, "{\"v\":1,\"client_id\":\"" + clientId + "\"}");
                Raise("Handshaking with Discord…", false);

                while (!stopping && pipe.IsConnected)
                {
                    int opcode;
                    string payload;
                    if (!ReadFrame(out opcode, out payload)) break;
                    if (opcode == 3) SendFrame(4, payload);
                    else if (opcode == 1 && payload.IndexOf("\"evt\":\"READY\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ready = true;
                        PublishCurrentSelection(true);
                        Raise("Rich Presence active", true);
                    }
                    else if (opcode == 1 && payload.IndexOf("\"evt\":\"ERROR\"", StringComparison.OrdinalIgnoreCase) >= 0)
                        Raise("Discord error: check the Application ID and Rich Presence image name", false);
                    else if (opcode == 2) break;
                }
            }
            catch (Exception ex)
            {
                if (!stopping) Raise("Connection error: " + ShortMessage(ex.Message), false);
            }
            finally
            {
                ready = false;
                StopRotation();
                ClosePipe();
            }

            if (!stopping)
            {
                Raise("Discord disconnected — reconnecting…", false);
                SleepInterruptibly(1800);
            }
        }
    }

    private NamedPipeClientStream FindDiscordPipe()
    {
        for (int i = 0; i < 10 && !stopping; i++)
        {
            NamedPipeClientStream candidate = new NamedPipeClientStream(".", "discord-ipc-" + i,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                candidate.Connect(180);
                if (candidate.IsConnected) return candidate;
            }
            catch { }
            candidate.Dispose();
        }
        return null;
    }

    public void SetStartupDelayEnabled(bool enabled)
    {
        bool wasActive;
        bool isActive;
        lock (stateLock)
        {
            wasActive = startupDelayEnabled && UnixNow() < sessionStarted + StartupDelaySeconds;
            startupDelayEnabled = enabled;
            isActive = startupDelayEnabled && UnixNow() < sessionStarted + StartupDelaySeconds;
        }
        if (!ready || stopping) return;

        try
        {
            if (isActive)
            {
                StopRotation();
                PublishCurrentSelection();
            }
            else if (wasActive)
            {
                StopRotation();
                PublishCurrentSelection();
            }
        }
        catch { }
    }

    public void SetJoinButtonEnabled(bool enabled)
    {
        string details;
        string state;
        lock (stateLock)
        {
            joinButtonEnabled = enabled;
            details = currentActivity;
            state = currentState;
        }
        if (!ready || stopping) return;
        try { SendActivity(details, state); }
        catch { }
    }

    public void SetJoinButtonSettings(string label, string url)
    {
        string details;
        string state;
        lock (stateLock)
        {
            joinButtonLabel = string.IsNullOrWhiteSpace(label) ? DefaultActivityButtonLabel : label.Trim();
            if (joinButtonLabel.Length > 32) joinButtonLabel = joinButtonLabel.Substring(0, 32);
            joinButtonUrl = string.IsNullOrWhiteSpace(url) ? DefaultActivityButtonUrl : url.Trim();
            if (joinButtonUrl.Length > 512) joinButtonUrl = joinButtonUrl.Substring(0, 512);
            details = currentActivity;
            state = currentState;
        }
        if (!ready || stopping) return;
        try { SendActivity(details, state); }
        catch { }
    }

    public void SetSecondButtonEnabled(bool enabled)
    {
        string details;
        string state;
        lock (stateLock)
        {
            secondButtonEnabled = enabled;
            details = currentActivity;
            state = currentState;
        }
        if (!ready || stopping) return;
        try { SendActivity(details, state); }
        catch { }
    }

    public void SetSecondButtonSettings(string label, string url)
    {
        string details;
        string state;
        lock (stateLock)
        {
            secondButtonLabel = string.IsNullOrWhiteSpace(label) ? DefaultSecondButtonLabel : label.Trim();
            if (secondButtonLabel.Length > 32) secondButtonLabel = secondButtonLabel.Substring(0, 32);
            secondButtonUrl = string.IsNullOrWhiteSpace(url) ? DefaultSecondButtonUrl : url.Trim();
            if (secondButtonUrl.Length > 512) secondButtonUrl = secondButtonUrl.Substring(0, 512);
            details = currentActivity;
            state = currentState;
        }
        if (!ready || stopping) return;
        try { SendActivity(details, state); }
        catch { }
    }

    public void SetRotationIntervalMinutes(int minutes)
    {
        lock (stateLock)
            rotationIntervalMinutes = Math.Max(0, Math.Min(120, minutes));
    }

    public void UseAutomaticSequence(string character)
    {
        string normalizedActor = NormalizeActor(character);
        string detailsToRefresh = null;
        string stateToRefresh = null;
        bool keepCurrentScene;
        lock (stateLock)
        {
            keepCurrentScene = automaticSequenceConfigured && selectedActivity == null &&
                string.Equals(actorFilter, normalizedActor, StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(currentActivity);
            if (keepCurrentScene)
            {
                detailsToRefresh = currentActivity;
                stateToRefresh = currentState;
            }
            else
            {
                selectedActivity = null;
                selectedState = null;
                actorFilter = normalizedActor;
                automaticSequenceConfigured = true;
                ResetAutomaticProgressLocked();
            }
        }
        if (keepCurrentScene)
        {
            if (!ready || stopping) return;
            try { SendActivity(detailsToRefresh, stateToRefresh); }
            catch { }
            return;
        }
        ApplySelectionNowOrQueue();
    }

    public void ResumeAutomaticSequence(string character, string activity, string state)
    {
        lock (stateLock)
        {
            selectedActivity = null;
            selectedState = null;
            actorFilter = NormalizeActor(character);
            automaticSequenceConfigured = true;
            ResetAutomaticProgressLocked();
            resumeActivity = string.IsNullOrWhiteSpace(activity) ? null : activity.Trim();
            resumeState = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        }
        ApplySelectionNowOrQueue();
    }

    public void AdvanceAutomaticScene(string character)
    {
        lock (stateLock)
        {
            selectedActivity = null;
            selectedState = null;
            actorFilter = NormalizeActor(character);
            automaticSequenceConfigured = true;
            resumeActivity = null;
            resumeState = null;
            automaticSequenceFresh = false;
        }
        ApplySelectionNowOrQueue();
    }

    public void UseActivity(string activity, string state)
    {
        if (string.IsNullOrWhiteSpace(activity)) return;
        string value = activity.Trim();
        if (value.Length > 128) value = value.Substring(0, 128);
        string stateValue = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        if (stateValue != null && stateValue.Length > 128) stateValue = stateValue.Substring(0, 128);
        lock (stateLock)
        {
            selectedActivity = value;
            selectedState = stateValue;
            automaticSequenceConfigured = false;
            resumeActivity = null;
            resumeState = null;
        }
        ApplySelectionNowOrQueue();
    }

    private void ApplySelectionNowOrQueue()
    {
        if (!ready || stopping) return;
        try
        {
            if (IsStartupDelayActive())
            {
                RaiseActivity("Starting the game\r\nSelection will apply after loading  •  {0}", sessionStarted + StartupDelaySeconds);
                return;
            }
            StopRotation();
            PublishCurrentSelection();
        }
        catch { }
    }

    private void PublishCurrentSelection()
    {
        PublishCurrentSelection(false);
    }

    private void PublishCurrentSelection(bool preserveCurrentAutomaticScene)
    {
        if (stopping || !ready || pipe == null || !pipe.IsConnected) return;

        if (IsStartupDelayActive())
        {
            SendActivity(null, null);
            long remainingSeconds = Math.Max(1, sessionStarted + StartupDelaySeconds - UnixNow());
            RaiseActivity("Starting the game\r\nNo description for {0}", sessionStarted + StartupDelaySeconds);
            ScheduleTimer((int)Math.Min(int.MaxValue, remainingSeconds * 1000));
            return;
        }

        string manual;
        string manualState;
        string existingAutomaticDetails;
        string existingAutomaticState;
        lock (stateLock)
        {
            manual = selectedActivity;
            manualState = selectedState;
            existingAutomaticDetails = preserveCurrentAutomaticScene && automaticSequenceConfigured ? currentActivity : null;
            existingAutomaticState = preserveCurrentAutomaticScene && automaticSequenceConfigured ? currentState : null;
        }
        if (manual != null)
        {
            SendActivity(manual, manualState);
            RaiseActivity((manualState ?? "Story Mode") + "\r\n" + manual + "  •  manual", 0);
            return;
        }

        if (!string.IsNullOrEmpty(existingAutomaticDetails))
        {
            PresenceScene existingScene = FindScene(existingAutomaticDetails);
            int reconnectDelay = rotationIntervalMinutes > 0
                ? rotationIntervalMinutes * 60
                : random.Next(existingScene.MinSeconds, existingScene.MaxSeconds + 1);
            SendActivity(existingAutomaticDetails, existingAutomaticState);
            RaiseActivity((existingAutomaticState ?? "Story Mode") + "\r\n" + existingAutomaticDetails + "  •  changes in {0}", UnixNow() + reconnectDelay);
            ScheduleTimer(reconnectDelay * 1000);
            return;
        }

        PresenceScene scene;
        int delaySeconds;
        lock (stateLock)
        {
            scene = GetNextSceneLocked();
            delaySeconds = rotationIntervalMinutes > 0
                ? rotationIntervalMinutes * 60
                : random.Next(scene.MinSeconds, scene.MaxSeconds + 1);
        }
        SendActivity(scene.Details, scene.State);
        RaiseActivity(scene.State + "\r\n" + scene.Details + "  •  changes in {0}", UnixNow() + delaySeconds);
        ScheduleTimer(delaySeconds * 1000);
    }

    private bool IsStartupDelayActive()
    {
        lock (stateLock)
            return startupDelayEnabled && UnixNow() < sessionStarted + StartupDelaySeconds;
    }

    private PresenceScene GetNextSceneLocked()
    {
        if (automaticSequenceFresh)
        {
            automaticSequenceFresh = false;
            PresenceScene resumed = GetResumedSceneLocked();
            if (resumed != null) return resumed;

            // A brand-new automatic session always starts in Story Mode. It
            // never jumps straight to a random character or to Main Menu.
            int openingIndex = FindNextMatchingSceneIndexLocked(0, true);
            if (openingIndex >= 0) return RecordSceneLocked(openingIndex, false);
        }

        if (automaticStepCount >= 6 && scenesSinceRepeat >= 3 && random.Next(100) < 20)
        {
            int repeatIndex = FindRepeatableVisitedSceneIndexLocked();
            if (repeatIndex >= 0) return RecordSceneLocked(repeatIndex, true);
        }

        for (int attempt = 0; attempt < RealisticScenes.Length; attempt++)
        {
            // The initial "Starting A New Session" card is used only once.
            // A full loop continues at the first playable scene instead.
            if (automaticStepCount > 0 && nextActivityIndex == 0) nextActivityIndex = 1;
            int candidateIndex = nextActivityIndex;
            PresenceScene candidate = RealisticScenes[candidateIndex];
            nextActivityIndex = (nextActivityIndex + 1) % RealisticScenes.Length;
            if (ActorMatches(candidate.Actor, actorFilter)) return RecordSceneLocked(candidateIndex, false);
        }
        return RealisticScenes[0];
    }

    private PresenceScene GetResumedSceneLocked()
    {
        string activity = resumeActivity;
        string state = resumeState;
        resumeActivity = null;
        resumeState = null;
        if (string.IsNullOrWhiteSpace(activity)) return null;

        for (int sceneIndex = 0; sceneIndex < RealisticScenes.Length; sceneIndex++)
        {
            PresenceScene scene = RealisticScenes[sceneIndex];
            if (!string.Equals(scene.Details, activity, StringComparison.Ordinal)) continue;
            if (!ActorMatches(scene.Actor, actorFilter)) continue;
            nextActivityIndex = (sceneIndex + 1) % RealisticScenes.Length;
            PresenceScene recorded = RecordSceneLocked(sceneIndex, false);
            return string.IsNullOrWhiteSpace(state) || string.Equals(state, scene.State, StringComparison.Ordinal)
                ? recorded
                : new PresenceScene(recorded.Details, state, recorded.Actor,
                    recorded.MinSeconds, recorded.MaxSeconds, recorded.Repeatable);
        }
        return null;
    }

    private int FindNextMatchingSceneIndexLocked(int startIndex, bool includeOpening)
    {
        for (int attempt = 0; attempt < RealisticScenes.Length; attempt++)
        {
            int index = (startIndex + attempt) % RealisticScenes.Length;
            if (!includeOpening && index == 0) continue;
            if (ActorMatches(RealisticScenes[index].Actor, actorFilter)) return index;
        }
        return -1;
    }

    private int FindRepeatableVisitedSceneIndexLocked()
    {
        int upperBound = Math.Min(highestVisitedIndex, RealisticScenes.Length - 1);
        if (upperBound < 1) return -1;
        int start = random.Next(1, upperBound + 1);
        for (int attempt = 0; attempt < upperBound; attempt++)
        {
            int index = 1 + ((start - 1 + attempt) % upperBound);
            PresenceScene scene = RealisticScenes[index];
            if (!scene.Repeatable || index == lastSceneIndex || index == previousSceneIndex) continue;
            if (ActorMatches(scene.Actor, actorFilter)) return index;
        }
        return -1;
    }

    private PresenceScene RecordSceneLocked(int sceneIndex, bool repeated)
    {
        previousSceneIndex = lastSceneIndex;
        lastSceneIndex = sceneIndex;
        highestVisitedIndex = Math.Max(highestVisitedIndex, sceneIndex);
        automaticStepCount++;
        scenesSinceRepeat = repeated ? 0 : scenesSinceRepeat + 1;
        return RealisticScenes[sceneIndex];
    }

    private void ResetAutomaticProgressLocked()
    {
        nextActivityIndex = 0;
        automaticSequenceFresh = true;
        resumeActivity = null;
        resumeState = null;
        automaticStepCount = 0;
        scenesSinceRepeat = 0;
        highestVisitedIndex = -1;
        lastSceneIndex = -1;
        previousSceneIndex = -1;
        currentActivity = null;
        currentState = null;
    }

    private static PresenceScene FindScene(string activity)
    {
        for (int i = 0; i < RealisticScenes.Length; i++)
            if (string.Equals(RealisticScenes[i].Details, activity, StringComparison.Ordinal)) return RealisticScenes[i];
        return RealisticScenes[0];
    }

    private static bool ActorMatches(string sceneActor, string filter)
    {
        if (filter == "Automatic" || sceneActor == "Any") return true;
        if (filter == "Jason Duval") return sceneActor == "Jason";
        if (filter == "Lucia Caminos") return sceneActor == "Lucia";
        if (filter == "Jason & Lucia") return sceneActor == "Both";
        return true;
    }

    private static string NormalizeActor(string character)
    {
        if (character == "Jason Duval" || character == "Lucia Caminos" || character == "Jason & Lucia")
            return character;
        return "Automatic";
    }

    private static string FormatSceneDelay(int seconds)
    {
        int minutes = Math.Max(1, (int)Math.Round(seconds / 60.0));
        return "~" + minutes + (minutes == 1 ? " min" : " min");
    }

    private void SendActivity(string details, string state)
    {
        bool showJoinButton;
        string buttonLabel;
        string buttonUrl;
        bool showSecondButton;
        string secondLabel;
        string secondUrl;
        lock (stateLock)
        {
            currentActivity = details;
            currentState = state;
            showJoinButton = joinButtonEnabled;
            buttonLabel = joinButtonLabel;
            buttonUrl = joinButtonUrl;
            showSecondButton = secondButtonEnabled;
            secondLabel = secondButtonLabel;
            secondUrl = secondButtonUrl;
        }
        // Discord renders details above state, so send the mode/character as
        // details and the current action as state for a natural hierarchy.
        string detailPart = state == null ? "" : ",\"details\":\"" + JsonEscape(state) + "\"";
        string statePart = details == null ? "" : ",\"state\":\"" + JsonEscape(details) + "\"";
        string buttonPart = "";
        if (showJoinButton || showSecondButton)
        {
            string buttons = "";
            if (showJoinButton)
                buttons = "{\"label\":\"" + JsonEscape(buttonLabel) + "\",\"url\":\"" + JsonEscape(buttonUrl) + "\"}";
            if (showSecondButton)
            {
                if (buttons.Length > 0) buttons += ",";
                buttons += "{\"label\":\"" + JsonEscape(secondLabel) + "\",\"url\":\"" + JsonEscape(secondUrl) + "\"}";
            }
            buttonPart = ",\"buttons\":[" + buttons + "]";
        }
        string largeImageKey = ResolveLargeImageKey();
        string json = "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" + Process.GetCurrentProcess().Id +
            ",\"activity\":{\"name\":\"Grand Theft Auto VI\",\"type\":0" + detailPart + statePart +
            ",\"timestamps\":{\"start\":" + sessionStarted + "}," +
            "\"assets\":{\"large_image\":\"" + JsonEscape(largeImageKey) + "\",\"large_text\":\"Grand Theft Auto VI\"}" +
            buttonPart + ",\"instance\":true}},\"nonce\":\"" + Guid.NewGuid().ToString("N") + "\"}";
        SendFrame(1, json);
    }

    private static string ResolveLargeImageKey()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-image.txt");
            if (File.Exists(path))
            {
                string value = File.ReadAllText(path).Trim();
                if (value.Length >= 2 && value.Length <= 256) return value;
            }
        }
        catch { }
        return DefaultLargeImageKey;
    }

    private void ScheduleTimer(int delay)
    {
        if (stopping || !ready) return;
        System.Threading.Timer oldTimer = Interlocked.Exchange(ref rotationTimer,
            new System.Threading.Timer(ActivityTimerElapsed, null, delay, Timeout.Infinite));
        if (oldTimer != null) oldTimer.Dispose();
    }

    private void ActivityTimerElapsed(object state)
    {
        System.Threading.Timer finishedTimer = Interlocked.Exchange(ref rotationTimer, null);
        if (finishedTimer != null) finishedTimer.Dispose();
        if (stopping || !ready) return;
        try { PublishCurrentSelection(); }
        catch { }
    }

    private void StopRotation()
    {
        System.Threading.Timer timer = Interlocked.Exchange(ref rotationTimer, null);
        if (timer != null) timer.Dispose();
    }

    private void SendFrame(int opcode, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        byte[] header = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes(opcode), 0, header, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, header, 4, 4);
        lock (writeLock)
        {
            pipe.Write(header, 0, header.Length);
            pipe.Write(body, 0, body.Length);
            pipe.Flush();
        }
    }

    private bool ReadFrame(out int opcode, out string payload)
    {
        opcode = 0;
        payload = "";
        byte[] header = new byte[8];
        if (!ReadExactly(header, 8)) return false;
        opcode = BitConverter.ToInt32(header, 0);
        int length = BitConverter.ToInt32(header, 4);
        if (length < 0 || length > 1024 * 1024) throw new InvalidDataException("Invalid Discord response");
        byte[] body = new byte[length];
        if (!ReadExactly(body, length)) return false;
        payload = Encoding.UTF8.GetString(body);
        return true;
    }

    private bool ReadExactly(byte[] buffer, int length)
    {
        int offset = 0;
        while (offset < length && !stopping)
        {
            int read = pipe.Read(buffer, offset, length - offset);
            if (read <= 0) return false;
            offset += read;
        }
        return offset == length;
    }

    private void Raise(string message, bool connected)
    {
        Action<string, bool> handler = StatusChanged;
        if (handler != null) handler(message, connected);
    }

    private void RaiseActivity(string message, long deadline)
    {
        Action<string, long> handler = ActivityChanged;
        if (handler != null) handler(message, deadline);
    }

    private static long UnixNow()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    private static string FormatDuration(long totalSeconds)
    {
        long minutes = totalSeconds / 60;
        long seconds = totalSeconds % 60;
        return minutes + ":" + seconds.ToString("00");
    }

    private static string JsonEscape(string value)
    {
        StringBuilder result = new StringBuilder(value.Length + 8);
        foreach (char character in value)
        {
            switch (character)
            {
                case '\"': result.Append("\\\""); break;
                case '\\': result.Append("\\\\"); break;
                case '\b': result.Append("\\b"); break;
                case '\f': result.Append("\\f"); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                default:
                    if (character < 32) result.Append("\\u").Append(((int)character).ToString("x4"));
                    else result.Append(character);
                    break;
            }
        }
        return result.ToString();
    }

    private void SleepInterruptibly(int milliseconds)
    {
        int waited = 0;
        while (!stopping && waited < milliseconds)
        {
            Thread.Sleep(100);
            waited += 100;
        }
    }

    private static string ShortMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return "unknown";
        return message.Length <= 70 ? message : message.Substring(0, 70) + "…";
    }

    private void ClosePipe()
    {
        try { if (pipe != null) pipe.Close(); } catch { }
        try { if (pipe != null) pipe.Dispose(); } catch { }
        pipe = null;
    }

    public void Stop()
    {
        if (ready)
        {
            try
            {
                string clear = "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" +
                    Process.GetCurrentProcess().Id + ",\"activity\":null},\"nonce\":\"" +
                    Guid.NewGuid().ToString("N") + "\"}";
                SendFrame(1, clear);
            }
            catch { }
        }
        stopping = true;
        ready = false;
        StopRotation();
        ClosePipe();
        if (worker != null && worker.IsAlive) worker.Join(800);
        worker = null;
    }

    public void Dispose() { Stop(); }
}
