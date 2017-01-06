using System;
using System.Collections.Generic;

using NUnit.Framework;

[TestFixture]
public class StatementsTest {

  [Test]
  public void testComment() {
    Assert.AreEqual(new Comment() { Body = "123" }.ToString(), "comment(123)");
  }

  [Test]
  public void testCreateDatabaseStatement() {
    Assert.AreEqual(new CreateDatabaseStatement() {
      Name       = "123",
      Parameters = new Dictionary<string,string>() {
        { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
      }
    }.ToString(), "database(123){p1=v1,p2=v2,p3=v3}");
  }

  [Test]
  public void testCreateTablespaceStatement() {
    Assert.AreEqual(new CreateTablespaceStatement() {
      Name         = "123",
      Database     = "456",
      StorageGroup = "789",
      Parameters   = new Dictionary<string,string>() {
        { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
      }
    }.ToString(), "tablespace(123 in 456 using 789){p1=v1,p2=v2,p3=v3}");
  }

  [Test]
  public void testField() {
    Assert.AreEqual(new Field() {
      Name       = "Field1",
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
      Name        = "Table1",
      Database    = "Database1",
      Fields      = new List<Field>() {
        new Field() {
          Name       = "Field1",
          Type       = "Type1",
          Parameters = new Dictionary<string,string>() {
            { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
          }
        },
        new Field() {
          Name       = "Field2",
          Type       = "Type2",
          Parameters = new Dictionary<string,string>() {
            { "p4", "v4" }
          }
        }
      },
      Constraints = new List<Constraint>() {
        new Constraint() {
          Name       = "Constraint1",
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
      Name = "Index1",
      Table = "Table1",
      Fields = "F1,F2,F3",
      Parameters = new Dictionary<string,string>() {
        { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
      }
    }.ToString(),
    "index(Index1 on Table1[F1,F2,F3]){p1=v1,p2=v2,p3=v3}");
  }
}
