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
    private static Regex discardedCharacters   = new Regex( "\r"          );
    private static Regex repeatedWhitespace    = new Regex( "[ \t][ \t]*" );
    private static Regex leadingWhitespace     = new Regex( "^[\n \t]+"   );
    private static Regex trailingWhitespace    = new Regex( "[ \t]+$"     );
    private static Regex newlinesAndWhitespace = new Regex( "\n[ \t]*"    );

    // trim leading and trailing whitespace
    // also replace new-line characters and optionally addition whitespace
    private string trim(string text) {
      text = Parsable.leadingWhitespace    .Replace(text, "");
      text = Parsable.trailingWhitespace   .Replace(text, "");
      text = Parsable.newlinesAndWhitespace.Replace(text, " ");
      return text;
    }
    
    public Parsable(string text) {
      this.text = text;
      this.text = Parsable.discardedCharacters  .Replace(this.text, ""  );
      this.text = Parsable.repeatedWhitespace   .Replace(this.text, " " );
    }

    public bool empty { get { return this.text.Length == 0; } }

    public void trimLeadingWhitespace() {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
    }

    public bool consume(string text) {
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
    
    public string peek(int length) {
      return this.text.Substring(0, length);
    }
    
    public int Length { get { return this.text.Length; } }
  }
    
  private Parsable ddl;
      
  public bool parse(String ddl) {
    this.ddl = new Parsable(ddl);

    while(! this.ddl.empty ) {
      this.ddl.trimLeadingWhitespace();

      this.showParsingInfo();

      if(! ( this.parseComment() || this.parseStatement() ) ) {
        return this.notifyParseFailure();
      }
    }

    return true;
  }

  private bool parseComment() {
    if( this.ddl.consume("--") ) {
      string comment = this.ddl.consumeUpTo("\n");
      this.statements.Add(new Comment() { Body = comment });
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
      string statement = this.ddl.consumeUpTo(";");
      this.ddl.consume(";");
      this.statements.Add(new CreateStatement() { Body = statement });
      return true;
    }
    return false;
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
