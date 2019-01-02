using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Container;
using mRemoteNG.Themes;
using mRemoteNG.Tools;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Forms.Input;
using mRemoteNG.UI.TaskDialog;
using WeifenLuo.WinFormsUI.Docking;
using ConnectionTab = mRemoteNG.UI.Tabs.ConnectionTab;
using Message = System.Windows.Forms.Message; 

namespace mRemoteNG.UI.Window
{
	public partial class ConnectionWindow : BaseWindow
    {
        private readonly IConnectionInitiator _connectionInitiator = new ConnectionInitiator();
        private VisualStudioToolStripExtender vsToolStripExtender;
        private readonly ToolStripRenderer _toolStripProfessionalRenderer = new ToolStripProfessionalRenderer();


        private  List<ConnectionTab> tabsReferences = new  List<ConnectionTab>();


        #region Public Methods
        public ConnectionWindow(DockContent panel, string formText = "")
        {
            if (formText == "")
            {
                formText = Language.strNewPanel;
            }

            WindowType = WindowType.Connection;
            DockPnl = panel;
            InitializeComponent();
            SetEventHandlers();
            // ReSharper disable once VirtualMemberCallInConstructor
            Text = formText;
            TabText = formText;
            connDock.DocumentStyle = DocumentStyle.DockingWindow;
        }

        private InterfaceControl GetInterfaceControl()
        {
            return InterfaceControl.FindInterfaceControl(connDock);
        }

        private void SetEventHandlers()
        {
            SetFormEventHandlers();
            SetTabControllerEventHandlers();
            SetContextMenuEventHandlers();
        }

        private void SetFormEventHandlers()
        {
            Load += Connection_Load;
            DockStateChanged += Connection_DockStateChanged;
            FormClosing += Connection_FormClosing;
        }

        private void SetTabControllerEventHandlers()
        {
            //Menu handle
            cmenTab.Opening += ShowHideMenuButtons;
            //TabController.ClosePressed += TabController_ClosePressed;
            // TabController.DoubleClickTab += TabController_DoubleClickTab;
            // TabController.DragDrop += TabController_DrouagDrop;
            // TabController.DragOver += TabController_DragOver;
            // TabController.SelectionChanged += TabController_SelectionChanged;
            //MouseUp += TabController_MouseUp; 
            // TabController.PageDragEnd += TabController_PageDragStart;
            // TabController.PageDragStart += TabController_PageDragStart;
            // TabController.PageDragMove += TabController_PageDragMove;
            // TabController.PageDragEnd += TabController_PageDragEnd;
            // TabController.PageDragQuit += TabController_PageDragEnd;
        }

        private void SetContextMenuEventHandlers()
        {
            cmenTabFullscreen.Click += (sender, args) => ToggleFullscreen();
            cmenTabSmartSize.Click += (sender, args) => ToggleSmartSize();
            cmenTabViewOnly.Click += (sender, args) => ToggleViewOnly();
            cmenTabScreenshot.Click += (sender, args) => CreateScreenshot();
            cmenTabStartChat.Click += (sender, args) => StartChat();
            cmenTabTransferFile.Click += (sender, args) => TransferFile();
            cmenTabRefreshScreen.Click += (sender, args) => RefreshScreen();
            cmenTabSendSpecialKeysCtrlAltDel.Click += (sender, args) => SendSpecialKeys(ProtocolVNC.SpecialKeys.CtrlAltDel);
            cmenTabSendSpecialKeysCtrlEsc.Click += (sender, args) => SendSpecialKeys(ProtocolVNC.SpecialKeys.CtrlEsc);
            cmenTabRenameTab.Click += (sender, args) => RenameTab();
            cmenTabDuplicateTab.Click += (sender, args) => DuplicateTab();
            cmenTabReconnect.Click += (sender, args) => Reconnect();
            cmenTabDisconnect.Click += (sender, args) => CloseTabMenu();
            cmenTabDisconnectOthers.Click += (sender, args) => CloseOtherTabs();
            cmenTabDisconnectOthersRight.Click += (sender, args) => CloseOtherTabsToTheRight();
            cmenTabPuttySettings.Click += (sender, args) => ShowPuttySettingsDialog();
        }

