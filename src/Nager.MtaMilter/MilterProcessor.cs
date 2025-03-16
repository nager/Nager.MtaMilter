using System.Text;
using Nager.MtaMilter.Helpers;
using Nager.MtaMilter.Models;

namespace Nager.MtaMilter
{
    public static class MilterProcessor
    {
        public static byte[]? ProcessData(Span<byte> received)
        {
            var answerContinueByte = new byte[] { 0x63 }; //c = continue
            var answerAbortByte = new byte[] { 0x61 }; //a = abort

            var tempMessageSize = received[..4];
            tempMessageSize.Reverse();
            var messageSize = BitConverter.ToInt32(tempMessageSize);

            received = received.Slice(4);

            //if (received.Length != messageSize)
            //{
            //    //TODO: Check SMFIC_MACRO multiple data packages in one received package

            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine($"data fragment received {received.Length}/{messageSize}");
            //    Console.WriteLine(BitConverter.ToString(e.Data.ToArray()));
            //    Console.ResetColor();
            //}

            var command = received[..1][0];

            received = received.Slice(1);


            //var hexData = BitConverter.ToString(received.ToArray());
            //Console.WriteLine($"MessageSize:{messageSize} Hex:{hexData}");

            if (command.Equals(MilterCommand.SMFIC_OPTNEG))
            {
                var protocolVersionTemp = received.Slice(0, 4);
                protocolVersionTemp.Reverse();
                int protocolVersion = BitConverter.ToInt32(protocolVersionTemp);

                var allowedActionsTemp = received.Slice(4, 4);
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


                var possibleProtocolContent = received.Slice(8, 4); //Bitmask of possible protocol content from SMFIP_*
                /*
                 * 0x01	SMFIP_NOCONNECT		Skip SMFIC_CONNECT
                 * 0x02	SMFIP_NOHELO		Skip SMFIC_HELO
                 * 0x04	SMFIP_NOMAIL		Skip SMFIC_MAIL
                 * 0x08	SMFIP_NORCPT		Skip SMFIC_RCPT
                 * 0x10	SMFIP_NOBODY		Skip SMFIC_BODY
                 * 0x20	SMFIP_NOHDRS		Skip SMFIC_HEADER
                 * 0x40	SMFIP_NOEOH	        Skip SMFIC_EOH
                */

                Console.WriteLine($"SMFIC_OPTNEG - Protocol Version:{protocolVersion}");

                byte[] response = [0x4F, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x02];

                return response;
            }

            if (command.Equals(MilterCommand.SMFIC_CONNECT))
            {
                var splitIndex = received.IndexOf((byte)0x00);
                var mailserverHostTemp = received.Slice(0, splitIndex);
                var mailserverHost = Encoding.ASCII.GetString(mailserverHostTemp);

                received = received.Slice(splitIndex + 4); //3bytes family + port ignore at the moment

                //char	hostname[]	Hostname, NUL terminated
                //char family      Protocol family(see below)
                //uint16 port        Port number(SMFIA_INET or SMFIA_INET6 only)
                //char	address[]	IP address (ASCII) or unix socket path, NUL terminated

                splitIndex = received.IndexOf((byte)0x00);

                var mailserverIpAddressTem = received.Slice(0, splitIndex);
                var mailserverIpAddress = Encoding.ASCII.GetString(mailserverIpAddressTem);

                Console.WriteLine($"SMFIC_CONNECT - Mailserver Host:{mailserverHost} IpAddress:{mailserverIpAddress}");

                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_MACRO))
            {
                if (received[0] == 0x43) //0x43 C
                {
                    var splitIndex = received.IndexOf((byte)0x00);
                    var macroVariableTemp = received.Slice(1, splitIndex);
                    var macroVariable = Encoding.ASCII.GetString(macroVariableTemp);

                    received = received.Slice(splitIndex + 1);
                    var macroVariableValue = Encoding.ASCII.GetString(received);

                    Console.WriteLine($"SMFIC_MACRO - {macroVariable}:{macroVariableValue}");
                    return answerContinueByte;
                }

                if (received[0] == 0x52) //0x52 R
                {
                    var splitIndex = received.IndexOf((byte)0x00);
                    var macroVariableTemp = received.Slice(1, splitIndex);
                    var macroVariable = Encoding.ASCII.GetString(macroVariableTemp);

                    received = received.Slice(splitIndex + 1);
                    var macroVariableValue = Encoding.ASCII.GetString(received);

                    Console.WriteLine($"SMFIC_MACRO - {macroVariable}:{macroVariableValue}");
                    return answerContinueByte;
                }

                if (received[0] == 0x4D) //0x4D M
                {
                    var splitIndex = received.IndexOf((byte)0x00);
                    var macroVariableTemp = received.Slice(1, splitIndex);
                    var macroVariable = Encoding.ASCII.GetString(macroVariableTemp);

                    received = received.Slice(splitIndex + 1);
                    var macroVariableValue = Encoding.ASCII.GetString(received);

                    Console.WriteLine($"SMFIC_MACRO - {macroVariable}:{macroVariableValue}");
                    return answerContinueByte;
                }

                Console.WriteLine("SMFIC_MACRO - unknown macro?");
                return null;
            }

            if (command.Equals(MilterCommand.SMFIC_HEADER))
            {
                var splitIndex = received.IndexOf((byte)0x00);
                var headerNameTemp = received.Slice(0, splitIndex);
                var headerName = Encoding.ASCII.GetString(headerNameTemp);

                var header = Encoding.ASCII.GetString(received);

                received = received.Slice(splitIndex + 1);
                var headerData = Encoding.ASCII.GetString(received);

                Console.WriteLine($"SMFIC_HEADER received: {headerName}:{headerData}");
                return answerContinueByte;
            }


            if (command.Equals(MilterCommand.SMFIC_MAIL))
            {
                var mailFrom = Encoding.ASCII.GetString(received);

                Console.WriteLine($"SMFIC_MAIL - Mail From:{mailFrom}");

                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_EOH))
            {
                Console.WriteLine("SMFIC_EOH - End of Header received");
                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_RCPT))
            {
                var rcpt = Encoding.ASCII.GetString(received);
                Console.WriteLine($"SMFIC_RCPT {rcpt}");
                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_BODY))
            {
                var dataBody = Encoding.ASCII.GetString(received);
                Console.WriteLine($"SMFIC_BODY {dataBody}");
                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_BODYEOB))
            {
                var dataBodyEnd = Encoding.ASCII.GetString(received);
                Console.WriteLine($"SMFIC_BODYEOB {dataBodyEnd}");
                return answerContinueByte;
            }

            if (command.Equals(MilterCommand.SMFIC_QUIT))
            {
                Console.WriteLine("Quit received");
                return null;
            }

            if (command.Equals(MilterCommand.SMFIC_DATA))
            {
                // Mail Data Start Header and Body
                Console.WriteLine($"SMFIC_DATA");
                return answerContinueByte;
            }

            var readable = Encoding.ASCII.GetString(received);

            Console.WriteLine($"Unknown command:{command} {readable}");

            return answerAbortByte;
        }
    }
}
