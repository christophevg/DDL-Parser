// unit tests for Statement hierarchy classes
// author: Christophe VG <contact@christophe.vg>

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace DDL_Parser {

  [TestFixture]
  public class StatementsTest {

    [Test]
    public void testComment() {
      Assert.AreEqual(new Comment() { Body = "123" }.ToString(), "comment(123)");
    }

    [Test]
    public void testCreateDatabaseStatement() {
      Assert.AreEqual(new CreateDatabaseStatement() {
        Name       = new QualifiedName() { Name = "123" },
        Parameters = new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
      }.ToString(), "database(123){p1=v1,p2=v2,p3=v3}");
    }

    [Test]
    public void testCreateTablespaceStatement() {
      Assert.AreEqual(new CreateTablespaceStatement() {
        Name         = new QualifiedName() { Name = "123" },
        Database     = new QualifiedName() { Name = "456" },
        Parameters   = new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
      }.ToString(), "tablespace(123 in 456){p1=v1,p2=v2,p3=v3}");
    }

    [Test]
    public void testField() {
      Assert.AreEqual(new Field() {
        Name       = new QualifiedName() { Name = "Field1" },
        Type       = "Type1",
        Parameters = new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
      }.ToString(),
        "Field1:Type1{p1=v1,p2=v2,p3=v3}"
      );
    }

    [Test]
    public void testCreateTableStatement() {
      Assert.AreEqual(new CreateTableStatement() {
        Name        = new QualifiedName() { Name = "Table1"    },
        Database    = new QualifiedName() { Name = "Database1" },
        Fields      = new List<Field>() {
          new Field() {
            Name       = new QualifiedName() { Name = "Field1" },
            Type       = "Type1",
            Parameters = new Dictionary<string,string>() {
              { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
            }
          },
          new Field() {
            Name       = new QualifiedName() { Name = "Field2" },
            Type       = "Type2",
            Parameters = new Dictionary<string,string>() {
              { "p4", "v4" }
            }
          }
        },
        Constraints = new List<Constraint>() {
          new Constraint() {
            Name       = new QualifiedName() { Name = "Constraint1" },
            Parameters = new Dictionary<string,string>() {
              { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
            }
          }
        },
        Parameters  = new Dictionary<string,string>() {
          { "p1", "v1" }
        }
      }.ToString(),
        "table(Table1 in Database1)[Field1:Type1{p1=v1,p2=v2,p3=v3},Field2:Type2{p4=v4}]<Constraint1{p1=v1,p2=v2,p3=v3}>{p1=v1}"
      );
    }

    [Test]
    public void testCreateIndexStatement() {
      Assert.AreEqual(new CreateIndexStatement() {
        Name       = new QualifiedName() { Name = "Index1" },
        Table      = new QualifiedName() { Name = "Table1" },
        Fields     = "F1,F2,F3",
        Parameters = new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
      }.ToString(),
        "index(Index1 on Table1[F1,F2,F3]){p1=v1,p2=v2,p3=v3}"
      );
    }

    [Test]
    public void testCreateIndexStatementSimpleName() {
      var index = new CreateIndexStatement() {
        Name       = new QualifiedName() { Name = "Index1" },
        Table      = new QualifiedName() { Name = "Table1" },
        Fields     = "F1,F2,F3",
        Parameters = new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
      };
      Assert.AreEqual( "Index1",      index.SimpleName );
      index.SimpleName = "SimpleName1";
      Assert.AreEqual( "SimpleName1", index.SimpleName );
      // SimpleName == QualifiedName.Name
      Assert.AreEqual( "SimpleName1", index.Name.Name );
    }
  
    [Test]
    public void testCreateViewStatement() {
      Assert.AreEqual(new CreateViewStatement() {
        Name       = new QualifiedName() { Name = "View1" },
        Definition = "SELECT * FROM Table1"
      }.ToString(),
        "view(View1)[SELECT * FROM Table1]"
      );
    }

    [Test]
    public void testSetParameterStatement() {
      Assert.AreEqual(new SetParameterStatement() {
        Variable = "Variable1",
        Value    = "Value1"
      }.ToString(),
        "param(Variable1=Value1)"
      );
    }

    [Test]
    public void testFullyQualifiedName() {
      Assert.AreEqual(new QualifiedName() {
        Scope = "Scope1",
        Name  = "Name1"
      }.ToString(),
        "Scope1.Name1"
      );
    }

    [Test]
    public void testSimpleQualifiedName() {
      Assert.AreEqual(new QualifiedName() {
        Name  = "Name1"
      }.ToString(),
        "Name1"
      );
    }

    [Test]
    public void testCheckConstraint() {
      string rules = "Field1 = '0' OR Field1 = '1'";
      Assert.AreEqual(
        "check:Field1:="+rules,
        new CheckConstraint() {
          Field = new QualifiedName() { Name  = "Field1" },
          Rules = rules
        }.ToString()
      );
    }
  }
}
