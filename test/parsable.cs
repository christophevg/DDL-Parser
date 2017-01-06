using System;
using System.Collections.Generic;

using NUnit.Framework;

[TestFixture]
public class ParsableTest {

  [Test]
  public void testImplicitConstructorBehaviour() {
    Parsable text = new Parsable(" \t\r\n123456789");
    // NOTE: \r is discards
    //       repeated whitespace (space and tab) are replaced by single space
    Assert.AreEqual(text.Length, 11);
  }

  [Test]
  public void testLength() {
    Assert.AreEqual(new Parsable("123456789").Length, 9);
  }
  
  [Test]
  public void testSkipLeadingWhitespace() {
    Parsable text = new Parsable(" \t\r\n123456789");
    text.SkipLeadingWhitespace();
    Assert.AreEqual(text.Length, 9);
  }
  
  [Test]
  public void testConsume() {
    Assert.IsTrue (new Parsable("123 456")  .Consume("123"));
    Assert.IsTrue (new Parsable("  123 456").Consume("123"));
    Assert.IsFalse(new Parsable("  123 456").Consume("456"));
  }
  
  [Test]
  public void testConsumeUpTo() {
    Parsable text = new Parsable("  123 456;789");
    Assert.AreEqual(text.ConsumeUpTo(";"), "123 456");
    Assert.AreEqual(text.Length, 4);
  }
  
  [Test]
  public void testConsumeId() {
    Assert.AreEqual(new Parsable("   123 456;789").ConsumeId(), "123");
    Assert.IsNull  (new Parsable("  @123 456;789").ConsumeId());
    Assert.AreEqual(new Parsable("   123.456;789").ConsumeId(), "123.456");
  }

  [Test]
  public void testConsumeNumber() {
    Assert.AreEqual(new Parsable("   123 456;789").ConsumeNumber(), "123");
    Assert.IsNull  (new Parsable("  Hi123456;789").ConsumeNumber());
    Assert.AreEqual(new Parsable("   123.456;789").ConsumeNumber(), "123.456");
  }
  
  [Test]
  public void testConsumeDictionary() {
    Assert.AreEqual(
      new Parsable("p1 v1 p2 v2 p3 v3;").ConsumeDictionary(),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 p2 v2 p3 v3\n").ConsumeDictionary(upTo:"\n"),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1,v1,p2,v2,p3,v3;").ConsumeDictionary(separator:','),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
    );
    List<string> keys = new List<string>() { "p1 bis" };
    Assert.AreEqual(
      new Parsable("p1 bis v1 p2 v2 p3 v3;").ConsumeDictionary(merge: keys),
        new Dictionary<string,string>() {
          { "p1_bis", "v1" }, { "p2", "v2" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 NOT p2 p3 v3;").ConsumeDictionary(),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "False" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 p2 NO p3 v3;").ConsumeDictionary(),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "False" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 WITH p2 p3 v3;").ConsumeDictionary(),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "True" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 p2 p3 v3;").ConsumeDictionary(
        options: new List<string>() { "p2" }
      ),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "True" }, { "p3", "v3" }
        }
    );
    Assert.AreEqual(
      new Parsable("p1 v1 p2 2 G p3 v3;").ConsumeDictionary(),
        new Dictionary<string,string>() {
          { "p1", "v1" }, { "p2", "2G" }, { "p3", "v3" }
        }
    );
  }
  
  [Test]
  public void testPeek() {
    Assert.AreEqual(new Parsable("  123").Peek(6), " 123");
  }
}
