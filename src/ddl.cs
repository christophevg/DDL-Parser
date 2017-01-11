// DDL - parsing DDL class representation, Linq queryable using Statements
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Diagnostics;

namespace DDL_Parser {

  public class DDL {
  
    // internal representation of a parsable DDL-text
    private Parsable ddl;
  
    // resulting statements from parsing
    public List<Statement> statements = new List<Statement>  {};

    // information about parse errors
    public List<ParseException> errors = new List<ParseException> {};

    // initiates parsing of DLL into statements
    public bool Parse(String ddl) {
      this.ddl = new Parsable(ddl);

      while( this.ddl.Length > 0 ) {
        this.ShowParsingInfo();

        try {

          if(                         this.ParseComment()   ) { continue; }
          if( this.ddl.Length == 0 || this.ParseStatement() ) { continue; }

          // failed to parse a comment or a statement, skip up to end of statement
          // to skip garbage. comments are no problem
          throw new ParseException(
            "-->" + this.ddl.ConsumeUpTo(";", include:true)
          );

        } catch(ParseException e) {
          // track exception and continue parsing
          this.errors.Add(e);
        }
      }

      return true;
    }
  
    // COMMENTS

    private bool ParseComment() {
      if( this.ddl.TryConsume("--") ) {
        Comment stmt = new Comment() { Body = this.ddl.ConsumeUpTo("\n") };
        // this.ddl.Consume("\n");
        this.statements.Add(stmt);
        return true;
      }
      return false;
    }
  
    // STATEMENTS

    private bool ParseStatement() {
      return this.ParseCreateStatement()
          || this.ParseAlterStatement()
          || this.ParseSetStatement();
    }

    private bool ParseCreateStatement() {
      if( ! this.ddl.TryConsume("CREATE ")
       && ! this.ddl.TryConsume("CREATE\n") )
      {
        return false;
      }
      return this.ParseCreateDatabaseStatement()
          || this.ParseCreateTablespaceStatement()
          || this.ParseCreateTableStatement()
          || this.ParseCreateIndexStatement()
          || this.ParseCreateViewStatement();
    }

    private bool ParseCreateDatabaseStatement() {
      if( ! this.ddl.TryConsume("DATABASE ") &&
          ! this.ddl.TryConsume("DATABASE\n") )
      {
        return false;
      }
      string name = this.ddl.ConsumeId();
      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary();
      CreateDatabaseStatement stmt = new CreateDatabaseStatement() {
        Name       = name,
        Parameters = parameters
      };
      this.statements.Add(stmt);
      return true;
    }

