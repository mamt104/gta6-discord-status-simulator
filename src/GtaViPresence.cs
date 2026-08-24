using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("GTA VI Discord Presence Community")]
[assembly: AssemblyDescription("Community Discord Rich Presence demo")]
[assembly: AssemblyCompany("Community Edition")]
[assembly: AssemblyProduct("GTA VI Discord Presence Community")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PresenceForm());
    }
}

internal sealed class PresenceForm : Form
{
    private const string DefaultAppId = "";
    private readonly TextBox appIdBox;
    private readonly Label connectionLabel;
    private readonly Button connectButton;
    private readonly CheckBox officialModeBox;
    private readonly CheckBox startupDelayBox;
    private readonly CheckBox joinGameBox;
    private readonly ComboBox sessionModeBox;
    private readonly ComboBox characterBox;
    private readonly ComboBox statusBox;
    private readonly ComboBox rotationIntervalBox;
    private readonly TextBox customStatusBox;
    private readonly Label currentStatusLabel;
    private readonly NotifyIcon trayIcon;
    private readonly DiscordRpcClient rpc;
    private readonly string configPath;
    private readonly string profilePath;
    private bool exiting;
    private bool loadingProfile;
    private bool officialModeEnabled = true;
    private string activeMode = "Auto";
    private string activeStatus = "";
    private string activeState = "";
    private string activeSessionMode = "Realistic Auto";
    private string activeCharacter = "Automatic";
    private int activeRotationMinutes;

