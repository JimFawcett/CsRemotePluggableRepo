///////////////////////////////////////////////////////////////////////////
// RepoServer.cs - shares checkin files among PluggableRepoClients       //
// Ver 1.0                                                               //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package contains a class RepoServer with public methods:
 * - testComponent()      : self-test
 * 
 * It also contains a TestFileSync class used to demonstrate that the FileSync class 
 * functions as expected.
 * 
 * Required Files:
 * ---------------
 * - RepoServer.cs           - shares checkin files with all PluggableRepoClients
 * - IPluggable.cs           - Repository interfaces and shared data
 * - FileSynchronizer        - supports finding storage file inconsistencies between 
 *                             server and clients
 * - PluggableCommService.cs - sends and receives messages and files
 * - MetaData.cs             - builds, saves, loads, and queries package metadata
 * - FileNameEditor          - helper for manipulatiing file names
 * - TestUtilities.cs        - helper functions used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.0 : 24 Jul 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PluggableRepository
{
  using FileSpec = String;   // c:/.../category/filename
  using FileRef = String;   // category/filename
  using FileName = String;   // filename
  using FullPath = String;   // full path with no filename
  using DirName = String;   // directory name 
  using FileList = List<string>;
  using DirList = List<string>;
  using Command = String;
  using Msg = CommMessage;

  public struct ServerEnvironment
  {
    public const string storagePath = "../../../ServerStorage/";
  }

  ///////////////////////////////////////////////////////////////////
  // RepoServer class
  // - Stores code and metadata, shared by all RepoClients.
  // - RepoClients are local pluggable repositories.  
  // - This class is a common store for all RepoClients

  public class RepoServer
  {
    Comm comm_ = null;
    MessageDispatcher dispatcher_ = new MessageDispatcher();

    /*----< initializes server message dispatching >---------------*/

    public RepoServer()
    {
      Console.Title = "Repository Server";
      initializeDispatcher();
    }
    /*----< server finalizer >-------------------------------------*/
    /*
     *  Exception handling is necessary because WCF will throw
     *  an Exception if two servers are started.  That's because
     *  you can only start one listener, per port, per machine.
     */
    ~RepoServer()
    {
      try
      {
        comm_.close();
      }
      finally
      {
        /* nothing to do - just preventing unhandle exception */
      }
    }

    /*----< create comm if needed >--------------------------------*/
    /*
     *  You can only start one listerner on a given port for this machine.
     *  If two instances of server are started, that is attempted,
     *  resulting in a WCF exception.  In order to shutdown cleanly
     *  in this circumstance, we need to surppress Finialization.
     */
    void createCommIfNeeded()
    {
      try
      {
        if (comm_ == null)
        {
          string serverMachine = "http://localhost";
          int serverPort = 8080;
          comm_ = new Comm(serverMachine, serverPort);
        }
      }
      catch(Exception ex)
      {
        Console.Write("\n-- {0}", ex.Message);
        GC.SuppressFinalize(this);
        System.Diagnostics.Process.GetCurrentProcess().Close();
      }
    }
    /*----< here server responses to messages are defined >--------*/

    void initializeDispatcher()
    {
      // doTest
      Func<Msg, Msg> action1 = (Msg msg) =>
      {
        testComponent();
        Msg returnMsg = new Msg(Msg.MessageType.noReply);
        returnMsg.to = msg.from;
        returnMsg.from = msg.to;
        returnMsg.command = msg.command;
        return returnMsg;
      };
      dispatcher_.addCommand(Msg.Command.doTest, action1);

      // getCategories
      Func<Msg, Msg> action2 = (Msg msg) =>
      {
        DirList dirList = getCategories();
        Msg returnMsg = new Msg(Msg.MessageType.reply);
        returnMsg.to = msg.from;
        returnMsg.from = msg.to;
        returnMsg.argument = msg.argument;
        foreach (DirName cat in dirList)
        {
          returnMsg.arguments.Add(cat);
        }
        returnMsg.command = msg.command;
        return returnMsg;
      };
      dispatcher_.addCommand(Msg.Command.getCategories, action2);

      // getFiles
      Func<Msg, Msg> action3 = (Msg msg) =>
      {
        FileList fileList = getFiles(msg.argument);
        Msg returnMsg = new Msg(Msg.MessageType.reply);
        returnMsg.to = msg.from;
        returnMsg.from = msg.to;
        returnMsg.argument = msg.argument;
        foreach(FileName file in fileList)
        {
          returnMsg.arguments.Add(file);
        }
        //returnMsg.arguments = fileList;
        returnMsg.command = msg.command;
        return returnMsg;
      };
      dispatcher_.addCommand(Msg.Command.getFiles, action3);

      // synchLocal
      Func<Msg, Msg> action4 = (Msg msg) =>
      {
        string synchPath = FileNameEditor.pathCombine(ServerEnvironment.storagePath, msg.argument);
        FileSynch fs = new FileSynch(synchPath);
        fs.isSynched(msg.arguments);
        Msg replyMsg = new Msg(Msg.MessageType.reply);
        replyMsg.to = msg.from;
        replyMsg.from = msg.to;
        replyMsg.argument = msg.argument;
        foreach (string file in fs.notInList)
        {
          replyMsg.arguments.Add(file);
        }
        replyMsg.command = msg.command;
        return replyMsg;
      };
      dispatcher_.addCommand(Msg.Command.synchLocal, action4);

      // synchRemote
      Func<Msg, Msg> action5 = (Msg msg) =>
      {
        string synchPath = FileNameEditor.pathCombine(ServerEnvironment.storagePath, msg.argument);
        FileSynch fs = new FileSynch(synchPath);
        fs.isSynched(msg.arguments);
        Msg replyMsg = new Msg(Msg.MessageType.reply);
        replyMsg.to = msg.from;
        replyMsg.from = msg.to;
        replyMsg.argument = msg.argument;
        foreach (string file in fs.notInSyncDir)
        {
          replyMsg.arguments.Add(file);
        }
        replyMsg.command = msg.command;
        return replyMsg;
      };
      dispatcher_.addCommand(Msg.Command.synchRemote, action5);

      // sendFile
      Func<Msg, Msg> action6 = (Msg msg) =>
      {
        string fileRef = msg.argument;
        string fileSpec = FileNameEditor.pathCombine(CommEnvironment.serverStoragePath, fileRef);
        fileSpec = System.IO.Path.GetFullPath(fileSpec);
        comm_.postFile(fileSpec);
        Msg replyMsg = new Msg(Msg.MessageType.noReply);
        replyMsg.to = msg.from;
        replyMsg.from = msg.to;
        return replyMsg;
      };
      dispatcher_.addCommand(Msg.Command.sendFile, action6);

      // acceptFile
      Func<Msg, Msg> action7 = (Msg msg) =>
      {
        comm_.sndr.setFileDestinationPath(ServerEnvironment.storagePath);
        //string fileRef = msg.argument;
        //string fileSpec = FileNameEditor.pathCombine(CommEnvironment.serverStoragePath, fileRef);
        //fileSpec = System.IO.Path.GetFullPath(fileSpec);
        //comm_.postFile(fileSpec);
        Msg replyMsg = new Msg(Msg.MessageType.noReply);
        replyMsg.to = msg.from;
        replyMsg.from = msg.to;
        return replyMsg;
      };
      dispatcher_.addCommand(Msg.Command.acceptFile, action7);
    }
    /*----< returns names of folders in storagePath >--------------*/

    public static DirList getCategories()
    {
      DirList cats = new DirList();
      try
      {
        cats = System.IO.Directory.GetDirectories(ServerEnvironment.storagePath).ToList<string>();
        for(int i=0; i<cats.Count; ++i)
        {
          cats[i] = System.IO.Path.GetFileName(cats[i]);
        }
        return cats;
      }
      catch
      {
        return cats;
      }
    }
    /*----< returns names of files stored in server folder >-------*/

    public static FileList getFiles(DirName category)
    {
      FileList files = new FileList();
      try
      {
        string path = System.IO.Path.Combine(ServerEnvironment.storagePath + "/", category);
        files = System.IO.Directory.GetFiles(path).ToList<string>();
        for(int i=0; i<files.Count; ++i)
        {
          files[i] = System.IO.Path.GetFileName(files[i]);
        }
        return files;
      }
      catch
      {
        return files;
      }
    }
    /*----< start comm and receive thread >------------------------*/

    bool start()
    {
      try
      {
        createCommIfNeeded();
        Thread rcvThrd = new Thread(threadProc);
        rcvThrd.IsBackground = true;
        rcvThrd.Start();
        return true;
      }
      catch(Exception ex)
      {
        Console.Write("\n  -- {0}", ex.Message);
        return false;
      }
    }
    /*----< filters messages to which server replies >-------------*/

    bool doReply(Msg msg, Msg reply)
    {
      if (msg.type == Msg.MessageType.noReply)
        return false;
      if (msg.type == Msg.MessageType.connect)
        return false;
      if (reply.type == Msg.MessageType.procError)
        return false;
      return true;
    }
    /*----< receive thread processing >----------------------------*/

    void threadProc()
    {
      while (true)
      {
        try
        {
          CommMessage msg = comm_.getMessage();
          Console.Write("\n  Received {0} message : {1}", msg.type.ToString(), msg.command.ToString());
          CommMessage reply = dispatcher_.doCommand(msg.command, msg);
          if (reply.command == Msg.Command.show)
          {
            reply.show(reply.arguments.Count < 7);
            Console.Write("  -- no reply sent");
          }
          if (doReply(msg, reply))
            comm_.postMessage(reply);
        }
        catch
        {
          break;
        }
      }
    }
    /*----< used for debugging >-----------------------------------*/
    /*
     *  Not needed for server application processing
     */
    bool testComponent()
    {
      //MessageDispatcher dispatcher = new MessageDispatcher();
      string remoteUrl = "http://localhost:8080/IPluggableComm";
      string localUrl = "http://localhost:8081/IPluggableComm";

      Msg inMsg1 = new Msg(Msg.MessageType.request);
      inMsg1.command = Msg.Command.getCategories;
      inMsg1.to = remoteUrl;
      inMsg1.from = localUrl;
      Func<Msg, Msg> action1 = (Msg msg) =>
      {
        DirList dirList = getCategories();
        Msg returnMsg = new Msg(Msg.MessageType.reply);
        returnMsg.to = msg.from;
        returnMsg.from = msg.to;
        foreach (DirName cat in dirList)
        {
          returnMsg.arguments.Add(cat);
        }
        returnMsg.command = msg.command;
        return returnMsg;
      };
      dispatcher_.addCommand(inMsg1.command, action1);

      Msg inMsg2 = new Msg(Msg.MessageType.request);
      inMsg2.to = remoteUrl;
      inMsg2.from = localUrl;
      inMsg2.command = Msg.Command.getFiles;
      inMsg2.argument = "project";
      Func<Msg, Msg> action2 = (Msg msg) =>
      {
        FileList fileList = getFiles(msg.argument);
        Msg returnMsg = new Msg(Msg.MessageType.reply);
        returnMsg.to = msg.from;
        returnMsg.from = msg.to;
        returnMsg.argument = msg.argument;
        returnMsg.arguments = fileList;
        returnMsg.command = msg.command;
        return returnMsg;
      };
      dispatcher_.addCommand(inMsg2.command, action2);

      Msg replyMsg1 = dispatcher_.doCommand(inMsg1.command, inMsg1);
      replyMsg1.show();
      comm_.postMessage(replyMsg1);

      Msg replyMsg2 = dispatcher_.doCommand(inMsg2.command, inMsg2);
      replyMsg2.show(false);
      comm_.postMessage(replyMsg2);

      return true;
    }
    /*----< starts the server process >----------------------------*/

    static void Main(string[] args)
    {
      TestUtilities.title("Starting Repository Server", '=');
      TestUtilities.putLine();

      RepoServer server = new RepoServer();
      if(!server.start())
        return;

#if (TEST_REPOSERVER)
      server.testComponent();
#endif

      Console.Write("\n  Press a key to exit");
      Console.ReadKey();
      TestUtilities.putLine();
    }
  }
}
