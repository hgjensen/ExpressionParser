using ExpressionParser.Model.Nodes;

namespace ExpressionParser.Model.Tokens {
  internal class NameToken : Token {
    private readonly string name;

    internal NameToken(string name, string nodeType) {
      this.name = name;
      NodeType = nodeType;
    }

    public string NodeType { get; set; }

    internal override Node CreateNode() {
      return NodeType == "Method" ? (Node)new MethodNode(name) : new PropertyNode(name);
    }
  }
}