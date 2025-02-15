﻿using ExpressionParser.Model.Nodes;

namespace ExpressionParser.Model.Tokens {
  internal class SymbolToken : Token {
    internal SymbolToken(string symbol) {
      Symbol = symbol;
    }

    internal string Symbol { get; }

    internal override bool StartsIndex => Symbol == "[";
    internal override bool EndsIndex => Symbol == "]";
    internal override bool StartsExpressionOrParameters => Symbol == "(";
    internal override bool IsParameterSeparator => Symbol == ",";
    internal override bool EndsExpressionOrParameters => Symbol == ")";

    internal override Node CreateNode() {
      return TokenList.SupportedOperators[Symbol]();
    }
  }
}