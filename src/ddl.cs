// DDL - parsing DDL class representation, Linq queryable using Statements
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

public class DDL {
  
  // internal representation of a parsable DDL-text
  private Parsable ddl;
  
  // resulting statements from parsing
  private List<Statement> statements = new List<Statement>  {};

  // length of DDL is the number of statements that were parsed
  public int Length {
    get { return this.statements.Count; }
  }

  // Indexer to retrieve a given (parsed) statement
  public Statement this[int key] {
      get { return this.statements[key]; }
  }

  // initiates parsing of DLL into statements
  public bool Parse(String ddl) {
    this.ddl = new Parsable(ddl);

    while( this.ddl.Length > 0 ) {
      this.ShowParsingInfo();

      if(                         this.ParseComment()   ) { continue; }
      if( this.ddl.Length == 0 || this.ParseStatement() ) { continue; }
      if( this.ddl.Length  > 0) { return this.NotifyParseFailure();   }
    }

    return true;
  }
  
  // TODO expose differently
  public void Dump() {
    var statements = from statement in this.statements
                     where !(statement is Comment)
                     select statement;
                       
    foreach(var statement in statements) {
      Console.WriteLine(statement);
    }
  }

  // internal parsing steps

  private bool ParseComment() {
    if( this.ddl.Consume("--") ) {
      Comment stmt = new Comment() { Body = this.ddl.ConsumeUpTo("\n") };
      this.ddl.Consume("\n");
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool ParseStatement() {
    return this.ParseCreateStatement()
        || this.ParseAlterStatement()
        || this.ParseSetStatement()

        || this.NotifyParseStatementFailure();
  }

  private bool ParseCreateStatement() {
    if( this.ddl.Consume("CREATE ") || this.ddl.Consume("CREATE\n") ) {
      return this.ParseCreateDatabaseStatement()
          || this.ParseCreateTablespaceStatement()
          || this.ParseCreateTableStatement()
          || this.ParseCreateIndexStatement()
          || this.ParseCreateViewStatement()

          || this.NotifyParseCreateStatementFailure();
    }
    return false;
  }

  private bool ParseCreateDatabaseStatement() {
    if( this.ddl.Consume("DATABASE ") || this.ddl.Consume("DATABASE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name == null ) { return false; }
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary();
      CreateDatabaseStatement stmt = new CreateDatabaseStatement() {
        Name       = name,
        Parameters = parameters
      };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool ParseCreateTablespaceStatement() {
    if( this.ddl.Consume("TABLESPACE ") || this.ddl.Consume("TABLESPACE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name == null                         ) { return false; }
      if( ! this.ddl.Consume("IN")             ) { return false; }
      string database = this.ddl.ConsumeId();
      if( database == null                     ) { return false; }
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary(
        merge:   new List<string>() { "USING STOGROUP" },
        options: new List<string>() { "LOGGED"         }      
      );
      CreateTablespaceStatement stmt = 
        new CreateTablespaceStatement() {
          Name         = name,
          Database     = database,
          Parameters   = parameters
        };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  // the currently being parsed field and constraint lists
  private List<Field>      fields      = new List<Field>();
  private List<Constraint> constraints = new List<Constraint>();

  private bool ParseCreateTableStatement() {
    if( this.ddl.Consume("TABLE ") || this.ddl.Consume("TABLE\n") ) {
      string name = this.ddl.ConsumeId();
      if( name == null             ) { return false; }

      if( ! this.ddl.Consume("(")  ) { return false; }

      this.fields      = new List<Field>();
      this.constraints = new List<Constraint>();
      
      while( this.ParseConstraint() || this.ParseField() ) {}

      if( ! this.ddl.Consume(")")  ) { return false; }

      if( ! this.ddl.Consume("IN") ) { return false; }
      string database = this.ddl.ConsumeId();
      if( database == null         ) { return false; }
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary(
        merge:   new List<string>() { "DATA CAPTURE" }
      );
      CreateTableStatement stmt = 
        new CreateTableStatement() {
          Name        = name,
          Fields      = this.fields,
          Constraints = this.constraints,
          Database    = database,
          Parameters  = parameters
        };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool ParseField() {
    string name = this.ddl.ConsumeId();
    if( name == null ) { return false; }
    string type = this.ParseType();
    if( type == null ) { return false; }
    Dictionary<string,string> parameters =
      this.ddl.ConsumeDictionary(upTo: ",", merge: new List<string> { "SBCS DATA" });
    this.fields.Add(new Field() {
      Name       = name,
      Type       = type,
      Parameters = parameters
    });
    return true;
  }

  private bool ParseConstraint() {
    if( ! this.ddl.Consume("CONSTRAINT") ) { return false; }
    string name = this.ddl.ConsumeId();
    if( name == null )                     { return false; }

    return this.ParsePrimaryKeyConstraint(name);
  }

  private bool ParsePrimaryKeyConstraint(string name) {
    if( this.ddl.Consume("PRIMARY KEY") ) {
      if( ! this.ddl.Consume("(") ) { return false; }
      string fields = this.ddl.ConsumeUpTo(")");
      if( ! this.ddl.Consume(")") ) { return false; }
      this.constraints.Add(new Constraint() {
        Name       = name,
        Parameters = new Dictionary<string,string>() {
          { "PRIMARY_KEY", fields }
        }
      });
      return true;
    }
    return false;
  }

  private string ParseType() {
    string type = this.ddl.ConsumeId();
    if( type == null )              { return null; }
    // optional parameter
    if( this.ddl.Consume("(") ) {
      type += "(" + this.ddl.ConsumeNumber() + ")";
      if( ! this.ddl.Consume(")") ) { return null; }
    }
    return type;
  }

  private bool ParseCreateIndexStatement() {
    bool unique = this.ddl.Consume("UNIQUE");
    if( ! this.ddl.Consume("INDEX") ) { return false; }
    string name = this.ddl.ConsumeId();
    if( name == null )                { return false; }
    if( ! this.ddl.Consume("ON") )    { return false; }
    string table = this.ddl.ConsumeId();
    if( ! this.ddl.Consume("(") )    { return false; }
    string fields = this.ddl.ConsumeUpTo(")");
    if( ! this.ddl.Consume(")") )    { return false; }
    Dictionary<string,string> parameters = this.ddl.ConsumeDictionary(
      merge:   new List<string>() { "USING STOGROUP" },
      options: new List<string>() { "CLUSTER"        }
    );
    parameters.Add("UNIQUE", unique.ToString());
    this.statements.Add( new CreateIndexStatement() {
      Name = name,
      Table = table,
      Fields = fields,
      Parameters = parameters
    });
    return true;
  }

  private bool ParseCreateViewStatement() {
    if( ! this.ddl.Consume("VIEW") ) { return false; }
    string name = this.ddl.ConsumeId();
    if( name == null )               { return false; }
    if( ! this.ddl.Consume("AS") ) { return false; }
    string definition = this.ddl.ConsumeUpTo(";");
    if( ! this.ddl.Consume(";") )    { return false; }
    this.statements.Add( new CreateViewStatement() {
      Name = name,
      Definition = definition
    });
    return true;
  }

  private bool ParseAlterStatement() {
    if( this.ddl.Consume("ALTER ") || this.ddl.Consume("ALTER\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new AlterStatement() { Body = statement });
      return true;
    }
    return false;
  }

  private bool ParseSetStatement() {
    if( this.ddl.Consume("SET ") || this.ddl.Consume("SET\n") ) {
      string statement = this.ddl.ConsumeUpTo(";");
      this.ddl.Consume(";");
      this.statements.Add(new SetStatement() { Body = statement });
      return true;
    }
    return false;
  }
  
  private bool NotifyParseCreateStatementFailure() {
    Console.WriteLine("Failed to parse Create Statement!");
    return false;
  }

  private bool NotifyParseStatementFailure() {
    Console.WriteLine("Failed to parse Statement!");
    return false;
  }
  
  private bool NotifyParseFailure() {
    Console.WriteLine("Failed to parse DDL! Aborting..");
    Console.WriteLine(this.ddl.Peek(75));
    return false;
  }

  [ConditionalAttribute("DEBUG")]
  private void ShowParsingInfo() {
    Console.WriteLine(new String('-', 75));
    Console.WriteLine(this.ddl.Length + " bytes remaining:");
    Console.WriteLine(this.ddl.Peek(50) + " [...]");
    Console.WriteLine(new String('-', 75));
  }

  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.WriteLine("!!! " + msg);
  }
}
