using Nager.MtaMilter.UnitTest.Helpers;

namespace Nager.MtaMilter.UnitTest
{
    [TestClass]
    public sealed class MilterProcessorTest
    {
        [TestMethod]
        public void SMFIC_OPTNEG_Test()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-0D-4F-00-00-00-06-00-00-00-FF-00-00-00-42");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_MACRO_Test1()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-72-44-43-7B-64-61-65-6D-6F-6E-5F-6E-61-6D-65-7D-00-53-74-61-6C-77-61-72-74-20-4D-61-69-6C-20-53-65-72-76-65-72-20-76-30-2E-31-31-2E-35-00-6A-00-00-7B-63-6C-69-65-6E-74-5F-61-64-64-72-7D-00-31-37-32-2E-31-37-2E-30-2E-31-00-7B-63-6C-69-65-6E-74-5F-70-6F-72-74-7D-00-33-35-32-38-32-00-7B-63-6C-69-65-6E-74-5F-70-74-72-7D-00-75-6E-6B-6E-6F-77-6E-00");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_MACRO_Test2()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-1D-44-4D-7B-6D-61-69-6C-5F-61-64-64-72-7D-00-73-65-6E-64-65-72-40-74-65-73-74-2E-64-65-00");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_QUIT_Test()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-01-51");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_CONNECT_Test()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-28-43-6D-61-69-6C-73-65-72-76-65-72-2E-74-65-73-74-73-79-73-74-65-6D-2E-64-65-00-34-8C-7A-31-37-32-2E-31-37-2E-30-2E-31-00");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_MAIL_Test()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-12-4D-3C-73-65-6E-64-65-72-40-74-65-73-74-2E-64-65-3E-00");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }

        [TestMethod]
        public void SMFIC_RCPT_Test()
        {
            var data = ByteHelper.HexToByteArray("00-00-00-10-52-3C-74-65-73-74-40-74-65-73-74-2E-64-65-3E-00");

            var answerByte = MilterProcessor.ProcessData(data.AsSpan());

            Assert.IsNotNull(answerByte);
        }
    }
}