    private bool ParseCreateTablespaceStatement() {
      if( ! this.ddl.TryConsume("TABLESPACE ")
       && ! this.ddl.TryConsume("TABLESPACE\n") )
      {
        return false;
      }
      string name     = this.ddl.ConsumeId();
                        this.ddl.Consume("IN");
      string database = this.ddl.ConsumeId();

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

    // the currently being parsed field and constraint lists
    private List<Field>      fields      = new List<Field>();
    private List<Constraint> constraints = new List<Constraint>();

    private bool ParseCreateTableStatement() {
      if( ! this.ddl.TryConsume("TABLE ")
       && ! this.ddl.TryConsume("TABLE\n") )
      {
        return false;
      }

      string name     = this.ddl.ConsumeId();
                        this.ddl.Consume("(");

      this.fields      = new List<Field>();
      this.constraints = new List<Constraint>();
    
      while( this.ParseConstraint() || this.ParseField() ) {}

                        this.ddl.Consume(")");
                        this.ddl.Consume("IN");

      string database = this.ddl.ConsumeId();

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

    private bool ParseConstraint() {
      if( ! this.ddl.TryConsume("CONSTRAINT ")
       && ! this.ddl.TryConsume("CONSTRAINT\n") )
      {
        return false;
      }

      string name = this.ddl.ConsumeId();

      return this.ParsePrimaryKeyConstraint(name)
          || this.ParseForeignKeyConstraint(name);
    }

    private bool ParsePrimaryKeyConstraint(string name) {
      if( ! this.ddl.TryConsume("PRIMARY KEY ")
       && ! this.ddl.TryConsume("PRIMARY KEY\n") ) {
        return false;
      }
    
                      this.ddl.Consume("(");
      string fields = this.ddl.ConsumeUpTo(")");
                      this.ddl.Consume(")");

      this.constraints.Add(new Constraint() {
        Name       = name,
        Parameters = new Dictionary<string,string>() {
          { "PRIMARY_KEY", fields }
        }
      });
      return true;
    }

    private bool ParseForeignKeyConstraint(string name) {
      if( ! this.ddl.TryConsume("FOREIGN KEY ")
       && ! this.ddl.TryConsume("FOREIGN KEY\n") ) {
        return false;
      }
                          this.ddl.Consume("(");
      string keys       = this.ddl.ConsumeUpTo(")").Replace(" ", "");
                          this.ddl.Consume(")");

                          this.ddl.Consume("REFERENCES");

      string table      = this.ddl.ConsumeId();
                          this.ddl.Consume("(");
      string references = this.ddl.ConsumeUpTo(")").Replace(" ", "");
                          this.ddl.Consume(")");

      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary(
        merge:   new List<string>() { "ON DELETE", "SET NULL" },
        options: new List<string>() { "ENFORCED"              }
      );

      parameters.Add("KEYS",       keys      );
      parameters.Add("TABLE",      table     );
      parameters.Add("REFERENCES", references);

      this.constraints.Add(new Constraint() {
        Name       = name,
        Parameters = parameters
      });
      return true;
    }

    private bool ParseField() {
      string name;
      try {
        name = this.ddl.ConsumeId();
      } catch(ParseException) {
        // no ID, here means not a Field (anymore)
        return false;
      }

      string type = this.ParseType();

      Dictionary<string,string> parameters = this.ddl.ConsumeDictionary(
        upTo: ",", merge: new List<string> { "SBCS DATA" }
      );

      this.fields.Add(new Field() {
        Name       = name,
        Type       = type,
        Parameters = parameters
      });
      return true;
    }

    private string ParseType() {
      string type = this.ddl.ConsumeId();
      // optional length
      if( ! this.ddl.TryConsume("(") ) { return type; }
      // optional length is present
      type += "(" + this.ddl.ConsumeNumber();
      // optional second part length
      if( ! this.ddl.TryConsume(",") ) {
        this.ddl.Consume(")");
        return type + ")";
      }
      // second part to type number
      type += "," + this.ddl.ConsumeNumber();
                    this.ddl.Consume(")");
      type += ")";

      return type;
    }

    private bool ParseCreateIndexStatement() {
      // unique is optional
      bool unique = this.ddl.TryConsume("UNIQUE ")
                 || this.ddl.TryConsume("UNIQUE\n");

      if( ! this.ddl.TryConsume("INDEX ")
       && ! this.ddl.TryConsume("INDEX\n") )
      {
        if( unique ) {
          throw new ParseException(
            "could not consume 'INDEX' at " + this.ddl.Context
          );
        }
        // without unique it might simply be some other create statement
        return false;
      }
      string name   = this.ddl.ConsumeId();
                      this.ddl.Consume("ON");
      string table  = this.ddl.ConsumeId();
                      this.ddl.Consume("(");
      string fields = this.ddl.ConsumeUpTo(")");
                      this.ddl.Consume(")");

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
      if( ! this.ddl.TryConsume("VIEW ")
       && ! this.ddl.TryConsume("VIEW\n") )
      {
        return false;
      }
      string name       = this.ddl.ConsumeId();
                          this.ddl.Consume("AS");
      string definition = this.ddl.ConsumeUpTo(";");
                          this.ddl.Consume(";");

      this.statements.Add( new CreateViewStatement() {
        Name = name,
        Definition = definition
      });
      return true;
    }

    // SET STATEMENTS

    private bool ParseSetStatement() {
      if( ! this.ddl.TryConsume("SET ") 
       && ! this.ddl.TryConsume("SET\n") )
      {
        return false;
      }

      return this.ParseSetParameterStatement();
    }
  
    private bool ParseSetParameterStatement() {
      string name  = this.ddl.ConsumeUpTo("=");
                     this.ddl.Consume("=");
      string value = this.ddl.ConsumeUpTo(";", include:true);
      this.statements.Add(new SetParameterStatement() {
        Variable = name,
        Value = value
      });
      return true;
    }

    // ALTER STATEMENTS

    private bool ParseAlterStatement() {
      if( ! this.ddl.TryConsume("ALTER ")
       && ! this.ddl.TryConsume("ALTER\n") )
      {
        return false;
      }

      return this.ParseAlterTableStatement();
    }
  
    private bool ParseAlterTableStatement() {
      if( ! this.ddl.TryConsume("TABLE ")
       && ! this.ddl.TryConsume("TABLE\n") )
      {
        return false;
      }
      string name = this.ddl.ConsumeId();

      return this.ParseAlterTableAddStatement(name);
    }

    private bool ParseAlterTableAddStatement(string name) {
      if( ! this.ddl.TryConsume("ADD ")
       && ! this.ddl.TryConsume("ADD\n") )
      {
        return false;
      }
      this.constraints = new List<Constraint>();

      if( this.ParseConstraint() ) {
        this.statements.Add(new AlterTableAddConstraintStatement() {
          Name       = name,
          Constraint = this.constraints[0]
        });
      }
      return true;
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
}
