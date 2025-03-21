using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nager.MtaMilter.Helpers;
using Nager.MtaMilter.MilterMessages;
using Nager.MtaMilter.Models;
using System.Text;

namespace Nager.MtaMilter
{
    public class MilterProcessor
    {
        private readonly ILogger<MilterProcessor> _logger;

        public event Func<OptnegMilterMessage, bool>? OptnegMessageReceived;
        public event Func<ConnectMilterMessage, bool>? ConnectMessageReceived;
        private Dictionary<byte, Func<Memory<byte>, byte[]?>> _commandProcessors;

        private static byte[] answerContinueBytes = [0x63]; //c = continue
        private static byte[] answerAbortBytes = [0x61]; //a = abort

        public MilterProcessor(ILogger<MilterProcessor>? logger = default)
        {
            this._logger = logger ?? new NullLogger<MilterProcessor>();

            this._commandProcessors = new Dictionary<byte, Func<Memory<byte>, byte[]?>>
            {
                { MilterCommand.SMFIC_OPTNEG, this.OptnegProcessor },
                { MilterCommand.SMFIC_CONNECT, this.ConnectProcessor },
                { MilterCommand.SMFIC_MAIL, this.MailProcessor },
                { MilterCommand.SMFIC_RCPT, this.RcptProcessor },
                { MilterCommand.SMFIC_MACRO, this.MacroProcessor },
                { MilterCommand.SMFIC_DATA, this.DataProcessor },
                { MilterCommand.SMFIC_HEADER, this.HeaderProcessor },
                { MilterCommand.SMFIC_EOH, this.EndOfHeaderProcessor },
                { MilterCommand.SMFIC_BODY, this.BodyProcessor },
                { MilterCommand.SMFIC_BODYEOB, this.EndOfBodyProcessor },
                { MilterCommand.SMFIC_QUIT, this.QuitProcessor }       
            };
        }

        public byte[]? ProcessData(byte[] received)
        {
            if (received.Length < 5)
            {
                return null;
            }

            var tempMessageSize = received[..4].AsSpan();
            tempMessageSize.Reverse();
            var messageSize = BitConverter.ToInt32(tempMessageSize);

            //if (received.Length != messageSize)
            //{
            //    //TODO: Check SMFIC_MACRO multiple data packages in one received package

            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine($"data fragment received {received.Length}/{messageSize}");
            //    Console.WriteLine(BitConverter.ToString(e.Data.ToArray()));
            //    Console.ResetColor();
            //}

            var command = received[4];

            if (this._commandProcessors.TryGetValue(command, out var processor))
            {
                return processor.Invoke(received.AsMemory().Slice(5));
            }

            var readable = Encoding.ASCII.GetString(received);
            this._logger.LogWarning($"{nameof(ProcessData)} - Unknown command:{command} {readable}");

            return answerContinueBytes;
        }

        private byte[]? OptnegProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var protocolVersionTemp = commandDataSpan.Slice(0, 4);
            protocolVersionTemp.Reverse();
            int protocolVersion = BitConverter.ToInt32(protocolVersionTemp);

            var allowedActionsTemp = commandDataSpan.Slice(4, 4);
            allowedActionsTemp.Reverse();
            var bitInfo = BitHelper.GetBits(allowedActionsTemp[0]);

            /*
             *  BIT INFO (https://github.com/emersion/go-milter/blob/master/milter-protocol.txt)
             *  0	0x00000001	Can modify headers
             *  1	0x00000002	Can modify body
             *  2	0x00000004	Supports quarantine
             *  3	0x00000008	Supports macro extension
             *  4	0x00000010	Can change the sender
             *  5	0x00000020	Can add recipients
             *  6	0x00000040	Can remove recipients
             *  7	0x00000080	Supports protocol-specific extensions
            */


            var possibleProtocolContent = commandDataSpan.Slice(8, 4); //Bitmask of possible protocol content from SMFIP_*
            /*
             * 0x01	SMFIP_NOCONNECT		Skip SMFIC_CONNECT
             * 0x02	SMFIP_NOHELO		Skip SMFIC_HELO
             * 0x04	SMFIP_NOMAIL		Skip SMFIC_MAIL
             * 0x08	SMFIP_NORCPT		Skip SMFIC_RCPT
             * 0x10	SMFIP_NOBODY		Skip SMFIC_BODY
             * 0x20	SMFIP_NOHDRS		Skip SMFIC_HEADER
             * 0x40	SMFIP_NOEOH	        Skip SMFIC_EOH
            */


            if (this.OptnegMessageReceived is not null)
            {
                this.OptnegMessageReceived.Invoke(new OptnegMilterMessage { ProtocolVersion = protocolVersion });
            }

            this._logger.LogDebug($"{nameof(OptnegProcessor)} - SMFIC_OPTNEG - Protocol Version:{protocolVersion}");

            byte[] response = [0x4F, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x02];

            return response;
        }

        private byte[]? ConnectProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var splitIndex = commandDataSpan.IndexOf((byte)0x00);
            var mailserverHostTemp = commandDataSpan.Slice(0, splitIndex);
            var mailserverHost = Encoding.ASCII.GetString(mailserverHostTemp);

            commandDataSpan = commandDataSpan.Slice(splitIndex + 4); //3bytes family + port ignore at the moment

