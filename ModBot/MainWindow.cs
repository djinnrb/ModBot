﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace ModBot
{
    public partial class MainWindow : CustomForm
    {
        private iniUtil ini = Program.ini;
        public Dictionary<string, Dictionary<string, string>> dSettings = new Dictionary<string, Dictionary<string, string>>();
        private bool bIgnoreUpdates = false;
        public int iSettingsPresent = -2;
        //private bool g_bLoaded = false;
        public Dictionary<CheckBox, Panel> Windows = new Dictionary<CheckBox, Panel>();
        public Panel CurrentWindow = null;
        private List<Thread> Threads = new List<Thread>();

        public MainWindow()
        {
            InitializeComponent();
            Text = "ModBot v" + (VersionLabel.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Replace("." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision.ToString(), "");

            CenterSpacer(ConnectionLabel, ConnectionSpacer);
            CenterSpacer(CurrencyLabel, CurrencySpacer);
            CenterSpacer(SubscribersLabel, SubscribersSpacer);
            CenterSpacer(DonationsLabel, DonationsSpacer);
            CenterSpacer(HandoutLabel, HandoutSpacer);
            CenterSpacer(GiveawayTypeLabel, GiveawayTypeSpacer);
            CenterSpacer(GiveawayRulesLabel, GiveawayRulesSpacer, false, true);
            CenterSpacer(GiveawayBansLabel, GiveawayBansSpacer);

            Panel panel = new Panel();
            panel.Size = new Size(1, 1);
            panel.Location = new Point(GiveawayTypeSpacer.Location.X + GiveawayTypeSpacer.Size.Width - 1, GiveawayTypeSpacer.Location.Y + 9);
            GiveawayTypeSpacer.Parent.Controls.Add(panel);
            panel.BringToFront();

            Windows.Add(SettingsWindowButton, SettingsWindow);
            Windows.Add(ChannelWindowButton, ChannelWindow);
            Windows.Add(CurrencyWindowButton, CurrencyWindow);
            Windows.Add(GiveawayWindowButton, GiveawayWindow);
            Windows.Add(DonationsWindowButton, DonationsWindow);
            Windows.Add(AboutWindowButton, AboutWindow);

            int y = -((Height - 38) / Windows.Keys.Count * Windows.Keys.Count - Height + 38);
            int y2 = y;
            foreach (CheckBox btn in Windows.Keys)
            {
                btn.Size = new Size(100, (Height - 38) / Windows.Keys.Count);
            }
            while(y > 0)
            {
                foreach (CheckBox btn in Windows.Keys)
                {
                    if (y == 0) break;
                    btn.Size = new Size(btn.Size.Width, btn.Size.Height + 1);
                    y--;
                }
            }
            y = 30;
            foreach(CheckBox btn in Windows.Keys)
            {
                btn.Location = new Point(8, y);
                y += btn.Size.Height;
            }

            CurrentWindow = SettingsWindow;
            SettingsWindow.BringToFront();

            ini.SetValue("Settings", "BOT_Name", BotNameBox.Text = ini.GetValue("Settings", "BOT_Name", "ModBot"));
            ini.SetValue("Settings", "BOT_Password", BotPasswordBox.Text = ini.GetValue("Settings", "BOT_Password", ""));
            ini.SetValue("Settings", "Channel_Name", ChannelBox.Text = ini.GetValue("Settings", "Channel_Name", "ModChannel"));
            ini.SetValue("Settings", "Currency_Name", CurrencyNameBox.Text = ini.GetValue("Settings", "Currency_Name", "Mod Coins"));
            ini.SetValue("Settings", "Currency_Command", CurrencyCommandBox.Text = ini.GetValue("Settings", "Currency_Command", "ModCoins"));
            int interval = Convert.ToInt32(ini.GetValue("Settings", "Currency_Interval", "5"));
            if (interval > CurrencyHandoutInterval.Maximum || interval < CurrencyHandoutInterval.Minimum)
            {
                interval = 5;
            }
            ini.SetValue("Settings", "Currency_Interval", (CurrencyHandoutInterval.Value = interval).ToString());
            int payout = Convert.ToInt32(ini.GetValue("Settings", "Currency_Payout", "1"));
            if (payout > CurrencyHandoutAmount.Maximum || payout < CurrencyHandoutAmount.Minimum)
            {
                payout = 1;
            }
            ini.SetValue("Settings", "Currency_Payout", (CurrencyHandoutAmount.Value = payout).ToString());
            ini.SetValue("Settings", "Subsribers_URL", SubLinkBox.Text = ini.GetValue("Settings", "Subsribers_URL", ""));
            ini.SetValue("Settings", "Donations_Key", DonationsKeyBox.Text = ini.GetValue("Settings", "Donations_Key", ""));

            ini.SetValue("Settings", "Donations_UpdateTop", (UpdateTopDonorsCheckBox.Checked = (ini.GetValue("Settings", "Donations_UpdateTop", "0") == "1")) ? "1" : "0");
            ini.SetValue("Settings", "Donations_Top_Limit", (TopDonorsLimit.Value = Convert.ToInt32(ini.GetValue("Settings", "Donations_Top_Limit", "20"))).ToString());
            ini.SetValue("Settings", "Donations_UpdateRecent", (UpdateRecentDonorsCheckBox.Checked = (ini.GetValue("Settings", "Donations_UpdateRecent", "0") == "1")) ? "1" : "0");
            ini.SetValue("Settings", "Donations_Recent_Limit", (RecentDonorsLimit.Value = Convert.ToInt32(ini.GetValue("Settings", "Donations_Recent_Limit", "5"))).ToString());
            ini.SetValue("Settings", "Donations_UpdateLast", (UpdateLastDonorCheckBox.Checked = (ini.GetValue("Settings", "Donations_UpdateLast", "0") == "1")) ? "1" : "0");

            ini.SetValue("Settings", "Currency_LockCmd", ini.GetValue("Settings", "Currency_LockCmd", "0"));

            string sCurrencyHandout = ini.GetValue("Settings", "Currency_Handout", "0");
            ini.SetValue("Settings", "Currency_Handout", sCurrencyHandout);
            if (sCurrencyHandout.Equals("0"))
            {
                Currency_HandoutEveryone.Checked = true;
            }
            else if (sCurrencyHandout.Equals("1"))
            {
                Currency_HandoutActiveStream.Checked = true;
            }
            else if (sCurrencyHandout.Equals("2"))
            {
                Currency_HandoutActiveTime.Checked = true;
            }
            ini.SetValue("Settings", "Currency_HandoutTime", (Currency_HandoutLastActive.Value = Convert.ToInt32(ini.GetValue("Settings", "Currency_HandoutTime", "5"))).ToString());

            //string[] lines = File.ReadAllLines("modbot.txt");
            //Dictionary<string, string> dict = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
            //iniUtil ini = new iniUtil(@"C:\program files (x86)\myapp\myapp.ini");

            /*Type colorType = typeof(System.Drawing.Color);
            // We take only static property to avoid properties like Name, IsSystemColor ...
            PropertyInfo[] propInfos = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (PropertyInfo propInfo in propInfos)
            {
                Console.WriteLine(propInfo.Name);
            }*/

            //GetSettings();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            //Settings loading
            /*Dictionary<Control, bool> dState = new Dictionary<Control, bool>();
            Thread tLoad = new Thread(() =>
            {
                while (!g_bLoaded)
                {
                    foreach (Control ctrl in Controls)
                    {
                        if (!BaseControls.Contains(ctrl) && !dState.ContainsKey(ctrl))
                        {
                            dState.Add(ctrl, ctrl.Enabled);
                            BeginInvoke((MethodInvoker)delegate
                            {
                                ctrl.Enabled = false;
                            });
                        }
                    }
                    Thread.Sleep(100);
                }

                BeginInvoke((MethodInvoker)delegate
                {
                    foreach (Control ctrl in dState.Keys)
                    {
                        if (Controls.Contains(ctrl))
                        {
                            ctrl.Enabled = dState[ctrl];
                        }
                    }
                    GetSettings();
                });
            });
            tLoad.Start();*/

            //Update checking
            Thread thread = new Thread(() =>
            {
                try
                {
                    bool bUpdateNote = false, bNote = false;
                    while (!bUpdateNote)
                    {
                        bNote = false;
                        if (IsActivated)
                        {
                            bUpdateNote = bNote = true;
                        }
                        Program.Updates.CheckUpdate(true, bNote);
                        Thread.Sleep(60000);
                    }
                }
                catch(Exception)
                {
                }
            });
            Threads.Add(thread);
            thread.Name = "Update checking";
            thread.Start();
        }

        private void CenterSpacer(Label label, GroupBox spacer, bool hideleft = false, bool hideright = false)
        {
            label.Location = new Point(spacer.Location.X + spacer.Size.Width / 2 - label.Size.Width / 2, spacer.Location.Y);
            if (hideleft)
            {
                Panel panel = new Panel();
                panel.Size = new Size(1, 2);
                panel.Location = new Point(spacer.Location.X, spacer.Location.Y + 9);
                spacer.Parent.Controls.Add(panel);
                panel.BringToFront();
            }
            if (hideright)
            {
                Panel panel = new Panel();
                panel.Size = new Size(1, 2);
                panel.Location = new Point(spacer.Location.X + spacer.Size.Width - 1, spacer.Location.Y + 9);
                spacer.Parent.Controls.Add(panel);
                panel.BringToFront();
            }
        }

        public void GetSettings()
        {
            /*if (!File.Exists("ModBot.ini"))
            {
                File.WriteAllText("ModBot.ini", "\r\n[Default]");
            }*/

            if (!bIgnoreUpdates)
            {
                ////SettingsPresents.TabPages.Clear();
                //Console.WriteLine("Getting Settings");
                bIgnoreUpdates = true;
                Dictionary<Control, bool> dState = new Dictionary<Control, bool>();
                foreach (Control ctrl in GiveawayWindow.Controls)
                {
                    if (!dState.ContainsKey(ctrl))
                    {
                        dState.Add(ctrl, ctrl.Enabled);
                        ctrl.Enabled = false;
                    }
                }
                bool bRecreateSections = false;
                foreach (string section in ini.GetSectionNames())
                {
                    if(section != "Settings" && !dSettings.ContainsKey(section))
                    {
                        bRecreateSections = true;
                        SettingsPresents.TabPages.Clear();
                        dSettings.Clear();
                        break;
                    }
                }
                foreach (string section in ini.GetSectionNames())
                {
                    if (section != "")
                    {
                        //Console.WriteLine(section);
                        if (!section.Equals("Settings"))
                        {
                            Dictionary<string, string> dSectionSettings = new Dictionary<string, string>();
                            string sGiveawayType = ini.GetValue(section, "Giveaway_Type", "0");
                            ini.SetValue(section, "Giveaway_Type", sGiveawayType);
                            dSectionSettings.Add("Giveaway_Type", sGiveawayType);
                            string sMinCurrencyChecked = ini.GetValue(section, "Giveaway_MinCurrencyChecked", "0");
                            ini.SetValue(section, "Giveaway_MinCurrencyChecked", sMinCurrencyChecked);
                            dSectionSettings.Add("Giveaway_MinCurrencyChecked", sMinCurrencyChecked);
                            string sMustFollow = ini.GetValue(section, "Giveaway_MustFollow", "0");
                            ini.SetValue(section, "Giveaway_MustFollow", sMustFollow);
                            dSectionSettings.Add("Giveaway_MustFollow", sMustFollow);
                            string sMinCurrency = ini.GetValue(section, "Giveaway_MinCurrency", "1");
                            ini.SetValue(section, "Giveaway_MinCurrency", sMinCurrency);
                            dSectionSettings.Add("Giveaway_MinCurrency", sMinCurrency);
                            string sActiveUserTime = ini.GetValue(section, "Giveaway_ActiveUserTime", "5");
                            ini.SetValue(section, "Giveaway_ActiveUserTime", sActiveUserTime);
                            dSectionSettings.Add("Giveaway_ActiveUserTime", sActiveUserTime);
                            string sAutoBanWinnerChecked = ini.GetValue(section, "Giveaway_AutoBanWinner", "0");
                            ini.SetValue(section, "Giveaway_AutoBanWinner", sAutoBanWinnerChecked);
                            dSectionSettings.Add("Giveaway_AutoBanWinner", sAutoBanWinnerChecked);
                            string sBanList = ini.GetValue(section, "Giveaway_BanList", "");
                            ini.SetValue(section, "Giveaway_BanList", sBanList);
                            dSectionSettings.Add("Giveaway_BanList", sBanList);

                            /*foreach (KeyValuePair<string, string> kv in dSectionSettings)
                            {
                                Console.WriteLine(kv.Key + " = " + kv.Value);
                            }*/

                            if (bRecreateSections)
                            {
                                dSettings.Add(section, dSectionSettings);

                                SettingsPresents.TabPages.Add(section);
                            }
                            else
                            {
                                if (dSettings.ContainsKey(section))
                                {
                                    dSettings[section] = dSectionSettings;
                                }
                                else
                                {
                                    dSettings.Add(section, dSectionSettings);
                                }
                            }
                        }
                    }
                }

                foreach (Control ctrl in dState.Keys)
                {
                    if (GiveawayWindow.Controls.Contains(ctrl))
                    {
                        ctrl.Enabled = dState[ctrl];
                    }
                }

                string sSelectedPresent = ini.GetValue("Settings", "SelectedPresent", "Default");
                if (sSelectedPresent != "")
                {
                    for (int i = 0; i < SettingsPresents.TabPages.Count; i++)
                    {
                        if (SettingsPresents.TabPages[i].Text.Equals(sSelectedPresent))
                        {
                            iSettingsPresent = SettingsPresents.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (Giveaway_BanListListBox.Items.Count > 0)
                {
                    Giveaway_BanListListBox.Items.Clear();
                }

                if (SettingsPresents.SelectedIndex > -1)
                {
                    if (dSettings.ContainsKey(SettingsPresents.TabPages[SettingsPresents.SelectedIndex].Text))
                    {
                        foreach (KeyValuePair<string, string> KeyValue in dSettings[SettingsPresents.TabPages[SettingsPresents.SelectedIndex].Text])
                        {
                            if (KeyValue.Key != "")
                            {
                                if (KeyValue.Key.Equals("Giveaway_Type"))
                                {
                                    Giveaway_TypeActive.Checked = (KeyValue.Value == "0");
                                    Giveaway_TypeKeyword.Checked = (KeyValue.Value == "1");
                                    Giveaway_TypeTickets.Checked = (KeyValue.Value == "2");
                                }
                                else if (KeyValue.Key.Equals("Giveaway_MinCurrencyChecked"))
                                {
                                    Giveaway_MinCurrencyCheckBox.Checked = (KeyValue.Value == "1");
                                }
                                else if (KeyValue.Key.Equals("Giveaway_MustFollow"))
                                {
                                    Giveaway_MustFollowCheckBox.Checked = (KeyValue.Value == "1");
                                }
                                else if (KeyValue.Key.Equals("Giveaway_MinCurrency"))
                                {
                                    Giveaway_MinCurrency.Value = Convert.ToInt32(KeyValue.Value);
                                }
                                else if (KeyValue.Key.Equals("Giveaway_ActiveUserTime"))
                                {
                                    Giveaway_ActiveUserTime.Value = Convert.ToInt32(KeyValue.Value);
                                }
                                else if (KeyValue.Key.Equals("Giveaway_AutoBanWinner"))
                                {
                                    Giveaway_AutoBanWinnerCheckBox.Checked = (KeyValue.Value == "1");
                                }
                                else if (KeyValue.Key.Equals("Giveaway_BanList"))
                                {
                                    string[] bans = KeyValue.Value.Split(';');
                                    foreach (string ban in bans)
                                    {
                                        //Console.WriteLine(ban);
                                        if (!ban.Equals("") && !Giveaway_BanListListBox.Items.Contains(Api.capName(ban)))
                                        {
                                            Giveaway_BanListListBox.Items.Add(Api.capName(ban));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (SettingsPresents.TabPages.Count == 0)
                {
                    SettingsPresents.TabPages.Add("Default");
                    iSettingsPresent = 0;
                    Dictionary<string, string> dSectionSettings = new Dictionary<string, string>();
                    string sMinCurrencyChecked = ini.GetValue("Default", "Giveaway_MinCurrencyChecked", "0");
                    ini.SetValue("Default", "Giveaway_MinCurrencyChecked", sMinCurrencyChecked);
                    dSectionSettings.Add("Giveaway_MinCurrencyChecked", sMinCurrencyChecked);
                    string sMustFollow = ini.GetValue("Default", "Giveaway_MustFollow", "0");
                    ini.SetValue("Default", "Giveaway_MustFollow", sMustFollow);
                    dSectionSettings.Add("Giveaway_MustFollow", sMustFollow);
                    string sMinCurrency = ini.GetValue("Default", "Giveaway_MinCurrency", "1");
                    ini.SetValue("Default", "Giveaway_MinCurrency", sMinCurrency);
                    dSectionSettings.Add("Giveaway_MinCurrency", sMinCurrency);
                    string sActiveUserTime = ini.GetValue("Default", "Giveaway_ActiveUserTime", "5");
                    ini.SetValue("Default", "Giveaway_ActiveUserTime", sActiveUserTime);
                    dSectionSettings.Add("Giveaway_ActiveUserTime", sActiveUserTime);
                    string sAutoBanWinnerChecked = ini.GetValue("Default", "Giveaway_AutoBanWinner", "0");
                    ini.SetValue("Default", "Giveaway_AutoBanWinner", sAutoBanWinnerChecked);
                    dSectionSettings.Add("Giveaway_AutoBanWinner", sAutoBanWinnerChecked);
                    string sBanList = ini.GetValue("Default", "Giveaway_BanList", "");
                    ini.SetValue("Default", "Giveaway_BanList", sBanList);
                    dSectionSettings.Add("Giveaway_BanList", sBanList);

                    dSettings.Add("Default", dSectionSettings);

                    SaveSettings();
                }
                bIgnoreUpdates = false;
            }
        }

        private void SetDonationsList(List<Transaction> transactions, string[] sRecentIgnores, string[] sLatestIgnores, string[] sTopIgnores)
        {
            if (IsHandleCreated && !DonationsWindowButton.Checked)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    Donations_List.Rows.Clear();
                    for (int i = 0; i < transactions.Count; i++)
                    {
                        Donations_List.Rows.Add(transactions[i].date, transactions[i].donor, transactions[i].amount, transactions[i].id, transactions[i].notes, !sRecentIgnores.Contains(transactions[i].id), !sLatestIgnores.Contains(transactions[i].id), !sTopIgnores.Contains(transactions[i].id), true);
                    }
                });
            }
        }

        public void GrabData()
        {
            while (true)
            {
                List<Transaction> transactions = Api.UpdateTransactions().OrderByDescending(key => Convert.ToDateTime(key.date)).ToList();
                if (transactions.Count > 0)
                {
                    string sDonationsIgnoreRecent = ini.GetValue("Settings", "Donations_Ignore_Recent", "");
                    ini.SetValue("Settings", "Donations_Ignore_Recent", sDonationsIgnoreRecent);
                    string[] sRecentIgnores = sDonationsIgnoreRecent.Split(',');
                    string sDonationsIgnoreLatest = ini.GetValue("Settings", "Donations_Ignore_Latest", "");
                    ini.SetValue("Settings", "Donations_Ignore_Latest", sDonationsIgnoreLatest);
                    string[] sLatestIgnores = sDonationsIgnoreLatest.Split(',');
                    string sDonationsIgnoreTop = ini.GetValue("Settings", "Donations_Ignore_Top", "");
                    ini.SetValue("Settings", "Donations_Ignore_Top", sDonationsIgnoreTop);
                    string[] sTopIgnores = sDonationsIgnoreTop.Split(',');

                    /*if (IsHandleCreated && !DonationsWindowButton.Checked)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            Donations_List.Rows.Clear();
                            for (int i = 0; i < transactions.Count; i++)
                            {
                                Console.WriteLine(i + ": " + transactions[i].date + ", " + transactions[i].donor + ", " + transactions[i].amount + ", " + transactions[i].id + ".");
                                Donations_List.Rows.Add(transactions[i].date, transactions[i].donor, transactions[i].amount, transactions[i].id, transactions[i].notes, !sRecentIgnores.Contains(transactions[i].id), !sLatestIgnores.Contains(transactions[i].id), !sTopIgnores.Contains(transactions[i].id), true);
                            }
                        });
                    }*/
                    SetDonationsList(transactions, sRecentIgnores, sLatestIgnores, sTopIgnores); // for some reason the method above lost items...

                    int count = Convert.ToInt32(RecentDonorsLimit.Value);
                    if (transactions.Count < count)
                    {
                        count = transactions.Count;
                    }
                    string sTopDonors = "", sRecentDonors = "", sLatestDonor = "";
                    int iCount = 0;
                    List<Transaction> Donors = new List<Transaction>();
                    foreach (Transaction transaction in transactions)
                    {
                        if (UpdateRecentDonorsCheckBox.Checked)
                        {
                            if (!sRecentIgnores.Contains(transaction.id) && iCount < count)
                            {
                                if (iCount > 0)
                                {
                                    sRecentDonors += ", ";
                                }
                                sRecentDonors += transaction.ToString("$AMOUNT - DONOR");
                                iCount++;
                            }
                        }
                        if (UpdateLastDonorCheckBox.Checked && !sLatestIgnores.Contains(transaction.id) && sLatestDonor == "")
                        {
                            File.WriteAllText("LatestDonation.txt", (sLatestDonor = transaction.ToString("$AMOUNT - DONOR")));
                        }

                        if (UpdateTopDonorsCheckBox.Checked)
                        {
                            if (!sTopIgnores.Contains(transaction.id))
                            {
                                if (!Donors.Any(c => c.donor.ToLower() == transaction.donor.ToLower()))
                                {
                                    Donors.Add(transaction);
                                }
                                else
                                {
                                    foreach (Transaction trans in Donors)
                                    {
                                        if (transaction.donor.ToLower() == trans.donor.ToLower())
                                        {
                                            trans.amount = (float.Parse(trans.amount, CultureInfo.InvariantCulture.NumberFormat) + float.Parse(transaction.amount, CultureInfo.InvariantCulture.NumberFormat)).ToString("0.00");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (UpdateRecentDonorsCheckBox.Checked)
                    {
                        File.WriteAllText("RecentDonors.txt", sRecentDonors);
                    }

                    transactions = Donors.OrderByDescending(key => float.Parse(key.amount)).ToList();
                    if (UpdateTopDonorsCheckBox.Checked)
                    {
                        count = Convert.ToInt32(TopDonorsLimit.Value);
                        if (Donors.Count < count)
                        {
                            count = Donors.Count;
                        }
                        iCount = 0;
                        foreach (Transaction transaction in Donors)
                        {
                            if (iCount < count)
                            {
                                if (iCount > 0)
                                {
                                    sTopDonors += "\r\n";
                                }
                                sTopDonors += transactions[iCount].ToString("$AMOUNT - DONOR");
                                iCount++;
                            }
                        }
                        File.WriteAllText("TopDonors.txt", sTopDonors);
                    }
                }

                string sTitle = "Unavailable...", sGame = "Unavailable...";
                int iStatus = 0;
                if (Irc.irc.Connected)
                {
                    if (Irc.g_bIsStreaming)
                    {
                        iStatus = 2;
                    }
                    else
                    {
                        iStatus = 1;
                    }
                }
                using (WebClient w = new WebClient())
                {
                    string json_data = "";
                    try
                    {
                        w.Proxy = null;
                        json_data = w.DownloadString("https://api.twitch.tv/kraken/channels/" + Irc.channel.Substring(1));
                        JObject stream = JObject.Parse(json_data);
                        if (!stream["status"].ToString().Equals("")) sTitle = stream["status"].ToString();
                        if (!stream["game"].ToString().Equals("")) sGame = stream["game"].ToString();
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("Unable to connect to Twitch API to check stream data.");
                    }
                    catch (Exception e)
                    {
                        Api.LogError("*************Error Message (via GrabData()): " + DateTime.Now + "*********************************\r\nUnable to connect to Twitch API to check stream data.\r\n" + e + "\r\n");
                    }
                }

                if (IsHandleCreated)
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        ChannelStatusLabel.Text = "DISCONNECTED";
                        ChannelStatusLabel.ForeColor = Color.Red;
                        if (iStatus == 2)
                        {
                            ChannelStatusLabel.Text = "ON AIR";
                            ChannelStatusLabel.ForeColor = Color.Green;
                            int iViewers = Irc.ActiveUsers.Count;
                            foreach (string user in Irc.IgnoredUsers)
                            {
                                if (Irc.ActiveUsers.ContainsKey(user))
                                {
                                    iViewers--;
                                }
                            }
                            ChannelStatusLabel.Text += " (" + iViewers + ")";
                        }
                        else if (iStatus == 1)
                        {
                            ChannelStatusLabel.Text = "OFF AIR";
                            ChannelStatusLabel.ForeColor = Color.Blue;
                        }
                        ChannelTitleTextBox.Text = sTitle;
                        ChannelGameTextBox.Text = sGame;

                        //g_bLoaded = true;
                    });
                }
                /*else
                {
                    Currency_HandoutLabel.Text = "Handout " + IRC.currency + " to :";

                    Giveaway_MinCurrencyCheckBox.Text = "Min. " + IRC.currency;

                    ChannelLabel.Text = sName;
                    ChannelLabel.ForeColor = Color.Red;
                    if (iStatus == 2)
                    {
                        ChannelLabel.ForeColor = Color.Green;
                        ChannelLabel.Text += sViewers;
                    }
                    else if (iStatus == 1)
                    {
                        ChannelLabel.ForeColor = Color.Blue;
                    }
                    ChannelTitleTextBox.Text = sTitle;
                    ChannelGameTextBox.Text = sGame;
                }*/

                if (Irc.g_bResourceKeeper)
                {
                    Thread.Sleep(30000);
                }
            }
        }

        private void Giveaway_MinCurrencyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Giveaway_MinCurrency.Enabled = Giveaway_MinCurrencyCheckBox.Checked;
            SaveSettings();
        }

        private void Giveaway_RerollButton_Click(object sender, EventArgs e)
        {
            Giveaway.getWinner();
        }

        private void Giveaway_StartButton_Click(object sender, EventArgs e)
        {
            Giveaway.startGiveaway();
        }

        private void Giveaway_StopButton_Click(object sender, EventArgs e)
        {
            Giveaway.endGiveaway();
        }

        private void Giveaway_AnnounceWinnerButton_Click(object sender, EventArgs e)
        {
            TimeSpan t = Database.getTimeWatched(Giveaway_WinnerLabel.Text);
            Irc.sendMessage(Giveaway_WinnerLabel.Text + " has won the giveaway! (" + (Api.IsFollowingChannel(Giveaway_WinnerLabel.Text) ? "Currently follows the channel | " : "") + "Has " + Database.checkCurrency(Giveaway_WinnerLabel.Text) + " " + Irc.currencyName + " | Has watched the stream for " + t.Days + " days, " + t.Hours + " hours and " + t.Minutes + " minutes | Chance : " + Giveaway.Chance.ToString("0.00") + "%)");
        }

        private void Giveaway_BanButton_Click(object sender, EventArgs e)
        {
            Giveaway_BanListListBox.Items.Add(Giveaway_AddBanTextBox.Text);
            Giveaway_AddBanTextBox.Text = "";
            Giveaway_BanButton.Enabled = false;
            SaveSettings();
        }

        private void Giveaway_AddBanTextBox_TextChanged(object sender, EventArgs e)
        {
            if (Giveaway_AddBanTextBox.Text == "" || Giveaway_AddBanTextBox.Text.Length < 5 || Giveaway_AddBanTextBox.Text.Contains(" ") || Giveaway_AddBanTextBox.Text.Contains(".") || Giveaway_AddBanTextBox.Text.Contains(",") || Giveaway_AddBanTextBox.Text.Contains("\"") || Giveaway_AddBanTextBox.Text.Contains("'") || Irc.IgnoredUsers.Any(user => user.ToLower() == Giveaway_AddBanTextBox.Text.ToLower()) || Giveaway_BanListListBox.Items.Contains(Giveaway_AddBanTextBox.Text))
            {
                Giveaway_BanButton.Enabled = false;
            }
            else
            {
                Giveaway_BanButton.Enabled = true;
            }
        }

        private void Giveaway_BanListListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(Giveaway_BanListListBox.SelectedIndex >= 0) Giveaway_UnbanButton.Enabled = true;
        }

        private void Giveaway_UnbanButton_Click(object sender, EventArgs e)
        {
            int iOldIndex = Giveaway_BanListListBox.SelectedIndex;
            Giveaway_BanListListBox.Items.RemoveAt(iOldIndex);
            Giveaway_UnbanButton.Enabled = false;
            SaveSettings();
            if(Giveaway_BanListListBox.Items.Count > 0)
            {
                if (iOldIndex > Giveaway_BanListListBox.Items.Count - 1)
                {
                    Giveaway_BanListListBox.SelectedIndex = Giveaway_BanListListBox.Items.Count-1;
                }
                else
                {
                    Giveaway_BanListListBox.SelectedIndex = iOldIndex;
                }
            }
        }

        private void Giveaway_CopyWinnerButton_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(Giveaway_WinnerLabel.Text);
        }

        private void Giveaway_Settings_Changed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void Currency_LockCmdCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
            Irc.g_iLastCurrencyLockAnnounce = 0;
        }

        public void SaveSettings(int SettingsPresent=-2)
        {
            if (SettingsPresent == -2)
            {
                if (iSettingsPresent != -2)
                {
                    SettingsPresent = iSettingsPresent;
                }
            }
            if (!bIgnoreUpdates)
            {
                ini.SetValue("Settings", "Currency_LockCmd", Currency_LockCmdCheckBox.Checked ? "1" : "0");
                if (SettingsPresent > -1)
                {
                    string Present = SettingsPresents.TabPages[SettingsPresent].Text;
                    if (dSettings.ContainsKey(Present))
                    {
                        //ini.SetValue("Settings", "SelectedPresent", Present);
                        if (Giveaway_TypeActive.Checked)
                        {
                            ini.SetValue(Present, "Giveaway_Type", "0");
                        }
                        else if (Giveaway_TypeKeyword.Checked)
                        {
                            ini.SetValue(Present, "Giveaway_Type", "1");
                        }
                        else if (Giveaway_TypeTickets.Checked)
                        {
                            ini.SetValue(Present, "Giveaway_Type", "2");
                        }
                        Giveaway_ActiveUserTime.Enabled = Giveaway_TypeActive.Checked;

                        ini.SetValue(Present, "Giveaway_MustFollow", Giveaway_MustFollowCheckBox.Checked ? "1" : "0");
                        ini.SetValue(Present, "Giveaway_MinCurrencyChecked", Giveaway_MinCurrencyCheckBox.Checked ? "1" : "0");
                        ini.SetValue(Present, "Giveaway_MinCurrency", Giveaway_MinCurrency.Value.ToString());
                        ini.SetValue(Present, "Giveaway_ActiveUserTime", Giveaway_ActiveUserTime.Value.ToString());
                        ini.SetValue(Present, "Giveaway_AutoBanWinner", Giveaway_AutoBanWinnerCheckBox.Checked ? "1" : "0");
                        string items = "";
                        foreach (object item in Giveaway_BanListListBox.Items)
                        {
                            items = items + item.ToString() + ";";
                            //Console.WriteLine("Ban : " + item.ToString());
                        }
                        ini.SetValue(Present, "Giveaway_BanList", items);
                    }
                }
                GetSettings();
            }
        }

        private void Giveaway_WinnerTimer_Tick(object sender, EventArgs e)
        {
            if (Irc.ActiveUsers.ContainsKey(Api.capName(Giveaway_WinnerLabel.Text)))
            {
                int time = Api.GetUnixTimeNow() - Irc.ActiveUsers[Api.capName(Giveaway_WinnerLabel.Text)];
                int color = time - 120;
                if (color >= 0 && color < 120)
                {
                    color = 200 / 120 * color;
                    Giveaway_WinnerTimerLabel.ForeColor = Color.FromArgb(color, 200, 0);
                }
                else if (color >= 120)
                {
                    if (color <= 180)
                    {
                        color = 255 / 60 * (color - 120);
                        int red = 200;
                        if (color > 200)
                        {
                            red = color;
                            color = 200;
                        }
                        Giveaway_WinnerTimerLabel.ForeColor = Color.FromArgb(red, 200 - color, 0);
                    }
                    else 
                    {
                        Giveaway_WinnerTimerLabel.ForeColor = Color.FromArgb(255, 0, 0);
                    }
                }

                TimeSpan t = TimeSpan.FromSeconds(time);
                if(t.Days > 0)
                {
                    Giveaway_WinnerTimerLabel.Text = t.ToString(@"d\:hh\:mm\:ss");
                }
                else if (t.Hours > 0)
                {
                    Giveaway_WinnerTimerLabel.Text = t.ToString(@"h\:mm\:ss");
                }
                else
                {
                    Giveaway_WinnerTimerLabel.Text = t.ToString(@"m\:ss");
                }
            }

            if (Giveaway.LastRoll > 0)
            {
                int time = Api.GetUnixTimeNow() - Giveaway.LastRoll;
                int color = time;
                if (color >= 0 && color < 60)
                {
                    color = 200 / 60 * color;
                    Giveaway_WinTimeLabel.ForeColor = Color.FromArgb(color, 200, 0);
                }
                else if (color >= 60)
                {
                    if (color <= 90)
                    {
                        color = 255 / 30 * (color - 60);
                        int red = 200;
                        if (color > 200)
                        {
                            red = color;
                            color = 200;
                        }
                        Giveaway_WinTimeLabel.ForeColor = Color.FromArgb(red, 200 - color, 0);
                    }
                    else
                    {
                        Giveaway_WinTimeLabel.ForeColor = Color.FromArgb(255, 0, 0);
                    }
                }

                TimeSpan t = TimeSpan.FromSeconds(time);
                if (t.Days > 0)
                {
                    Giveaway_WinTimeLabel.Text = t.ToString(@"d\:hh\:mm\:ss");
                }
                else if (t.Hours > 0)
                {
                    Giveaway_WinTimeLabel.Text = t.ToString(@"h\:mm\:ss");
                }
                else
                {
                    Giveaway_WinTimeLabel.Text = t.ToString(@"m\:ss");
                }
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!(e.Cancel = (MessageBox.Show(DisconnectButton.Enabled ? "ModBot is currently active! Are you sure you want to close it?" : "Are you sure you want to close ModBot?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)))
            {
                Environment.Exit(0);
                /*Console.WriteLine("Closing...");
                List<Thread> Ts = new List<Thread>();
                foreach (Thread t in Threads)
                {
                    t.Abort();
                    Ts.Add(t);
                }
                Threads.Clear();
                foreach (Thread t in Irc.Threads)
                {
                    t.Abort();
                    Ts.Add(t);
                }
                Irc.Threads.Clear();
                foreach (Thread t in Api.dCheckingDisplayName.Values)
                {
                    t.Abort();
                    Ts.Add(t);
                }
                Api.dCheckingDisplayName.Clear();
                foreach (Thread t in Ts)
                {
                    while (t.IsAlive) Thread.Sleep(10);
                }*/
            }
        }

        private void SettingsPresents_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!bIgnoreUpdates)
            {
                Giveaway.endGiveaway();
                ini.SetValue("Settings", "SelectedPresent", SettingsPresents.TabPages[SettingsPresents.SelectedIndex].Text);
                SaveSettings(iSettingsPresent);
                iSettingsPresent = SettingsPresents.SelectedIndex;
            }
        }

        private void Currency_HandoutType_Changed(object sender, EventArgs e)
        {
            if (Currency_HandoutEveryone.Checked)
            {
                ini.SetValue("Settings", "Currency_Handout", "0");
            }
            else if (Currency_HandoutActiveStream.Checked)
            {
                ini.SetValue("Settings", "Currency_Handout", "1");
            }
            else if (Currency_HandoutActiveTime.Checked)
            {
                ini.SetValue("Settings", "Currency_Handout", "2");
            }
            Currency_HandoutLastActive.Enabled = Currency_HandoutActiveTime.Checked;
        }

        private void Currency_HandoutLastActive_ValueChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Currency_HandoutTime", Currency_HandoutLastActive.Value.ToString());
        }

        private void WindowChanged(object sender, EventArgs e)
        {
            /*CheckBox CB = (CheckBox)sender;
            if (CB.Checked)
            {
                CB.Enabled = false;
                WindowButtons[CB].Visible = true;
                foreach (CheckBox cb in WindowButtons.Keys)
                {
                    if (cb != CB)
                    {
                        cb.Enabled = true;
                        cb.Checked = false;
                        WindowButtons[cb].Visible = false;
                    }
                }
            }*/
            CheckBox CB = (CheckBox)sender;
            if (CB.Checked)
            {
                //CB.Enabled = false;
                CurrentWindow = Windows[CB];
                Windows[CB].BringToFront();
                foreach (CheckBox cb in Windows.Keys)
                {
                    if (cb != CB)
                    {
                        //cb.Enabled = true;
                        cb.Checked = false;
                    }
                }
            }
            else
            {
                if(Windows[CB] == CurrentWindow)
                {
                    CB.Checked = true;
                }
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "BOT_Name", Irc.nick = BotNameBox.Text);
            Irc.nick = BotNameBox.Text.ToLower();
            ini.SetValue("Settings", "BOT_Password", Irc.password = BotPasswordBox.Text);
            ini.SetValue("Settings", "Channel_Name", ChannelBox.Text);
            Irc.admin = ChannelBox.Text.Replace("#", "");
            Irc.channel = "#" + ChannelBox.Text.Replace("#", "").ToLower();
            ini.SetValue("Settings", "Currency_Name", Irc.currencyName = CurrencyNameBox.Text);
            ini.SetValue("Settings", "Currency_Command", Irc.currency = CurrencyCommandBox.Text);
            ini.SetValue("Settings", "Currency_Interval", CurrencyHandoutInterval.Value.ToString());
            Irc.interval = Convert.ToInt32(CurrencyHandoutInterval.Value.ToString());
            ini.SetValue("Settings", "Currency_Payout", CurrencyHandoutAmount.Value.ToString());
            Irc.payout = Convert.ToInt32(CurrencyHandoutAmount.Value.ToString());
            ini.SetValue("Settings", "Donations_Key", Irc.donationkey = DonationsKeyBox.Text);
            if (SubLinkBox.Text != "")
            {
                if ((SubLinkBox.Text.StartsWith("https://spreadsheets.google.com") || SubLinkBox.Text.StartsWith("http://spreadsheets.google.com")) && SubLinkBox.Text.EndsWith("?alt=json"))
                {
                    ini.SetValue("Settings", "Subsribers_URL", SubLinkBox.Text);
                }
                else
                {
                    Console.WriteLine("Invalid subscriber link. Reverting to the last known good link, or blank. Restart the program to fix it.");
                }
            }

            new Thread(() => { Irc.Initialize(); }).Start();
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            new Thread(() => { Irc.Disconnect(); }).Start();
        }

        private void WebsiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://sourceforge.net/projects/twitchmodbot/");
            System.Diagnostics.Process.Start("http://modbot.wordpress.com/");
        }

        private void SupportLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://modbot.wordpress.com/about/");
        }

        private void EmailLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:DorCoMaNdO@gmail.com");
        }

        private void Donations_List_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex > 4)
            {
                string sDonationsIgnoreRecent = "";
                string sDonationsIgnoreLatest = "";
                string sDonationsIgnoreTop = "";

                foreach (DataGridViewRow row in Donations_List.Rows)
                {
                    string sId = row.Cells["ID"].Value.ToString();
                    if (row.Cells["IncludeRecent"].Value.ToString().Equals("False"))
                    {
                        sDonationsIgnoreRecent += sId + ",";
                    }
                    if (row.Cells["IncludeLatest"].Value.ToString().Equals("False"))
                    {
                        sDonationsIgnoreLatest += sId + ",";
                    }
                    if (row.Cells["IncludeTop"].Value.ToString().Equals("False"))
                    {
                        sDonationsIgnoreTop += sId + ",";
                    }
                }
                ini.SetValue("Settings", "Donations_Ignore_Recent", sDonationsIgnoreRecent);
                ini.SetValue("Settings", "Donations_Ignore_Latest", sDonationsIgnoreLatest);
                ini.SetValue("Settings", "Donations_Ignore_Top", sDonationsIgnoreTop);
            }
        }

        private void UpdateTopDonorsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Donations_UpdateTop", UpdateTopDonorsCheckBox.Checked ? "1" : "0");
            TopDonorsLimit.Enabled = UpdateTopDonorsCheckBox.Checked;
        }

        private void UpdateRecentDonorsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Donations_UpdateRecent", UpdateRecentDonorsCheckBox.Checked ? "1" : "0");
            RecentDonorsLimit.Enabled = UpdateRecentDonorsCheckBox.Checked;
        }

        private void UpdateLastDonorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Donations_UpdateLast", UpdateLastDonorCheckBox.Checked ? "1" : "0");
        }

        private void RecentDonorsLimit_ValueChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Donations_Recent_Limit", RecentDonorsLimit.Value.ToString());
        }

        private void TopDonorsLimit_ValueChanged(object sender, EventArgs e)
        {
            ini.SetValue("Settings", "Donations_Top_Limit", TopDonorsLimit.Value.ToString());
        }

        private void Donations_List_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if(e.Column.Name == "Date")
            {
                e.SortResult = Convert.ToDateTime(e.CellValue1.ToString()).CompareTo(Convert.ToDateTime(e.CellValue2.ToString()));
                e.Handled = true;
            }
            else if (e.Column.Name == "Amount")
            {
                e.SortResult = float.Parse(e.CellValue1.ToString()).CompareTo(float.Parse(e.CellValue2.ToString()));
                e.Handled = true;
            }
        }

        private void ConnectionDetailsChanged(object sender, EventArgs e)
        {
            //ConnectButton.Enabled = !(BotNameBox.Text.Length < 5 || !BotPasswordBox.Text.StartsWith("oauth:") || ChannelBox.Text.Length < 5 || CurrencyNameBox.Text.Length < 5);
            ConnectButton.Enabled = ((SettingsErrorLabel.Text = (BotNameBox.Text.Length < 5 ? "Bot name too short or the field is empty.\r\n" : "") + (!BotPasswordBox.Text.StartsWith("oauth:") ? (BotPasswordBox.Text == "" ? "Password field is empty.\r\n" : "Password must be an oauth token.\r\n") : "") + (ChannelBox.Text.Length < 5 ? "Channel name too short or the field is empty.\r\n" : "") + (CurrencyNameBox.Text.Length < 2 ? "Currency name too short or the field is empty." : "") + (CurrencyCommandBox.Text.Length < 2 ? "Currency command too short or the field is empty." : "") + (CurrencyCommandBox.Text.Contains(" ") ? "The currency command can not contain spaces." : "")) == "");
            /*ConnectButton.Enabled = (new Func<bool>(() =>
            {
                if (BotNameBox.Text.Length < 5 || !BotPasswordBox.Text.StartsWith("oauth:") || ChannelBox.Text.Length < 5 || CurrencyNameBox.Text.Length < 5)
                {
                    SettingsErrorLabel.Text = (BotNameBox.Text.Length < 5 ? "Bot name too short or the field is empty.\r\n" : "") + (!BotPasswordBox.Text.StartsWith("oauth:") ? (BotPasswordBox.Text == "" ? "Password field is empty.\r\n" : "Password must be an oauth token.\r\n") : "") + (ChannelBox.Text.Length < 5 ? "Channel name too short or the field is empty.\r\n" : "");
                    return false;
                }
                return true;
            }) == new Func<bool>(() => { return true; }));*/
        }

        private void Giveaway_TypeActive_CheckedChanged(object sender, EventArgs e)
        {
            Giveaway_ActiveUserTime.Enabled = Giveaway_TypeActive.Checked;
            SaveSettings();
        }

        private void Giveaway_TypeTickets_CheckedChanged(object sender, EventArgs e)
        {
            Giveaway_MinCurrencyCheckBox.Enabled = Giveaway_MinCurrency.Enabled = !Giveaway_TypeTickets.Checked;
            if (Giveaway_MinCurrencyCheckBox.Checked && Giveaway_TypeTickets.Checked) Giveaway_MinCurrencyCheckBox.Checked = false;
            SaveSettings();
        }
    }
}
