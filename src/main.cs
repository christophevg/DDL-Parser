// main demo application driver for DDL
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Linq;


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
    ddl.Parse(input);

    var statements = from statement in ddl.statements
                     where !(statement is Comment)
                     select statement;
                       
    foreach(var statement in statements) {
      Console.WriteLine(statement);
    }

  }
}
