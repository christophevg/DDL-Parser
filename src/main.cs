using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

public class DDL {

  private abstract class Statement {
    public string Body { get; set; }
  }
  
  private class Comment : Statement {
    public override string ToString() {
      return "comment   " + this.Body;
    }
  }

  private class CreateStatement : Statement {
    public override string ToString() {
      return "create    " + this.Body;
    }
  }

  private class CreateDatabaseStatement : Statement {
    public string Name { get; set; }
    public Dictionary<string,string> Parameters { get; set; } =
      new Dictionary<string,string>();
    public override string ToString() {
      return "database(" + this.Name + ")" + "{" +
        string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  private class CreateTablespaceStatement : Statement {
    public string Name { get; set; }
    public string Database { get; set; }
    public string StorageGroup { get; set; }
    public Dictionary<string,string> Parameters { get; set; } =
      new Dictionary<string,string>();
    public override string ToString() {
      return "tablespace(" + this.Name +
        " in " + this.Database +
        " using " + this.StorageGroup + ")" + "{" +
        string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }


  private class AlterStatement : Statement {
    public override string ToString() {
      return "alter     " + this.Body;
    }
  }

  private class SetStatement : Statement {
    public override string ToString() {
      return "set       " + this.Body;
    }
  }

  private List<Statement> statements = new List<Statement>  {};
  
  public DDL() {}
  
  /*

    A DDL basically consists of two types of "statements": comments and actual
    statements. The former is identified by a double leading dash (--), the
    latter are separated using semi-colons (;).

    This parser starts of by checking of its current input starts with a double
    dash, if so, the remainder of the line, up to a new-line character
    (optional carriage returns are discarted anyway), is wrapped in a Comment
    object. If it is a statement, everything up to the next semi-colon is
    passed to a Statement.

  */
  
  private class Parsable {

    // the parsable text
    private string text = "";

    // helper Regular Expressions for general clean-up
    private static Regex discardedCharacters    = new Regex( "\r"           );
    private static Regex repeatedWhitespace     = new Regex( "[ \t][ \t]*"  );

    private static Regex leadingWhitespace      = new Regex( "^\\s+"        );
    private static Regex trailingWhitespace     = new Regex( "\\s+$"        );
    private static Regex newlinesWithWhitespace = new Regex( "\n[ \t]*"     );

    private static Regex nextId                 = new Regex( "^([A-Z0-9]+)" );

    // trim leading and trailing whitespace
    // also replace new-line characters and optionally addition whitespace
    private string trim(string text) {
      text = Parsable.leadingWhitespace    .Replace(text, "");
      text = Parsable.trailingWhitespace   .Replace(text, "");
      text = Parsable.newlinesWithWhitespace.Replace(text, " ");
      return text;
    }
    
    public Parsable(string text) {
      this.text = text;
      this.text = Parsable.discardedCharacters  .Replace(this.text, ""  );
      this.text = Parsable.repeatedWhitespace   .Replace(this.text, " " );
    }

    public bool empty { get { return this.text.Length == 0; } }

    public void skipLeadingWhitespace() {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
    }
    
    public void skipEmptyLines() {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
    }

    public bool consume(string text) {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
      if(this.text.StartsWith(text)) {
        return this.consume(text.Length) == this.trim(text);
      }
      return false;
    }
  
    public string consumeUpTo(string text) {
      return this.consume(this.text.IndexOf(text));
    }

    private string consume(int length) {
      string consumed = this.text.Substring(0, length);
      this.text = this.text.Substring(length);
      return this.trim(consumed);
    }

    public string consumeId() {
      this.skipLeadingWhitespace();
      Match m = Parsable.nextId.Match(this.text);
      if(m.Success) {
        int length = m.Groups[0].Captures[0].ToString().Length;
        return this.consume(length);
      }
      return "";
    }

    public Dictionary<string,string> consumeDictionary() {
      string[] mappings = this.trim(this.consumeUpTo(";")).Split(' ');
      this.consume(";");
      Dictionary<string,string> dict = new Dictionary<string,string>();
      for(int i=0; i<mappings.Length; i+=2) {
        dict[mappings[i]] = mappings[i+1];
      }
      return dict;
    }

    public string peek(int length) {
      return this.text.Substring(0, length);
    }
    
    public int Length { get { return this.text.Length; } }
  }
    
  private Parsable ddl;
      
  public bool parse(String ddl) {
    this.ddl = new Parsable(ddl);

    while(! this.ddl.empty ) {
      this.showParsingInfo();

      if(! ( this.parseComment() || this.parseStatement() ) ) {
        return this.notifyParseFailure();
      }
    }

    return true;
  }

  private bool parseComment() {
    if( this.ddl.consume("--") ) {
      Comment stmt = new Comment() { Body = this.ddl.consumeUpTo("\n") };
      this.ddl.consume("\n");
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool parseStatement() {
    return this.parseCreateStatement()
        || this.parseAlterStatement()
        || this.parseSetStatement()

        || this.notifyParseStatementFailure();
  }

  private bool parseCreateStatement() {
    if( this.ddl.consume("CREATE ") || this.ddl.consume("CREATE\n") ) {
      return this.parseCreateDatabaseStatement()
          || this.parseCreateTablespaceStatement()
          || this.parseCreateTableStatement()
          || this.parseCreateIndexStatement()
          || this.parseCreateViewStatement()
          || this.notifyParseCreateStatementFailure();
    }
    return false;
  }

  private bool parseCreateDatabaseStatement() {
    if( this.ddl.consume("DATABASE ") || this.ddl.consume("DATABASE\n") ) {
      string name = this.ddl.consumeId();
      if( name.Length > 0 ) {
        Dictionary<string,string> parameters = this.ddl.consumeDictionary();
        CreateDatabaseStatement stmt = new CreateDatabaseStatement() {
          Name       = name,
          Parameters = parameters
        };
        this.statements.Add(stmt);
        return true;
      }
    }
    return false;
  }

  private bool parseCreateTablespaceStatement() {
    if( this.ddl.consume("TABLESPACE ") || this.ddl.consume("TABLESPACE\n") ) {
      string name = this.ddl.consumeId();
      if( ! (name.Length > 0)                  ) { return false; }
      if( ! this.ddl.consume("IN")             ) { return false; }
      string database = this.ddl.consumeId();
      if( ! (database.Length > 0)              ) { return false; }
      if( ! this.ddl.consume("USING STOGROUP") ) { return false; }
      string storageGroup = this.ddl.consumeId();
      if( ! (storageGroup.Length > 0)          ) { return false; }
      this.showParsingInfo();
      Dictionary<string,string> parameters = this.ddl.consumeDictionary();
      CreateTablespaceStatement stmt = 
        new CreateTablespaceStatement() {
          Name         = name,
          Database     = database,
          StorageGroup = storageGroup,
          Parameters   = parameters
        };
      this.statements.Add(stmt);
      return true;
    }
    return false;
  }

  private bool parseCreateTableStatement() {
    string statement = this.ddl.consumeUpTo(";");
    this.ddl.consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseCreateIndexStatement() {
    string statement = this.ddl.consumeUpTo(";");
    this.ddl.consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseCreateViewStatement() {
    string statement = this.ddl.consumeUpTo(";");
    this.ddl.consume(";");
    this.statements.Add(new CreateStatement() { Body = statement });
    return true;
  }

  private bool parseAlterStatement() {
    if( this.ddl.consume("ALTER ") || this.ddl.consume("ALTER\n") ) {
      string statement = this.ddl.consumeUpTo(";");
      this.ddl.consume(";");
      this.statements.Add(new AlterStatement() { Body = statement });
      return true;
    }
    return false;
  }

  private bool parseSetStatement() {
    if( this.ddl.consume("SET ") || this.ddl.consume("SET\n") ) {
      string statement = this.ddl.consumeUpTo(";");
      this.ddl.consume(";");
      this.statements.Add(new SetStatement() { Body = statement });
      return true;
    }
    return false;
  }
  
  private bool notifyParseCreateStatementFailure() {
    Console.WriteLine("Failed to parse Create Statement!");
    return false;
  }

  private bool notifyParseStatementFailure() {
    Console.WriteLine("Failed to parse Statement!");
    return false;
  }
  
  private bool notifyParseFailure() {
    Console.WriteLine("Failed to parse DDL! Aborting..");
    Console.WriteLine(this.ddl.peek(75));
    return false;
  }

  public void dump() {
    var statements = from statement in this.statements
                     where !(statement is Comment)
                     select statement;
                       
    foreach(var statement in statements) {
      Console.WriteLine(statement);
    }
  }

  [ConditionalAttribute("DEBUG")]
  private void showParsingInfo() {
    Console.WriteLine(new String('-', 75));
    Console.WriteLine(this.ddl.Length + " bytes remaining:");
    Console.WriteLine(this.ddl.peek(50) + " [...]");
    Console.WriteLine(new String('-', 75));
  }

  [ConditionalAttribute("DEBUG")]
  private void log(string msg) {
    Console.WriteLine("!!! " + msg);
  }
}

public class Program {
  public static void Main(string[] args) {

    if(args.Length != 1) {
      Console.WriteLine("USAGE: main.exe <filename>");
      return;
    }
    
    if(! File.Exists(args[0])) {
      Console.WriteLine("Unknown file");
      return;
    }
    
    string input = System.IO.File.ReadAllText(args[0]);
    
    DDL ddl = new DDL();
    ddl.parse(input);
    ddl.dump();
  }
}
