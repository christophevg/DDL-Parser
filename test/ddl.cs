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
    this.ddl.Parse(ddl);
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
            PARAM2 param2;
",
      "database(TEST001){PARAM1=param1,PARAM2=param2}"
    );
  }

  [Test]
  public void testCreateTablespaceStatement() {
    this.parseAndCompare(
      @"     CREATE TABLESPACE
       TEST001
         IN TEST002
           USING STOGROUP TEST003
          PARAM1 param1
            PARAM2 param2;
",
      "tablespace(TEST001 in TEST002){USING_STOGROUP=TEST003,PARAM1=param1,PARAM2=param2}"
    );
  }

  [Test]
  public void testCreateTableStatement() {
    this.parseAndCompare(
      @"     CREATE TABLE
       TEST.001  (
         F1 T1(123) NOT NULL FOR SBCS DATA,
         F2 T2      NOT NULL,
         F3 T3      FOR SBCS DATA,
         F4 T4,
         CONSTRAINT PK_TEST PRIMARY KEY ( F4 )
       )
       IN TEST.002
            DATA CAPTURE CHANGES
            PARAM1 param1
            NOT PARAM2
            PARAM3 NO;
",
      "table(TEST.001 in TEST.002)" +
      "[F1:T1(123){NULL=False,FOR=SBCS_DATA},F2:T2{NULL=False},F3:T3{FOR=SBCS_DATA},F4:T4{}]" +
      "<PK_TEST{PRIMARY_KEY=F4}>" +
      "{DATA_CAPTURE=CHANGES,PARAM1=param1,PARAM2=False,PARAM3=False}"
    );
  }
}
