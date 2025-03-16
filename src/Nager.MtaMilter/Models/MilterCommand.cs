namespace Nager.MtaMilter.Models
{
    /// <summary>
    /// Defines Milter protocol command constants
    /// </summary>
    public static class MilterCommand
    {
        /// <summary>
        /// MAIL FROM command
        /// </summary>
        public const byte SMFIC_MAIL = 0x4D; // M

        /// <summary>
        /// End of headers
        /// </summary>
        public const byte SMFIC_EOH = 0x4E; // N

        /// <summary>
        /// RCPT TO command
        /// </summary>
        public const byte SMFIC_RCPT = 0x52; // R

        /// <summary>
        /// Mail header
        /// </summary>
        public const byte SMFIC_HEADER = 0x4C; // L

        /// <summary>
        /// Message body
        /// </summary>
        public const byte SMFIC_BODY = 0x42; // B

        /// <summary>
        /// End of body
        /// </summary>
        public const byte SMFIC_BODYEOB = 0x45; // E

        /// <summary>
        /// Quit communication
        /// </summary>
        public const byte SMFIC_QUIT = 0x51; // Q

        /// <summary>
        /// DATA command
        /// </summary>
        public const byte SMFIC_DATA = 0x54; // T

        /// <summary>
        /// Macro definition
        /// </summary>
        public const byte SMFIC_MACRO = 0x44; // D

        /// <summary>
        /// Connection information
        /// </summary>
        public const byte SMFIC_CONNECT = 0x43; // C

        /// <summary>
        /// Protocol negotiation
        /// </summary>
        public const byte SMFIC_OPTNEG = 0x4F; // O
    }
}
