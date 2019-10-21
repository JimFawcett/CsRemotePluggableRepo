/////////////////////////////////////////////////////////////////////
// IPCommService.cs - service interface for PluggableComm          //
// ver 1.0                                                         //
// Jim Fawcett, CSE681-OnLine, Summer 2017                         //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 */
/*
 * This package provides:
 * ----------------------
 * - ServiceClientEnvironment : client-side path and address
 * - ServiceEnvironment       : server-side path and address
 * - IPluggableComm           : interface used for message passing and file transfer
 * - CommMessage              : class representing serializable messages
 * 
 * Required Files:
 * ---------------
 * - IPCommService.cs         : Service interface and Message definition
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 15 Jun 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace PluggableRepository
{
  using EndPoint = String;            // string is (ip address or machine name):(port number)
  using Argument = String;      
  using ErrorMessage = String;

  public struct ServiceClientEnvironment
  {
    public const string fileStorage = "../../ClientFileStore";
    public const long blockSize = 1024;
    public static string baseAddress { get; set; }
  }

  public struct ServiceEnvironment
  {
    public const string fileStorage = "../../ServiceFileStore";
    public static string baseAddress { get; set; }
  }

  [ServiceContract(Namespace = "IPluggableRepository")]
  public interface IPluggableComm
  {
    /*----< support for message passing >--------------------------*/

    [OperationContract(IsOneWay = true)]
    void postMessage(CommMessage msg);

    // private to receiver so not an OperationContract
    CommMessage getMessage();

    /*----< support for sending file in blocks >-------------------*/

    [OperationContract]
    bool openFileForWrite(string name);

    [OperationContract]
    bool writeFileBlock(byte[] block);

    [OperationContract(IsOneWay = true)]
    void closeFile();
  }

  [DataContract]
  public class CommMessage
  {
    public enum MessageType
    {
      [EnumMember]
      connect,           // initial message sent on successfully connecting
      [EnumMember]
      listen,            // starting listener
      [EnumMember]
      request,           // request for action from receiver (usually server)
      [EnumMember]
      reply,             // response to a request (usually by server)
      [EnumMember]
      test,              // used for debugging
      [EnumMember]
      noReply,           // used when server should not return message
      [EnumMember]
      commError,         // error in channel
      [EnumMember]
      procError,         // processing error - usually in server
      [EnumMember]
      closeSender,       // close down client
      [EnumMember]
      closeReceiver      // close down server for graceful termination
    }

    public enum Command
    {
      [EnumMember]
      connect,              // sent to destination when connect succeeds
      [EnumMember]
      synchLocal,           // request for RemoteView local updates
      [EnumMember]
      synchRemote,          // request for RemoteView remote updates
      [EnumMember]
      updateLocalFileView,  // files held by client, not on server
      [EnumMember]
      updateRemoteFileView, // files held by server, not on client
      [EnumMember]
      doTest,               // used for debugging
      [EnumMember]
      getCategories,        // request for server category list
      [EnumMember]
      getFiles,             // request for server files in specified category
      [EnumMember]
      sendFile,             // request for file held by server or by client
      [EnumMember]
      acceptFile,           // may remove this commmand
      [EnumMember]
      show                  // demonstrate server processing
    }

    /*----< constructor requires message type >--------------------*/

    public CommMessage(MessageType mt)
    {
      type = mt;
    }
    /*----< data members - all serializable public properties >----*/

    [DataMember]
    public MessageType type { get; set; } = MessageType.connect;

    [DataMember]
    public string to { get; set; } = "";

    [DataMember]
    public string from { get; set; } = "";

    [DataMember]
    public string author { get; set; } = "";

    [DataMember]
    public Command command { get; set; } = Command.show;

    [DataMember]
    public string argument { get; set; } = "";

    [DataMember]
    public List<Argument> arguments { get; set; } = new List<Argument>();

    [DataMember]
    public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

    [DataMember]
    public ErrorMessage errorMsg { get; set; } = "no error";

    public void show(bool compressed = true)
    {
      Console.Write("\n  CommMessage:");
      Console.Write("\n    MessageType : {0}", type.ToString());
      Console.Write("\n    to          : {0}", to);
      Console.Write("\n    from        : {0}", from);
      Console.Write("\n    author      : {0}", author);
      Console.Write("\n    command     : {0}", command);
      Console.Write("\n    argument    : {0}", argument);
      Console.Write("\n    arguments   :");
      if (arguments.Count > 0)
      {
        if (compressed)
        {
          Console.Write("\n      ");
          foreach (string arg in arguments)
            Console.Write("{0} ", arg);
        }
        else
        {
          foreach (string arg in arguments)
            Console.Write("\n      {0}", arg);
        }
      }
      Console.Write("\n    ThreadId    : {0}", threadId);
      Console.Write("\n    errorMsg    : {0}\n", errorMsg);
    }
  }
}
