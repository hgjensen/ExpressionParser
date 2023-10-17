using System.Linq;
using System.Linq.Expressions;
using ExpressionParser.Model;
using ExpressionParser.Model.Nodes;

namespace ExpressionParser.Engine; 

internal static class Builder {
  internal static LambdaExpression BuildExpression(TokenList tokens) {
    var root = buildTree(tokens);
    var body = root.BuildExpression();
    return Expression.Lambda(body);
  }

  internal static LambdaExpression BuildExpressionFor<TInput>(TokenList tokens, string parameterName) {
    var root = buildTree(tokens);
    var parameterExpression = parameterName == null ? Expression.Parameter(typeof(TInput)) : Expression.Parameter(typeof(TInput), parameterName);
    var body = root.BuildExpression(parameterExpression);
    return Expression.Lambda(body, parameterExpression);
  }

  private static Node buildTree(TokenList tokens) {
    var nodes = new NodeStack();
    while (tokens.Any() && !(tokens.Current.EndsExpressionOrParameters || tokens.Current.IsParameterSeparator || tokens.Current.EndsIndex)) {
      if (tokens.Current.StartsExpressionOrParameters && nodes.LastAdded is MethodNode method) {
        tokens.MoveNext();
        processParameters(tokens, method);
      } else if (tokens.Current.StartsExpressionOrParameters) {
        tokens.MoveNext();
        processExpression(tokens, nodes);
      } else if (tokens.Current.StartsIndex) {
        tokens.MoveNext();
        processIndex(tokens, nodes);
      } else {
        nodes.Add(tokens.Current.CreateNode());
      }

      tokens.MoveNext();
    }

    return nodes.Pop();
  }

  private static void processParameters(TokenList tokens, MethodNode methodNode) {
    while (!tokens.Current.EndsExpressionOrParameters) {
      var childNode = buildTree(tokens);
      methodNode.Parameters.Add(childNode);
      if (tokens.Current.IsParameterSeparator) {
        tokens.MoveNext();
      }
    }
  }

  private static void processExpression(TokenList tokens, NodeStack nodes) {
    var childNode = buildTree(tokens);
    childNode.KickPrecedenceUp();
    nodes.Add(childNode);
  }

  private static void processIndex(TokenList tokens, NodeStack nodes) {
    nodes.Add(new ArrayIndexNode());
    var childNode = buildTree(tokens);
    nodes.Add(childNode);
  }
}