        public ConnectionTab AddConnectionTab(ConnectionInfo connectionInfo)
        {
            try
            {
                var conTab = new ConnectionTab {Tag = connectionInfo};

                //Set the connection text based on name and preferences
                string titleText;
                if (Settings.Default.ShowProtocolOnTabs)
                    titleText = connectionInfo.Protocol + @": ";
                else
                    titleText = "";

                titleText += connectionInfo.Name;

                if (Settings.Default.ShowLogonInfoOnTabs)
                {
                    titleText += @" (";
                    if (connectionInfo.Domain != "")
                        titleText += connectionInfo.Domain;

                    if (connectionInfo.Username != "")
                    {
                        if (connectionInfo.Domain != "")
                            titleText += @"\";
                        titleText += connectionInfo.Username;
                    }

                    titleText += @")";
                }

                titleText = titleText.Replace("&", "&&");

                conTab.TabText = titleText;
                conTab.TabPageContextMenuStrip = cmenTab;

                 

                //Fix MagicRemove, i dont see no icons -.-
                conTab.Icon = ConnectionIcon.FromString(connectionInfo.Icon);

                //Add to the references as is easier to keep track of the tabs than connTab
                tabsReferences.Add(conTab);

                //Show the tab
                conTab.DockAreas = DockAreas.Document | DockAreas.Float;
                conTab.Show(connDock,DockState.Document);
                conTab.Focus(); 
                return conTab;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("AddConnectionTab (UI.Window.ConnectionWindow) failed", ex);
            }

            return null;
        }

        public void UpdateSelectedConnection()
        {
          /*  if (TabController.SelectedTab == null)
            {
	            FrmMain.Default.SelectedConnection = null;
            }
            else
            {
                var interfaceControl = TabController.SelectedTab?.Tag as InterfaceControl;
	            FrmMain.Default.SelectedConnection = interfaceControl?.Info;
            }*/
        }
        #endregion

        #region Form
        private void Connection_Load(object sender, EventArgs e)
        {
            ApplyTheme();
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyLanguage();
        }

        private new void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ThemingActive) return;
            base.ApplyTheme();
            try
            {
                connDock.Theme = ThemeManager.getInstance().ActiveTheme.Theme;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("UI.Window.ConnectionWindow.ApplyTheme() failed", ex);
            }


