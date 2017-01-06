# DDL Query

Straightforward DDL parser with Linq query interface.  
Christophe VG (<contact@christophe.vg>)
[https://github.com/christophevg/ddl-query](https://github.com/christophevg/ddl-query)

## Notes

A DDL basically consists of two types of "statements": comments and actual
statements. The former is identified by a double leading dash (--), the
latter are separated using semi-colons (;).

The DDL parser starts of by checking if its current input starts with a double
dash, if so, the remainder of the line, up to a new-line character (optional
carriage returns are discarded anyway), is wrapped in a Comment object. If it
is a statement, everything up to the next semi-colon is further parsed as a Statement.
