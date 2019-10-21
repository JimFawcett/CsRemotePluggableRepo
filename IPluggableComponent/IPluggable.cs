///////////////////////////////////////////////////////////////////////////
// IPluggable.cs - Pluggable policies for Repository packages            //
// Version 1.2                                                           //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Spring 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * This package provides:
 * ----------------------
 * - RepoEnviroment    : struct containing Repository values used in multiple packages
 * - ClientEnvironment : struct containing values unique to each client
 * - IPluggable : base interface used for all pluggable components
 * - IStorage   : interface for storing repository contents
 * - IVersion   : interface for versioning repository contents
 * - ICheckin   : interface for checking in code to the repository
 * - ICheckout  : interface for extracting code with dependencies
 * - IBrowse    : deprecated interface that will be removed in a later version
 * - IOwnership : interface for authentication - placeholder, not yet used
 * 
 * The Repository pluggin policies are all activated at startup, and bound to
 * members of RepoEnvironment typed as their respective interfaces. That means
 * that any code in the repository or its policies can use any policy's code.
 * It also means that clients can access policy processing through the
 * RepoEnviornment members, without incurring any dependency on the concrete
 * code of the policies.
 * 
 * Required Files:
 * ---------------
 * - IPluggable.cs  : Repository interfaces and shared data
 * 
 * Maintenance History:
 * --------------------
 * ver 1.2 : 15 Jul 2017
 * - added function declaration in IStorage interface
 * - updated prologue comments
 * ver 1.1 : 11 Jun 2017
 * - added and extended interface information for several components 
 * ver 1.0 : 31 May 2017
 * - first release
 */
using System.Collections.Generic;
using System;

namespace PluggableRepository
{
  // type aliases with semantic meaning
  using Dependencies = List<string>;
  using FileSpec     = String;   // c:/.../category/filename
  using FileRef      = String;   // category/filename
  using FileName     = String;   // filename
  using FullPath     = String;   // full path with no filename
  using DirName      = String;   // directory name 

  ///////////////////////////////////////////////////////////////////
  // RepoEnvironment struct
  // - holds shared paths and component properties

  public struct RepoEnvironment
  {
    public const string componentsPath = "../../../ComponentLibraries/";
    public const string storagePath = "../../../Storage/";
    public const string stagingPath = "../../../StagingStorage/";
    public const string categories = "Categories.xml";
    public static IStorage storage { get; set; }
    public static IVersion version { get; set; }
    public static ICheckin checkin { get; set; }
    public static ICheckout checkout { get; set; }
    public static IBrowse browse { get; set; }
    public static IOwnership ownership { get; set; }
    public static IMetaData metadata { get; set; }
  }

  ///////////////////////////////////////////////////////////////////
  // ClientEnvironment struct
  // - holds paths and properties used in many places in client

  public struct ClientEnvironment
  {
    public static string credentials { get; set; } = "no cred";
    public static string stagingDir { get; set; } = ".";
    public static bool verbose { get; set; } = false;
  }

  ///////////////////////////////////////////////////////////////////
  // CommEnvironment struct
  // - holds paths and properties used for Comm operations

  public struct CommEnvironment
  {
    public static string clientStoragePath = RepoEnvironment.storagePath;
    public static string serverStoragePath = "../../../ServerStorage/";
    public static string defaultStoragePath = ".";
    public static int fileBlockSize { get; set; } = 1024;
  }

  ///////////////////////////////////////////////////////////////////
  // Result class
  // - not currently used, will probably be removed

  public class Result
  {
    public bool success { get; set; }
    public string msg { get; set; }
  }

  ///////////////////////////////////////////////////////////////////
  // IMetaData interface
  // - interface for managing metadata files

  public interface IMetaData
  {
    string name { get; set; }
    string author { get; set; }
    bool isOpen { get; set; }
    string fileSpec { get; set; }
    DateTime dateTime { get; set; }
    string description { get; set; }
    List<string> dependencies { get; set; }
    bool save(FullPath path);
    bool load(FullPath path);
    void show();
    string showStr();
  }

