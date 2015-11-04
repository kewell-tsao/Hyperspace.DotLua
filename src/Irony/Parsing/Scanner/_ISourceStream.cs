namespace Irony.Parsing
{
    /// <summary>
    ///     Interface for Terminals to access the source stream and produce tokens.
    /// </summary>
    public interface ISourceStream
    {
        /// <summary>
        ///     Returns the source text
        /// </summary>
        string Text { get; }

        /// <summary>
        ///     Gets or sets the start location (position, row, column) of the new token
        /// </summary>
        SourceLocation Location { get; set; }

        /// <summary>
        ///     Gets or sets the current position in the source file. When reading the value, returns Location.Position value.
        ///     When a new value is assigned, the Location is modified accordingly.
        /// </summary>
        int Position { get; set; }

        /// <summary>
        ///     Gets or sets the current preview position in the source file. Must be greater or equal to Location.Position
        /// </summary>
        int PreviewPosition { get; set; }

        /// <summary>
        ///     Gets a char at preview position
        /// </summary>
        char PreviewChar { get; }

        /// <summary>
        ///     Gets the char at position next after the PrevewPosition
        /// </summary>
        char NextPreviewChar { get; } //char at PreviewPosition+1

        /// <summary>
        ///     Creates a new token based on current preview position.
        /// </summary>
        /// <param name="terminal">A terminal associated with the token.</param>
        /// <returns>New token.</returns>
        Token CreateToken(Terminal terminal);

        /// <summary>
        ///     Creates a new token based on current preview position and sets its Value field.
        /// </summary>
        /// <param name="terminal">A terminal associated with the token.</param>
        /// <param name="value">The value associated with the token.</param>
        /// <returns>New token.</returns>
        Token CreateToken(Terminal terminal, object value);

        /// <summary>Tries to match the symbol with the text at current preview position. </summary>
        /// <param name="symbol">A symbol to match</param>
        /// <returns>True if there is a match; otherwise, false.</returns>
        bool MatchSymbol(string symbol);

        bool EOF();
    } //interface
}