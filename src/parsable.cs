using System;

using System.Collections.Generic;

using System.Text.RegularExpressions;


public class Parsable {

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
    return this.text.Substring(0, Math.Min(length, this.text.Length));
  }
  
  public int Length { get { return this.text.Length; } }
}
