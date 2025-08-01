// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

module internal FSharp.Compiler.ParseHelpers

open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Features
open FSharp.Compiler.Text
open FSharp.Compiler.Xml
open Internal.Utilities.Text.Lexing
open Internal.Utilities.Text.Parsing

/// The error raised by the parse_error_rich function, which is called by the parser engine
/// when a syntax error occurs. The first object is the ParseErrorContext which contains a dump of
/// information about the grammar at the point where the error occurred, e.g. what tokens
/// are valid to shift next at that point in the grammar. This information is processed in CompileOps.fs.
[<NoEquality; NoComparison>]
exception SyntaxError of obj * range: range

exception IndentationProblem of string * range

val warningStringOfCoords: line: int -> column: int -> string

val warningStringOfPos: p: pos -> string

val posOfLexPosition: p: Position -> pos

val mkSynRange: p1: Position -> p2: Position -> range

type LexBuffer<'Char> with

    member LexemeRange: range

val lhs: parseState: IParseState -> range

val rhs2: parseState: IParseState -> i: int -> j: int -> range

val rhs: parseState: IParseState -> i: int -> range

type LexerIfdefStackEntry =
    | IfDefIf
    | IfDefElse

type LexerIfdefStackEntries = (LexerIfdefStackEntry * range) list

type LexerIfdefStack = LexerIfdefStackEntries

type LexerEndlineContinuation =
    | Token
    | IfdefSkip of int * range: range

[<RequireQualifiedAccess>]
type LexerStringStyle =
    | Verbatim
    | TripleQuote
    | SingleQuote
    | ExtendedInterpolated

[<RequireQualifiedAccess; Struct>]
type LexerStringKind =
    { IsByteString: bool
      IsInterpolated: bool
      IsInterpolatedFirst: bool }

    static member ByteString: LexerStringKind

    static member InterpolatedStringFirst: LexerStringKind

    static member InterpolatedStringPart: LexerStringKind

    static member String: LexerStringKind

type LexerInterpolatedStringNesting = (int * LexerStringStyle * int * range option * range) list

[<RequireQualifiedAccess; NoComparison; NoEquality>]
type LexerContinuation =
    | Token of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting
    | IfDefSkip of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting * int * range: range
    | String of
        ifdef: LexerIfdefStackEntries *
        nesting: LexerInterpolatedStringNesting *
        style: LexerStringStyle *
        kind: LexerStringKind *
        delimLen: int *
        range: range
    | Comment of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting * int * range: range
    | SingleLineComment of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting * int * range: range
    | StringInComment of
        ifdef: LexerIfdefStackEntries *
        nesting: LexerInterpolatedStringNesting *
        style: LexerStringStyle *
        int *
        range: range
    | MLOnly of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting * range: range
    | EndLine of ifdef: LexerIfdefStackEntries * nesting: LexerInterpolatedStringNesting * LexerEndlineContinuation

    member LexerIfdefStack: LexerIfdefStackEntries

    member LexerInterpStringNesting: LexerInterpolatedStringNesting

    static member Default: LexerContinuation

and LexCont = LexerContinuation

val ParseAssemblyCodeInstructions:
    s: string ->
    reportLibraryOnlyFeatures: bool ->
    langVersion: LanguageVersion ->
    strictIndentation: bool option ->
    m: range ->
        ILInstr[]

val grabXmlDocAtRangeStart: parseState: IParseState * optAttributes: SynAttributeList list * range: range -> PreXmlDoc

val grabXmlDoc: parseState: IParseState * optAttributes: SynAttributeList list * elemIdx: int -> PreXmlDoc

val ParseAssemblyCodeType:
    s: string ->
    reportLibraryOnlyFeatures: bool ->
    langVersion: LanguageVersion ->
    strictIndentation: bool option ->
    m: range ->
        ILType

val reportParseErrorAt: range -> (int * string) -> unit

val raiseParseErrorAt: range -> (int * string) -> 'a

val mkSynMemberDefnGetSet:
    parseState: IParseState ->
    opt_inline: range option ->
    mWith: range ->
    classDefnMemberGetSetElements:
        (range option *
        SynAttributeList list *
        (SynPat * range) *
        (range option * SynReturnInfo) option *
        range option *
        SynExpr *
        range) list ->
    mAnd: range option ->
    mWhole: range ->
    propertyNameBindingPat: SynPat ->
    optPropertyType: (range option * SynReturnInfo) option ->
    visNoLongerUsed: SynAccess option ->
    flagsBuilderAndLeadingKeyword: (SynMemberKind -> SynMemberFlags) * SynLeadingKeyword ->
        attrs: SynAttributeList list ->
        rangeStart: range ->
            SynMemberDefn list

