using System.Collections.Generic;

using System.Linq;

public abstract class Statement {
  public string Body { get; set; }
}

public class Comment : Statement {
  public override string ToString() {
    return "comment(" + this.Body + ")";
  }
}

public class CreateStatement : Statement {
  public override string ToString() {
    return "create    " + this.Body;
  }
}

public class CreateDatabaseStatement : Statement {
  public string Name { get; set; }
  public Dictionary<string,string> Parameters { get; set; } =
    new Dictionary<string,string>();
  public override string ToString() {
    return "database(" + this.Name + ")" + "{" +
      string.Join(",", this.Parameters.Select(x => x.Key + "=" + x.Value)) +
      "}";
  }
}

public class CreateTablespaceStatement : Statement {
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


public class AlterStatement : Statement {
  public override string ToString() {
    return "alter     " + this.Body;
  }
}

public class SetStatement : Statement {
  public override string ToString() {
    return "set       " + this.Body;
  }
}
