// unit tests for parsing DDL class
// author: Christophe VG <contact@christophe.vg>

using System;
using NUnit.Framework;

namespace DDL_Parser {

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

      if(this.ddl.errors.Count > 0) {
        foreach(var error in this.ddl.errors) {
          Console.WriteLine(error);
        }
      }
      Assert.AreEqual(    0, this.ddl.errors.Count             );
      Assert.AreEqual(    1, this.ddl.statements.Count         );
      Assert.AreEqual( text, this.ddl.statements[0].ToString() );
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
        "tablespace(TEST001 in TEST002)" +
        "{USING_STOGROUP=TEST003,PARAM1=param1,PARAM2=param2}"
      );
    }

    [Test]
    public void testCreateTableStatement() {
      this.parseAndCompare(
        @"     CREATE TABLE
         TEST.001  (
           F1 T1(123)  NOT NULL FOR SBCS DATA,
           F2 T2(4, 5) NOT NULL,
           F3 T3       FOR SBCS DATA,
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
        "[F1:T1(123){NULL=False,FOR=SBCS_DATA},F2:T2(4,5){NULL=False}," +
        "F3:T3{FOR=SBCS_DATA},F4:T4{}]" +
        "<PK_TEST{PRIMARY_KEY=F4}>" +
        "{DATA_CAPTURE=CHANGES,PARAM1=param1,PARAM2=False,PARAM3=False}"
      );
    }

    [Test]
    public void testCreateIndexStatement() {
      this.parseAndCompare(
        @"     CREATE UNIQUE
          INDEX Index1
         ON Table1
          (
           Field1 ASC
          )
           USING STOGROUP Group1
           PARAM1 param1;",
        "index(Index1 on Table1[Field1 ASC])" +
        "{USING_STOGROUP=Group1,PARAM1=param1,UNIQUE=True}"
      );
    }

    [Test]
    public void testCreateViewStatement() {
      this.parseAndCompare(
        @"     CREATE
          VIEW Index1
            AS SELECT * FROM Table1;",
        "view(Index1)[SELECT * FROM Table1]"
      );
    }

    [Test]
    public void testSetParameterStatement() {
      this.parseAndCompare(
        @"     SET
          VARIABLE =
            ""SOME VALUE"";",
        "param(VARIABLE=\"SOME VALUE\")"
      );
    }

    [Test]
    public void testSetParameterNameWithSpaceStatement() {
      this.parseAndCompare(
        @"     SET
          VARIABLE NAME =
            ""SOME VALUE"";",
        "param(VARIABLE NAME=\"SOME VALUE\")"
      );
    }

    [Test]
    public void testAlterTableAddForeignKeyConstraint() {
      this.parseAndCompare(
        @"     ALTER TABLE
         Table1
         ADD
           CONSTRAINT FK_Name
           FOREIGN KEY
           (
            Field1
            ,Field2
           )
           REFERENCES
           FK_Table
           (
            FK_Field1
            ,FK_Field2
           )
           ON DELETE RESTRICT
           ENFORCED
        ;",
        "alter(Table1:FK_Name ON FK_Table{ON_DELETE=RESTRICT,ENFORCED=True,KEYS=Field1,Field2,REFERENCES=FK_Field1,FK_Field2})"
      );
    }

    [Test]
    public void testAlterTableAddForeignKeyConstraintWithSetNullParameter() {
      this.parseAndCompare(
        @"     ALTER TABLE
         Table1
         ADD
           CONSTRAINT FK_Name
           FOREIGN KEY
           (
            Field1
            ,Field2
           )
           REFERENCES
           FK_Table
           (
            FK_Field1
            ,FK_Field2
           )
           ON DELETE SET NULL
           ENFORCED
        ;",
        "alter(Table1:FK_Name ON FK_Table{ON_DELETE=SET_NULL,ENFORCED=True,KEYS=Field1,Field2,REFERENCES=FK_Field1,FK_Field2})"
      );
    }

    [Test]
    public void testUnknownStatement() {
      string unknown = "UNKNOWN unknown1 ON Table1 WITH DEFAULT CREATE";
      string ddl = @"     SET
          VARIABLE =
            ""SOME VALUE"";
      CREATE "+ unknown +@";
      -- comment test
      ";

      this.ddl.Parse(ddl);

      Assert.AreEqual(
        2,
        this.ddl.statements.Count
      );
      Assert.AreEqual(
        "param(VARIABLE=\"SOME VALUE\")",
        this.ddl.statements[0].ToString()
      );
      Assert.AreEqual(
        "comment(comment test)",
        this.ddl.statements[1].ToString()
      );

      Assert.AreEqual(
        1,
        this.ddl.errors.Count
      );
      Assert.AreEqual(
        "-->" + unknown,
        this.ddl.errors[0].Message
      );
    }

    [Test]
    public void testColumnConstraint() {
      this.parseAndCompare(
        @"
          CREATE TABLE Test1
          (
            Field1 Type(1) NOT NULL
            CONSTRAINT Field1 CHECK (
              Field1 = '0' OR Field1 = '1'
            )
          ) IN Space1;
        ",
        "table(Test1 in Space1)" +
        "[Field1:Type(1){NULL=False}"+
        "<check:Field1:=Field1 = '0' OR Field1 = '1'>]"
      );
    }

    [Test]
    public void testWithDefaultWithValue() {
      this.parseAndCompare(
        @"CREATE TABLE Test1 (
            Field1 Type(1) WITH DEFAULT '0'
          ) IN Space1;",
        "table(Test1 in Space1)[Field1:Type(1){DEFAULT=0}]"
      );
    }
  }
}
