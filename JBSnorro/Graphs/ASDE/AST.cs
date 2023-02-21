#nullable enable
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Graphs;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ASDE;


public interface IPositioned // <TSelf> : IGreenNode<TSelf> where TSelf : class, IPositioned<TSelf>
{
    public IPosition Position { get; }

    // IReadOnlyList<TSelf> IGreenNode<TSelf>.Elements => ((TSelf)this).Elements; // circular
    // TSelf IGreenNode<TSelf>.With(IReadOnlyList<TSelf> elements) => ((TSelf)this).With(elements);  // circular
}
public interface IParseNode<TSelf> : IGreenNode<TSelf>, IPositioned where TSelf : class, IParseNode<TSelf>, IGreenNode<TSelf>
{

}

public class ParseNode : IParseNode<ParseNode>
{
    public IReadOnlyList<ParseNode> Elements { get; }
    public IPosition Position { get; }

    public ParseNode(IPosition position, IReadOnlyList<ParseNode> elements)
    {
        Position = position;
        Elements = elements;
    }

    public ParseNode With(IReadOnlyList<ParseNode> elements)
    {
        return new ParseNode(this.Position, elements);
    }
}




public interface IASTNode<TSelf, TParseNode> : IRedNode<TSelf, TParseNode> where TSelf : class, IASTNode<TSelf, TParseNode> where TParseNode : class, IParseNode<TParseNode>
{
    /// <summary>
    /// Gets the semantics of this node if they're computed already; otherwise <code>null</code>.
    /// </summary>
    public IModel? Semantics { get; }
    public IModel ComputeSemantics(IBinder binder);

    //static TSelf IRedNode<TSelf, IParseNode<TSelf>>.Create(IParseNode<TSelf> green) => throw new UnreachableException("Must be implemented"); // Hmm 🤔
}



public class ASTNode : IASTNode<ASTNode, ParseNode>
{
    protected ParseNode Green { get; }
    public ASTNode? Parent { get; }
    public IReadOnlyList<ASTNode> Elements { get; }
    public IModel? Semantics { get; private set; }
    private readonly int indexInParent;

    public static ASTNode Create(ParseNode green, ASTNode? parent, int? indexInParent) => new ASTNode(green, parent, indexInParent);
    public ASTNode(ParseNode green, ASTNode? parent, int? indexInParent)
    {
        this.Green = green;
        this.Parent = parent;
        this.Elements = green.Elements.MapLazily((green, i) => ASTNode.Create(green, this, i));
        this.indexInParent = indexInParent ?? -1;
    }


    ParseNode IRedNode<ASTNode, ParseNode>.Green => this.Green;
    int IRedNode<ASTNode, ParseNode>.IndexInParent => indexInParent;
    public IModel ComputeSemantics(IBinder binder) => throw new NotImplementedException();
}

























public interface IPosition
{
    static IPosition RootPosition { get; } = default!;
}

public interface IBinder
{
    public IModel GetSemanticModel(ASTNode node);
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
// I think we'll call them lexemes. I lexeme consists of characters or graphemes more generally, but I don't think we'll have to go there?
// Let's define a lexicon to be a set of lexemes. A lexer is then a process taking a stream of characters and identifying the lexemes in them and to output a stream of those, then called "tokens".
// That sort of maps to the concepts of IName (=lexeme), and INamePart (=grapheme) that I had before.
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

interface IGrapheme<TSelf> : IParseNode<IGrapheme<TSelf>> // should have TSelf?
{
    string? Text { get; }
    ILexeme<Grapheme> Lexeme { get; }
}
interface ILexeme<TGrapheme> where TGrapheme : IGrapheme<TGrapheme>
{
    TGrapheme MainRepresentation { get; }
}


sealed class Grapheme : IGrapheme<Grapheme>
{
    public string? Text { get; }
    public Lexeme Lexeme { get; }
    public IReadOnlyList<Grapheme> Elements { get; }
    public IPosition Position { get; }


    /// <param name="getText">A function that gets the text from a specific source node. </param>
    public static Grapheme Create<TSource>(Lexeme lexeme, TSource tree, Func<TSource, string?> getText) where TSource : class, IParseNode<TSource>
    {
        return RedGreenExtensions.Map<TSource, Grapheme>(tree, create);
        Grapheme create(TSource element, IReadOnlyList<Grapheme> elements)
        {
            if (elements.Count == 0)
            {
                var text = getText(element);
                if (string.IsNullOrEmpty(text))
                {
                    throw new InvalidCastException($"Return value of {nameof(getText)} cannot be null or empty for nodes without child nodes");
                }
                return new Grapheme(lexeme, element.Position, null, text);
            }
            else
            {
                return new Grapheme(lexeme, element.Position, elements, null);
            }
        }
    }
    public static Grapheme Create(Lexeme lexeme, IPosition position, params (IPosition Position, string Text)[] leaves)
    {
        return new Grapheme(lexeme, position, leaves.Map(_ => new Grapheme(lexeme, _.Position, null, _.Text)), null);
    }
    private Grapheme(Lexeme lexeme, IPosition position, IReadOnlyList<Grapheme>? elements, string? text)
    {
        Contract.Requires(lexeme != null);
        Contract.Requires(position != null);
        Contract.Requires(elements is null != text is null);
        if (elements is null)
        {
            Contract.Requires(!string.IsNullOrEmpty(text));
        }
        else
        {
            Contract.Requires(elements.Count != 0);
        }

        this.Lexeme = lexeme;
        this.Position = position;
        this.Text = text;
        this.Elements = elements ?? EmptyCollection<Grapheme>.ReadOnlyList;
    }


    public Grapheme With(IReadOnlyList<Grapheme> elements)
    {
        Contract.Requires(this.Text == null, "{0}: Cannot add elements to node with text");
        Contract.Requires(elements != null);
        Contract.Requires(elements.Count != 0);

        return new Grapheme(this.Lexeme, this.Position, elements, null);
    }
    public Grapheme With(string text)
    {
        Contract.Requires(this.Text != null, "{0}: Cannot add text to node with elements");
        Contract.Requires(!string.IsNullOrEmpty(text));

        return new Grapheme(this.Lexeme, this.Position, null, text);
    }
    public Grapheme With(IPosition position)
    {
        return new Grapheme(this.Lexeme, position, this.Elements, this.Text);
    }
    IReadOnlyList<IGrapheme<Grapheme>> IGreenNode<IGrapheme<Grapheme>>.Elements => Elements;

    ILexeme<Grapheme> IGrapheme<Grapheme>.Lexeme => this.Lexeme;
    IGrapheme<Grapheme> IGreenNode<IGrapheme<Grapheme>>.With(IReadOnlyList<IGrapheme<Grapheme>> elements)
    {
        return With(elements as IReadOnlyList<Grapheme> ?? elements.Map(e => e as Grapheme ?? throw new NotImplementedException("IGrapheme<Grapheme> not castable to Grapheme")));
    }
}
class Lexeme : ILexeme<Grapheme>
{
    public Grapheme MainRepresentation { get; }
    public Lexeme(Func<Lexeme, Grapheme> mainRepresentationFactory)
    {
        this.MainRepresentation = mainRepresentationFactory(this);
    }
}
