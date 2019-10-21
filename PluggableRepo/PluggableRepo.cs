///////////////////////////////////////////////////////////////////////////
// PluggableRepo.cs - Repository with Pluggable Policies                 //
// Version 1.1                                                           //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Spring 2017  //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines the Pluggable Repository.  Most of its functionality
 * derives from the pluggable components it installs:
 * - Storage handles saving and retrieving repository contents
 * - Version is responsible for adding and removing versions from contents
 * - Checkin handles storing files with metadata describing contents and dependencies
 * - Checkout extracts named files and all their dependencies
 * - Browsing manages dependency-based traversal of repository contents
 *
 * Required Files:
 * ---------------
 * - PluggableRepo.cs  - Repository that accepts pluggin components to define its operations
 * - IPluggable.cs     - Repository interfaces and shared data
 * - MetaData          - Creates, saves, and loads metadata files
 * - Storage.cs        - manages directories and their contents
 * - TestUtilities.cs  - helper functions used mostly for testing
 * 
 * Maintenance History:
 * --------------------
 * ver 1.1 : 11 Jul 2017
 * - added library binding event handler to enable use of dependent libraries
 * ver 1.0 : 31 May 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace PluggableRepository
{
  using Util = TestUtilities;

  ///////////////////////////////////////////////////////////////////
  // PluggableRepo class

  public class PluggableRepo
  {
    IStorage storage     = null;
    IVersion version     = null;
    ICheckin checkin     = null;
    ICheckout checkout   = null;
    IBrowse browse       = null;
    IOwnership ownership = null;
    //IMetaData metadata   = null;

    /*----< construct Repository >---------------------------------*/

    public PluggableRepo()
    {
    }
    /*----< Finalize Repository >----------------------------------*/

    ~PluggableRepo()
    {
    }
    /*----< library binding error event handler >------------------*/
    /*
     *  This function is an event handler for binding errors when
     *  loading libraries.  These occur when a loaded library has
     *  dependent libraries that are not located in the directory
     *  where the PluggableRepository is running.
     */
    static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
    {
      string folderPath = RepoEnvironment.componentsPath;
      string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
      if (!File.Exists(assemblyPath)) return null;
      Assembly assembly = Assembly.LoadFrom(assemblyPath);
      return assembly;
    }
    /*----< activate pluggable component libraries >---------------*/
    /*
     *  Loads each of the libraries from the ComponentLibraries path.
     *  Each component is activated and bound to a RepoEnvironment
     *  reference.
     */
    public bool loadAndActivateComponents()
    {
      AppDomain currentDomain = AppDomain.CurrentDomain;
      currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);

      // get names of component dlls

      if (!Directory.Exists(RepoEnvironment.componentsPath))
      {
        Console.Write("\n  can't find path to components");
        return false;
      }
      string[] libraries = Directory.GetFiles(RepoEnvironment.componentsPath, "*.dll");

      // load libraries and, for each component, activate

      if (libraries.Length > 0)
      {
        foreach (string libName in libraries)
        {
          // load library

          string fileName = Path.GetFileName(libName);
          Console.Write("\n  loading library: {0}", fileName);
          string fullLibName = Path.GetFullPath(libName);
          //Assembly asm = Assembly.LoadFile(fullLibName);
          Assembly asm = Assembly.LoadFrom(fullLibName);

          if (fileName.Contains("IPluggaleComponent.dll"))
            continue;

          // if library contains one of the Pluggable Components then activate

          Type[] types = asm.GetExportedTypes();
          foreach(Type t in types)
          {
            if (t.IsClass && typeof(IStorage).IsAssignableFrom(t))
            {
              storage = (IStorage)Activator.CreateInstance(t);
              RepoEnvironment.storage = storage;
              Console.Write("\n  creating {0} component", storage.componentType);
              continue;
            }
            if (t.IsClass && typeof(IVersion).IsAssignableFrom(t))
            {
              version = (IVersion)Activator.CreateInstance(t);
              RepoEnvironment.version = version;
              Console.Write("\n  creating {0} component", version.componentType);
              continue;
            }
            if (t.IsClass && typeof(ICheckin).IsAssignableFrom(t))
            {
              checkin = (ICheckin)Activator.CreateInstance(t);
              RepoEnvironment.checkin = checkin;
              Console.Write("\n  creating {0} component", checkin.componentType);
              continue;
            }
            if (t.IsClass && typeof(ICheckout).IsAssignableFrom(t))
            {
              checkout = (ICheckout)Activator.CreateInstance(t);
              RepoEnvironment.checkout = checkout;
              Console.Write("\n  creating {0} component", checkout.componentType);
              continue;
            }
            if (t.IsClass && typeof(IBrowse).IsAssignableFrom(t))
            {
              browse = (IBrowse)Activator.CreateInstance(t);
              RepoEnvironment.browse = browse;
              Console.Write("\n  creating {0} component", browse.componentType);
              continue;
            }
            if (t.IsClass && typeof(IOwnership).IsAssignableFrom(t))
            {
              ownership = (IOwnership)Activator.CreateInstance(t);
              RepoEnvironment.ownership = ownership;
              Console.Write("\n  creating {0} component", ownership.componentType);
              continue;
            }
          }
        }
      }
      Console.Write("\n");
      return (libraries.Length > 0);
    }
    /*----< return reference to activated checkin >----------------*/

    public IBrowse getBrowse()
    {
      return browse;
    }
    /*----< return reference to activated checkin >----------------*/

    public ICheckout getCheckout()
    {
      return checkout;
    }
    /*----< return reference to activated checkin >----------------*/

    public ICheckin getCheckin()
    {
      return checkin;
    }
    /*----< return reference to activated storage >----------------*/

    public IStorage getStorage()
    {
      return storage;
    }
    /*----< return reference to activated version >----------------*/

    public IVersion getVersion()
    {
      return version;
    }
    /*----< return reference to activated ownership >--------------*/

    public IOwnership getOwnership()
    {
      return ownership;
    }
    /*----< build dependency relationships for storage >-----------*/

    public void analyzeDependencies()
    {
      storage.analyzeDependencies();
    }

    /*----< demonstrate requirements have been met >---------------*/

    static void Main(string[] args)
    {
      TestUtilities.title("Starting Repository", '=');

      ClientEnvironment.verbose = true;  // if true, display test results

      PluggableRepo repo = new PluggableRepo();

      if(!repo.loadAndActivateComponents())
      {
        Console.Write("\n  Couldn't find Repository components.\n\n");
        return;
      }
    
      // if activation succeeded, then do self test on each

      IVersion version = repo.getVersion();
      IStorage storage = repo.getStorage();
      ICheckin checkin = repo.getCheckin();
      ICheckout checkout = repo.getCheckout();
      IBrowse browse = repo.getBrowse();
      IOwnership ownership = repo.getOwnership();

      storage.analyzeDependencies();

      if (
        Util.checkNull(version) || Util.checkNull(storage) || Util.checkNull(checkin)  || 
        Util.checkNull(checkout) || Util.checkNull(browse) || Util.checkNull(ownership)
      )
      {
        Console.Write("\n  failed to load one or more required components");
      }
      else
      {
        Util.checkResult(version.testComponent(), version.componentType);
        TestUtilities.putLine();
        Util.checkResult(storage.testComponent(), storage.componentType);
        TestUtilities.putLine();
        Util.checkResult(checkin.testComponent(), checkin.componentType);
        TestUtilities.putLine();
        Util.checkResult(checkout.testComponent(), checkout.componentType);
        TestUtilities.putLine();
        Util.checkResult(browse.testComponent(), browse.componentType);
        TestUtilities.putLine();
        Util.checkResult(ownership.testComponent(), ownership.componentType);

        TestUtilities.putLine();
        Console.Write("\n  finished testing all loaded components");
      }

      Console.Write("\n\n");
    }
  }
}
