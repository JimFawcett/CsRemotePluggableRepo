/////////////////////////////////////////////////////////////////////////////
// Relationships.cs - supports package dependency and version collections  //
// Ver 1.0                                                                 //
// Jim Fawcett, CSE681-OnLine Software Modeling & Analysis, Summer 2017    //
/////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides three classes:
 * - Dependency manages parent:child and child:parent relationships
 * - VersionChain supports building versioned packages
 * - TestRelationships tests Dependency and VersionChain
 * 
 * Required Files:
 * ---------------
 * - Relationships.cs
 * - TestUtilities.cs
 * 
 * Maintenance History:
 * --------------------
 * 11 Jul 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluggableRepository
{
  using Child = String;
  using Parent = String;
  using Package = String;
  using VersionNum = Int32;

  ///////////////////////////////////////////////////////////////////
  // Dependency class
  // - helps manage parent-child relationships in Repository

  public class Dependency
  {
    private Dictionary<Parent, List<Child>> children_ = new Dictionary<Child, List<Child>>();
    private Dictionary<Child, List<Parent>> parents_ = new Dictionary<Child, List<Child>>();

    public List<Child> getChildren(Parent parent)
    {
      if (children_.Keys.Contains(parent))
        return children_[parent];
      else
        return new List<Child>();
    }

    public Dependency addChild(Parent parent, Child child)
    {
      if (children_.Keys.Contains(parent))
      {
        if (!children_[parent].Contains(child))
        {
          children_[parent].Add(child);
        }
      }
      else
      {
        List<Child> children = new List<Child>();
        children.Add(child);
        children_[parent] = children;
      }
      return this;
    }

    public void showChildren()
    {
      foreach(Parent parent in children_.Keys)
      {
        Console.Write("\n  Parent {0} has Children:", parent);
        foreach (Child child in children_[parent])
        {
          Console.Write("\n    {0}", child);
        }
      }
      Console.Write("\n");
    }

    public List<Child> getParents(Child child)
    {
      if (parents_.Keys.Contains(child))
        return parents_[child];
      else
        return new List<Parent>();
    }

    public Dependency addParent(Child child, Parent parent)
    {
      if (parents_.Keys.Contains(child))
      {
        if (!parents_[child].Contains(parent))
        {
          parents_[child].Add(parent);
        }
      }
      else
      {
        List<Parent> parents = new List<Parent>();
        parents.Add(parent);
        parents_[child] = parents;
      }
      return this;
    }
    public void showParents()
    {
      foreach (Child child in parents_.Keys)
      {
        Console.Write("\n  Child {0} has Parents:", child);
        foreach (Parent parent in parents_[child])
        {
          Console.Write("\n    {0}", parent);
        }
      }
      Console.Write("\n");
    }

  }
  ///////////////////////////////////////////////////////////////////
  // VersionChain class 
  // - helps manage versioning in repository

  public class VersionChain
  {
    private Dictionary<Package, List<VersionNum>> chain_ = new Dictionary<Package, List<VersionNum>>();

    /*----< adds version number for package to chains List >-------*/
    /*
     *  - will throw exception if version is not higher than all versions in list
     *  - You can turn that off by setting mustBeOrdered to false.
     *    That allows you to build version chain by reading directories and then sorting.
     */
    public void addVersion(Package package, VersionNum ver, bool mustBeOrdered = true)
    {
      if(chain_.Keys.Contains(package))
      {
        int latestVer = getLatestVersion(package);
        if (ver <= latestVer && mustBeOrdered)
        {
          string msg = String.Format("attempt to add version {0} in package {1} failed, version out of order", ver, package);
          Exception ex = new Exception(msg);
          throw ex;
        }
        if (!chain_[package].Contains(ver))
        {
          chain_[package].Add(ver);
        }
      }
      else
      {
        List<VersionNum> versions = new List<VersionNum>();
        versions.Add(ver);
        chain_[package] = versions;
      }
    }

    public void sort(Package package)
    {
      if(chain_.Keys.Contains(package))
        chain_[package].Sort();
    }

    public void sort()
    {
      foreach(Package pkg in chain_.Keys)
      {
        sort(pkg);
      }
    }

    List<VersionNum> getVersions(Package package)
    {
      if (chain_.Keys.Contains(package))
        return chain_[package];
      else
        return new List<VersionNum>();
    }
    /*----< return largest version number >------------------------*/
    /*
     *  assumes version list is ordered
     */
    public VersionNum getLatestVersion(Package package)
    {
      if (chain_.Keys.Contains(package))
      {
        List<VersionNum> versions = chain_[package];
        int count = versions.Count;
        if (count == 0)
        {
          return 0;
        }
        else
        {
          return versions[count - 1];
        }
      }
      return 0;
    }

    public void show()
    {
      foreach(Package package in chain_.Keys)
      {
        Console.Write("\n  Package {0} has versions: ", package);
        foreach(VersionNum ver in chain_[package])
        {
          Console.Write("{0} ", ver);
        }
      }
      Console.Write("\n");
    }
  }

#if (TEST_RELATIONSHIPS)

  class TestRelationships
  {
    static void Main(string[] args)
    {
      TestUtilities.title("Testing Relationships", '=');
      Dependency deps = new Dependency();
      deps.addChild("A", "B").addChild("A", "E");
      deps.addChild("B", "C").addChild("B", "D");
      deps.addChild("F", "G");
      deps.addChild("H", "C").addChild("I", "G");
      deps.showChildren();

      deps.addParent("B", "A").addParent("E", "A");
      deps.addParent("C", "B").addParent("D", "B");
      deps.addParent("G", "F");
      deps.addParent("C", "H").addParent("G", "I");
      deps.showParents();

      VersionChain vers = new VersionChain();

      for(int i=1; i<6; ++i)
      {
        vers.addVersion("test1", i);
      }
      for(int i=1; i<3; ++i)
      {
        vers.addVersion("test2", i);
      }

      vers.show();

      try
      {
        vers.addVersion("test1", 2);
      }
      catch (Exception ex)
      {
        Console.Write("  --- {0} ---\n", ex.Message);
      }

      try
      {
        vers.addVersion("test1", -1);
      }
      catch (Exception ex)
      {
        Console.Write("  --- {0} ---", ex.Message);
      }
      vers.show();

      try
      {
        vers.addVersion("test1", 17);
      }
      catch (Exception ex)
      {
        Console.Write("  --- {0} ---", ex.Message);
      }
      vers.show();

      VersionNum[] versions = { 17, 43, 2, 5, 8 };

      foreach (VersionNum num in versions)
      {
        vers.addVersion("test3", num, false);
      }
      vers.show();
      vers.sort("test3");
      vers.show();

      Console.Write("\n\n");
    }
  }
#endif
}
