///////////////////////////////////////////////////////////////////////////
// IPluggable.cs - Pluggable policies for Repository packages            //
// Version 1.1                                                           //
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
 * 
 * Required Files:
 * ---------------
 * - IPluggable.cs  : Repository interfaces and shared data
 * 
 * Maintenance History:
 * --------------------
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

  public struct ClientEnvironment
  {
    public static string credentials { get; set; } = "no cred";
    public static string stagingDir { get; set; } = ".";
    public static bool verbose { get; set; } = false;
  }

  public class Result
  {
    public bool success { get; set; }
    public string msg { get; set; }
  }

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

  public interface IPluggable
  {
    string componentType { get; }
    bool testComponent();
  }

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

  public interface ICheckin : IPluggable
  {
    bool isClosed { get; set; }
    bool doCheckin(CheckinInfo info, bool prevIsOpen);
    FileName findStoredMetaData(string category, string fileName);
    IMetaData loadMetaData(DirName category, FileName filename);
    bool editMetadata(CheckinInfo info);
    bool addDependencies(string fileSpec, Dependencies dependencies);
  }

  public interface ICheckout : IPluggable
  {
    DirName extractCategory(FileRef fileRef);
    FileName extractFileName(FileRef fileRef);
    bool retrieveDependencies(DirName category, FileName metaData);
    bool doCheckout(DirName category, FileName metaData);
  }

  public interface IBrowse : IPluggable
  {

  }

  public interface IOwnership : IPluggable
  {
    bool saveCredentials(string cred);
    bool isAuthorized(DirName category, FileName fileName);
  }
}