/// Incorporate a '^' for an qualified access to a generic type parameter
val adjustHatPrefixToTyparLookup: mFull: range -> rightExpr: SynExpr -> SynExpr

val mkSynTypeTuple: elementTypes: SynTupleTypeSegment list -> SynType

#if DEBUG
val debugPrint: s: string -> unit
#else
val debugPrint: s: 'a -> unit
#endif

val exprFromParseError: e: SynExpr -> SynExpr

val patFromParseError: e: SynPat -> SynPat

val rebindRanges:
    first: (RecordFieldName * range option * SynExpr option) ->
    fields: ((RecordFieldName * range option * SynExpr option) * BlockSeparator option) list ->
    lastSep: BlockSeparator option ->
        SynExprRecordField list

val mkUnderscoreRecdField: m: range -> SynLongIdent * bool

val mkRecdField: lidwd: SynLongIdent -> SynLongIdent * bool

val mkSynDoBinding: vis: SynAccess option * mDo: range * expr: SynExpr * m: range -> SynBinding

val mkSynExprDecl: e: SynExpr -> SynModuleDecl

val addAttribs: attrs: SynAttributes -> p: SynPat -> SynPat

val unionRangeWithPos: r: range -> p: pos -> range

val checkEndOfFileError: t: LexerContinuation -> unit

type BindingSet =
    | BindingSetPreAttrs of
        range *
        bool *
        bool *
        (SynAttributes -> SynAccess option -> SynAttributes * SynBinding list) *
        range

val mkClassMemberLocalBindings:
    isStatic: bool * initialRangeOpt: range option * attrs: SynAttributes * vis: SynAccess option * BindingSet ->
        SynMemberDefn

/// Creates either SynExpr.LetOrUse or SynExpr.LetOrUseBang based on isBang parameter
/// Handles all four cases: 'let', 'let!', 'use', and 'use!'
val mkLetExpression:
    isBang: bool *
    mKeyword: range *
    mIn: range option *
    mWhole: range *
    body: SynExpr *
    bindingInfo: (bool * BindingSet) option *
    bangInfo: (SynPat * SynExpr * SynExprAndBang list * range option * bool) option ->
        SynExpr

val mkAndBang:
    mKeyword: range * pat: SynPat * rhs: SynExpr * mWhole: range * mEquals: range * mIn: range option -> SynExprAndBang

val mkDefnBindings:
    mWhole: range * BindingSet * attrs: SynAttributes * vis: SynAccess option * attrsm: range -> SynModuleDecl list

val idOfPat: parseState: IParseState -> m: range -> p: SynPat -> Ident

val checkForMultipleAugmentations: m: range -> a1: 'a list -> a2: 'a list -> 'a list

val rangeOfLongIdent: lid: LongIdent -> range

val appendValToLeadingKeyword: mVal: range -> leadingKeyword: SynLeadingKeyword -> SynLeadingKeyword

val mkSynUnionCase:
    attributes: SynAttributes ->
    access: SynAccess option ->
    id: SynIdent ->
    kind: SynUnionCaseKind ->
    mDecl: range ->
    (PreXmlDoc * range) ->
        SynUnionCase

val mkAutoPropDefn:
    mVal: range ->
    access: SynAccess option ->
    ident: Ident ->
    typ: SynType option ->
    mEquals: range option ->
    expr: SynExpr ->
    accessors: range option * (SynMemberKind * GetSetKeywords option * SynAccess option * SynAccess option) ->
        xmlDoc: PreXmlDoc ->
        attribs: SynAttributes ->
        flags: (SynMemberKind -> SynMemberFlags) * SynLeadingKeyword ->
            rangeStart: range ->
                SynMemberDefn

val mkValField:
    parseState: IParseState ->
    mVal: range ->
    isMutable: range option ->
    access: SynAccess option ->
    idOpt: Ident option ->
    typ: SynType option ->
    rangeStart: range ->
    SynAttributes ->
    range option ->
        SynMemberDefn

val mkSynField:
    parseState: IParseState ->
    idOpt: Ident option ->
    t: SynType option ->
    isMutable: range option ->
    vis: SynAccess option ->
    attributes: SynAttributeList list ->
    mStatic: range option ->
    rangeStart: range ->
    leadingKeyword: SynLeadingKeyword option ->
        SynField

val leadingKeywordIsAbstract: SynLeadingKeyword -> bool