            //char	hostname[]	Hostname, NUL terminated
            //char family      Protocol family(see below)
            //uint16 port        Port number(SMFIA_INET or SMFIA_INET6 only)
            //char	address[]	IP address (ASCII) or unix socket path, NUL terminated

            splitIndex = commandDataSpan.IndexOf((byte)0x00);

            var mailserverIpAddressTemp = commandDataSpan.Slice(0, splitIndex);
            var mailserverIpAddress = Encoding.ASCII.GetString(mailserverIpAddressTemp);

            this._logger.LogDebug($"{nameof(ConnectProcessor)} - SMFIC_CONNECT - Mailserver Host:{mailserverHost} IpAddress:{mailserverIpAddress}");

            if (this.ConnectMessageReceived is not null)
            {
                var connectMessage = new ConnectMilterMessage
                {
                    MailserverHost = mailserverHost,
                    MailserverIpAddress = mailserverIpAddress
                };

                var continueProcessing = this.ConnectMessageReceived.Invoke(connectMessage);

                if (continueProcessing)
                {
                    return answerContinueBytes;
                }

                return answerAbortBytes;
            }

            return answerContinueBytes;
        }

        private byte[]? QuitProcessor(Memory<byte> commandData)
        {
            this._logger.LogDebug($"{nameof(QuitProcessor)} - SMFIC_QUIT");
            return null;
        }

        private byte[]? MailProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var mailFrom = Encoding.ASCII.GetString(commandDataSpan);
            this._logger.LogDebug($"{nameof(MailProcessor)} - SMFIC_MAIL - Mail From:{mailFrom}");

            return answerContinueBytes;
        }

        private byte[]? RcptProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var rcpt = Encoding.ASCII.GetString(commandDataSpan);
            this._logger.LogDebug($"{nameof(RcptProcessor)} - SMFIC_RCPT {rcpt}");
            return answerContinueBytes;
        }

        private byte[]? DataProcessor(Memory<byte> commandData)
        {
            // Mail Data Start Header and Body
            this._logger.LogDebug($"{nameof(DataProcessor)} - SMFIC_DATA");
            return answerContinueBytes;
        }

        private byte[]? HeaderProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var splitIndex = commandDataSpan.IndexOf((byte)0x00);
            var headerNameTemp = commandDataSpan.Slice(0, splitIndex);
            var headerName = Encoding.ASCII.GetString(headerNameTemp);

            var header = Encoding.ASCII.GetString(commandDataSpan);

            commandDataSpan = commandDataSpan.Slice(splitIndex + 1);
            var headerData = Encoding.ASCII.GetString(commandDataSpan);

            this._logger.LogDebug($"{nameof(HeaderProcessor)} - SMFIC_HEADER received: {headerName}:{headerData}");
            return answerContinueBytes;
        }

        private byte[]? EndOfHeaderProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            this._logger.LogDebug($"{nameof(EndOfHeaderProcessor)} - SMFIC_EOH - End of Header received");
            return answerContinueBytes;
        }

        private byte[]? BodyProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var dataBody = Encoding.ASCII.GetString(commandDataSpan);
            this._logger.LogDebug($"{nameof(BodyProcessor)} - SMFIC_BODY {dataBody}");
            return answerContinueBytes;
        }

        private byte[]? EndOfBodyProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            var dataBodyEnd = Encoding.ASCII.GetString(commandDataSpan);
            this._logger.LogDebug($"{nameof(EndOfBodyProcessor)} - SMFIC_BODYEOB {dataBodyEnd}");
            return answerContinueBytes;
        }

        private byte[]? MacroProcessor(Memory<byte> commandData)
        {
            var commandDataSpan = commandData.Span;

            if (commandDataSpan[0] == 0x43) //0x43 C
            {
                var macroPart = this.MacroParser(commandDataSpan);

                this._logger.LogDebug($"{nameof(MacroProcessor)} - SMFIC_MACRO - [C] {macroPart.Name}:{macroPart.Value}");
                return answerContinueBytes;
            }

            if (commandDataSpan[0] == 0x52) //0x52 R
            {
                var macroPart = this.MacroParser(commandDataSpan);

                this._logger.LogDebug($"{nameof(MacroProcessor)} - SMFIC_MACRO - [R] {macroPart.Name}:{macroPart.Value}");
                return answerContinueBytes;
            }

            if (commandDataSpan[0] == 0x4D) //0x4D M
            {
                var macroPart = this.MacroParser(commandDataSpan);

                this._logger.LogDebug($"{nameof(MacroProcessor)} - SMFIC_MACRO - [M] {macroPart.Name}:{macroPart.Value}");
                return answerContinueBytes;
            }

            this._logger.LogWarning($"{nameof(MacroProcessor)} - SMFIC_MACRO - unknown macro?");
            return null;
        }

        private MacroPart MacroParser(Span<byte> commandData)
        {
            var splitIndex = commandData.IndexOf((byte)0x00);
            var macroVariableTemp = commandData.Slice(1, splitIndex);
            var macroVariable = Encoding.ASCII.GetString(macroVariableTemp);

            commandData = commandData.Slice(splitIndex + 1);
            var macroVariableValue = Encoding.ASCII.GetString(commandData);

            return new MacroPart
            {
                Name = macroVariable,
                Value = macroVariableValue
            };
        }
    }
}
