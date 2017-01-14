// Statements - simple class hierarchy to hold parsed statements
// author: Christophe VG <contact@christophe.vg>

using System.Collections.Generic;
using System.Linq;

namespace DDL_Parser {

  public class QualifiedName {
    public string Scope { get; set; }
    public string Name  { get; set; }
    public override string ToString() {
      return (!(this.Scope == null) ? this.Scope + "." : "") + this.Name;
    }
  }

  public abstract class Statement {
    public string Body { get; set; }
  }

  public class Comment : Statement {
    public override string ToString() {
      return "comment(" + this.Body + ")";
    }
  }

  public abstract class CreateStatement : Statement {
    public override string ToString() {
      return "create    " + this.Body;
    }
  }

  public class CreateDatabaseStatement : Statement {
    public string Name { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateDatabaseStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "database(" + this.Name + ")" + "{" +
        string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateTablespaceStatement : Statement {
    public string Name { get; set; }
    public string Database { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateTablespaceStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "tablespace(" + this.Name +
        " in " + this.Database +
        ")" +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class Field {
    public string Name { get; set; }
    public string Type { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public Field() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return this.Name + ":" + this.Type +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class Constraint {
    public string Name { get; set; }
    public Dictionary<string,string> Parameters { get; set; } 
    public Constraint() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return this.Name +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateTableStatement : Statement {
    public string Name { get; set; }
    public string Database { get; set; }
    public List<Field> Fields { get; set; }
    public List<Constraint> Constraints { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateTableStatement() {
      this.Fields      = new List<Field>();
      this.Constraints = new List<Constraint>();
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "table(" + this.Name +
        " in " + this.Database + ")" +
        "["+
          string.Join(",", this.Fields) +
        "]"+
        "<"+
          string.Join(",", this.Constraints) +
        ">"+
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateIndexStatement : Statement {
    public string Name   { get; set; }
    public string Table  { get; set; }
    public string Fields { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateIndexStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "index(" +  this.Name +
        " on " + this.Table + "[" + this.Fields + "]" +
        ")" +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateViewStatement : Statement {
    public string Name       { get; set; }
    public string Definition { get; set; }
    public override string ToString() {
      return "view(" + this.Name + ")[" + this.Definition + "]";
    }
  }

  public abstract class SetStatement : Statement {
    public override string ToString() {
      return "set(" + this.Body + ")";
    }
  }

  public class SetParameterStatement : SetStatement {
    public string Variable { get; set; }
    public string Value    { get; set; }
    public override string ToString() {
      return "param(" + this.Variable + "=" + this.Value + ")";
    }
  }

  public abstract class AlterStatement : Statement {
    public override string ToString() {
      return "alter(" + this.Body + ")";
    }
  }

  public class AlterTableAddConstraintStatement : AlterStatement {
    public string     Name       { get; set; }
    public Constraint Constraint { get; set; }
  
    public override string ToString() {
      return "alter(" + this.Name + ":" + this.Constraint + ")";
    }
  }

}
