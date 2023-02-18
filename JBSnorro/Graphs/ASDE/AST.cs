#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ASDE;


public interface IASTNode : IGreenNode<IASTNode>
{
    public IPosition Position { get; }
}
public interface AST : IRedNode<AST, IASTNode>, IASTNode /*inherits just for Position 🤔 */
{
    /// <summary>
    /// Gets the semantics of this node if they're computed already; otherwise <code>null</code>.
    /// </summary>
    public IModel? Semantics { get; }
    public IModel ComputeSemantics(IBinder binder);


    static AST IRedNode<AST, IASTNode>.Create(IASTNode green) => throw new UnreachableException("Must be implemented"); // Hmm 🤔
}


public interface IPosition { }

public interface IBinder
{
    public IModel GetSemanticModel(AST node);
}
public interface IModel : IGreenNode<IModel>
{

}
// okay. there are semantics and non-semantic trees.
// in C#, the elements of the semantic tree are called Symbols; let's use the same this time. The semantic tree is called a Model.
// in parsers in general, the non-semantic elements are called tokens.
// 
// The semantic representation of a token is called a symbol if it has a direct mapping, otherwise a Model.
//
// If the meaning of a token need not have a unambiguous mapping to a symbol, then we need a class for token types.
// Symbols can be the user defined ones, but there's also the non-user defined ones. Or aren't there? Maybe ideally not.
//
// an object connecting tokens to symbols is called a Binder.
//
// Now some of the difficult parts:
// first of all, what is a token. Sure operators are simple, but names aren't. A lexer tokenizes characters, meaning, creates tokens (which are combinations of 1 or more characters) into tokens.
// however, the way I had it before was that the tokenization was not unique: a lexer had to retokenize depending on the findings by the parser.

// suppose you're defining the arrangement of tokens can be mapped to a symbol, what do you call such? Not the tokens, but the things the tokens are being mapped to? 
// I think we'll call them lexemes. I lexeme consists of characters or morphemes more generally, but I don't think we'll have to go there?
// Let's define a lexicon to be a set of lexemes. A lexer is then a process taking a stream of characters and identifying the lexemes in them and to output a stream of those, then called "tokens".
// That sort of maps to the concepts of IName (=lexeme), and INamePart (=morpheme) that I had before.
//
// Do we ever have to parse streams of semantics? I hope not...
//
// Now I have the question what is the difference between a token and an AST?
// and maybe the more important question: those names assume a linear stream, but we don't have that, now do we...
// AST obviously implies more knowledge about the structure. So our input isn't a stream, but it's a tree already. I'm sure there is a linear representation of the tree, but we're not going to parse that.
// That may be a foolish decision, but I just don't want to.

// So then, what is the input? 
// A tree, where each node has a position and, at least leaf nodes must have some sort of character, right? Or maybe semantics is enough? I just don't think that's going to be relevant in the beginning:
// if you think about use cases: parsing happens when the user types. All other operations must be typed. Those cases where it's "not compiling" are more complicated and I just don't want to deal with them this soon.
// we can assume the input is a tree with positions, and they _may_ have semantics associated already. The values of each node is determined by the node itself, is is not generally accessible from the input element, only their more specific forms will know.
// 
// know, given that the output of parsing is an AST, what is the input tree called? LexTree? InputTree? Does it matter? 
// well after a bit of googling, ParseTree is acceptable. But the node? ParseNode? Or just IParseTree? Implementation details....

// the names of NotationForm and INotation etc will come when discussing the parser, we're only talking about lexing here now.
// maybe a bit more important is to talk about the semantic output names first. So we have Symbol already. But from a mathematical perspective, where we have sets and operators, and the meanings of punctuations. The represent concepts that I've called Notions, and I'll stick with that name.
// but the notions are in the mathematical domain. The question now is, does there need to be a domain in between of the syntax domain and the mathematical domain? The semantic domain?
// so what I see now is that line ASTBuilder.cs:231 even has parsing depend on semantic interpretation (and given that lexing depends on parsing, it implicitly also depends on semantic interpretation). Great :P I mean, generality is nice, but this is getting dangerous. Anyway, let's proceed.
// The name of the concept in between of the syntactic domain and the mathematical domain is currently called IRepresentation with a method `IsAssignableTo(IRepresentation)`.
// Could it be that lexemes (i.e. parse tree nodes/one-to-one mappable to tokens) and ast-nodes are IRepresentations too? But notions where more archetypically IRepresentations.
// Do we need the `IsAssignableTo` for lexemes, ast-nodes? Maybe more a one-way right, lexeme.IsAssignableTo(ast_node). Probably it's best to have these data types be data only, no logic. Have the logic be implemented in the binder or something.
// It's just that the positions obviously already provide _some_ answers to whether lexemes and ast-nodes are assignable, so for a lexeme to be asked whether it could be mapped to a node makes sense because it can ask its position.


// blergh,  what about this conundrum: suppose you're lexing and you encounter a node with semantics already. well, maybe not that a big of a conundrum because the lexer couldn't possibly take it into account. It'd just have to ignore it, and the binder will get to it at some point and possible reject.
// I mean, maybe there are shortcuts to be made for performance, but let's not just yet right :P

// ok next topic: wrapped vs unwrapped. Look, that only makes sense when thinking from the perspective of UI. ASTs don't have that problem, then it's a problem of AST selection, or range. Is a range of AST nodes inclusive or exclusive. That's the mindset how it should be framed.

// and btw, the binder will know about which lexemes bind to which notions; that's not something the lexemes will have to know

interface IMorpheme : IGreenNode<IMorpheme>
{

}
interface ILexeme : IRedNode<ILexeme, IMorpheme>
{
    static ILexeme IRedNode<ILexeme, IMorpheme>.Create(IMorpheme green) => throw new UnreachableException("Must be implemented"); // Hmm 🤔 can't make it abstract. can't create a abstract overload
}





sealed class Character : IASTNode
{
    public string Text { get; }
    public IPosition Position { get; }

    public IReadOnlyList<IASTNode> Elements => EmptyCollection<IASTNode>.ReadOnlyList;

    public IASTNode With(IReadOnlyList<IASTNode> elements)
    {
        Contract.Requires(elements != null);
        Contract.Requires(elements.Count == 1);

        return this;
    }
    internal Character(IPosition position, string text)
    {
        this.Text = text;
        this.Position = position;
    }
}
sealed class ParseTreeNode : IASTNode
{
    public IPosition Position { get; }
    public IReadOnlyList<IASTNode> Elements { get; }

    public static IASTNode Create(IPosition position, IEnumerable<IASTNode> elements)
    {
        return new ParseTreeNode(position, elements.ToList());
    }
    public static IASTNode Create(IPosition position, ImmutableList<IASTNode> elements)
    {
        return new ParseTreeNode(position, elements);
    }
    internal static IASTNode Create(IPosition position, IReadOnlyList<IASTNode> elements)
    {
        return new ParseTreeNode(position, elements);
    }

    public static IASTNode Create(IPosition position, string text)
    {
        return new Character(position, text);
    }

    public IASTNode With(IReadOnlyList<IASTNode> elements)
    {
        return new ParseTreeNode(this.Position, elements);
    }

    private ParseTreeNode(IPosition position, IReadOnlyList<IASTNode> elements)
    {
        this.Position = position;
        this.Elements = elements;
    }
}