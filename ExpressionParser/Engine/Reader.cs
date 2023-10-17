using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ExpressionParser.Model;
using ExpressionParser.Model.Tokens;

namespace ExpressionParser.Engine {
  internal class Reader {
    private static readonly IDictionary<string, Type> availableTypes = new Dictionary<string, Type>(Keywords.BuiltInTypes);

    private readonly TokenList result = new TokenList();
    private int characterPosition;
    public static CultureInfo ReaderCulture { get; set; } = CultureInfo.InvariantCulture;

    internal static void AddTypeMap(string alias, Type type) {
      availableTypes[alias ?? type?.Name ?? throw new ArgumentNullException(nameof(type))] = type;
    }

    public TokenList ReadFrom(string input) {
      for (characterPosition = 0; characterPosition < input.Length;)
        processCharacter(input);
      return result;
    }

    private void processCharacter(string input) {
      if (findValidToken(input)) return;
      throw new ArgumentException($"Invalid token at position {characterPosition + 1}.", nameof(input));
    }

    private bool findValidToken(string input) {
      return findWhiteSpace(input) || findChar(input) || findString(input) || findDecimal(input) || findInteger(input) || findToken(input) || findCandidate(input);
    }

    private bool findWhiteSpace(string input) {
      return tryCreateToken(input.Substring(characterPosition), @"^\s+", _ => null);
    }

    private bool findChar(string input) {
      return tryCreateToken(input.Substring(characterPosition), @"^('[^\\']'|'\\[\\'trn]')", a => new LiteralToken<char>(convertToChar(a.Substring(1, a.Length - 2))));
    }

    private static char convertToChar(string source) {
      switch (source) {
        case @"\t": return '\t';
        case @"\r": return '\r';
        case @"\n": return '\n';
        case @"\'": return '\'';
        case @"\\": return '\\';
        default: return source[0];
      }
    }

    private bool findString(string input) {
      return tryCreateToken(input.Substring(characterPosition), @"^""[^""]*""", a => new LiteralToken<string>(a.Trim('"')));
    }

    private bool findDecimal(string input) {
      return tryCreateToken(input.Substring(characterPosition), @"^((\d*\.\d+)|(\d+\.\d*))", a => new LiteralToken<decimal>(Convert.ToDecimal(a, ReaderCulture)));
    }

    private bool findInteger(string input) {
      return tryCreateToken(input.Substring(characterPosition), @"^\d+", a => new LiteralToken<int>(Convert.ToInt32(a, ReaderCulture)));
    }

    private bool findCandidate(string input) {
      var match = Regex.Match(input.Substring(characterPosition), @"^[\w]*", RegexOptions.IgnoreCase);
      var candidate = match.Value;
      return findNull(candidate) || findBoolean(candidate) || findNamedOperator(candidate) || findType(candidate) || findName(candidate);
    }

    private bool findNull(string candidate) {
      return tryCreateToken(candidate, @"^null$", _ => new LiteralToken<object>(null));
    }

    private bool findBoolean(string candidate) {
      return tryCreateToken(candidate, @"^(true|false)$", a => new LiteralToken<bool>(Convert.ToBoolean(a, ReaderCulture)));
    }

    private bool findNamedOperator(string candidate) {
      return tryCreateToken(candidate, @"^(is|as)$", a => new SymbolToken(a));
    }

    private bool findType(string candidate) {
      if (!availableTypes.TryGetValue(candidate, out var type)) return false;
      result.Add(new TypeToken(type, "Type"));
      characterPosition += candidate.Length;
      return true;
    }

    private bool findName(string candidate) {
      return tryCreateToken(candidate, @"^[a-zA-Z_][\w]*$", a => new NameToken(a, "Property"));
    }

    private bool tryCreateToken(string source, string regex, Func<string, Token> creator) {
      var match = Regex.Match(source, regex, RegexOptions.IgnoreCase);
      if (!match.Success) return false;
      var token = creator(match.Value);
      if (token != null) result.Add(token);
      characterPosition += match.Length;
      return true;
    }

    private bool findToken(string input) {
      var current = input[characterPosition];
      var next = characterPosition < input.Length - 1 ? input[characterPosition + 1] : (char?)null;
      return findSupportedSymbol($"{current}{next}") || findSupportedSymbol($"{current}");
    }

    private bool findSupportedSymbol(string token) {
      var candidates = TokenList.SupportedOperators.Keys.Where(i => i.Length == token.Length).ToArray();
      var symbol = candidates.FirstOrDefault(s => s == token);
      if (symbol == null) return false;
      switch (symbol) {
        case "is":
        case "as":
          return false;
        case "+" when isUnaryOperatorPattern():
          result.Add(new SymbolToken("[+]"));
          break;
        case "-" when isUnaryOperatorPattern():
          result.Add(new SymbolToken("[-]"));
          break;
        case "(" when isMethodPattern(out var methodToken):
          methodToken.NodeType = "Method";
          result.Add(new SymbolToken(token));
          break;
        case ")" when isTypeCastPattern(out var typeCastToken):
          typeCastToken.NodeType = "TypeCast";
          result.RemoveAt(result.Count - 2);
          break;
        default:
          result.Add(new SymbolToken(token));
          break;
      }

      characterPosition += token.Length;
      return true;
    }

    private bool isUnaryOperatorPattern() {
      return !result.Any() || result.TokenAt(result.Count - 1) is SymbolToken;
    }

    private bool isTypeCastPattern(out TypeToken token) {
      token = result.TokenAt(result.Count - 1) is TypeToken candidate
              && result.TokenAt(result.Count - 2) is SymbolToken previousSymbol
              && previousSymbol.Symbol == "("
        ? candidate
        : null;
      return token != null;
    }

    private bool isMethodPattern(out NameToken token) {
      token = result.TokenAt(result.Count - 1) as NameToken;
      return token != null;
    }
  }
}