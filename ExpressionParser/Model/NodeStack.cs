using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionParser.Model.Nodes;

namespace ExpressionParser.Model;

internal class NodeStack : Stack<Node> {
  internal Node LastAdded;

  internal void Add(Node node) {
    if (!this.Any())
      Push(node);
    else
      switch (node) {
        case BinaryNode binaryNode when binaryNode.IsClosed:
          attachNodeToRoot(node);
          break;
        case BinaryNode binaryNode when Peek() is BinaryNode root && root.Precedence <= binaryNode.Precedence:
          attachRootToNodeLeft(binaryNode);
          break;
        case BinaryNode binaryNode when Peek() is BinaryNode root:
          moveRootRightToNodeLeft(root, binaryNode);
          attachNodeToRootRight(root, binaryNode);
          break;
        case BinaryNode binaryNode:
          attachRootToNodeLeft(binaryNode);
          break;
        default:
          attachNodeToRoot(node);
          break;
      }

    LastAdded = node;
  }

  private void attachNodeToRoot(Node node) {
    if (!Peek().TryAddNode(node))
      throw new InvalidOperationException($"Error adding '{node.GetType().Name}' to '{Peek().GetType().Name}'.");
  }

  private void attachRootToNodeLeft(BinaryNode node) {
    node.Left = Pop();
    Push(node);
  }

  private static void moveRootRightToNodeLeft(BinaryNode root, BinaryNode node) {
    node.Left = node.Left ?? root.Right;
  }

  private static void attachNodeToRootRight(BinaryNode root, Node node) {
    root.Right = node;
  }
}