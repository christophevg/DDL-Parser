// main demo application driver for DDL
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Linq;

namespace DDL_Parser {

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
    
      // create new DDL parsing object and parse the input
      DDL ddl = new DDL();
      ddl.Parse(input);

      // check for errors
      if(ddl.errors.Count > 0) {
        Console.WriteLine("ERRORS");
        Console.WriteLine("------");
        foreach(var error in ddl.errors) {
          Console.WriteLine(error.Message);
        }
      }

      // dump all parsed statements
      Console.WriteLine("All Parsed Statements");
      Console.WriteLine("----------------------------------------------------");
      var statements =  from statement in ddl.statements
                       where ! ( statement is Comment )
                         select statement;
      foreach(var statement in statements) {
        Console.WriteLine(statement.ToString());
      }
      Console.WriteLine();

      // example query 1: find all unique indexes with option cluster
      var indexes =  from index in ddl.statements.OfType<CreateIndexStatement>()
                    where index.Parameters.Keys.Contains("UNIQUE")
                       && index.Parameters["UNIQUE"] == "True"
                       && index.Parameters.Keys.Contains("CLUSTER")
                       && index.Parameters["CLUSTER"] == "True"
                   select index;

      Console.WriteLine("Query 1: find all unique indexes with option cluster");
      Console.WriteLine("----------------------------------------------------");
      foreach(var index in indexes) {
        Console.WriteLine(index.Name + " on " + index.Table );
      }
      Console.WriteLine();

      // example query 2: find all foreign keys with "on delete restrict"
      var constraints =  from alteration in ddl.statements.OfType<AlterTableAddConstraintStatement>()
                        where alteration.Constraint.Parameters.Keys.Contains("ON_DELETE")
                           && alteration.Constraint.Parameters["ON_DELETE"] == "RESTRICT"
                       select new {
                         Table       = alteration.Table,
                         Referencing = ((ForeignKeyConstraint)alteration.Constraint).Table
                       };

      Console.WriteLine("Query 2: find all foreign keys with 'on delete restrict'");
      Console.WriteLine("--------------------------------------------------------");
      foreach(var constraint in constraints) {
        Console.WriteLine(constraint.Table + " referencing " + constraint.Referencing );
      }
      Console.WriteLine();

      // example query 3: find all fields with "WITH DEFAULT"
      var fields =  from table in ddl.statements.OfType<CreateTableStatement>()
                    from field in table.Fields
                   where field.Parameters.Keys.Contains("DEFAULT")
                      && field.Parameters["DEFAULT"] == "True"
                  select new {
                    Name  = field.Name,
                    Table = table.Name
                  };

      Console.WriteLine("Query 3: find all fields with 'WITH DEFAULT'");
      Console.WriteLine("--------------------------------------------");
      foreach(var field in fields) {
        Console.WriteLine(field.Name + " in " + field.Table );
      }
      Console.WriteLine();
    }
  }

}
