using System;
using NUnit.Framework;

[TestFixture]
public class DDLTest {
  DDL ddl;

  [SetUp]
  public void SetUp() {
    this.ddl = new DDL();
  }

  [TearDown]
  public void TearDown() {
    this.ddl = null;
  }

  private void parseAndCompare(string ddl, string text) {
    this.ddl.parse(ddl);
    Assert.AreEqual(this.ddl.Length, 1);
    Assert.AreEqual(this.ddl[0].ToString(), text);
  }

  [Test]
  public void testParseComment() {
    this.parseAndCompare(
      "-- comment test\n",
      "comment(comment test)"
    );
  }

  [Test]
  public void testCreateDatabaseStatement() {
    this.parseAndCompare(
      @"     CREATE DATABASE
       TEST001
          PARAM1 param1
            PARAM2 param2;",
      "database(TEST001){PARAM1=param1,PARAM2=param2}"
    );
  }
}
