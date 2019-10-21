/////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Client prototype GUI for Pluggable Repository  //
// Ver 1.5                                                             //
// Jim Fawcett, CSE681-OnLine, Summer 2017                             //
/////////////////////////////////////////////////////////////////////////
/*  
 *  Purpose:
 *    GUI client for the Pluggable Repository.
 *    This application manages a local repository (PluggableRepo) and 
 *    communicates with a remote repository container (RepoServer).
 *
 *  Required Files:
 *    MainWindow.xaml, MainWindow.xaml.cs - GUI for repository navigation, checkin/checkout, ...
 *    Window1.xaml, Window1.xaml.cs       - GUI for repository code and MetaData viewer
 *    IPluggable.cs                       - interfaces and shared paths
 *    PluggableRepo.cs                    - Provides all functionality for local repository
 *    IPluggableComm.cs, PluggableComm.cs - message passing communication facilities
 *    FileNameEditor                      - helper for handling file name modifications
 *    MetaData                            - Builds, saves, and loads metadata files
 *    Relationships                       - dependency relationship and version chain storage
 *    TestUtilities.cs                    - helpers for testing and display
 *    version.cs                          - version pluggin
 *
 *  Maintenance History:
 *  --------------------
 *  ver 1.5 : 25 Jul 2017
 *  - added RepoServer, a remote shared repository file container
 *  - added PluggableCommService project and integrated with
 *    PluggableRepoClient and RepoServer
 *  - added RemoteView tab
 *  - added file transfer using Comm.postFile(...)
 *  - added tests for file synchronization
 *  ver 1.4 : 17 Jul 2017
 *  - modified button enabling in Checkin
 *  - on tab selection set views to category mode
 *  ver 1.3 : 15 Jul 2017
 *  - finished checkout processing
 *  - cleaned up checkin views
 *  ver 1.2 : 11 Jul 2017
 *  - added parent, child processing to Navigation view
 *  ver 1.1 : 24 Jun 2017
 *  - added functionality for browsing bound directly to Repository
 *  ver 1.0 : 15 Jun 2017
 *  - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;

using PluggableRepository;

namespace PluggableRepoClient
{
  // aliases with semantic meaning
  using Msg = CommMessage;

  ///////////////////////////////////////////////////////////////////
  // MainWindow class
  //   - provides tabbed display for all client functions

  public partial class MainWindow : Window
  {
    // Navigation view and code popups
    enum DisplayMode { categories, files }
    DisplayMode navDisplayMode = DisplayMode.categories;
    DisplayMode chkOutDisplayMode = DisplayMode.categories;
    enum ViewMode { code, metadata }
    enum RelationsMode { children, parents }
    RelationsMode relMode = RelationsMode.children;
    bool disableEvent = false;
    List<Window1> popups = new List<Window1>();

    // Checkout view

    // Checkin view
    DisplayMode chkInDisplayMode = DisplayMode.categories;
    CheckinInfo chkinInfo;
    bool prevIsOpen;
    string targetCategory = "";
    string checkInFileName = "";

    // Message view
    Comm comm_ = null;
    Thread rcvThrd = null;
    MessageDispatcher dispatcher_ = new MessageDispatcher();
    enum CommState { offLine, listening, connected, onLine };
    CommState commState = CommState.offLine;
    int maxOutMsgCount = 50;
    int maxInMsgCount = 50;

    // Admin view

    // RemoteView
    DisplayMode localNavDisplayMode = DisplayMode.categories;
    DisplayMode remoteNavDisplayMode = DisplayMode.categories;
    public StringBuilder Item { get; set; } = new StringBuilder();

    // Repository
    PluggableRepository.PluggableRepo repo;
    string currentCategory = "";

    /////////////////////////////////////////////////////////////////
    // Initialize Client and Repository

    /*----< constructor >------------------------------------------*/

    public MainWindow()
    {
      InitializeComponent();
      Console.Title = "Repository Client";
    }
    /*----< start Repository and initialize views >----------------*/

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      repo = new PluggableRepository.PluggableRepo();
      repo.loadAndActivateComponents();
      repo.analyzeDependencies();  // loads dependency cache from stored metadata files

      // Navigation Tab
      initializeFilesListBox();
      initializeDepsListBox();
      ChkInFile.Width = this.Width - 125;

      // CheckOut Tab
      initializeChkOut();

      // CheckIn Tab
      initializeChkIn();
      IVersion version = repo.getVersion();

      // Admin Tab
      InitializeAdmin();
      ///////////////////////////////////////////////////////////////
      // uncomment the following line to allow only admins to view
      //AdminTab.Visibility = Visibility.Collapsed;

      // Messages Tab
      initializeMessageView();
      //msgsTab.Visibility = Visibility.Collapsed;

      // RemoteView Tab
      initializeRemoteView();
    }
    /*----< close any open popup code view windows >---------------*/

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      foreach (var popup in popups)
        popup.Close();
      if (comm_ != null)
        comm_.close();
    }
    /*----< keep chkin file textbox size synched with Window >-----*/

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      ChkInFile.Width = this.Width - 125;
    }


    /////////////////////////////////////////////////////////////////
    // Navigation Tab

    /*----< loads Repository categories in main list >-------------*/

    void initializeFilesListBox()
    {
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        filesListBox.Items.Add(cat);
      }
      navDisplayMode = DisplayMode.categories;
    }
    /*----< clears dependency list >-------------------------------*/

    void initializeDepsListBox()
    {
      depsListBox.Items.Clear();
    }

    /*----< show dependencies on select >--------------------------*/

    private void filesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (disableEvent)
        return;

      if(navDisplayMode == DisplayMode.files)  // add child packages to deps list
      {
        depsListBox.Items.Clear();
        string selected = (string)filesListBox.SelectedItem;
        string category = currentCategory;
        string fileSpec = System.IO.Path.Combine(category + "/", selected);
        if (relMode == RelationsMode.children)
        {
          List<string> children = RepoEnvironment.storage.children(fileSpec);
          foreach (string child in children)
          {
            depsListBox.Items.Add(child);
          }
          setChildState();
          statusLabel.Text = "Status: displayed child package(s) if any";
        }
        else  // add parent packages to deps list
        {
          List<string> parents = RepoEnvironment.storage.parents(fileSpec);
          foreach(string parent in parents)
          {
            depsListBox.Items.Add(parent);
          }
          setParentState();
          statusLabel.Text = "Status: displayed parent package(s) if any";
        }
      }
    }
    /*----< set button colors and relationship mode >--------------*/

    private void setChildState()
    {
      childButton.Background = Brushes.Black;
      childButton.Foreground = Brushes.White;
      parentButton.Background = Brushes.LightGray;
      parentButton.Foreground = Brushes.Black;
      relMode = RelationsMode.children;
    }
    /*----< show children of selected file >-----------------------*/

    private void childButton_Click(object sender, RoutedEventArgs e)
    {
      if(sender != null)
      {
        depsListBox.Items.Clear();
      }
      setChildState();
      disableEvent = true;
      filesListBox.SelectedIndex = -1;
      disableEvent = false;
      statusLabel.Text = "Status: Shows selected file children";
    }
    /*----< set button colors and relationship mode >--------------*/

    private void setParentState()
    {
      childButton.Background = Brushes.LightGray;
      childButton.Foreground = Brushes.Black;
      parentButton.Background = Brushes.Black;
      parentButton.Foreground = Brushes.White;
      relMode = RelationsMode.parents;
    }
    /*----< show parents of selected file >------------------------*/

    private void parentButton_Click(object sender, RoutedEventArgs e)
    {
      if(sender != null)
      {
        depsListBox.Items.Clear();
      }
      setParentState();
      disableEvent = true;
      filesListBox.SelectedIndex = -1;
      disableEvent = false;
      statusLabel.Text = "Status: Shows selected file parents";
    }
    /*----< fill main list with category names >-------------------*/
    /*
     *  categories are simply folders in Repository storage
     */
    private void showCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
      disableEvent = true;
      filesListBox.Items.Clear();
      depsListBox.Items.Clear();
      disableEvent = false;
      NavCat.Visibility = Visibility.Collapsed;
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        filesListBox.Items.Add(cat);
      }
      navDisplayMode = DisplayMode.categories;
      statusLabel.Text = "Action: Show files by double clicking on category";
    }
    /*----< display code and metadata in text area of popup >------*/

    private void showFile(string fileName, ViewMode viewMode, Window1 popUp)
    {
      string path = System.IO.Path.Combine(RepoEnvironment.storagePath, currentCategory, fileName);
      Paragraph paragraph = new Paragraph();
      string fileText = "";
      try
      {
        fileText = System.IO.File.ReadAllText(path);
      }
      finally
      {
        paragraph.Inlines.Add(new Run(fileText));
      }

      if (viewMode == ViewMode.code)
      {
        // add code text to code view
        popUp.codeView.Blocks.Clear();
        popUp.codeView.Blocks.Add(paragraph);
      }
      else
      {
        // add metadata text to metadata view
        popUp.metaDataView.Blocks.Clear();
        popUp.metaDataView.Blocks.Add(paragraph);
      }
    }
    /*----< show item selected from main list >--------------------*/

    private void filesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (disableEvent)
        return;

      if (navDisplayMode == DisplayMode.categories)
      {
        // double click on category so show its files
        currentCategory = (string)filesListBox.SelectedItem;
        NavCat.Text = currentCategory;
        NavCat.Visibility = Visibility.Visible;
        filesListBox.Items.Clear();
        IStorage storage = RepoEnvironment.storage;
        List<string> files = storage.files(currentCategory);
        foreach (string file in files)
        {
          filesListBox.Items.Add(file);
        }
        navDisplayMode = DisplayMode.files;
        statusLabel.Text = "Action: Show file text by double clicking on file";
      }
      else
      {
        // double click on file name so show its text and metadata
        Window1 codePopup = new Window1();
        codePopup.Show();
        popups.Add(codePopup);

        codePopup.codeView.Blocks.Clear();
        ViewMode viewMode;
        string fileName = (string)filesListBox.SelectedItem;

        // is file versioned? if so, it has metadata
        IVersion version = RepoEnvironment.version;
        if (!version.hasVersion(fileName))
        {
          // unversioned file has no metadata so show code
          codePopup.codeLabel.Text = "Source code: " + fileName;
          codePopup.metaDataView.Blocks.Clear();
          showFile(fileName, ViewMode.code, codePopup);
          return;
        }
        if (fileName.Contains(".xml"))
        {
          // is a metadata file so show it
          viewMode = ViewMode.metadata;
          codePopup.deps2Label.Text = "Metadata: " + fileName;
          showFile(fileName, viewMode, codePopup);
          // also show its code file
          fileName = FileNameEditor.removeXmlExt(fileName);
          codePopup.codeLabel.Text = "Source code: " + fileName;
          showFile(fileName, ViewMode.code, codePopup);
        }
        else
        {
          // code file with metadata so show it
          viewMode = ViewMode.code;
          codePopup.codeLabel.Text = "Source code: " + fileName;
          showFile(fileName, viewMode, codePopup);
          // show its metadata
          fileName = FileNameEditor.addXmlExt(fileName);
          codePopup.deps2Label.Text = "Metadata: " + fileName;
          showFile(fileName, ViewMode.metadata, codePopup);
        }
      }
    }

    /////////////////////////////////////////////////////////////////
    // CheckOut Tab
    /*
     *  Note: 
     *  For multiple concurrent clients, each client will need a 
     *  dedicated staging directory.  That has not been implemented 
     *  yet.
     *  
     *  The mechanics to do that are in place, e.g., the staging
     *  directory is held in ClientEnvironment.StagingDir and will
     *  be appended to RepoEnvironment.StagingPath.
     */
    /*----< initialize state of checkout view >--------------------*/

    private void initializeChkOut()
    {
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        ChkOutFilesListBox.Items.Add(cat);
      }
      chkOutDisplayMode = DisplayMode.categories;
      ChkOutButton.IsEnabled = false;
    }
    /*----< show categories in checkout view's main list >---------*/

    private void ChkOutShowCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
      ChkOutMsg.Visibility = Visibility.Collapsed;
      disableEvent = true;
      ChkOutFilesListBox.Items.Clear();
      ChkOutDecsListBox.Items.Clear();
      disableEvent = false;
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        ChkOutFilesListBox.Items.Add(cat);
      }
      chkOutDisplayMode = DisplayMode.categories;
      ChkOutButton.IsEnabled = false;
      statusLabel.Text = "Action: show files by double clicking on category";
    }
    /*----< change from categories to files view >-----------------*/

    private void ChkOutFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (disableEvent)
        return;

      if (chkOutDisplayMode == DisplayMode.categories)
      {
        // double click on category so show its files
        currentCategory = (string)ChkOutFilesListBox.SelectedItem;
        ChkOutFilesListBox.Items.Clear();
        IStorage storage = RepoEnvironment.storage;
        List<string> files = storage.files(currentCategory);
        foreach (string file in files)
        {
          if(file.Contains(".xml."))
            ChkOutFilesListBox.Items.Add(file);
        }
        chkOutDisplayMode = DisplayMode.files;
        ChkOutButton.IsEnabled = true;
        statusLabel.Text = "Action: select file for checkout, cancel by showing categories";
      }
      else
      {
      }
    }
    /*----< show descendents of selected file >--------------------*/

    private void ChkOutFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (disableEvent)
        return;

      if(chkOutDisplayMode == DisplayMode.files)
      {
        string selected = (string)ChkOutFilesListBox.SelectedItem;
        string fileSpec = System.IO.Path.Combine(currentCategory + "/", selected);
        List<string> descs = RepoEnvironment.storage.descendents(fileSpec);
        disableEvent = true;
        ChkOutDecsListBox.Items.Clear();
        disableEvent = false;
        foreach(string desc in descs)
        {
          ChkOutDecsListBox.Items.Add(desc);
        }
      }
    }
    /*----< checkout file and its descendents >--------------------*/

    private void ChkOutButton_Click(object sender, RoutedEventArgs e)
    {
      ChkOutMsg.Visibility = Visibility.Visible;
      ICheckout checkout = RepoEnvironment.checkout;
      string file = (string)ChkOutFilesListBox.SelectedItem;
      if (checkout.doCheckout(currentCategory, file))
        statusLabel.Text = "Status: " + file + " and decendents checked out";
      else
        statusLabel.Text = "Error: checkout of " + file + " failed";
    }

    /////////////////////////////////////////////////////////////////
    // Message Tab

    /*----< initialize message view >------------------------------*/

    void initializeMessageView()
    {
      ConnectButton.IsEnabled = true;
      ListenButton.IsEnabled = true;
      disconnectButton.IsEnabled = false;
      commState = CommState.offLine;
      initializeDispatcher();
    }
    /*----< bind client processing to message types >--------------*/
    /*
     *  This is where we determine how incoming messages are
     *  processed by client.
     */
    void initializeDispatcher()
    {
      // getCategories
      Func<Msg, Msg> action1 = (Msg msg) =>
      {
        remoteFilesListBox.Items.Clear();
        int fileCount = 0;
        foreach (string dir in msg.arguments)
        {
          ++fileCount;
          remoteFilesListBox.Items.Add(new ItemList(fileCount, dir, ""));
        }
        Msg reply = new Msg(Msg.MessageType.noReply);
        return reply;
      };
      dispatcher_.addCommand(Msg.Command.getCategories, action1);

      // getFiles
      Func<Msg, Msg> action2 = (Msg msg) =>
      {
        remoteFilesListBox.Items.Clear();
        int fileCount = 0;
        foreach (string file in msg.arguments)
        {
          ++fileCount;
          remoteFilesListBox.Items.Add(new ItemList(fileCount, file, ""));
        }
        Msg reply = new Msg(Msg.MessageType.noReply);
        return reply;
      };
      dispatcher_.addCommand(Msg.Command.getFiles, action2);

      // synchLocal
      Func<Msg, Msg> action3 = (Msg msg) =>
      {
        if(msg.arguments.Count > 0)
        {
          statusLabel.Text = "Status: " + msg.argument + " files require synchronization";
        }
        else
        {
          statusLabel.Text = "Status: client " + msg.argument + " files up-to-date";
        }
        foreach (string file in msg.arguments)
        {
          int size = remoteFilesListBox.Items.Count;
          for (int i = 0; i < size; ++i)
          {
            ItemList itemContent = (ItemList)remoteFilesListBox.Items[i];
            if (itemContent.fileName == file)
            {
              itemContent.needsSynch = "get from server";
              remoteFilesListBox.Items.RemoveAt(i);
              remoteFilesListBox.Items.Insert(i, itemContent);
            }
          }
        }
        Msg reply = new Msg(Msg.MessageType.noReply);
        return reply;
      };
      dispatcher_.addCommand(Msg.Command.synchLocal, action3);

      // synchRemote
      Func<Msg, Msg> action4 = (Msg msg) =>
      {
        if (msg.arguments.Count > 0)
        {
          statusLabel.Text = "Status: " + msg.argument + " files require synchronization";
        }
        else
        {
          statusLabel.Text = "Status: " + msg.argument + " files up-to-date";
        }
        foreach (string file in msg.arguments)
        {
          int size = localFilesListBox.Items.Count;
          for (int i = 0; i < size; ++i)
          {
            ItemList itemContent = (ItemList)localFilesListBox.Items[i];
            if (itemContent.fileName == file)
            {
              itemContent.needsSynch = "send to server";
              localFilesListBox.Items.RemoveAt(i);
              localFilesListBox.Items.Insert(i, itemContent);
            }
          }
        }
        Msg reply = new Msg(Msg.MessageType.noReply);
        return reply;
      };
      dispatcher_.addCommand(Msg.Command.synchRemote, action4);
    }
    /*----< make local endpoint using GUI info >-------------------*/

    string makeLocalEndpoint()
    {
      return "http://" + LocalMachine.Text + ":" + LocalPort.Text + "/IPluggableComm";
    }
    /*----< make remote endpoint using GUI info >------------------*/

    string makeRemoteEndpoint()
    {
      return "http://" + RemoteMachine.Text + ":" + RemotePort.Text + "/IPluggableComm";
    }
    /*----< make CommMessage using GUI info >----------------------*/

    CommMessage makeMessage(CommMessage.MessageType msgType)
    {
      CommMessage msg = new CommMessage(msgType);
      msg.to = makeRemoteEndpoint();
      msg.from = makeLocalEndpoint();
      msg.author = "Fawcett";
      return msg;
    }
    /*----< make msg list display string >-------------------------*/

    string makeMsgDisplayStr(Msg msg)
    {
      string display = msg.type.ToString() + ":" + msg.command + " -- " + msg.to + " " + DateTime.Now.ToString();
      return display;
    }
    /*----< create comm if needed >--------------------------------*/
    /*
     *  - communication may start in several different ways
     *  - we do a lazy initialization of comm_, so this code
     *    will be invoked when needed in a few different code
     *    locations
     */
    void createCommIfNeeded()
    {
      if (comm_ == null)
      {
        string localMachine = "http://" + LocalMachine.Text;
        int localPort = Int32.Parse(LocalPort.Text);
        comm_ = new Comm(localMachine, localPort);
      }
    }
    /*----< set commState based on button states >-----------------*/

    void setCommState()
    {
      if (ConnectButton.IsEnabled && ListenButton.IsEnabled)
        commState = CommState.offLine;
      else if (ConnectButton.IsEnabled && !ListenButton.IsEnabled)
        commState = CommState.listening;
      else if (!ConnectButton.IsEnabled && ListenButton.IsEnabled)
        commState = CommState.connected;
      else
        commState = CommState.onLine;
      commStatus.Text = commState.ToString();
    }
    /*----< connect to remote server >-----------------------------*/

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        createCommIfNeeded();
        statusLabel.Text = "Status: connected to " + RemoteMachine.Text + ":" + RemotePort.Text;
        CommMessage msg = makeMessage(CommMessage.MessageType.connect);
        msg.command = Msg.Command.connect;
        displayOutGoingMsg(msg);

        // if connect succeeds it sends connect message to destination

        if (!comm_.sndr.connect(msg.to))
        {
          // if connect fails, posts a message to self to display
          // failure to user

          Msg errMsg = makeMessage(Msg.MessageType.commError);
          errMsg.to = msg.from;
          errMsg.from = msg.from;
          comm_.postMessage(errMsg);
        }
        if (comm_.lastSndrError.Length > 0)
          statusLabel.Text = comm_.lastSndrError;
        ConnectButton.IsEnabled = false;
        disconnectButton.IsEnabled = true;
        setCommState();
      }
      catch
      {
        statusLabel.Text = "Error: can't connect";
      }
    }
    /*----< send test message >------------------------------------*/
    /*
     *  - used primarily for debugging
     *  - also useful to help users understand how application works
     */
    private void testMsgButton_Click(object sender, RoutedEventArgs e)
    {
      if (comm_ == null)
        return;
      Msg msg = makeMessage(Msg.MessageType.test);
      if ((bool)DoTestRB.IsChecked)
        msg.command = Msg.Command.doTest;
      if ((bool)GetFilesRB.IsChecked)
      {
        msg.argument = "test";
        msg.command = Msg.Command.getFiles;
      }
      if ((bool)GetCatsRB.IsChecked)
        msg.command = Msg.Command.getCategories;
      if ((bool)sndFileRB.IsChecked)
        msg.command = Msg.Command.sendFile;
      if ((bool)acceptFileRB.IsChecked)
        msg.command = Msg.Command.acceptFile;
      if ((bool)showRB.IsChecked)
        msg.command = Msg.Command.show;
      comm_.postMessage(msg);
      displayOutGoingMsg(msg);
    }
    /*----< display outgoing message >-----------------------------*/

    void displayOutGoingMsg(Msg msg)
    {
      outMsgListBox.Items.Insert(0, makeMsgDisplayStr(msg));
      if (outMsgListBox.Items.Count > maxOutMsgCount)
        outMsgListBox.Items.RemoveAt(maxOutMsgCount);
    }
    /*----< display incoming message >-----------------------------*/

    public void displayInComingMsg(Msg msg)
    {
      inMsgListBox.Items.Insert(0, makeMsgDisplayStr(msg));
      if (inMsgListBox.Items.Count > maxInMsgCount)
        inMsgListBox.Items.RemoveAt(maxInMsgCount);
    }
    /*----< clear message lists >----------------------------------*/

    private void clearMsgButton_Click(object sender, RoutedEventArgs e)
    {
      outMsgListBox.Items.Clear();
      inMsgListBox.Items.Clear();
    }
    /*----< filter messages to process >---------------------------*/
    /*
     *  currently doesn't filter anything
     */
    bool doProcess(Msg msg)
    {
      //if (msg.type == CommMessage.MessageType.connect)
      //  return false;
      //if (msg.from == makeLocalEndpoint())
      //  return false;

      return true;  // currently doesn't filter anything
    }
    /*----< receive thread processing >----------------------------*/
    /*
     *  - received messages are processed by dispatcher
     *  - dispatcher has a dictionary of actions, based on the message type
     *  - this allows a lot of flexibility configuring application processing,
     *    e.g., can simply add or replace dispatcher actions
     */
    void rcvProc(string localMachine, string localPort)
    {
      createCommIfNeeded();
      string localEndpoint = "http://" + localMachine + ":" + localPort + "/IPluggableComm";
      while (true)
      {
        CommMessage msg = comm_.getMessage();
        msg.show(msg.arguments.Count < 7);
        if(doProcess(msg))
        {
          Action toMainThrd = () =>
          {
            displayInComingMsg(msg);
            Msg result = dispatcher_.doCommand(msg.command, msg);  // our Comm dispatcher
          };
          Dispatcher.BeginInvoke(toMainThrd);  // WPF's dispatcher lets child thread use window
        }
      }
    }
    /*----< start receiver thread >--------------------------------*/

    private void ListenButton_Click(object sender, RoutedEventArgs e)
    {
      string localBaseAddr = "http://" + LocalMachine.Text;
      int localPort = Int32.Parse(LocalPort.Text);

      createCommIfNeeded();
      string machine = string.Copy(LocalMachine.Text);  // child thread can't access GUI
      string port = string.Copy(LocalPort.Text);        // ditto
      rcvThrd = new Thread(
        () => rcvProc(machine, port)
      );
      rcvThrd.Start();
      CommMessage msg = new CommMessage(CommMessage.MessageType.listen);
      msg.to = "http://" + LocalMachine.Text + ":" + LocalPort.Text + "/IPluggableComm";
      msg.from = string.Copy(msg.to);
      msg.author = "Fawcett";
      comm_.postMessage(msg);
      Console.Write("\n  posting listener connect to self {0}", msg.to);
      ListenButton.IsEnabled = false;
      setCommState();
    }
    /*----< close remote connection >------------------------------*/

    private void disconnectButton_Click(object sender, RoutedEventArgs e)
    {
      ConnectButton.IsEnabled = true;
      disconnectButton.IsEnabled = false;
      setCommState();
      comm_.sndr.close();  // closes proxy
    }

    /////////////////////////////////////////////////////////////////
    // Checkin Tab

    /*----< loads Repository categories in ChkInDeps list >--------*/

    void initializeChkIn(bool changeEnabled = true)
    {
      IStorage storage = repo.getStorage();
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        RepoFiles.Items.Add(cat);
      }
      chkInDisplayMode = DisplayMode.categories;
      if (changeEnabled)
      {
        BrowseButton.IsEnabled = false;
        ChkInButton.IsEnabled = false;
      }
      ChkInMode.IsChecked = false;
      Instructions.Content = "First: dbl clk on target category below";
      ChkInCatName.Visibility = Visibility.Collapsed;
      statusLabel.Text = "Action: select checkin target directory by double clicking on category";
    }
    /*----< select file to checkin >-------------------------------*/
    /*
     *  If current version exists in Repository, show important
     *  parts of its metadata, so user doesn't have to specify
     *  unless it needs to change for this checkin.
     */
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog fileDlg = new OpenFileDialog();
      if(fileDlg.ShowDialog() == true)
      {
        checkInFileName = fileDlg.FileName;
        ChkInFile.Text = System.IO.Path.GetFileName(checkInFileName);
        ChkInButton.IsEnabled = true;
        string category = currentCategory;
        ICheckin checkin = repo.getCheckin();
        IVersion version = repo.getVersion();
        string fileName = version.removeVersion(ChkInFile.Text) + ".xml";
        string MetaDataFileName = checkin.findStoredMetaData(category, fileName);
        if (MetaDataFileName.Length > 0)
        {
          // has latest version in Repo, so loading info in CheckIn Tab
          string path = System.IO.Path.Combine(RepoEnvironment.storagePath, category);
          IMetaData md = checkin.loadMetaData(path, MetaDataFileName);
          PkgDescript.Text = md.description;

          ChkInDeps.Items.Clear();
          foreach(string dep in md.dependencies)
          {
            ChkInDeps.Items.Add(dep);
          }
          ChkInMode.IsChecked = md.isOpen;
          prevIsOpen = md.isOpen;
        }
        BrowseButton.IsEnabled = false;
        Instructions.Content = "Third: add child by dbl clk on file";
        statusLabel.Text = "Action: optionally add child by double clicking on file, then click CheckIn button";
      }
    }
    /*----< show categories in checkin view >----------------------*/

    private void ChkInCatsButton_Click(object sender, RoutedEventArgs e)
    {
      RepoFiles.Items.Clear();
      initializeChkIn(false);
    }
    /*----< manage view of files in checkin view >-----------------*/

    private void RepoFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (chkInDisplayMode == DisplayMode.categories)
      {
        if (ChkInCatName.Text.Length == 0)
        {
          ChkInCatName.Text = (string)RepoFiles.SelectedItem;
          BrowseButton.IsEnabled = true;
          Instructions.Content = "Second: browse for checkin file";
          statusLabel.Text = "Action: browse for checkin file";
        }
        
        // double clicked on category so show its files
        currentCategory = String.Copy((string)RepoFiles.SelectedItem);
        IStorage storage = RepoEnvironment.storage;
        List<string> files = storage.files(currentCategory);
        RepoFiles.Items.Clear();
        foreach (string file in files)
        {
          if (file.Contains(".xml."))
          {
            RepoFiles.Items.Add(file);
          }
        }
        chkInDisplayMode = DisplayMode.files;
        ChkInCatName.Visibility = Visibility.Visible;
      }
      else
      {
        // double click on file to add to dependency list
        string fileName = (string)RepoFiles.SelectedItem;
        string path = System.IO.Path.Combine(currentCategory + "/", fileName);
        if(!ChkInDeps.Items.Contains(path))
          ChkInDeps.Items.Add(path);
      }
    }
    /*----< remove file from dependencies list >-------------------*/

    private void ChkInDeps_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      object selection = ChkInDeps.SelectedItem;
      ChkInDeps.Items.Remove(selection);
      statusLabel.Text = "Status: Removed child relationship for next checkin";
    }
    /*----< checkin file selected from browse operation >----------*/

    private void ChkInButton_Click(object sender, RoutedEventArgs e)
    {
      ICheckin checkin = RepoEnvironment.checkin;
      string stagedFileName = System.IO.Path.Combine(RepoEnvironment.stagingPath, ChkInFile.Text);
      System.IO.File.Copy(checkInFileName, stagedFileName, true);

      chkinInfo = new CheckinInfo();
      chkinInfo.author = "Fawcett";
      chkinInfo.description = PkgDescript.Text;
      chkinInfo.category = ChkInCatName.Text;
      chkinInfo.name = System.IO.Path.Combine(targetCategory, ChkInFile.Text);
      chkinInfo.fileName = ChkInFile.Text;

      foreach (string dep in ChkInDeps.Items)
      {
        chkinInfo.deps.Add(dep);
      }
      chkinInfo.isOpen = (bool)ChkInMode.IsChecked;
      checkin.doCheckin(chkinInfo, prevIsOpen);

      // reset Nav files view to categories - will force loading files later
      disableEvent = true;
      showCategoriesButton_Click(null, null);
      disableEvent = false;

      statusLabel.Text = "Status: " + ChkInFile.Text + " Checked In";

      // get ready for next checkin
      RepoFiles.Items.Clear();
      ChkInCatName.Text = "";
      ChkInDeps.Items.Clear();
      ChkInFile.Text = "";
      PkgDescript.Text = "";
      initializeChkIn();

      ChkInButton.IsEnabled = false;
      ChkInFile.Text = "";
      Instructions.Content = "First: select target category, below";
      statusLabel.Text = "Action: select checkin category by double clicking on category";
    }
    /*----< cancel checkin by initializing view's state >----------*/

    private void ChkInClearButton_Click(object sender, RoutedEventArgs e)
    {
      RepoFiles.Items.Clear();
      ChkInCatName.Text = "";
      ChkInDeps.Items.Clear();
      ChkInFile.Text = "";
      PkgDescript.Text = "";
      initializeChkIn();
      statusLabel.Text = "Status: checkin canceled";
    }

    /////////////////////////////////////////////////////////////////
    // Admin Tab

    /*----< initialize display of Repository categories >----------*/

    private void InitializeAdmin()
    {
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      foreach (string cat in categories)
      {
        AdminRepoFiles.Items.Add(cat);
      }
      AdminAddCat.IsChecked = true;
    }
    /*----< initialize file list in categories view >--------------*/

    private void AdminCatsButton_Click(object sender, RoutedEventArgs e)
    {
      AdminRepoFiles.Items.Clear();
      InitializeAdmin();
    }
    /*----< add or remove categories >-----------------------------*/

    private void AdminModifyCatButton_Click(object sender, RoutedEventArgs e)
    {
      if (AdminCatName.Text.Length == 0)
        return;
      try
      {
        string path = System.IO.Path.Combine(RepoEnvironment.storagePath, AdminCatName.Text);
        if ((bool)AdminAddCat.IsChecked)
          System.IO.Directory.CreateDirectory(path);
        else
          System.IO.Directory.Delete(path, true);
        AdminRepoFiles.Items.Clear();
        InitializeAdmin();
      }
      catch (Exception ex)
      {
        statusLabel.Text = ex.Message + " - Error";
      }
    }
    /*----< refresh cache of file dependencies >-------------------*/

    private void AnalDepButton_Click(object sender, RoutedEventArgs e)
    {
      RepoEnvironment.storage.analyzeDependencies();
      statusLabel.Text = "refreshed dependency and version cache";
    }
    /////////////////////////////////////////////////////////////////
    // RemoteView Tab

    /*----< initialize Remote View >-------------------------------*/

    void initializeRemoteView()
    {
      initializeLocalFilesListBox();
      initializeRemoteFilesListBox();
      Synch.IsEnabled = false;
    }
    /*----< populates list with local categories >-----------------*/

    void initializeLocalFilesListBox()
    {
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      int catCount = 0;
      foreach (string cat in categories)
      {
        //localFilesListBox.Items.Add(cat);
        ++catCount;
        localFilesListBox.Items.Add(new ItemList(catCount, cat, ""));
      }
      localNavDisplayMode = DisplayMode.categories;
      remoteNavDisplayMode = DisplayMode.categories;
    }
    /*----< switch back to categories view >-----------------------*/

    private void showLocalCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
      SynchText.Visibility = Visibility.Collapsed;
      disableEvent = true;
      localFilesListBox.Items.Clear();
      disableEvent = false;
      LocalNavCat.Visibility = Visibility.Collapsed;
      IStorage storage = RepoEnvironment.storage;
      List<string> categories = RepoEnvironment.storage.categories();
      int fileCount = 0;
      foreach (string cat in categories)
      {
        ++fileCount;
        localFilesListBox.Items.Add(new ItemList(fileCount, cat, ""));
      }
      localNavDisplayMode = DisplayMode.categories;
      Synch.IsEnabled = false;
      statusLabel.Text = "Action: Show files by double clicking on category";
    }
    /*----< switch between categories and files view >-------------*/
    /*
     *  - double click on category shows its files
     *  - double click on unsynched file results in file update
     *  - double click on synched file does nothing
     */
    private void localFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (disableEvent)
        return;

      if (localNavDisplayMode == DisplayMode.categories)
      {
        // double click on category so show its files
        currentCategory = (localFilesListBox.SelectedItem).ToString();
        LocalNavCat.Text = currentCategory;
        LocalNavCat.Visibility = Visibility.Visible;
        localFilesListBox.Items.Clear();
        IStorage storage = RepoEnvironment.storage;
        List<string> files = storage.files(currentCategory);
        int fileCount = 0;
        foreach (string file in files)
        {
          ++fileCount;
          localFilesListBox.Items.Add(new ItemList(fileCount, file, ""));
        }
        localNavDisplayMode = DisplayMode.files;
        Synch.IsEnabled = true;
        statusLabel.Text = "Action: Show categories by clicking on Categories button";
      }
      else
      {
        ItemList item = (ItemList)(localFilesListBox.SelectedItem);
        if (item.needsSynch.Length != 0)
        {
          Msg msg = makeMessage(Msg.MessageType.request);
          string fileRef = FileNameEditor.pathCombine(currentCategory, item.fileName);
          msg.argument = fileRef;
          msg.command = Msg.Command.acceptFile;  // asks server to set its receive path
          comm_.postMessage(msg);
          string fileSpec = FileNameEditor.pathCombine(RepoEnvironment.storagePath, fileRef);
          if(comm_.postFile(fileSpec))
          {
            item.needsSynch = "";
          }
        }
      }
    }
    /*----< won't have contents until loaded by user clicks >------*/

    void initializeRemoteFilesListBox()
    {
      remoteNavDisplayMode = DisplayMode.categories;
    }
    /*----< switch back to categories view >-----------------------*/

    private void showRemoteCategoriesButton_Click(object sender, RoutedEventArgs e)
    {
      SynchText.Visibility = Visibility.Collapsed;
      disableEvent = true;
      remoteFilesListBox.Items.Clear();
      disableEvent = false;
      if (OnLineButton.IsEnabled)
        OnLineButton_Click(null, null);
      RemoteNavCat.Visibility = Visibility.Collapsed;
      Msg msg = makeMessage(Msg.MessageType.request);
      msg.command = Msg.Command.getCategories;
      createCommIfNeeded();
      comm_.postMessage(msg);

      // message dispatcher will populate the listbox

      remoteNavDisplayMode = DisplayMode.categories;
      Synch.IsEnabled = false;
      statusLabel.Text = "Action: Show files by double clicking on category";
    }
    /*----< switch between categories and files view >-------------*/
    /*
     *  - double click on category shows its files
     *  - double click on unsynched file results in file update
     *  - double click on synched file does nothing
     */
    private void remoteFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (disableEvent)
        return;

      if (remoteNavDisplayMode == DisplayMode.categories)
      {
        // double click on category so show its files
        currentCategory = (remoteFilesListBox.SelectedItem).ToString();
        RemoteNavCat.Text = currentCategory;
        RemoteNavCat.Visibility = Visibility.Visible;
        remoteFilesListBox.Items.Clear();
        Msg msg = makeMessage(Msg.MessageType.request);
        msg.command = Msg.Command.getFiles;
        msg.argument = currentCategory;
        createCommIfNeeded();
        comm_.postMessage(msg);

        // display changes result from dispatching the reply to this message

        remoteNavDisplayMode = DisplayMode.files;
        statusLabel.Text = "Action: Show categories by clicking on Categories button";
        Synch.IsEnabled = true;
      }
      else
      {
        comm_.sndr.setFileDestinationPath(CommEnvironment.clientStoragePath); 
        ItemList item = (ItemList)(remoteFilesListBox.SelectedItem);
        if(item.needsSynch.Length != 0)
        {
          Msg msg = makeMessage(Msg.MessageType.request);
          string fileRef = FileNameEditor.pathCombine(currentCategory, item.fileName);
          msg.argument = fileRef;
          msg.command = Msg.Command.sendFile;  // asks server to use Comm.postFile
          comm_.postMessage(msg);
          item.needsSynch = "";
        }
      }
    }

    private void OnLineButton_Click(object sender, RoutedEventArgs e)
    {
      ListenButton_Click(null, null);
      OnLineButton.IsEnabled = false;
    }

    private void Synch_Click(object sender, RoutedEventArgs e)
    {
      createCommIfNeeded();
      SynchText.Visibility = Visibility.Visible;
      // find local files that should go to server
      Msg synchLocalMsg = makeMessage(Msg.MessageType.request);
      synchLocalMsg.command = Msg.Command.synchLocal;
      synchLocalMsg.argument = currentCategory;
      IStorage storage = RepoEnvironment.storage;
      List<string> files = storage.files(currentCategory);
      foreach (string file in files)
      {
        synchLocalMsg.arguments.Add(file);
      }
      comm_.postMessage(synchLocalMsg);

      // find remote files that should come from server
      Msg synchRemoteMsg = makeMessage(Msg.MessageType.request);
      synchRemoteMsg.command = Msg.Command.synchRemote;
      synchRemoteMsg.argument = currentCategory;
      foreach (string file in files)
      {
        synchRemoteMsg.arguments.Add(file);
      }
      comm_.postMessage(synchRemoteMsg);
    }

    /////////////////////////////////////////////////////////////////
    // Tab management 
    /*
     *  Dispatcher.BeginInvoke(...) enqueues action for processing
     *  after view initializers run.  Avoids write conflicts.
     */
    private void navTab_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        showCategoriesButton_Click(null, null);
        statusLabel.Text = "Action: show Repo files by double clicking on category";
      }));
    }

    private void ChkOut_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        ChkOutShowCategoriesButton_Click(null, null);
        statusLabel.Text = "Action: show Repo files by double clicking on category";
      }));
    }

    private void check_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        ChkInCatsButton_Click(null, null);
        statusLabel.Text = "Action: select checkin category by double clicking on category";
      }));
    }

    private void msgsTab_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        statusLabel.Text = "Status: Message View Opened";
      }));
    }

    private void AdminTab_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        statusLabel.Text = "Action: type category name to add or delete";
      }));
    }

    private void RemoteViewTab_Selected(object sender, RoutedEventArgs e)
    {
      Dispatcher.BeginInvoke((Action)(() => {
        statusLabel.Text = "Status: Remote View Opened";
      }));
    }
  }

  ///////////////////////////////////////////////////////////////////
  // ItemList class
  // - used to support ListBox DataTemplates for the Remote View
  // - implements INotifyPropertyChanged so that item changes
  //   are rendered by the UI in almost real time, e.g., this
  //   class supports data binding
  // - see MainWindow.xaml RemoteView Tab markup
  // - see MainWindow.xaml.cs RemotView codebehind, e.g., look
  //   for item.needsSynch = "..."

  public class ItemList : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    public int itemNum { get; set; }
    public string fileName { get; set; }

    string needsSynch_ = "";
    public string needsSynch
    {
      get { return this.needsSynch_; }
      set
      {
        if (this.needsSynch_ == value)
        {
          return;
        }
        this.needsSynch_ = value;
        Notify("needsSynch");
      }
    }

    public ItemList(int num, string name, string synch)
    {
      itemNum = num; fileName = name; needsSynch = synch;
    }
    protected void Notify(string propName)
    {
      if (this.PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propName));
      }
    }
    public override string ToString()
    {
      return fileName;
    }
  }
}