  ///////////////////////////////////////////////////////////////////
  // IPluggable interface
  // - base component interface

  public interface IPluggable
  {
    string componentType { get; }
    bool testComponent();
  }

  ///////////////////////////////////////////////////////////////////
  // IStorage interface
  // - interface for managing local repository storage

  public interface IStorage : IPluggable
  {
    bool createStorage(FullPath path);
    void analyzeDependencies();
    void upDateDependencyCache(FileSpec parent, FileSpec child);
    List<FileSpec> children(FileSpec package);
    List<FileSpec> parents(FileSpec package);
    List<FileRef> descendents(FileSpec package);
    FullPath stagingPath(DirName subfolder = "");
    FileSpec stagingFilePath(FileName fileName);
    FullPath storagePath(DirName category="");
    FileSpec storageFilePath(DirName category, FileName fileName);
    bool addFile(DirName category, FileName srcFile, FileName dstFile = "", bool move = true);
    bool modifyFile(DirName category, FileName srcFile, FileName dstFile = "", bool move = true);
    bool retrieveFile(DirName category, FileName fileName, bool overwrite=true);
    FileSpec findFile(DirName category, FileName fileName);
    List<FileSpec> findVersions(DirName category, FileName fileName);
    bool deleteFile(DirName category, FileName fileName);
    void addCategory(DirName categoryName);
    List<FileSpec> files(DirName category, bool showPath=false);
    List<FileName> stagedFiles(bool showPath = false);
    void deleteStagedFiles();
    List<DirName> categories(bool showPath=false);
  }

  ///////////////////////////////////////////////////////////////////
  // IVersion interface
  // - interface for handling versioning of repository items

  public interface IVersion : IPluggable
  {
    bool hasVersion(string fileName);
    string addVersion(string category, string fileName);
    string removeVersion(string fileName);
    int getVersion(string fileName);
    int getLatestVersion(string category, string fileName);
    void restoreVersionsFromFiles(string category="");
    void restoreVersionsFromCategories();
  }

  ///////////////////////////////////////////////////////////////////
  // CheckinInfo class
  // - holds checkin information as public properties

  public class CheckinInfo
  {
    public bool isOpen { get; set; } = true;
    public string category { get; set; } = "";
    public string name { get; set; } = "";
    public string author { get; set; } = "";
    public string fileName { get; set; } = "";
    public string description { get; set; } = "";
    public Dependencies deps { get; set; } = new Dependencies();
  }

  ///////////////////////////////////////////////////////////////////
  // ICheckin interface
  // - interface for managing the checkin process
  
  public interface ICheckin : IPluggable
  {
    bool isClosed { get; set; }
    bool doCheckin(CheckinInfo info, bool prevIsOpen);
    FileName findStoredMetaData(string category, string fileName);
    IMetaData loadMetaData(DirName category, FileName filename);
    bool editMetadata(CheckinInfo info);
    bool addDependencies(string fileSpec, Dependencies dependencies);
  }

  ///////////////////////////////////////////////////////////////////
  // ICheckout interface
  // - interface for extracting packages and their dependencies

  public interface ICheckout : IPluggable
  {
    DirName extractCategory(FileRef fileRef);
    FileName extractFileName(FileRef fileRef);
    bool retrieveDependencies(DirName category, FileName metaData);
    bool doCheckout(DirName category, FileName metaData);
  }

  ///////////////////////////////////////////////////////////////////
  // IBrowse interface
  // - is deprecated and will probably be removed

  public interface IBrowse : IPluggable
  {

  }
  ///////////////////////////////////////////////////////////////////
  // IOwnership interface
  // - interface for managing login and authentication
  // - not currently used, but will eventually be incorporated

  public interface IOwnership : IPluggable
  {
    bool saveCredentials(string cred);
    bool isAuthorized(DirName category, FileName fileName);
  }
}