    public PresenceForm()
    {
        Text = "GTA VI — Discord Presence Community";
        ClientSize = new Size(760, 760);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(10, 9, 15);
        ForeColor = Color.White;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-presence.txt");
        profilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta6-profile.ini");
        rpc = new DiscordRpcClient();
        rpc.StatusChanged += OnRpcStatusChanged;
        rpc.ActivityChanged += OnActivityChanged;

        Color pink = Color.FromArgb(255, 70, 156);
        Color orange = Color.FromArgb(255, 151, 62);
        Color muted = Color.FromArgb(184, 181, 196);
        Color card = Color.FromArgb(24, 22, 32);

        Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8, BackColor = pink });
        Panel connectionCard = new Panel { Location = new Point(24, 151), Size = new Size(712, 132), BackColor = card, BorderStyle = BorderStyle.FixedSingle };
        Panel sessionCard = new Panel { Location = new Point(24, 298), Size = new Size(712, 300), BackColor = card, BorderStyle = BorderStyle.FixedSingle };
        Panel presenceCard = new Panel { Location = new Point(24, 613), Size = new Size(712, 80), BackColor = card, BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(connectionCard);
        Controls.Add(sessionCard);
        Controls.Add(presenceCard);
        Controls.Add(new Panel { Location = new Point(24, 151), Size = new Size(5, 132), BackColor = pink });
        Controls.Add(new Panel { Location = new Point(24, 298), Size = new Size(5, 300), BackColor = orange });
        Controls.Add(new Panel { Location = new Point(24, 613), Size = new Size(5, 80), BackColor = Color.FromArgb(63, 214, 127) });
        Controls.Add(new Label
        {
            Location = new Point(38, 24), Size = new Size(680, 58),
            Text = "GTA  VI", Font = new Font("Arial Black", 31f, FontStyle.Bold),
            ForeColor = Color.White
        });
        Controls.Add(new Label
        {
            Location = new Point(43, 81), Size = new Size(650, 28),
            Text = "COMMUNITY EDITION  •  SESSION DIRECTOR", Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = orange
        });

        connectionLabel = new Label
        {
            Location = new Point(43, 115), Size = new Size(650, 28),
            Text = "●  Waiting for Discord detection", Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            ForeColor = muted
        };
        Controls.Add(connectionLabel);

        Controls.Add(new Label
        {
            Location = new Point(43, 161), Size = new Size(650, 22), Text = "DISCORD CONNECTION",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = muted
        });
        Controls.Add(new Label
        {
            Location = new Point(43, 187), Size = new Size(650, 19),
            Text = "APPLICATION ID", Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold),
            ForeColor = muted
        });

        appIdBox = new TextBox
        {
            Location = new Point(43, 210), Size = new Size(390, 31),
            Font = new Font("Consolas", 11.5f), BackColor = Color.FromArgb(34, 31, 44),
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(appIdBox);

        connectButton = MakeButton("START", new Point(448, 208), 120, pink);
        connectButton.Click += delegate { ConnectPresence(); };
        Controls.Add(connectButton);

        Button portalButton = MakeButton("PORTAL", new Point(583, 208), 120, Color.FromArgb(74, 79, 177));
        portalButton.Click += delegate { Process.Start("https://discord.com/developers/applications"); };
        Controls.Add(portalButton);

        officialModeBox = new CheckBox
        {
            Location = new Point(43, 250), Size = new Size(250, 24), Checked = true,
            Text = "Discord detection only",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(63, 214, 127), BackColor = card
        };
        officialModeBox.CheckedChanged += delegate
        {
            if (loadingProfile) return;
            officialModeEnabled = officialModeBox.Checked;
            ApplyPresenceMode();
            SaveProfile();
        };
        Controls.Add(officialModeBox);

        startupDelayBox = new CheckBox
        {
            Location = new Point(295, 250), Size = new Size(235, 24), Checked = true,
            Text = "No description for 5 minutes",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = card
        };
        startupDelayBox.CheckedChanged += delegate
        {
            rpc.SetStartupDelayEnabled(startupDelayBox.Checked);
            SaveProfile();
        };
        Controls.Add(startupDelayBox);

        joinGameBox = new CheckBox
        {
            Location = new Point(535, 250), Size = new Size(75, 24), Checked = true,
            Text = "Join",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = card
        };
        joinGameBox.CheckedChanged += delegate
        {
            rpc.SetJoinButtonEnabled(joinGameBox.Checked);
            SaveProfile();
        };
        Controls.Add(joinGameBox);

        Controls.Add(new Label
        {
            Location = new Point(43, 308), Size = new Size(650, 22), Text = "SESSION DIRECTOR",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), ForeColor = orange
        });
        Controls.Add(new Label
        {
            Location = new Point(43, 337), Size = new Size(315, 19), Text = "MODE",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        });
        Controls.Add(new Label
        {
            Location = new Point(390, 337), Size = new Size(315, 19), Text = "PLAYING AS",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        });

        sessionModeBox = MakeComboBox(new Point(43, 360), 315);
        sessionModeBox.Items.AddRange(new object[]
        {
            "Realistic Auto", "Free Roam", "Story Mission", "Main Menu", "Paused", "Manual"
        });
        sessionModeBox.SelectedIndex = 0;
        Controls.Add(sessionModeBox);

        characterBox = MakeComboBox(new Point(390, 360), 315);
        characterBox.Items.AddRange(new object[] { "Automatic", "Jason Duval", "Lucia Caminos", "Jason & Lucia" });
        characterBox.SelectedIndex = 0;
        Controls.Add(characterBox);

        Controls.Add(new Label
        {
            Location = new Point(43, 405), Size = new Size(650, 19), Text = "ACTIVITY / SCENE",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        });
        statusBox = MakeComboBox(new Point(43, 428), 390);
        statusBox.Items.Add("Suggested activity for this mode");
        foreach (string activity in DiscordRpcClient.GetActivities()) statusBox.Items.Add(activity);
        statusBox.SelectedIndex = 0;
        Controls.Add(statusBox);

        Button applyPreset = MakeButton("APPLY", new Point(448, 426), 120, pink);
        applyPreset.Click += delegate { ApplySessionProfile(); };
        Controls.Add(applyPreset);

        Button nextScene = MakeButton("NEXT", new Point(583, 426), 120, Color.FromArgb(62, 59, 74));
        nextScene.Click += delegate { AdvanceAutomaticScene(); };
        Controls.Add(nextScene);

        Controls.Add(new Label
        {
            Location = new Point(43, 475), Size = new Size(650, 19), Text = "CUSTOM DESCRIPTION",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        });
        customStatusBox = new TextBox
        {
            Location = new Point(43, 498), Size = new Size(390, 31), MaxLength = 128,
            Font = new Font("Segoe UI", 10.5f), BackColor = Color.FromArgb(34, 31, 44),
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        customStatusBox.KeyDown += delegate(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter) { ApplyCustomStatus(); args.SuppressKeyPress = true; }
        };
        Controls.Add(customStatusBox);

        Button applyCustom = MakeButton("USE CUSTOM", new Point(448, 496), 120, orange);
        applyCustom.Click += delegate { ApplyCustomStatus(); };
        Controls.Add(applyCustom);

        Controls.Add(new Label
        {
            Location = new Point(43, 542), Size = new Size(315, 19), Text = "AUTO ROTATION",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = muted
        });

        rotationIntervalBox = MakeComboBox(new Point(43, 562), 315);
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
        };
        Controls.Add(rotationIntervalBox);

        Controls.Add(new Label
        {
            Location = new Point(390, 542), Size = new Size(315, 42),
            Text = "Realistic mode uses different scene lengths. A fixed interval starts after the next change.",
            Font = new Font("Segoe UI", 8.7f), ForeColor = muted
        });

        Controls.Add(new Label
        {
            Location = new Point(43, 622), Size = new Size(650, 19), Text = "CURRENT PRESENCE",
            Font = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold), ForeColor = Color.FromArgb(63, 214, 127)
        });
        currentStatusLabel = new Label
        {
            Location = new Point(43, 645), Size = new Size(650, 42),
            Text = "Waiting for Discord", Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 190, 80)
        };
        Controls.Add(currentStatusLabel);

        Button minimize = MakeButton("MINIMIZE TO TRAY", new Point(43, 707), 185, pink);
        minimize.Click += delegate { HideToTray(); };
        Controls.Add(minimize);

        Button recentHelp = MakeButton("REGISTERED GAME", new Point(243, 707), 170, Color.FromArgb(74, 79, 177));
        recentHelp.Click += delegate { ShowRecentGamesHelp(); };
        Controls.Add(recentHelp);

        Button exit = MakeButton("EXIT", new Point(428, 707), 110, Color.FromArgb(62, 59, 74));
        exit.Click += delegate { exiting = true; Close(); };
        Controls.Add(exit);

        // Decorative panels are siblings of the controls. Keep them at the
        // back of the Z-order so Windows cannot cover menus or input fields.
        connectionCard.SendToBack();
        sessionCard.SendToBack();
        presenceCard.SendToBack();

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
            SaveProfile();
            rpc.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
        };

        LoadProfile();
        LoadSavedId();
    }

    private void LoadSavedId()
    {
        try
        {
            appIdBox.Text = DefaultAppId;
            if (File.Exists(configPath))
                appIdBox.Text = File.ReadAllText(configPath).Trim();
            ApplyPresenceMode();
        }
        catch { }
    }

    private void ApplyPresenceMode()
    {
        officialModeEnabled = officialModeBox.Checked;
        bool customEnabled = !officialModeEnabled;
        appIdBox.Enabled = customEnabled;
        connectButton.Enabled = customEnabled;
        startupDelayBox.Enabled = customEnabled;
        joinGameBox.Enabled = customEnabled;
        sessionModeBox.Enabled = customEnabled;
        characterBox.Enabled = customEnabled;
        statusBox.Enabled = customEnabled;
        customStatusBox.Enabled = customEnabled;
        rotationIntervalBox.Enabled = customEnabled;

        if (officialModeEnabled)
        {
            rpc.Stop();
            connectionLabel.Text = "●  Discord detection mode active";
            connectionLabel.ForeColor = Color.FromArgb(63, 214, 127);
            currentStatusLabel.Text = "Discord controls the detected game profile and icon";
            currentStatusLabel.ForeColor = Color.FromArgb(63, 214, 127);
        }
        else if (IsValidAppId(appIdBox.Text.Trim()))
        {
            ConnectPresence();
        }
        else
        {
            connectionLabel.Text = "●  Enter an Application ID for custom mode";
            connectionLabel.ForeColor = Color.FromArgb(255, 190, 80);
        }
    }

    private void LoadProfile()
    {
        loadingProfile = true;
        try
        {
            bool startupDelay = true;
            bool joinButton = true;
            int rotationMinutes = 0;
            string mode = "Auto";
            string status = "";
            string state = "";
            string sessionMode = "Realistic Auto";
            string character = "Automatic";

            if (File.Exists(profilePath))
            {
                foreach (string line in File.ReadAllLines(profilePath))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    bool parsed;
                    if (key == "StartupDelay" && bool.TryParse(value, out parsed)) startupDelay = parsed;
                    else if (key == "JoinButton" && bool.TryParse(value, out parsed)) joinButton = parsed;
                    else if (key == "RotationMinutes" && int.TryParse(value, out rotationMinutes))
                        rotationMinutes = Math.Max(0, Math.Min(120, rotationMinutes));
                    else if (key == "Mode") mode = value;
                    else if (key == "SessionMode") sessionMode = DecodeProfileValue(value, "Realistic Auto");
                    else if (key == "Character") character = DecodeProfileValue(value, "Automatic");
                    else if (key == "StatusBase64" && value.Length > 0)
                        status = DecodeProfileValue(value, "");
                    else if (key == "StateBase64" && value.Length > 0)
                        state = DecodeProfileValue(value, "");
                }
            }

            startupDelayBox.Checked = startupDelay;
            joinGameBox.Checked = joinButton;
            // Always start in pure Discord detection mode. Users may switch to
            // custom Rich Presence manually for the current session.
            officialModeBox.Checked = true;
            officialModeEnabled = true;
            activeMode = mode;
            activeStatus = status;
            activeState = state;
            activeSessionMode = sessionMode;
            activeCharacter = character;
            activeRotationMinutes = rotationMinutes;
            SelectComboValue(sessionModeBox, sessionMode, 0);
            SelectComboValue(characterBox, character, 0);
            SelectRotationValue(rotationMinutes);
            rpc.SetRotationIntervalMinutes(rotationMinutes);

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
                activeStatus = "";
                activeState = "";
                sessionModeBox.SelectedIndex = 0;
                statusBox.SelectedIndex = 0;
                rpc.UseAutomaticSequence(characterBox.SelectedItem.ToString());
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
            string encodedSessionMode = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeSessionMode ?? "Realistic Auto"));
            string encodedCharacter = Convert.ToBase64String(Encoding.UTF8.GetBytes(activeCharacter ?? "Automatic"));
            File.WriteAllLines(profilePath, new[]
            {
                "Version=3",
                "StartupDelay=" + startupDelayBox.Checked,
                "JoinButton=" + joinGameBox.Checked,
                "RotationMinutes=" + activeRotationMinutes,
                "Mode=" + activeMode,
                "SessionMode=" + encodedSessionMode,
                "Character=" + encodedCharacter,
                "StatusBase64=" + encodedStatus,
                "StateBase64=" + encodedState
            });
        }
        catch { }
    }

    private void ConnectPresence()
    {
        if (officialModeEnabled)
        {
            ApplyPresenceMode();
            return;
        }
        string id = appIdBox.Text.Trim();
        if (!IsValidAppId(id))
        {
            MessageBox.Show("The Application ID must contain digits only and be at least 15 digits long.",
                "Invalid Application ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try { File.WriteAllText(configPath, id); }
        catch (Exception ex)
        {
            MessageBox.Show("The Application ID could not be saved next to the executable:\r\n" + ex.Message,
                "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        connectButton.Enabled = false;
        appIdBox.Enabled = false;
        connectionLabel.Text = "●  Connecting to Discord…";
        connectionLabel.ForeColor = Color.FromArgb(255, 190, 80);
        rpc.Start(id);
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
                connectionLabel.Text = "●  " + message;
                connectionLabel.ForeColor = connected ? Color.FromArgb(63, 214, 127) : Color.FromArgb(255, 190, 80);
                if (!connected && message.StartsWith("Error"))
                {
                    connectButton.Enabled = true;
                    appIdBox.Enabled = true;
                }
            });
        }
        catch { }
    }

    private void OnActivityChanged(string message)
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke((MethodInvoker)delegate
            {
                if (officialModeEnabled) return;
                currentStatusLabel.Text = message;
                currentStatusLabel.ForeColor = message.StartsWith("Starting")
                    ? Color.FromArgb(255, 190, 80) : Color.FromArgb(63, 214, 127);
            });
        }
        catch { }
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
    }

    private void ApplySessionProfile()
    {
        if (officialModeEnabled) { ShowCustomModeRequired(); return; }
        string sessionMode = sessionModeBox.SelectedItem.ToString();
        string character = characterBox.SelectedItem.ToString();
        activeSessionMode = sessionMode;
        activeCharacter = character;

        if (sessionMode == "Realistic Auto")
        {
            activeMode = "Auto";
            activeStatus = "";
            activeState = "";
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
    }

    private void AdvanceAutomaticScene()
    {
        if (officialModeEnabled) { ShowCustomModeRequired(); return; }
        sessionModeBox.SelectedIndex = 0;
        activeMode = "Auto";
        activeStatus = "";
        activeState = "";
        activeSessionMode = "Realistic Auto";
        activeCharacter = characterBox.SelectedItem.ToString();
        rpc.AdvanceAutomaticScene(activeCharacter);
        SaveProfile();
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
        MessageBox.Show(
            "First launch this GTA6.exe, then open Discord > User Settings > Registered Games " +
            "and add the running executable. Keep ‘Discord detection only’ enabled for Discord's " +
            "detected game card.\r\n\r\nCustom mode sends your Application ID and enables descriptions, " +
            "rotation, and Join, but it replaces the detected game card.",
            "Registered Game setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        Button button = new Button
        {
            Text = text, Location = location, Size = new Size(width, 40), BackColor = background,
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static ComboBox MakeComboBox(Point location, int width)
    {
        ComboBox combo = new ComboBox
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

internal sealed class DiscordRpcClient : IDisposable
{
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

        public PresenceScene(string details, string state, string actor, int minSeconds, int maxSeconds)
        {
            Details = details;
            State = state;
            Actor = actor;
            MinSeconds = minSeconds;
            MaxSeconds = maxSeconds;
        }
    }

    private static readonly PresenceScene[] RealisticScenes =
    {
        new PresenceScene("Main Menu", "Story Mode", "Any", 60, 120),
        new PresenceScene("Exploring The Leonida Keys", "Free Roam • Jason Duval", "Jason", 480, 900),
        new PresenceScene("Working For Local Runners", "Story Mode • Jason Duval", "Jason", 360, 720),
        new PresenceScene("Meeting At The Boatyard", "With Brian Heder • Jason", "Jason", 300, 600),
        new PresenceScene("Snooping On Coast Guard Comms", "With Cal Hampton • Jason", "Jason", 300, 600),
        new PresenceScene("Cruising Through Vice City", "Free Roam • Lucia Caminos", "Lucia", 480, 900),
        new PresenceScene("Changing The Look In Vice City", "Story Mode • Lucia Caminos", "Lucia", 240, 480),
        new PresenceScene("Planning The Next Score", "Jason & Lucia", "Both", 360, 720),
        new PresenceScene("Meeting Raul Bautista", "Preparing A Score • Jason & Lucia", "Both", 300, 600),
        new PresenceScene("Pulling Off An Easy Score", "Story Mission • Jason & Lucia", "Both", 720, 1500),
        new PresenceScene("Escaping A Botched Robbery", "Wanted • Jason & Lucia", "Both", 360, 720),
        new PresenceScene("Laying Low In Port Gellhorn", "Story Mode • Lucia Caminos", "Lucia", 480, 900),
        new PresenceScene("Hitting The Vice City Clubs", "Nightlife • Lucia Caminos", "Lucia", 360, 720),
        new PresenceScene("Talking Business With Boobie Ike", "Jack Of Hearts • Vice City", "Any", 300, 600),
        new PresenceScene("Recording At Only Raw Records", "With Dre'Quan Priest", "Any", 300, 600),
        new PresenceScene("Chasing A Viral Hit", "With Real Dimez", "Lucia", 300, 600),
        new PresenceScene("Crossing The Grassrivers", "Free Roam • Jason Duval", "Jason", 480, 900),
        new PresenceScene("Moving Product Through The Keys", "Working For Brian Heder • Jason", "Jason", 480, 900),
        new PresenceScene("Riding Through Mount Kalaga", "Free Roam • Lucia Caminos", "Lucia", 480, 900),
        new PresenceScene("Watching The Neon Glow", "Vice City • Night", "Both", 480, 900),
        new PresenceScene("Living Another Day In Paradise", "Story Mode • Jason & Lucia", "Both", 480, 900)
    };

    public event Action<string, bool> StatusChanged;
    public event Action<string> ActivityChanged;
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
    private bool startupDelayEnabled = true;
    private bool joinButtonEnabled = true;
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
        lock (stateLock)
        {
            nextActivityIndex = 0;
            currentActivity = null;
            currentState = null;
        }
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
                        PublishCurrentSelection();
                        Raise("Rich Presence active", true);
                    }
                    else if (opcode == 1 && payload.IndexOf("\"evt\":\"ERROR\"", StringComparison.OrdinalIgnoreCase) >= 0)
                        Raise("Discord error: check the Application ID and image asset key", false);
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

    public void SetRotationIntervalMinutes(int minutes)
    {
        lock (stateLock)
            rotationIntervalMinutes = Math.Max(0, Math.Min(120, minutes));
    }

    public void UseAutomaticSequence(string character)
    {
        lock (stateLock)
        {
            selectedActivity = null;
            selectedState = null;
            actorFilter = NormalizeActor(character);
            nextActivityIndex = 0;
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
                RaiseActivity("Starting the game\r\nSelection will apply after loading");
                return;
            }
            StopRotation();
            PublishCurrentSelection();
        }
        catch { }
    }

    private void PublishCurrentSelection()
    {
        if (stopping || !ready || pipe == null || !pipe.IsConnected) return;

        if (IsStartupDelayActive())
        {
            SendActivity(null, null);
            long remainingSeconds = Math.Max(1, sessionStarted + StartupDelaySeconds - UnixNow());
            RaiseActivity("Starting the game\r\nNo description for " + FormatDuration(remainingSeconds));
            ScheduleTimer((int)Math.Min(int.MaxValue, remainingSeconds * 1000));
            return;
        }

        string manual;
        string manualState;
        lock (stateLock)
        {
            manual = selectedActivity;
            manualState = selectedState;
        }
        if (manual != null)
        {
            SendActivity(manual, manualState);
            RaiseActivity((manualState ?? "Story Mode") + "\r\n" + manual + "  •  manual");
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
        RaiseActivity(scene.State + "\r\n" + scene.Details + "  •  changes in " + FormatSceneDelay(delaySeconds));
        ScheduleTimer(delaySeconds * 1000);
    }

    private bool IsStartupDelayActive()
    {
        lock (stateLock)
            return startupDelayEnabled && UnixNow() < sessionStarted + StartupDelaySeconds;
    }

    private PresenceScene GetNextSceneLocked()
    {
        for (int attempt = 0; attempt < RealisticScenes.Length; attempt++)
        {
            PresenceScene candidate = RealisticScenes[nextActivityIndex];
            nextActivityIndex = (nextActivityIndex + 1) % RealisticScenes.Length;
            if (ActorMatches(candidate.Actor, actorFilter)) return candidate;
        }
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
        lock (stateLock)
        {
            currentActivity = details;
            currentState = state;
            showJoinButton = joinButtonEnabled;
        }
        // Discord renders details above state, so send the mode/character as
        // details and the current action as state for a natural hierarchy.
        string detailPart = state == null ? "" : ",\"details\":\"" + JsonEscape(state) + "\"";
        string statePart = details == null ? "" : ",\"state\":\"" + JsonEscape(details) + "\"";
        string buttonPart = "";
        if (showJoinButton)
        {
            string buttons = "{\"label\":\"Join\",\"url\":\"https://discord.gg/rockstargames\"}";
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

    private void RaiseActivity(string message)
    {
        Action<string> handler = ActivityChanged;
        if (handler != null) handler(message);
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
