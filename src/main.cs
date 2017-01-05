using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

public class DDL {

  private class Statement {
    public string Body { get; set; }
    public override string ToString() {
      return "STATEMENT " + this.Body;
    }
  }
  
  private class Comment : Statement {
    public override string ToString() {
      return "COMMENT   " + this.Body;
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

  private Regex discardedCharacters   = new Regex( "\r"          );
  private Regex repeatedWhitespace    = new Regex( "[ \t][ \t]*" );
  private Regex leadingWhitespace     = new Regex( "^[\n \t]+"   );
  private Regex trailingWhitespace    = new Regex( "[ \t]+$"     );
  private Regex newlinesAndWhitespace = new Regex( "\n[ \t]*"    );
  
  private string ddl = "";
    
  // discard some character(sequences) in general
  private void prepare() {
    this.ddl = this.discardedCharacters  .Replace(this.ddl, ""  );
    this.ddl = this.repeatedWhitespace   .Replace(this.ddl, " " );
  }

  private void trimLeadingWhitespace() {
    this.ddl = this.leadingWhitespace.Replace(this.ddl, "");
  }

  // trim leading and trailing whitespace
  // also replace new-line characters and optionally addition whitespace
  private string trim(string text) {
    text = this.leadingWhitespace    .Replace(text, "");
    text = this.trailingWhitespace   .Replace(text, "");
    text = this.newlinesAndWhitespace.Replace(text, " ");
    return text;
  }
  
  private bool done { get { return this.ddl.Length == 0; } }

  public void parse(String ddl) {
    this.ddl = ddl;

    this.prepare();

    while(! this.done ) {
      this.trimLeadingWhitespace();

      // Console.WriteLine(new String('-', 75));
      // Console.WriteLine(
      //   "Parsing: " + this.ddl.Length + " " +
      //   '"' + this.ddl.Substring(0, 50) + '"'
      // );

      if( ! this.parseComment() ) {
        this.parseStatement();
      }

    }
  }
  
  private string consume(int length) {
    string consumed = this.ddl.Substring(0, length);
    this.ddl = this.ddl.Substring(length);
    return consumed;
  }
  
  private string consume(string text) {
    if(this.ddl.StartsWith(text)) {
      return this.consume(text.Length);
    }
    return null;
  }
  
  private string consumeUpTo(string text) {
    return this.consume(this.ddl.IndexOf(text));
  }

  private bool parseComment() {
    if( this.consume("--") == "--" ) {
      string comment = this.trim(this.consumeUpTo("\n"));
      this.statements.Add(new Comment() { Body = comment });
      return true;
    }
    return false;
  }

  private bool parseStatement() {
    string statement = this.trim(this.consumeUpTo(";"));
    this.consume(";");
    this.statements.Add(new Statement() { Body = statement });
    return true;
  }
  
  public void dump() {
    foreach(Statement statement in this.statements) {
      Console.WriteLine(statement);
    }
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
