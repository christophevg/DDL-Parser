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
      Parameters = new Dictionary<string,string>() {
        { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
      }
    }.ToString(), "tablespace(123 in 456 using 789){p1=v1,p2=v2,p3=v3}");
  }

}
