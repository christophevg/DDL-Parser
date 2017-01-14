// Parsable - a text wrapping class with helper functions and behaviour for
//            easier construction of (manually) crafted parsers.
// author: Christophe VG <contact@christophe.vg>

// implicit behaviour:
// - ignores whitespace
// - ignores carriage returns
// - trims consumed tokens

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DDL_Parser {

  public class ParseException : System.Exception {
    public ParseException() : base() { }
    public ParseException(string message) : base(message) { }
    public ParseException(string message, System.Exception inner) : base(message, inner) { }
  }

  public class Parsable {

    // the parsable text
    private string text = "";

    // helper Regular Expressions for general clean-up
    private static Regex discardedCharacters    = new Regex( "\r"           );
    private static Regex repeatedWhitespace     = new Regex( "[ \t][ \t]*"  );

    private static Regex leadingWhitespace      = new Regex( "^\\s+"        );
    private static Regex trailingWhitespace     = new Regex( "\\s+$"        );
    private static Regex newlinesWithWhitespace = new Regex( "\n[ \t]*"     );

    // IDs can be composed of writeable characters
    private static Regex identifier             = new Regex( "^([\\w]+)" );

    // matching numbers only
    private static Regex number                 = new Regex( "^([0-9\\.]+)" );

    // matching of negated parameters, e.g. "NOT VOLATILE"
    private static Regex notParameter           = new Regex( "NOT (\\w+)"   );

    // matching of enabled parameters, e.g. "WITH DEFAULT"
    private static Regex withParameter          = new Regex( "WITH (\\w+)"   );

    // rewriting of unit-enabled values
    private static Regex withUnit               = new Regex( "([0-9]+) ([KMG]\\s)");

    // function
    private static Regex function               = new Regex( "([\\w]+)\\(\\s*([^\\)\\s]+)\\s*\\)");

    public Parsable(string text) {
      this.text = text;
      this.text = Parsable.discardedCharacters  .Replace(this.text, ""  );
      this.text = Parsable.repeatedWhitespace   .Replace(this.text, " " );
    }

    public int  Length {
      get {
        return this.text.Length;
      }
    }

    public string Context {
      get {
        return this.Peek(20) + "[...]";
      }
    }

    // skips any whitespace at the start of the current text buffer
    public void SkipLeadingWhitespace() {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
    }
  
    // tries to consume a give string, returns success
    public void Consume(string text) {
      this.text = Parsable.leadingWhitespace.Replace(this.text, "");
      if(! this.text.StartsWith(text) ) {
        throw new ParseException(
          "could not consume '" + text + "' at " + this.Context
        );
      }
      // do actual consumption
      this.Consume(text.Length);
    }
  
    public bool TryConsume(string text) {
      try {
        this.Consume(text);
        return true;
      } catch(ParseException) {
        return false;
      }
    }

    // consumes all characters up to a given string
    public string ConsumeUpTo(string upTo, bool include=false) {
      int upToPosition = this.text.IndexOf(upTo);
      if(upToPosition == -1) {
        throw new ParseException(
          "could not consume up to not found '" + upTo +
          "' at " + this.Context
        );
      }
      string text = this.Consume(upToPosition);
      // since whitespace is trimmed, trying to consume it here fails
      if( include && upTo != "\n" ) { this.Consume(upTo); }
      return text;
    }

    // consumes an ID, returning it or null in case of failure
    public string ConsumeId() {
      this.SkipLeadingWhitespace();
      Match m = Parsable.identifier.Match(this.text);
      if(m.Success) {
        int length = m.Groups[0].Captures[0].ToString().Length;
        return this.Consume(length);
      }
      throw new ParseException( "could not consume ID at " + this.Context );
    }

    public QualifiedName ConsumeQualifiedName() {
      string part1 = this.ConsumeId();
      try {
        this.Consume(".");
        string part2 = this.ConsumeId();
        return new QualifiedName() { Scope = part1, Name = part2 };
      } catch(ParseException) {} // just catch and go on
      return new QualifiedName() { Name = part1 };
    }

    // consumes a number, returning it or null in case of failure
    public string ConsumeNumber() {
      this.SkipLeadingWhitespace();
      Match m = Parsable.number.Match(this.text);
      if(m.Success) {
        int length = m.Groups[0].Captures[0].ToString().Length;
        return this.Consume(length);
      }
      throw new ParseException( "could not consume number at " + this.Context );
    }

    // consumes key value pairs into a dictionary
    public Dictionary<string,string> ConsumeDictionary(string upTo          = ";",
                                                       char   separator     = ' ',
                                                       char   merger        = '_',
                                                       List<string> merge   = null,
                                                       List<string> options = null )
    {
      string part = this.Trim(this.ConsumeUpTo(upTo, include:true));

      // pre-process DDL, substituting keys with separator to keys with merger
      if( merge != null ) {
        foreach( var key in merge ) {
          part = part.Replace(key, key.Replace(separator, merger));
        }
      }
      // pre-process DDL, substituting "NOT PARAM" to "PARAM False"
      part = Parsable.notParameter.Replace(part, "$1 False");

      // pre-process DDL, substituting "WITH PARAM" to "PARAM True"
      part = Parsable.withParameter.Replace(part, "$1 True");

      // pre-process DDL, substituting Functional Style Key/Value pairs
      part = Parsable.function.Replace(part, "$1 $2");

      // extend single keyword switches
      if( options != null ) {
        foreach( var option in options ) {
          // don't convert already converted NOT OPTIONs
          if( ! part.Contains( option + " False" ) ) {
            part = part.Replace(option, option + " True" );
          }
        }
      }

      // rewrite unit-enriched values
      part = Parsable.withUnit.Replace(part, "$1$2");

      List<string> mappings = new List<string>(part.Split(separator));

      Dictionary<string,string> dict = new Dictionary<string,string>();
      int pairs = mappings.Count - (mappings.Count % 2);
      for(int i=0; i<pairs; i+=2) {
        string value = this.Trim(mappings[i+1]);
        if(value == "NO" ) { value = "False"; }
        if(value == "YES") { value = "True";  }
        dict[this.Trim(mappings[i])] = value;
      }
      return dict;
    }

    // returns an amount of characters, without consuming, not trimming!
    public string Peek(int amount) {
      return this.text.Substring(0, Math.Min(amount, this.Length));
    }
  
    // ACTUAL CONSUMPTION

    // consumes an amount of characters
    private string Consume(int amount) {
      amount = Math.Min(amount, this.Length);
      // extract
      string consumed = this.text.Substring(0, amount);
      // drop
      this.text = this.text.Substring(amount);
      return this.Trim(consumed);
    }

    // helper function to trim leading and trailing whitespace
    // also replace new-line characters and optionally addition whitespace
    private string Trim(string text) {
      text = Parsable.leadingWhitespace    .Replace(text, "");
      text = Parsable.trailingWhitespace   .Replace(text, "");
      text = Parsable.newlinesWithWhitespace.Replace(text, " ");
      return text;
    }
  }

}
