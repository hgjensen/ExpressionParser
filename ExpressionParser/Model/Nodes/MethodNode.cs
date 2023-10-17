using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionParser.Extensions;

namespace ExpressionParser.Model.Nodes;

internal class MethodNode : IdentifierNode {
  private IList<Expression> arguments;
  private Type callerType;
  private bool isExtensionMethod;
  private MethodInfo methodInfo;

  internal MethodNode(string name) : base(name, 1) { }

  internal IList<Node> Parameters { get; } = new List<Node>();

  internal override Expression BuildExpression(Expression callerExpression = null) {
    if (callerExpression == null) throw new InvalidOperationException($"Invalid method '{Name}'");
    callerType = callerExpression.Type;
    getArguments(callerExpression);
    getMethodInfo(callerExpression);
    if (isExtensionMethod) updateExtensionMethodInfo();
    return getCallExpression(callerExpression);
  }

  private void getArguments(Expression callerExpression = null) {
    arguments = Parameters.Select(p => p.BuildExpression(callerExpression)).ToList();
  }

  private void getMethodInfo(Expression callerExpression = null) {
    methodInfo = getInstanceMethod(Name) ?? getExtensionMethod(Name, callerExpression);
  }

  private void updateExtensionMethodInfo() {
    var genericArgumentType = callerType == typeof(string) ? typeof(char) : callerType.IsArray ? callerType.GetElementType() : callerType.GetGenericArguments()[0];
    methodInfo = methodInfo.MakeGenericMethod(genericArgumentType);
  }

  private MethodInfo getInstanceMethod(string name) {
    var candidates = callerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Where(i => i.Name == name).ToArray();
    return getMethodInfoFromCandidates(candidates);
  }

  private MethodInfo getExtensionMethod(string name, Expression callExpression) {
    arguments.Insert(0, callExpression);
    var candidates = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(i => i.Name == name).ToArray();
    var method = getMethodInfoFromCandidates(candidates);
    isExtensionMethod = method != null;
    return method;
  }

  private MethodInfo getMethodInfoFromCandidates(IEnumerable<MethodInfo> candidates) {
    return (from candidate in candidates
      let parametersTypes = candidate.GetParameters().ToArray(p => p.ParameterType)
      where parametersTypes.Length == arguments.Count
            && parametersTypes.Zip(arguments, areEquivalent).All(e => e)
      select candidate).FirstOrDefault();
  }

  private static bool areEquivalent(Type parameterType, Expression argument) {
    return argument.Type.Name == parameterType.Name || argument.Type.GetInterfaces().Any(i => i.Name.Equals(parameterType.Name));
  }

  private Expression getCallExpression(Expression parameter) {
    return isExtensionMethod ? Expression.Call(methodInfo, arguments) : Expression.Call(parameter, methodInfo, arguments);
  }
}