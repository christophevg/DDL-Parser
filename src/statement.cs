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

  public abstract class Statement {}
  
  public abstract class GenericBodyStatement : Statement {
    public string Body { get; set; }
    public override string ToString() {
      return this.GetType().Name.ToLower() + "(" + this.Body + ")";
    }
  }

  public class Comment : GenericBodyStatement {}
  
  public abstract class NamedStatement : Statement {
    public QualifiedName Name { get; set; }
    public string SimpleName {
      get {
        if( this.Name != null ) {
          return this.Name.Name;
        }
        return null;
      }
      set {
        if( this.Name == null ) {
          this.Name = new QualifiedName();
        }
        this.Name.Name = value;
      }
    }
  }

  public abstract class CreateStatement : NamedStatement {}

  public class CreateDatabaseStatement : CreateStatement {
    public Dictionary<string,string> Parameters { get; set; }
    public CreateDatabaseStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "database(" + this.Name.ToString() + ")" + "{" +
        string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateTablespaceStatement : CreateStatement {
    public QualifiedName Database               { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateTablespaceStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "tablespace(" + this.Name.ToString() +
        " in " + this.Database +
        ")" +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class Field : NamedStatement {
    public string Type                          { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public List<Constraint>          Constraints { get; set; }
    public Field() {
      this.Parameters = new Dictionary<string,string>();
      this.Constraints = new List<Constraint>();
    }
    public override string ToString() {
      return this.Name.ToString() + ":" + this.Type +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}" +
        ( 
          this.Constraints.Count > 0 ?
            "<" +
              string.Join(";", this.Constraints.Select(x => x.ToString())) +
            ">"
          : ""
        );
    }
  }

  public class Constraint : NamedStatement {
    public Dictionary<string,string> Parameters { get; set; } 
    public Constraint() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return this.Name.ToString() +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }
  
  public class ForeignKeyConstraint : Constraint {
    public QualifiedName Table { get; set; }
    public override string ToString() {
      return this.Name.ToString() + " ON " + this.Table.ToString() +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CheckConstraint : Constraint {
    public QualifiedName Field { get; set; }
    public string        Rules { get; set; }
    public override string ToString() {
      return "check:" + this.Field.ToString() + ":=" + this.Rules;
    }
  }

  public class CreateTableStatement : CreateStatement {
    public QualifiedName             Database    { get; set; }
    public List<Field>               Fields      { get; set; }
    public List<Constraint>          Constraints { get; set; }
    public Dictionary<string,string> Parameters  { get; set; }
    public CreateTableStatement() {
      this.Fields      = new List<Field>();
      this.Constraints = new List<Constraint>();
      this.Parameters  = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "table(" + this.Name.ToString() +
        " in " + this.Database + ")" +
        "["+
          string.Join(",", this.Fields) +
        "]"+
        ( this.Constraints.Count > 0 ?
          "<"+
            string.Join(",", this.Constraints) +
          ">"
          : ""
        ) +
        ( this.Parameters.Count > 0 ?
          "{" +
            string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
          "}"
          : ""
        );
    }
  }

  public class CreateIndexStatement : CreateStatement {
    public QualifiedName             Table      { get; set; }
    public string                    Fields     { get; set; }
    public Dictionary<string,string> Parameters { get; set; }
    public CreateIndexStatement() {
      this.Parameters = new Dictionary<string,string>();
    }
    public override string ToString() {
      return "index(" +  this.Name.ToString() +
        " on " + this.Table + "[" + this.Fields + "]" +
        ")" +
        "{" +
          string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
        "}";
    }
  }

  public class CreateViewStatement : CreateStatement {
    public string Definition { get; set; }
    public override string ToString() {
      return "view(" + this.Name.ToString() + ")[" + this.Definition + "]";
    }
  }

  public abstract class SetStatement : GenericBodyStatement {
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

  public abstract class AlterStatement : Statement {}

  public class AlterTableAddConstraintStatement : AlterStatement {
    public QualifiedName Table      { get; set; }
    public Constraint    Constraint { get; set; }
  
    public override string ToString() {
      return "alter(" + this.Table.ToString() + ":" + this.Constraint + ")";
    }
  }

}