            vsToolStripExtender = new VisualStudioToolStripExtender(components)
            {
                DefaultRenderer = _toolStripProfessionalRenderer
            };
            vsToolStripExtender.SetStyle(cmenTab, ThemeManager.getInstance().ActiveTheme.Version, ThemeManager.getInstance().ActiveTheme.Theme);
            connDock.DockBackColor = ThemeManager.getInstance().ActiveTheme.ExtendedPalette.getColor("Tab_Item_Background");
        }

        private bool _documentHandlersAdded;
        private bool _floatHandlersAdded;
        private void Connection_DockStateChanged(object sender, EventArgs e)
        {
            if (DockState == DockState.Float)
            {
                if (_documentHandlersAdded)
                {
	                FrmMain.Default.ResizeBegin -= Connection_ResizeBegin;
	                FrmMain.Default.ResizeEnd -= Connection_ResizeEnd;
                    _documentHandlersAdded = false;
                }
                DockHandler.FloatPane.FloatWindow.ResizeBegin += Connection_ResizeBegin;
                DockHandler.FloatPane.FloatWindow.ResizeEnd += Connection_ResizeEnd;
                _floatHandlersAdded = true;
            }
            else if (DockState == DockState.Document)
            {
                if (_floatHandlersAdded)
                {
                    DockHandler.FloatPane.FloatWindow.ResizeBegin -= Connection_ResizeBegin;
                    DockHandler.FloatPane.FloatWindow.ResizeEnd -= Connection_ResizeEnd;
                    _floatHandlersAdded = false;
                }
	            FrmMain.Default.ResizeBegin += Connection_ResizeBegin;
	            FrmMain.Default.ResizeEnd += Connection_ResizeEnd;
                _documentHandlersAdded = true;
            }
        }

        private void ApplyLanguage()
        {
            cmenTabFullscreen.Text = Language.strMenuFullScreenRDP;
            cmenTabSmartSize.Text = Language.strMenuSmartSize;
            cmenTabViewOnly.Text = Language.strMenuViewOnly;
            cmenTabScreenshot.Text = Language.strMenuScreenshot;
            cmenTabStartChat.Text = Language.strMenuStartChat;
            cmenTabTransferFile.Text = Language.strMenuTransferFile;
            cmenTabRefreshScreen.Text = Language.strMenuRefreshScreen;
            cmenTabSendSpecialKeys.Text = Language.strMenuSendSpecialKeys;
            cmenTabSendSpecialKeysCtrlAltDel.Text = Language.strMenuCtrlAltDel;
            cmenTabSendSpecialKeysCtrlEsc.Text = Language.strMenuCtrlEsc;
            cmenTabExternalApps.Text = Language.strMenuExternalTools;
            cmenTabRenameTab.Text = Language.strMenuRenameTab;
            cmenTabDuplicateTab.Text = Language.strMenuDuplicateTab;
            cmenTabReconnect.Text = Language.strMenuReconnect;
            cmenTabDisconnect.Text = Language.strMenuDisconnect;
            cmenTabDisconnectOthers.Text = Language.strMenuDisconnectOthers;
            cmenTabDisconnectOthersRight.Text = Language.strMenuDisconnectOthersRight;
            cmenTabPuttySettings.Text = Language.strPuttySettings;
        }

        private void Connection_FormClosing(object sender, FormClosingEventArgs e)
        {
           if (!FrmMain.Default.IsClosing &&
                (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.All & connDock.Documents.Any() ||
                Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.Multiple & connDock.Documents.Count() > 1))
            {
                var result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName, string.Format(Language.strConfirmCloseConnectionPanelMainInstruction, Text), "", "", "", Language.strCheckboxDoNotShowThisMessageAgain, ETaskDialogButtons.YesNo, ESysIcons.Question, ESysIcons.Question);
                if (CTaskDialog.VerificationChecked)
                {
                    Settings.Default.ConfirmCloseConnection--;
                }
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            try
            {
                foreach (var dockContent in connDock.Documents)
                {
                    var tabP = (ConnectionTab) dockContent;
                    if (tabP.Tag == null) continue;
                    tabP.silentClose = true;
                    tabP.Close();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("UI.Window.Connection.Connection_FormClosing() failed", ex);
            }
        }

        public new event EventHandler ResizeBegin;
        private void Connection_ResizeBegin(object sender, EventArgs e)
        {
            ResizeBegin?.Invoke(this, e);
        }

        public new event EventHandler ResizeEnd;
        private void Connection_ResizeEnd(object sender, EventArgs e)
        {
            ResizeEnd?.Invoke(sender, e);
        }
        #endregion
         

        #region TabController
 

        private void TabController_DoubleClickTab(TabControl sender, TabPage page)
        {
            _firstClickTicks = 0;
            if (Settings.Default.DoubleClickOnTabClosesIt)
            {
               // CloseConnectionTab();
            }
        }

        #region Drag and Drop
        private void TabController_DragDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data is OLVDataObject dropDataAsOlvDataObject)) return;
            var modelObjects = dropDataAsOlvDataObject.ModelObjects;
            foreach (var model in modelObjects)
            {
                var modelAsContainer = model as ContainerInfo;
                var modelAsConnection = model as ConnectionInfo;
                if (modelAsContainer != null)
                    _connectionInitiator.OpenConnection(modelAsContainer);
                else if (modelAsConnection != null)
                    _connectionInitiator.OpenConnection(modelAsConnection);
            }
        }

        private void TabController_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            var dropDataAsOlvDataObject = e.Data as OLVDataObject;
            var modelObjects = dropDataAsOlvDataObject?.ModelObjects;
            if (modelObjects == null) return;
            if (!modelObjects.OfType<ConnectionInfo>().Any()) return;
            e.Effect = DragDropEffects.Move;
        }
        #endregion
        #endregion

        #region Tab Menu
        private void ShowHideMenuButtons(object sender, CancelEventArgs e)
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                if (interfaceControl.Info.Protocol == ProtocolType.RDP)
                {
                    var rdp = (RdpProtocol)interfaceControl.Protocol;
                    cmenTabFullscreen.Visible = true;
                    cmenTabFullscreen.Checked = rdp.Fullscreen;
                    cmenTabSmartSize.Visible = true;
                    cmenTabSmartSize.Checked = rdp.SmartSize;
                }
                else
                {
                    cmenTabFullscreen.Visible = false;
                    cmenTabSmartSize.Visible = false;
                }

                if (interfaceControl.Info.Protocol == ProtocolType.VNC)
                {
                    var vnc = (ProtocolVNC)interfaceControl.Protocol;
                    cmenTabSendSpecialKeys.Visible = true;
                    cmenTabViewOnly.Visible = true;
                    cmenTabSmartSize.Visible = true;
                    cmenTabStartChat.Visible = true;
                    cmenTabRefreshScreen.Visible = true;
                    cmenTabTransferFile.Visible = false;
                    cmenTabSmartSize.Checked = vnc.SmartSize;
                    cmenTabViewOnly.Checked = vnc.ViewOnly;
                }
                else
                {
                    cmenTabSendSpecialKeys.Visible = false;
                    cmenTabViewOnly.Visible = false;
                    cmenTabStartChat.Visible = false;
                    cmenTabRefreshScreen.Visible = false;
                    cmenTabTransferFile.Visible = false;
                }

                if (interfaceControl.Info.Protocol == ProtocolType.SSH1 | interfaceControl.Info.Protocol == ProtocolType.SSH2)
                {
                    cmenTabTransferFile.Visible = true;
                }

                cmenTabPuttySettings.Visible = interfaceControl.Protocol is PuttyBase;

                AddExternalApps();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ShowHideMenuButtons (UI.Window.ConnectionWindow) failed", ex);
            }
        }
        #endregion

        #region Tab Actions
        private void ToggleSmartSize()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();

                switch (interfaceControl.Protocol)
                {
                    case RdpProtocol rdp:
                        rdp.ToggleSmartSize();
                        break;
                    case ProtocolVNC vnc:
                        vnc.ToggleSmartSize();
                        break;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleSmartSize (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void TransferFile()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                if (interfaceControl.Info.Protocol == ProtocolType.SSH1 | interfaceControl.Info.Protocol == ProtocolType.SSH2)
                    SshTransferFile();
                else if (interfaceControl.Info.Protocol == ProtocolType.VNC)
                    VncTransferFile();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("TransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void SshTransferFile()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                Windows.Show(WindowType.SSHTransfer);
                var connectionInfo = interfaceControl.Info;

                Windows.SshtransferForm.Hostname = connectionInfo.Hostname;
                Windows.SshtransferForm.Username = connectionInfo.Username;
                Windows.SshtransferForm.Password = connectionInfo.Password;
                Windows.SshtransferForm.Port = Convert.ToString(connectionInfo.Port);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("SSHTransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void VncTransferFile()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.StartFileTransfer();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("VNCTransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleViewOnly()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var vnc = interfaceControl?.Protocol as ProtocolVNC;
                if (vnc == null) return;
                cmenTabViewOnly.Checked = !cmenTabViewOnly.Checked;
                vnc.ToggleViewOnly();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleViewOnly (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void StartChat()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.StartChat();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("StartChat (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void RefreshScreen()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.RefreshScreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RefreshScreen (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void SendSpecialKeys(ProtocolVNC.SpecialKeys keys)
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.SendSpecialKeys(keys);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("SendSpecialKeys (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var rdp = interfaceControl?.Protocol as RdpProtocol;
                rdp?.ToggleFullscreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleFullscreen (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ShowPuttySettingsDialog()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                var puttyBase = interfaceControl?.Protocol as PuttyBase;
                puttyBase?.ShowSettingsDialog();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ShowPuttySettingsDialog (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void AddExternalApps()
        {
            try
            {
                //clean up. since new items are added below, we have to dispose of any previous items first
                if (cmenTabExternalApps.DropDownItems.Count > 0)
                {
                    for (var i = cmenTabExternalApps.DropDownItems.Count - 1; i >= 0; i--)
                        cmenTabExternalApps.DropDownItems[i].Dispose();
                    cmenTabExternalApps.DropDownItems.Clear();
                }

                //add ext apps
                foreach (ExternalTool externalTool in Runtime.ExternalToolsService.ExternalTools)
                {
                    var nItem = new ToolStripMenuItem
                    {
                        Text = externalTool.DisplayName,
                        Tag = externalTool,
                        /* rare failure here. While ExternalTool.Image already tries to default this
                         * try again so it's not null/doesn't crash.
                         */
                        Image = externalTool.Image ?? Resources.mRemoteNG_Icon.ToBitmap()
                    };

                    nItem.Click += (sender, args) => StartExternalApp(((ToolStripMenuItem)sender).Tag as ExternalTool);
                    cmenTabExternalApps.DropDownItems.Add(nItem);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("cMenTreeTools_DropDownOpening failed (UI.Window.ConnectionWindow)", ex);
            }
        }

        private void StartExternalApp(ExternalTool externalTool)
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                externalTool.Start(interfaceControl?.Info);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("cmenTabExternalAppsEntry_Click failed (UI.Window.ConnectionWindow)", ex);
            }
        }


        private void CloseTabMenu()    
        {
            var selectedTab = (ConnectionTab)GetInterfaceControl()?.Parent;
            if (selectedTab == null) return;

            try
            {
                selectedTab.Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("CloseTabMenu (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void CloseOtherTabs()
        {
            var selectedTab = (ConnectionTab)GetInterfaceControl()?.Parent;
            if (selectedTab == null) return;
            if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.Multiple)
            {
                var result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName, string.Format(Language.strConfirmCloseConnectionOthersInstruction, selectedTab.TabText), "", "", "", Language.strCheckboxDoNotShowThisMessageAgain, ETaskDialogButtons.YesNo, ESysIcons.Question, ESysIcons.Question);
                if (CTaskDialog.VerificationChecked)
                {
                    Settings.Default.ConfirmCloseConnection--;
                }
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            foreach (ConnectionTab tab in tabsReferences )
            {
                if (selectedTab != tab)
                { 
                    tab.Close();
                }
            } 
        }

        private void CloseOtherTabsToTheRight()
        {

            try
            { 
                var selectedTab = (ConnectionTab)GetInterfaceControl()?.Parent;
                if (selectedTab == null) return;
                var dockPane = (DockPane)selectedTab.Pane;

                bool pastTabToKeepAlive= false;
                List<ConnectionTab> connectionsToClose = new List<ConnectionTab>();
                foreach (ConnectionTab tab in dockPane.Contents )
                {
                    if (pastTabToKeepAlive)
                        connectionsToClose.Add(tab);

                    if (selectedTab == tab)
                        pastTabToKeepAlive = true;
                }
                foreach (ConnectionTab tab in connectionsToClose)
                {
                    tab.Close();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("CloseTabMenu (UI.Window.ConnectionWindow) failed", ex);
            }
            
                /*   try
                   {
                       if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.Multiple)
                       {
                           var result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName, string.Format(Language.strConfirmCloseConnectionRightInstruction, TabController.SelectedTab.Title), "", "", "", Language.strCheckboxDoNotShowThisMessageAgain, ETaskDialogButtons.YesNo, ESysIcons.Question, ESysIcons.Question);
                           if (CTaskDialog.VerificationChecked)
                           {
                               Settings.Default.ConfirmCloseConnection--;
                           }
                           if (result == DialogResult.No)
                           {
                               return;
                           }
                       }
                       foreach (TabPage tab in TabController.TabPages)
                       {
                           if (TabController.TabPages.IndexOf(tab) > TabController.TabPages.IndexOf(TabController.SelectedTab))
                           {
                               if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.All)
                               {
                                   var result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName, string.Format(Language.strConfirmCloseConnectionMainInstruction, tab.Title), "", "", "", Language.strCheckboxDoNotShowThisMessageAgain, ETaskDialogButtons.YesNo, ESysIcons.Question, ESysIcons.Question);
                                   if (CTaskDialog.VerificationChecked)
                                   {
                                       Settings.Default.ConfirmCloseConnection--;
                                   }
                                   if (result == DialogResult.No)
                                   {
                                       continue;
                                   }
                               }
                               var interfaceControl = tab.Tag as InterfaceControl;
                               interfaceControl?.Protocol.Close();
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       Runtime.MessageCollector.AddExceptionMessage("CloseTabMenu (UI.Window.ConnectionWindow) failed", ex);
                   }*/
            }

        private void DuplicateTab()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;
                _connectionInitiator.OpenConnection(interfaceControl.Info, ConnectionInfo.Force.DoNotJump);
                _ignoreChangeSelectedTabClick = false;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("DuplicateTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void Reconnect()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;
                interfaceControl.Protocol.Close();
                _connectionInitiator.OpenConnection(interfaceControl.Info, ConnectionInfo.Force.DoNotJump);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Reconnect (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void RenameTab()
        {
            try
            {
                var interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;
                var newTitle = ((ConnectionTab)interfaceControl.Parent).TabText;
                using (FrmInputBox frmInputBox = new FrmInputBox(Language.strNewTitle, Language.strNewTitle, ref newTitle))
                {
                    DialogResult dr = frmInputBox.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        if(!string.IsNullOrEmpty(frmInputBox.returnValue))
                            ((ConnectionTab)interfaceControl.Parent).TabText = frmInputBox.returnValue.Replace("&", "&&"); 
                    }
                } 
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RenameTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void CreateScreenshot()
        {
            cmenTab.Close();
            Application.DoEvents();
            Windows.ScreenshotForm.AddScreenshot(MiscTools.TakeScreenshot(this));
        }
        #endregion

        #region Protocols
        public void Prot_Event_Closed(object sender)
        {
            var protocolBase = sender as ProtocolBase;
            if (protocolBase?.InterfaceControl.Parent is ConnectionTab tabPage)
                if(!tabPage.Disposing)
                {
                    tabPage.silentClose = true;
                    Invoke(new Action(() => tabPage.Close()));
                }
                    
        }
        #endregion

        #region Tabs

        private bool _ignoreChangeSelectedTabClick;
        private void TabController_SelectionChanged(object sender, EventArgs e)
        {
            _ignoreChangeSelectedTabClick = true;
            UpdateSelectedConnection();
            FocusInterfaceController();
            RefreshInterfaceController();
        }

        private int _firstClickTicks;
        private Rectangle _doubleClickRectangle;
        private void TabController_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (!(NativeMethods.GetForegroundWindow() == FrmMain.Default.Handle) && !_ignoreChangeSelectedTabClick)
                {
                    var clickedTab = connDock.ActivePane.GetChildAtPoint(e.Location);
                    if (clickedTab != null && connDock.ActivePane != clickedTab)
                    {
                        NativeMethods.SetForegroundWindow(Handle);
                        //connDock.active = clickedTab; //Fix MagicRemove , this doesnt work this way now
                    }
                }
                _ignoreChangeSelectedTabClick = false;

                switch (e.Button)
                {
                    case MouseButtons.Left:
                        var currentTicks = Environment.TickCount;
                        var elapsedTicks = currentTicks - _firstClickTicks;
                        if (elapsedTicks > SystemInformation.DoubleClickTime || !_doubleClickRectangle.Contains(MousePosition))
                        {
                            _firstClickTicks = currentTicks;
                            _doubleClickRectangle = new Rectangle(MousePosition.X - SystemInformation.DoubleClickSize.Width / 2, MousePosition.Y - SystemInformation.DoubleClickSize.Height / 2, SystemInformation.DoubleClickSize.Width, SystemInformation.DoubleClickSize.Height);
                            FocusInterfaceController();
                        }
                       /* else
                        {
                            TabController.OnDoubleClickTab(TabController.SelectedTab);
                        }*/
                        break;
                    case MouseButtons.Middle:
                        var activeTab = (ConnectionTab)GetInterfaceControl()?.Parent;
                        if (activeTab != null) activeTab.Close();
                        break;
                    case MouseButtons.Right:
                        if (connDock.ActivePane?.Tag == null) return;
                       // ShowHideMenuButtons();
                        NativeMethods.SetForegroundWindow(Handle);
                        cmenTab.Show(connDock, e.Location);
                        break;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("TabController_MouseUp (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void FocusInterfaceController()
        {/*
            try
            {
                var interfaceControl = TabController.SelectedTab?.Tag as InterfaceControl;
                interfaceControl?.Protocol?.Focus();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("FocusIC (UI.Window.ConnectionWindow) failed", ex);
            }*/
        }

        public void RefreshInterfaceController()
        {/*
            try
            {
                var interfaceControl = TabController.SelectedTab?.Tag as InterfaceControl;
                if (interfaceControl?.Info.Protocol == ProtocolType.VNC)
                    ((ProtocolVNC)interfaceControl.Protocol).RefreshScreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RefreshIC (UI.Window.Connection) failed", ex);
            }*/
        }
        #endregion

        #region Window Overrides
        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == NativeMethods.WM_MOUSEACTIVATE)
                {
                    var selectedTab = connDock.ActivePane;
                    if (selectedTab == null) return;
                    {
                        var tabClientRectangle = selectedTab.RectangleToScreen(selectedTab.ClientRectangle);
                        if (tabClientRectangle.Contains(MousePosition))
                        {
                            var interfaceControl = selectedTab.Tag as InterfaceControl;
                            if (interfaceControl?.Info?.Protocol == ProtocolType.RDP)
                            {
                                interfaceControl.Protocol.Focus();
                                return; // Do not pass to base class
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("UI.Window.Connection.WndProc() failed.", ex);
            }

            base.WndProc(ref m);
        }
        #endregion

        #region Tab drag and drop
        public bool InTabDrag { get; set; }

        private void TabController_PageDragStart(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.SizeWE;
        }

        private void TabController_PageDragMove(object sender, MouseEventArgs e)
        {/*
            InTabDrag = true; // For some reason PageDragStart gets raised again after PageDragEnd so set this here instead

            var sourceTab = TabController.SelectedTab;
            var destinationTab = TabController.TabPageFromPoint(e.Location);

            if (!TabController.TabPages.Contains(destinationTab) || sourceTab == destinationTab)
                return;

            var targetIndex = TabController.TabPages.IndexOf(destinationTab);

            TabController.TabPages.SuspendEvents();
            TabController.TabPages.Remove(sourceTab);
            TabController.TabPages.Insert(targetIndex, sourceTab);
            TabController.SelectedTab = sourceTab;
            TabController.TabPages.ResumeEvents();*/
        }

        private void TabController_PageDragEnd(object sender, MouseEventArgs e)
        {/*
            Cursor = Cursors.Default;
            InTabDrag = false;
            var interfaceControl = TabController?.SelectedTab?.Tag as InterfaceControl;
            interfaceControl?.Protocol.Focus();*/
        }
        #endregion
        private void ConnectionWindow_Load(object sender, EventArgs e)
        {

        }
    }
}